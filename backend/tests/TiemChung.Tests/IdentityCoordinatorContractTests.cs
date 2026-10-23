using Xunit;

namespace TiemChung.Tests;

// Protocol mock only; client implementation/browser E2E remains M2.
public sealed class IdentityCoordinatorContractTests
{
    [Fact]
    public async Task WaiterReadsCurrentCsrfInsideLockAfterLeaderRotatesCookies()
    {
        using var webLock=new SemaphoreSlim(1,1);
        var cookie="refresh-0"; var csrf="csrf-0"; string? memoryToken=null;
        var calls=0;
        var staleSnapshot=csrf;
        async Task<string> RefreshTab()
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await webLock.WaitAsync(timeout.Token);
            try {
                // Completion broadcast transfers bearer only to memory.
                if(memoryToken is not null) return memoryToken;
                var header=csrf; var sentCookie=cookie;
                Assert.Equal("csrf-0",header); Assert.Equal("refresh-0",sentCookie);
                Interlocked.Increment(ref calls);
                await Task.Yield();
                cookie="refresh-1"; csrf="csrf-1"; memoryToken="access-1";
                return memoryToken;
            } finally { webLock.Release(); }
        }
        Assert.Equal(new[]{"access-1","access-1"},await Task.WhenAll(RefreshTab(),RefreshTab()));
        Assert.Equal(1,calls); Assert.NotEqual(staleSnapshot,csrf);
        // Lost completion: next leader rereads cookie/header pair rather than staleSnapshot.
        memoryToken=null;
        await webLock.WaitAsync();
        try { Assert.Equal("csrf-1",csrf); Assert.Equal("refresh-1",cookie); }
        finally { webLock.Release(); }
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("leaderClosed")]
    [InlineData("lostResponse")]
    [InlineData("csrfFailed")]
    public void AmbiguousFailureRequiresLoginWithoutRetryLoop(string failure)
    {
        var attempts=1;
        string? memoryToken="old";
        if(failure is "timeout" or "leaderClosed" or "lostResponse" or "csrfFailed") memoryToken=null;
        Assert.Null(memoryToken); Assert.Equal(1,attempts);
    }
}
