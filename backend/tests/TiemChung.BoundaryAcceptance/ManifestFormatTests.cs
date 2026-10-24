using System.Diagnostics;
using System.Text.Json;
using TiemChung.IdentityOperator;
using Xunit;

namespace TiemChung.BoundaryAcceptance;

public sealed class ManifestFormatTests
{
    [Fact]
    public async Task ActualPowerShellWriterProducesDotNetRecoveryDatesWithoutExtendingValidity()
    {
        var directory=Path.Combine(Path.GetTempPath(),"m1br-manifest-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            var helper=Path.Combine(AppContext.BaseDirectory,"m1br-manifest-json.ps1");
            var driver=Path.Combine(directory,"writer.ps1");
            // Synthetic request only: no SQL connection, credential, or terminal acceptance simulation.
            await File.WriteAllTextAsync(driver,"""
                param([string]$Helper)
                $m=[ordered]@{OperationId='11111111-1111-1111-1111-111111111111';RecoveryDatabaseId='22222222-2222-2222-2222-222222222222';M1UpgradeEpoch='M1';MigrationId='20261023073816_M1Identity';TargetAccountId=1;EmployeeId=1;ExpectedRowVersion='AAAAAAAAAAA=';IssuedAt=[DateTimeOffset]::Parse('2026-10-24T04:18:16.601Z');ExpiresAt=[DateTimeOffset]::Parse('2026-10-24T04:43:16.601Z');AssignedDuty='Technical'}
                & $Helper -Manifest $m
                """);
            var start=new ProcessStartInfo(OperatingSystem.IsWindows()?"powershell.exe":"pwsh") {
                UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true };
            foreach(var arg in new[]{"-NoProfile","-NonInteractive","-File",driver,"-Helper",helper})start.ArgumentList.Add(arg);
            using var process=Process.Start(start)!;
            var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await process.WaitForExitAsync(timeout.Token);
            Assert.Equal(0,process.ExitCode);Assert.True(string.IsNullOrWhiteSpace(await error));
            var json=await output;
            Assert.DoesNotContain("/Date(",json);
            var result=JsonSerializer.Deserialize<RecoveryManifest>(json)!;
            Assert.Equal(DateTimeOffset.Parse("2026-10-24T04:18:16.601Z"),result.IssuedAt);
            Assert.Equal(TimeSpan.FromMinutes(25),result.ExpiresAt-result.IssuedAt);
            Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"),result.OperationId);
            Assert.Equal(1,result.TargetAccountId);Assert.Equal("AAAAAAAAAAA=",result.ExpectedRowVersion);
        } finally {
            File.Delete(Path.Combine(directory,"writer.ps1"));
            Directory.Delete(directory);
        }
    }
}
