using TiemChung.IdentityOperator;
using Xunit;

namespace TiemChung.Tests;

public sealed class OperatorAuthenticationTests
{
    [Fact]
    public void RequiresAllowlistedOsIdentityAndStrongMatchingSecret()
    {
        var secret=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        OperatorAuthentication.Validate("TEST\\operator",["test\\OPERATOR"],secret,secret);
        Assert.Throws<InvalidOperationException>(()=>OperatorAuthentication.Validate("OTHER\\operator",["TEST\\operator"],secret,secret));
        var failure=Assert.Throws<InvalidOperationException>(()=>OperatorAuthentication.Validate("TEST\\operator",["TEST\\operator"],secret,"wrong-secret"));
        Assert.DoesNotContain(secret,failure.Message); Assert.DoesNotContain("wrong-secret",failure.Message);
        Assert.Throws<InvalidOperationException>(()=>OperatorAuthentication.Validate("TEST\\operator",["TEST\\operator"],"bad-base64","bad-base64"));
        var weak=Convert.ToBase64String(new byte[8]);
        Assert.Throws<InvalidOperationException>(()=>OperatorAuthentication.Validate("TEST\\operator",["TEST\\operator"],weak,weak));
    }
}
