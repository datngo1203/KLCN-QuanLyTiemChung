using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Infrastructure.Data;

namespace TiemChung.IdentityOperator;

public static class SqlOperatorBoundary
{
    // Config is a credential to authenticate, never a self-declared allowlist or role.
    public static string ConnectionFromEnvironment()
    {
        var value=Environment.GetEnvironmentVariable("M1_OPERATOR_CONNECTION")??throw new InvalidOperationException("Operator connection required.");
        var builder=new SqlConnectionStringBuilder(value);
        if(string.IsNullOrWhiteSpace(builder.InitialCatalog) || string.IsNullOrWhiteSpace(builder.DataSource))
            throw new InvalidOperationException("Explicit database required.");
        return builder.ConnectionString;
    }

    public static async Task<string> RequireAsync(TiemChungDbContext db,CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        await using var command=db.Database.GetDbConnection().CreateCommand();
        command.CommandText="""
            SELECT CASE WHEN IS_ROLEMEMBER('m1_operator')=1 AND IS_ROLEMEMBER('m1_app')=0
              AND IS_ROLEMEMBER('db_owner')=0 AND IS_SRVROLEMEMBER('sysadmin')=0
              AND HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL')=0
              AND HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER ANY ROLE')=0
              AND HAS_PERMS_BY_NAME('dbo.TaiKhoan','OBJECT','ALTER')=0
              AND OBJECT_ID('dbo.M1GuardAccount','TR') IS NOT NULL
              AND OBJECT_ID('dbo.M1GuardAudit','TR') IS NOT NULL
              AND NOT EXISTS(SELECT 1 FROM sys.sql_modules WHERE object_id IN (OBJECT_ID('dbo.M1GuardAccount'),OBJECT_ID('dbo.M1GuardAudit')) AND execute_as_principal_id IS NOT NULL)
              AND NOT EXISTS(SELECT 1 FROM sys.triggers WHERE name IN ('M1GuardAccount','M1GuardAudit') AND (is_disabled=1 OR OBJECT_DEFINITION(object_id) IS NULL))
              AND EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name='KLCN.M1.SqlBoundary' AND CONVERT(nvarchar(20),value)='1')
              THEN 1 ELSE 0 END,
              ORIGINAL_LOGIN(),USER_NAME();
            """;
        await using var reader=await command.ExecuteReaderAsync(ct);
        if(!await reader.ReadAsync(ct) || reader.GetInt32(0)!=1) throw new InvalidOperationException("SQL operator denied.");
        var identity=reader.GetString(1)+"/"+reader.GetString(2);
        await reader.DisposeAsync();
        // Check effective permissions of this connection, not a caller-supplied principal name.
        foreach(var item in Permissions) {
            command.Parameters.Clear(); command.CommandText="SELECT HAS_PERMS_BY_NAME(@obj,'OBJECT',@permission)";
            var obj=command.CreateParameter();obj.ParameterName="@obj";obj.Value="dbo."+item.Table;command.Parameters.Add(obj);
            var permission=command.CreateParameter();permission.ParameterName="@permission";permission.Value=item.Permission;command.Parameters.Add(permission);
            if(Convert.ToInt32(await command.ExecuteScalarAsync(ct))!=1) throw new InvalidOperationException("SQL operator permissions incomplete.");
        }
        command.CommandText="SELECT HAS_PERMS_BY_NAME('dbo.NhanVien','OBJECT','UPDATE','TrangThai','COLUMN')";
        command.Parameters.Clear();
        if(Convert.ToInt32(await command.ExecuteScalarAsync(ct))!=1) throw new InvalidOperationException("SQL operator permissions incomplete.");
        foreach(var code in IdentityPermissions.Codes) {
            command.CommandText="SELECT COUNT(*) FROM dbo.QuyenHan WHERE Code=@code";
            command.Parameters.Clear();var p=command.CreateParameter();p.ParameterName="@code";p.Value=code;command.Parameters.Add(p);
            if(Convert.ToInt32(await command.ExecuteScalarAsync(ct))!=1) throw new InvalidOperationException("Deployment permission dictionary incomplete.");
        }
        return identity;
    }

    public static async Task<string> IdentityAsync(TiemChungDbContext db,CancellationToken ct) =>
        await db.Database.SqlQueryRaw<string>("SELECT CONCAT(ORIGINAL_LOGIN(), '/', USER_NAME()) AS Value").SingleAsync(ct);

    public static readonly (string Table,string Permission)[] Permissions=[
        ("TaiKhoan","SELECT"),("TaiKhoan","INSERT"),("TaiKhoan","UPDATE"),
        ("NhanVien","SELECT"),("QuyenHan","SELECT"),("TaiKhoan_QuyenHan","SELECT"),
        ("TaiKhoan_QuyenHan","INSERT"),("TaiKhoan_QuyenHan","DELETE"),
        ("IdentitySession","SELECT"),("IdentitySession","UPDATE"),
        ("RefreshCredential","SELECT"),("RefreshCredential","UPDATE"),
        ("IdentityRecoveryDatabase","SELECT"),("IdentityRecoveryOperation","SELECT"),
        ("IdentityRecoveryOperation","INSERT"),("SecurityAudit","INSERT"),("__EFMigrationsHistory","SELECT")];
}
