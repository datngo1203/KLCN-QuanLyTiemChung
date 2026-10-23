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
    var connection=Environment.GetEnvironmentVariable("M1_OPERATOR_CONNECTION")??throw new InvalidOperationException("Operator connection required.");
    await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(connection).Options);
    var ct=CancellationToken.None;
    if(command is "bootstrap-admin" or "recover-upgraded-admin") {
        var identity=Environment.UserDomainName+"\\"+Environment.UserName;
        var allowed=(Environment.GetEnvironmentVariable("M1_OPERATOR_ALLOWED_IDENTITIES")??"").Split(';',StringSplitOptions.RemoveEmptyEntries);
        if(!allowed.Contains(identity,StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("Operator identity denied.");
        var configured=Environment.GetEnvironmentVariable("M1_OPERATOR_SECRET")??throw new InvalidOperationException("Operator secret required.");
        var supplied=Hidden("Operator secret: ");
        OperatorAuthentication.Validate(identity,allowed,configured,supplied);
        if(Read("Maintenance active; public writes disabled? Type CONFIRM: ")!="CONFIRM") throw new InvalidOperationException("Maintenance confirmation required.");
        var recovery=new IdentityRecovery(db);
        if(command=="recover-upgraded-admin") {
            // Manifest is read from a protected secret-store export, never auto-created by this command.
            var manifestPath=Read("Protected manifest path: ");
            var manifest=JsonSerializer.Deserialize<RecoveryManifest>(await File.ReadAllTextAsync(manifestPath))??throw new InvalidOperationException("Manifest required.");
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
        if(command=="provision-employee") {
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
