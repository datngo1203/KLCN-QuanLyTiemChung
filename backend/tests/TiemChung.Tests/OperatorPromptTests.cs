using System.Diagnostics;
using TiemChung.IdentityOperator;
using Xunit;

namespace TiemChung.Tests;

public sealed class OperatorPromptTests
{
    [Fact]
    public async Task RedirectedStdinIsRefusedWithoutEchoingSecretMarker()
    {
        var start=new ProcessStartInfo("dotnet") { RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false };
        start.ArgumentList.Add(typeof(IdentityAdministration).Assembly.Location);
        start.ArgumentList.Add("reset-employee-password");
        start.Environment["M1_OPERATOR_CONNECTION"]="Server=unused;Database=unused;Integrated Security=true";
        using var process=Process.Start(start)!;
        await process.StandardInput.WriteLineAsync("synthetic-actor");
        await process.StandardInput.WriteLineAsync("SYNTHETIC_SECRET_MUST_NOT_ECHO");
        process.StandardInput.Close();
        var stdout=process.StandardOutput.ReadToEndAsync(); var stderr=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await process.WaitForExitAsync(timeout.Token);
        var output=await stdout+await stderr;
        Assert.Equal(1,process.ExitCode);
        Assert.DoesNotContain("SYNTHETIC_SECRET_MUST_NOT_ECHO",output);
        Assert.Contains("Operation failed",output);
    }
}
