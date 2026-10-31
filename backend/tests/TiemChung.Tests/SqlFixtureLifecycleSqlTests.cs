using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Infrastructure.Data;
using Xunit;
using Xunit.Abstractions;

namespace TiemChung.Tests;

// Opt-in only: full SQL suites must not run these against an existing demo/test instance.
public sealed class SqlFixtureLifecycleSqlTests(ITestOutputHelper output)
{
    [LifecycleSmokeFact, Trait("Category", "SqlIntegration")]
    public Task PassCreatesMigratesUsesAndDropsOnlyItsDatabase() => ExerciseBodyAsync(false);

    [LifecycleSmokeFact, Trait("Category", "SqlIntegration")]
    public Task SentinelFailureIsPreservedAndDatabaseIsDropped() => ExerciseBodyAsync(true);

    private async Task ExerciseBodyAsync(bool fail)
    {
        await using var master = await OpenIsolatedMasterAsync();
        var baseline = await CatalogAsync(master);
        string? name = null;
        var sentinel = new InvalidOperationException("lifecycle-smoke-sentinel");
        async Task Run() => await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async fixture => {
            name = fixture.Database.Name;
            output.WriteLine("Created fixture: " + name);
            await AssertOnlineAsync(master, name);
            Assert.NotEmpty(await fixture.Db.Database.GetAppliedMigrationsAsync());
            Assert.Equal(1, await fixture.Db.Database.SqlQueryRaw<int>("SELECT 1 AS Value").SingleAsync());
            if (fail) throw sentinel;
        });
        if (fail) Assert.Same(sentinel, await Assert.ThrowsAsync<InvalidOperationException>(Run));
        else await Run();
        Assert.NotNull(name);
        await AssertGoneAsync(master, name);
        Assert.Equal(baseline, await CatalogAsync(master));
        output.WriteLine("Catalog unchanged; fixture databases remaining: 0.");
    }

    [LifecycleSmokeFact, Trait("Category", "SqlIntegration")]
    public async Task HeldConnectionReportsCleanupFailureThenOwnerCanRetry()
    {
        await using var master = await OpenIsolatedMasterAsync();
        var baseline = await CatalogAsync(master);
        var fixture = await CatalogSqlTests.Fixture.Create();
        var name = fixture.Database.Name;
        output.WriteLine("Created fixture: " + name);
        await using var blocker = new SqlConnection(new SqlConnectionStringBuilder(fixture.Database.ConnectionString) {
            Pooling = false
        }.ConnectionString);
        try {
            await blocker.OpenAsync();
            await AssertOnlineAsync(master, name);
            var failure = await Assert.ThrowsAsync<AggregateException>(async () => await fixture.DisposeAsync());
            Assert.True(HasSqlError(failure, 3702), "Expected DROP refusal because the owned DB is in use.");
            Assert.Contains(name, failure.ToString());
            await AssertOnlineAsync(master, name);
            output.WriteLine("Cleanup reported SQL error 3702; no forced disconnect was used.");
        } finally {
            await blocker.CloseAsync();
            await fixture.DisposeAsync();
        }
        await AssertGoneAsync(master, name);
        Assert.Equal(baseline, await CatalogAsync(master));
        output.WriteLine("Owner retry succeeded; fixture databases remaining: 0.");
    }

    [LifecycleSmokeFact, Trait("Category", "SqlIntegration")]
    public async Task CreateFailureAfterDatabaseCreationDropsItsPartialDatabase()
    {
        await using var master = await OpenIsolatedMasterAsync();
        var baseline = await CatalogAsync(master);
        string? name = null;
        var sentinel = new InvalidOperationException("lifecycle-smoke-create-sentinel");
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => CatalogSqlTests.Fixture.Create(async db => {
            name = db.Database.GetDbConnection().Database;
            output.WriteLine("Partially created fixture: " + name);
            await AssertOnlineAsync(master, name);
            throw sentinel;
        }));
        Assert.Same(sentinel, failure);
        Assert.NotNull(name);
        await AssertGoneAsync(master, name);
        Assert.Equal(baseline, await CatalogAsync(master));
        output.WriteLine("Partial Create cleaned up; fixture databases remaining: 0.");
    }

    [LifecycleSmokeFact, Trait("Category", "SqlIntegration")]
    public async Task MissingApplicationNameStillClearsTheEfPool()
    {
        await using var master = await OpenIsolatedMasterAsync();
        var baseline = await CatalogAsync(master);
        var source = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")!);
        source.Remove("Application Name"); // Match the CI connection string that exposed SQL 3702.
        string? name = null;
        await SqlFixtureLifecycle.RunAsync(() => OwnedSqlTestDatabase.CreateAsync(source.ConnectionString), async owner => {
            name = owner.Name;
            output.WriteLine("Created fixture: " + name);
            await using var db = new TiemChungDbContext(
                new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(owner.ConnectionString).Options);
            Assert.Equal(owner.ConnectionString, db.Database.GetConnectionString());
            Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT 1 AS Value").SingleAsync());
            // Disposing the context returns an idle physical connection to its pool before DROP.
        });
        Assert.NotNull(name);
        await AssertGoneAsync(master, name);
        Assert.Equal(baseline, await CatalogAsync(master));
    }
    private static async Task<SqlConnection> OpenIsolatedMasterAsync()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable("KLCN_SQL_LIFECYCLE_SMOKE"));
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Disposable SQL connection required."));
        Assert.Equal("127.0.0.1,14335", builder.DataSource);
        Assert.Equal("master", builder.InitialCatalog);
        Assert.Equal("KLCN-Lifecycle-Smoke", builder.ApplicationName);
        builder.Pooling = false;
        var master = new SqlConnection(builder.ConnectionString);
        try {
            await master.OpenAsync();
            await using var machine = new SqlCommand("SELECT CAST(SERVERPROPERTY('MachineName') AS nvarchar(128))", master);
            var expected = Environment.GetEnvironmentVariable("KLCN_SQL_LIFECYCLE_HOSTNAME");
            Assert.False(string.IsNullOrWhiteSpace(expected));
            Assert.Equal(expected, (string)(await machine.ExecuteScalarAsync())!);
            await using var users = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE database_id>4", master);
            Assert.Equal(0, (int)(await users.ExecuteScalarAsync())!);
            return master;
        } catch { await master.DisposeAsync(); throw; }
    }

    private async Task<string[]> CatalogAsync(SqlConnection master)
    {
        List<string> rows = [];
        await using var query = new SqlCommand("SELECT name,database_id,create_date,state_desc FROM sys.databases ORDER BY name", master);
        await using var reader = await query.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows.Add($"{reader.GetString(0)}|{reader.GetInt32(1)}|{reader.GetDateTime(2):O}|{reader.GetString(3)}");
        output.WriteLine("Catalog databases: " + rows.Count);
        return rows.ToArray();
    }

    private static async Task AssertOnlineAsync(SqlConnection master, string name)
    {
        await using var query = new SqlCommand("SELECT state_desc FROM sys.databases WHERE name=@name", master);
        query.Parameters.AddWithValue("@name", name);
        Assert.Equal("ONLINE", await query.ExecuteScalarAsync());
    }

    private async Task AssertGoneAsync(SqlConnection master, string name)
    {
        await using var query = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name=@name", master);
        query.Parameters.AddWithValue("@name", name);
        Assert.Equal(0, (int)(await query.ExecuteScalarAsync())!);
        output.WriteLine("Removed fixture: " + name);
    }

    private static bool HasSqlError(Exception failure, int number) => failure is SqlException sql && sql.Number == number
        || failure is AggregateException aggregate && aggregate.InnerExceptions.Any(e => HasSqlError(e, number))
        || failure.InnerException is not null && HasSqlError(failure.InnerException, number);

    private sealed class LifecycleSmokeFactAttribute : FactAttribute
    {
        public LifecycleSmokeFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("KLCN_SQL_LIFECYCLE_SMOKE") != "1")
                Skip = "Requires a dedicated disposable SQL lifecycle smoke instance.";
        }
    }
}
