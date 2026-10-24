using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Api;
using TiemChung.IdentityOperator;
using TiemChung.Infrastructure.Data;

namespace TiemChung.IdentityOperator;

internal static class OperatorProgram
{
public static async Task<int> Main(string[] args)
{
// Connection and operator credentials are supplied by the OS/secret store, not arguments.
try {
    if(args.Length!=1) throw new InvalidOperationException("Specify exactly one command.");
    var command=args[0];
    var connection=SqlOperatorBoundary.ConnectionFromEnvironment();
    await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(connection).Options);
    var ct=CancellationToken.None;
    var sqlIdentity=await SqlOperatorBoundary.RequireAsync(db,ct);
    if(command is "bootstrap-admin" or "recover-upgraded-admin") {
        var identity=sqlIdentity;
        if(Read("Maintenance active; public writes disabled? Type CONFIRM: ")!="CONFIRM") throw new InvalidOperationException("Maintenance confirmation required.");
        var recovery=new IdentityRecovery(db);
        if(command=="recover-upgraded-admin") {
            // Manifest is read from a protected secret-store export, never auto-created by this command.
            var manifest=JsonSerializer.Deserialize<RecoveryManifest>(await File.ReadAllTextAsync(Environment.GetEnvironmentVariable("M1_RECOVERY_MANIFEST")??throw new InvalidOperationException("Manifest configuration required.")))??throw new InvalidOperationException("Manifest required.");
            var result=await recovery.RecoverAsync(manifest,identity,Hidden("Target password (12-128): "),ct);
            Console.WriteLine(result);
        } else {
            var employee=int.Parse(Read("Existing employee ID: "),System.Globalization.CultureInfo.InvariantCulture);
            var username=Read("New username: ");
            await recovery.BootstrapAsync(employee,username,Hidden("New password (12-128): "),identity,ct);
            Console.WriteLine("Bootstrap committed.");
        }
    } else {
        var admin=new IdentityAdministration(db);
        var username=Read("Actor username: "); var password=Hidden("Actor password: ");
        if(command=="reset-employee-password") {
            var snapshot=await admin.AuthenticateResetActorAsync(username,password,ct);
            var target=int.Parse(Read("Target account ID: "),System.Globalization.CultureInfo.InvariantCulture);
            var employee=int.Parse(Read("Target employee ID: "),System.Globalization.CultureInfo.InvariantCulture);
            var version=Convert.FromBase64String(Read("Expected target RowVersion (base64, read from current DB): "));
            var targetRow=await db.Set<TiemChung.Domain.Entities.TaiKhoan>().AsNoTracking().SingleOrDefaultAsync(x=>x.MaTaiKhoan==target);
            if(targetRow is null || targetRow.MaNhanVien!=employee) throw new IdentityFailure(400,"validation");
            Console.WriteLine($"Target account {target}, employee {employee}, username {targetRow.TenDangNhap}");
            if(Read("Identity checked; employee present and approved secure handoff? Type CONFIRM: ")!="CONFIRM") throw new IdentityFailure(400,"validation");
            var result=await admin.ResetPasswordAsync(snapshot,username,password,target,employee,version,Hidden("Employee enters new password: "),Hidden("Repeat new password: "),ct);
            Console.WriteLine($"Reset committed. Account {target}; employee {employee}; RowVersion {Convert.ToBase64String(result)}");
        } else if(command=="provision-employee") {
            var employee=int.Parse(Read("Existing employee ID: "),System.Globalization.CultureInfo.InvariantCulture);
            var targetUsername=Read("New username: ");
            await admin.ProvisionAsync(username,password,employee,targetUsername,Hidden("Initial password: "),ct);
        } else if(command is "set-account-status" or "set-extra-permissions" or "set-employee-status") {
            var target=int.Parse(Read("Target account ID: "),System.Globalization.CultureInfo.InvariantCulture);
            var values=Read("Value(s), comma separated permission codes or status: ").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
            await admin.ChangeAsync(username,password,target,command,values,ct);
        } else throw new InvalidOperationException("Unsupported command.");
        Console.WriteLine("Operation committed.");
    }
    return 0;
} catch(IdentityFailure failure) {
    Console.Error.WriteLine(failure.Code); return 1;
} catch(Exception) {
    // Provider exceptions may contain PII/connection data; do not print exception bodies.
    Console.Error.WriteLine("Operation failed; no success confirmed. Inspect restricted diagnostics."); return 1;
}

}
static string Read(string prompt) { Console.Write(prompt); return Console.ReadLine()??throw new InvalidOperationException("Input required."); }
static string Hidden(string prompt)
{
    if(Console.IsInputRedirected) throw new InvalidOperationException("Interactive hidden input required.");
    Console.Write(prompt); var value=new StringBuilder();
    while(true) {
        var key=Console.ReadKey(true);
        if(key.Key==ConsoleKey.Enter) { Console.WriteLine(); return value.ToString(); }
        if(key.Key==ConsoleKey.Backspace) { if(value.Length>0) value.Length--; }
        else if(!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
    }
}
}
