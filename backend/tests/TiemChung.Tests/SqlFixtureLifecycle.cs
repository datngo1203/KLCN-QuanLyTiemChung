using Microsoft.Data.SqlClient;

namespace TiemChung.Tests;

internal static class SqlFixtureLifecycle
{
    // Dispose outside the test body so a cleanup error cannot replace its failure.
    internal static async Task RunAsync<T>(Func<Task<T>> create, Func<T, Task> test) where T : IAsyncDisposable
    {
        var fixture = await create();
        try { await test(fixture); }
        catch (Exception failure) {
            await CleanupAfterFailureAsync(failure, fixture.DisposeAsync);
            throw;
        }
        await fixture.DisposeAsync();
    }

    internal static async Task CleanupAfterFailureAsync(Exception failure, Func<ValueTask> cleanup)
    {
        try { await cleanup(); }
        catch (Exception cleanupFailure) {
            throw new AggregateException("Test/fixture failed and cleanup also failed; both errors are retained.", failure, cleanupFailure);
        }
    }

    internal static async ValueTask CleanupAsync(params Func<ValueTask>[] actions)
    {
        List<Exception> failures = [];
        foreach (var action in actions) {
            try { await action(); }
            catch (Exception failure) { failures.Add(failure); }
        }
        if (failures.Count != 0) throw new AggregateException("SQL fixture cleanup failed.", failures);
    }
}

// Only this instance can acquire ownership: no API accepts an existing database name.
internal sealed class OwnedSqlTestDatabase : IAsyncDisposable
{
    private readonly Guid _token = Guid.NewGuid();
    private readonly string _masterConnection;
    private bool _created;
    private int? _databaseId;
    private DateTime _createdAt;
    internal string Name { get; }
    internal string ConnectionString { get; }

    private OwnedSqlTestDatabase(string source)
    {
        Name = "KLCN_M1_Test_" + _token.ToString("N");
        ConnectionString = new SqlConnectionStringBuilder(source) { InitialCatalog = Name }.ConnectionString;
        _masterConnection = new SqlConnectionStringBuilder(source) { InitialCatalog = "master", Pooling = false }.ConnectionString;
    }

    internal static async Task<OwnedSqlTestDatabase> CreateAsync(string source)
    {
        var owner = new OwnedSqlTestDatabase(source);
        try {
            await using var master = new SqlConnection(owner._masterConnection);
            await master.OpenAsync();
            // CREATE fails if the name already exists; never adopt or delete that database.
            await using (var create = new SqlCommand($"CREATE DATABASE [{owner.Name}]", master))
                await create.ExecuteNonQueryAsync();
            owner._created = true;
            var identity = await owner.ReadIdentityAsync(master)
                ?? throw new InvalidOperationException("Created fixture database identity is unavailable.");
            owner._databaseId = identity.Id;
            owner._createdAt = identity.CreatedAt;
            // Preserve EF SQL Server's database-creation isolation setting.
            await using (var isolation = new SqlCommand($"IF SERVERPROPERTY('EngineEdition') <> 5 ALTER DATABASE [{owner.Name}] SET READ_COMMITTED_SNAPSHOT ON", master))
                await isolation.ExecuteNonQueryAsync();
            return owner;
        } catch (Exception failure) {
            await SqlFixtureLifecycle.CleanupAfterFailureAsync(failure, owner.DisposeAsync);
            throw;
        }
    }

    internal static void ValidateOwnership(string name, Guid token, int? expectedId, DateTime expectedCreatedAt,
        int actualId, DateTime actualCreatedAt)
    {
        const string prefix = "KLCN_M1_Test_";
        if (token == Guid.Empty || !name.StartsWith(prefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(name[prefix.Length..], "N", out var parsed)
            || parsed != token || name != prefix + token.ToString("N")
            || expectedId is null || expectedId != actualId || expectedCreatedAt != actualCreatedAt)
            throw new InvalidOperationException("Refusing DROP: fixture database ownership does not match.");
    }

    private async Task<(int Id, DateTime CreatedAt)?> ReadIdentityAsync(SqlConnection master)
    {
        await using var query = new SqlCommand("SELECT database_id, create_date FROM sys.databases WHERE name=@name", master);
        query.Parameters.AddWithValue("@name", Name);
        await using var reader = await query.ExecuteReaderAsync();
        return await reader.ReadAsync() ? (reader.GetInt32(0), reader.GetDateTime(1)) : null;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_created) return;
        try { await DropOwnedAsync(); }
        catch (Exception failure) {
            throw new InvalidOperationException($"Cleanup failed for fixture database '{Name}'; inspect this exact database before retrying.", failure);
        }
    }

    private async Task DropOwnedAsync()
    {
        // Clear only this fixture's pool, after its clients/hosts/contexts are closed.
        using (var pool = new SqlConnection(ConnectionString)) SqlConnection.ClearPool(pool);
        await using var master = new SqlConnection(_masterConnection);
        await master.OpenAsync();
        var actual = await ReadIdentityAsync(master);
        if (actual is not null) {
            ValidateOwnership(Name, _token, _databaseId, _createdAt, actual.Value.Id, actual.Value.CreatedAt);
            // No SINGLE_USER/rollback: outstanding users make cleanup fail visibly.
            await using var drop = new SqlCommand($"DROP DATABASE [{Name}]", master);
            await drop.ExecuteNonQueryAsync();
        }
        _created = false;
    }
}
