using Microsoft.Extensions.Configuration;
using TiemChung.Api;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityTokenTests
{
    [Fact]
    public void SignedTokenBindsAccountSessionAndExpiresWithoutGrace()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Identity:SigningKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
        }).Build();
        var tokens = new IdentityTokens(config);
        var now = DateTimeOffset.FromUnixTimeSeconds(1792740000);
        var sid = Guid.NewGuid();
        var token = tokens.Issue(7, sid, now);
        Assert.Equal((7, sid), tokens.Validate(token, now)!.Value);
        Assert.Null(tokens.Validate(token, now.AddMinutes(5)));
        Assert.Null(tokens.Validate(token + "x", now));
        Assert.Null(tokens.Validate(token, now.AddSeconds(-1)));
        Assert.Equal(32, IdentityTokens.Hash(IdentityTokens.RandomToken()).Length);
    }
}
