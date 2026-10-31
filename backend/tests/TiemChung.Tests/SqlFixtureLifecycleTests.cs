using Xunit;

namespace TiemChung.Tests;

public sealed class SqlFixtureLifecycleTests
{
    [Fact]
    public async Task PassingBodyDisposesOnce()
    {
        var fixture = new FakeFixture();
        await SqlFixtureLifecycle.RunAsync(() => Task.FromResult(fixture), _ => Task.CompletedTask);
        Assert.Equal(1, fixture.Disposals);
    }

    [Fact]
    public async Task FailingBodyStillDisposesAndPreservesOriginalException()
    {
        var fixture = new FakeFixture();
        var failure = new InvalidOperationException("test failure");
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SqlFixtureLifecycle.RunAsync(() => Task.FromResult(fixture), _ => Task.FromException(failure)));
        Assert.Same(failure, actual);
        Assert.Equal(1, fixture.Disposals);
    }

    [Fact]
    public async Task BodyAndDisposeFailuresAreBothReported()
    {
        var failure = new InvalidOperationException("test failure");
        var cleanup = new IOException("cleanup failure");
        var fixture = new FakeFixture { Failure = cleanup };
        var actual = await Assert.ThrowsAsync<AggregateException>(() =>
            SqlFixtureLifecycle.RunAsync(() => Task.FromResult(fixture), _ => Task.FromException(failure)));
        Assert.Equal(new Exception[] { failure, cleanup }, actual.InnerExceptions);
        Assert.Equal(1, fixture.Disposals);
    }

    [Fact]
    public async Task PassingBodyCannotHideCleanupFailure()
    {
        var failure = new IOException("cleanup failure");
        var fixture = new FakeFixture { Failure = failure };
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() =>
            SqlFixtureLifecycle.RunAsync(() => Task.FromResult(fixture), _ => Task.CompletedTask)));
    }

    [Fact]
    public async Task FailedCreationReportsItsFailureTogetherWithCleanupFailure()
    {
        var create = new InvalidOperationException("creation failure");
        var cleanup = new IOException("cleanup failure");
        var actual = await Assert.ThrowsAsync<AggregateException>(() =>
            SqlFixtureLifecycle.CleanupAfterFailureAsync(create, () => ValueTask.FromException(cleanup)));
        Assert.Equal(new Exception[] { create, cleanup }, actual.InnerExceptions);
    }

    [Fact]
    public async Task CleanupContinuesInOrderAfterResourceDisposalFails()
    {
        List<string> order = [];
        var actual = await Assert.ThrowsAsync<AggregateException>(async () =>
            await SqlFixtureLifecycle.CleanupAsync(
                () => { order.Add("client"); return ValueTask.FromException(new IOException("client")); },
                () => { order.Add("host"); return ValueTask.FromException(new IOException("host")); },
                () => { order.Add("context"); return ValueTask.CompletedTask; },
                () => { order.Add("database"); return ValueTask.CompletedTask; }));
        Assert.Equal(new[] { "client", "host", "context", "database" }, order);
        Assert.Equal(2, actual.InnerExceptions.Count);
    }

    [Fact]
    public void OnlyExactOwnedGuidAndDatabaseIdentityPassTheDropGuard()
    {
        var token = Guid.NewGuid();
        var name = "KLCN_M1_Test_" + token.ToString("N");
        var created = new DateTime(2026, 10, 31);
        OwnedSqlTestDatabase.ValidateOwnership(name, token, 17, created, 17, created);
        Assert.Throws<InvalidOperationException>(() => OwnedSqlTestDatabase.ValidateOwnership(name, Guid.NewGuid(), 17, created, 17, created));
        Assert.Throws<InvalidOperationException>(() => OwnedSqlTestDatabase.ValidateOwnership(name, token, null, created, 17, created));
        Assert.Throws<InvalidOperationException>(() => OwnedSqlTestDatabase.ValidateOwnership(name, token, 17, created, 18, created));
        Assert.Throws<InvalidOperationException>(() => OwnedSqlTestDatabase.ValidateOwnership(name, token, 17, created, 17, created.AddSeconds(1)));
    }

    [Theory]
    [InlineData("TiemChung")]
    [InlineData("master")]
    [InlineData("KLCN_G3C_Test_ed4db92e1f5d4508b5da583a0be720d4")]
    [InlineData("KLCN_G3E_Test_a47a1fb7bfae41379b69c2ebab499a4b")]
    [InlineData("KLCN_M1_Test_")]
    [InlineData("KLCN_M1_Test_not-a-guid")]
    [InlineData("KLCN_M1_Test_00000000000000000000000000000000_Restored")]
    [InlineData("KLCN_M1_Test_x]; DROP DATABASE TiemChung;--")]
    public void DropGuardRejectsSharedAcceptanceRestoredAndMalformedNames(string name)
    {
        Assert.Throws<InvalidOperationException>(() => OwnedSqlTestDatabase.ValidateOwnership(
            name, Guid.NewGuid(), 17, DateTime.Today, 17, DateTime.Today));
    }

    private sealed class FakeFixture : IAsyncDisposable
    {
        internal int Disposals { get; private set; }
        internal Exception? Failure { get; init; }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return Failure is null ? ValueTask.CompletedTask : ValueTask.FromException(Failure);
        }
    }
}
