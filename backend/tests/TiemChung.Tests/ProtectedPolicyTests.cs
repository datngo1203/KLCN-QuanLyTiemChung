using System.Reflection;
using TiemChung.IdentityOperator;
using Xunit;

namespace TiemChung.Tests;

public sealed class ProtectedPolicyTests
{
    [Fact]
    public void ActualCallerOwnedPathIsNotAnAdministrativeBoundary()
    {
        if(!OperatingSystem.IsWindows()) { Assert.Throws<InvalidOperationException>(()=>ProtectedOperatorPolicy.Load()); return; }
        var path=Path.GetTempFileName();
        try {
            File.WriteAllText(path,"{\"AllowedSids\":[\"caller\"],\"ExpectedSecret\":\"synthetic\"}");
            // Uses real file owner/DACL/ancestors; no ACL edits or mock policy success.
            var method=typeof(ProtectedOperatorPolicy).GetMethod("CheckChain",BindingFlags.Static|BindingFlags.NonPublic)!;
            var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,[path]));
            Assert.IsType<InvalidOperationException>(failure.InnerException);
            var missing=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,[path+".missing"]));
            Assert.IsType<InvalidOperationException>(missing.InnerException);
        } finally { File.Delete(path); }
    }
}
