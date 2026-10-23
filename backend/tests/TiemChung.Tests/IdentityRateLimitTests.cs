using TiemChung.Api;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityRateLimitTests
{
    [Fact]
    public void LoginCombinesNormalizedIdentifierWithIp()
    {
        var limits=new IdentityRateLimits();
        for(var i=0;i<10;i++) limits.Check("login"," User@Example.invalid ","127.0.0.1");
        Assert.Equal(429,Assert.Throws<IdentityFailure>(()=>limits.Check("login","user@example.invalid","127.0.0.1")).Status);
        limits.Check("login","user@example.invalid","127.0.0.2");
    }
    [Fact]
    public void ResendCooldownDoesNotDependOnAccountExistence()
    {
        var limits=new IdentityRateLimits();
        limits.Check("resend","nobody@example.invalid","127.0.0.1");
        Assert.Equal("rateLimited",Assert.Throws<IdentityFailure>(()=>limits.Check("resend","NOBODY@example.invalid","127.0.0.2")).Code);
    }
}
