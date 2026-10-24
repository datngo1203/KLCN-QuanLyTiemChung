using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TiemChung.Api;
using TiemChung.Domain.Entities;
using TiemChung.IdentityOperator;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class EmployeeResetSqlTests
{
    [Fact,Trait("Category","SqlIntegration")]
    public async Task ResetRechecksActorAndAtomicallyRevokesPreviouslyActiveTokens()
    {
        var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required.")) { InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") };
        await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options);
        await db.Database.MigrateAsync();
        var adminEmployee=new NhanVien { HoTen="Synthetic Admin",CCCD="000000000001",SoDienThoai="0900000000",Email="admin@example.invalid",BoPhan="Test",ChucDanh="Admin",TrangThai="Active" };
        var employee=new NhanVien { HoTen="Synthetic Staff",CCCD="000000000002",SoDienThoai="0900000001",Email="staff@example.invalid",BoPhan="Test",ChucDanh="LeTan",TrangThai="Active" };
        db.AddRange(adminEmployee,employee); await db.SaveChangesAsync();
        var adminId=await new IdentityRecovery(db).BootstrapAsync(adminEmployee.MaNhanVien,"reset-admin","synthetic-admin-password","operator",CancellationToken.None);
        var admin=new IdentityAdministration(db);
        var targetId=await admin.ProvisionAsync("reset-admin","synthetic-admin-password",employee.MaNhanVien,"reset-staff","synthetic-old-password",CancellationToken.None);
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) }).Build();
        var tokens=new IdentityTokens(config); var sessions=new IdentitySessions(db,tokens);
        db.ChangeTracker.Clear();
        var old=await sessions.LoginAsync("reset-staff","synthetic-old-password","native","old-session",CancellationToken.None);
        var actorSession=await sessions.LoginAsync("reset-admin","synthetic-admin-password","native","actor-session",CancellationToken.None);
        db.ChangeTracker.Clear();
        var before=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==targetId);
        async Task<IdentityAdministration.ResetActorSnapshot> Snapshot()=>await admin.AuthenticateResetActorAsync("reset-admin","synthetic-admin-password",CancellationToken.None);
        async Task<byte[]> Reset(IdentityAdministration.ResetActorSnapshot snapshot,int id,int emp,byte[] version)=>
            await admin.ResetPasswordAsync(snapshot,"reset-admin","synthetic-admin-password",id,emp,version,"synthetic-new-password","synthetic-new-password",CancellationToken.None);
        foreach(var state in new[]{"locked","director","inactive","noPermission"}) {
            var snapshot=await Snapshot();
            if(state=="locked") await db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==adminId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Locked"));
            if(state=="director") await db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==adminId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.AdministrativeDuty,"Director"));
            if(state=="inactive") await db.Set<NhanVien>().Where(x=>x.MaNhanVien==adminEmployee.MaNhanVien).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Inactive"));
            if(state=="noPermission") await db.Set<TaiKhoan_QuyenHan>().Where(x=>x.MaTaiKhoan==adminId).ExecuteDeleteAsync();
            await Assert.ThrowsAsync<IdentityFailure>(()=>Reset(snapshot,targetId,employee.MaNhanVien,before.RowVersion));
            db.ChangeTracker.Clear();
            await db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==adminId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Active").SetProperty(x=>x.AdministrativeDuty,"Technical"));
            await db.Set<NhanVien>().Where(x=>x.MaNhanVien==adminEmployee.MaNhanVien).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Active"));
            if(state=="noPermission") {
                foreach(var p in await db.Set<QuyenHan>().ToListAsync()) db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan=adminId,MaQuyen=p.MaQuyen });
                await db.SaveChangesAsync();
            }
        }
        var customer=new TaiKhoan { AccountType="Customer",TenDangNhap="synthetic-customer",LoginEmail="c@example.invalid",NormalizedEmail="C@EXAMPLE.INVALID",MatKhauHash="synthetic",TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow };
        db.Add(customer); await db.SaveChangesAsync();
        var customerActor=await Snapshot();
        await Assert.ThrowsAsync<IdentityFailure>(()=>Reset(customerActor,customer.MaTaiKhoan,employee.MaNhanVien,customer.RowVersion));
        var actor=await Snapshot();
        // Identifiers below are fixed test literals, never input or configuration.
#pragma warning disable EF1002
        foreach(var injection in new[]{(Table:"TaiKhoan",Name:"RejectResetHash"),(Table:"RefreshCredential",Name:"RejectResetRevoke")}) {
            await db.Database.ExecuteSqlRawAsync($"CREATE TRIGGER {injection.Name} ON {injection.Table} AFTER UPDATE AS BEGIN THROW 51009, 'Synthetic failure', 1; END;");
            var error=await Record.ExceptionAsync(()=>Reset(actor,targetId,employee.MaNhanVien,before.RowVersion));
            Assert.True(error is DbUpdateException or SqlException);
            db.ChangeTracker.Clear();
            var unchanged=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==targetId);
            Assert.Equal(before.MatKhauHash,unchanged.MatKhauHash); Assert.Equal(before.SecurityVersion,unchanged.SecurityVersion);
            Assert.Null((await db.Set<IdentitySession>().AsNoTracking().SingleAsync(x=>x.Id==old.SessionId)).RevokedAt);
            Assert.Equal(1,await db.Set<RefreshCredential>().CountAsync(x=>x.SessionId==old.SessionId && x.RevokedAt==null && x.ConsumedAt==null));
            await db.Database.ExecuteSqlRawAsync($"DROP TRIGGER {injection.Name};");
        }
#pragma warning restore EF1002
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectActiveReset ON SecurityAudit AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action='employeePasswordReset') THROW 51009, 'Synthetic failure', 1; END;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>Reset(actor,targetId,employee.MaNhanVien,before.RowVersion));
        db.ChangeTracker.Clear();
        var afterFailure=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==targetId);
        Assert.Equal(before.MatKhauHash,afterFailure.MatKhauHash); Assert.Equal(before.SecurityVersion,afterFailure.SecurityVersion);
        Assert.Null((await db.Set<IdentitySession>().AsNoTracking().SingleAsync(x=>x.Id==old.SessionId)).RevokedAt);
        Assert.Equal(1,await db.Set<RefreshCredential>().CountAsync(x=>x.SessionId==old.SessionId && x.RevokedAt==null && x.ConsumedAt==null));
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectActiveReset;");
        await Reset(await Snapshot(),targetId,employee.MaNhanVien,before.RowVersion);
        db.ChangeTracker.Clear();
        var after=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==targetId);
        Assert.Equal(before.TrangThai,after.TrangThai); Assert.Equal(before.AdministrativeDuty,after.AdministrativeDuty); Assert.Equal(before.MaNhanVien,after.MaNhanVien);
        Assert.Equal("LeTan",(await db.Set<NhanVien>().AsNoTracking().SingleAsync(x=>x.MaNhanVien==employee.MaNhanVien)).ChucDanh);
        Assert.Empty(await db.Set<TaiKhoan_QuyenHan>().Where(x=>x.MaTaiKhoan==targetId).ToListAsync()); Assert.Empty(await db.Set<IdentityRecoveryOperation>().ToListAsync());
        Assert.Null((await db.Set<IdentitySession>().AsNoTracking().SingleAsync(x=>x.Id==actorSession.SessionId)).RevokedAt);
        var context=new Microsoft.AspNetCore.Http.DefaultHttpContext(); context.Request.Headers.Authorization="Bearer "+old.AccessToken;
        Assert.Equal(401,(await Assert.ThrowsAsync<IdentityFailure>(()=>new IdentityAuthorization(db,tokens).RequireAsync(context,false,CancellationToken.None))).Status);
        await Assert.ThrowsAsync<IdentityFailure>(()=>sessions.RefreshAsync(old.RefreshToken,"native",null,"old-refresh",CancellationToken.None));
        db.ChangeTracker.Clear();
        Assert.NotEqual(Guid.Empty,(await sessions.LoginAsync("reset-staff","synthetic-new-password","native","new-login",CancellationToken.None)).SessionId);
        await db.Set<NhanVien>().Where(x=>x.MaNhanVien==employee.MaNhanVien).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Inactive"));
        var inactive=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==targetId);
        await Reset(await Snapshot(),targetId,employee.MaNhanVien,inactive.RowVersion);
        Assert.Equal("Inactive",(await db.Set<NhanVien>().AsNoTracking().SingleAsync(x=>x.MaNhanVien==employee.MaNhanVien)).TrangThai);
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<IdentityFailure>(()=>sessions.LoginAsync("reset-staff","synthetic-new-password","native","inactive-login",CancellationToken.None));
    }
}
