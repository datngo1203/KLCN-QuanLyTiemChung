using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityHttpSqlTests
{
    [Fact,Trait("Category","SqlIntegration")]
    public async Task AnonymousUnlinkedAndWebCsrfAreEnforcedOverHttp()
    {
        var source=Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required.");
        var cs=new SqlConnectionStringBuilder(source) { InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") };
        var values=new Dictionary<string,string?> {
            ["ConnectionStrings:TiemChung"]=cs.ConnectionString,["Catalog:UseDemoData"]="false",
            ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["Identity:MailEncryptionKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["Cors:Origins:0"]="https://localhost"
        };
        await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options);
        await db.Database.MigrateAsync();
        await using var factory=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development")
            .ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(values)).ConfigureServices(s=> {
                s.RemoveAll<DbContextOptions<TiemChungDbContext>>();
                s.RemoveAll<TiemChungDbContext>();
                s.AddDbContext<TiemChungDbContext>(o=>o.UseSqlServer(cs.ConnectionString));
            }));
        using var client=factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost"),HandleCookies=false });
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/v1/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/v1/auth/register",new {email="a@example.invalid",password="synthetic-password-123",accountType="Employee"})).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted,(await client.PostAsJsonAsync("/api/v1/auth/register",new {email="a@example.invalid",password="synthetic-password-123"})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/auth/login",new {identifier="a@example.invalid",password="synthetic-password-123",clientType="native"})).StatusCode);
        var outbox=await db.Set<IdentityMailOutbox>().SingleAsync(); var payload=outbox.EncryptedPayload!; var plain=new byte[payload.Length-28];
        using(var aes=new System.Security.Cryptography.AesGcm(Convert.FromBase64String(values["Identity:MailEncryptionKey"]!),16)) aes.Decrypt(payload.AsSpan(0,12),payload.AsSpan(28),payload.AsSpan(12,16),plain);
        using var mail=JsonDocument.Parse(plain);
        Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsJsonAsync("/api/v1/auth/email-verifications/confirm",new {token=mail.RootElement.GetProperty("token").GetString()})).StatusCode);
        var login=await client.PostAsJsonAsync("/api/v1/auth/login",new {identifier="a@example.invalid",password="synthetic-password-123",clientType="web"});
        Assert.Equal(HttpStatusCode.OK,login.StatusCode);
        using var envelope=JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.False(envelope.RootElement.TryGetProperty("refreshToken",out _));
        client.DefaultRequestHeaders.Authorization=new("Bearer",envelope.RootElement.GetProperty("accessToken").GetString());
        using var list=JsonDocument.Parse(await client.GetStringAsync("/api/v1/customers"));
        Assert.Equal(0,list.RootElement.GetProperty("items").GetArrayLength());
        var account=await db.Set<TaiKhoan>().SingleAsync();
        var own=new KhachHang { HoTen="Synthetic Own",CCCD="000000000001",SoDienThoai="0900000000",Email="a@example.invalid" };
        var other=new KhachHang { HoTen="Synthetic Other",CCCD="000000000002",SoDienThoai="0900000001",Email="b@example.invalid" };
        db.AddRange(own,other); await db.SaveChangesAsync();
        var recipient=new NguoiDuocTiem { MaKhachHang=own.MaKhachHang,HoTen="Synthetic Child",NgaySinh=new DateOnly(2020,1,1),GioiTinh="Khác",QuanHeVoiKhachHang="Con" };
        db.Add(recipient); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>IdentityFixture.LinkAsync(db,"Production",account.MaTaiKhoan,own.MaKhachHang,null));
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>IdentityFixture.LinkAsync(db,"Test",account.MaTaiKhoan,own.MaKhachHang,null));
        await IdentityFixture.MarkSyntheticAsync(db);
        await IdentityFixture.LinkAsync(db,"Test",account.MaTaiKhoan,own.MaKhachHang,null);
        using var scoped=JsonDocument.Parse(await client.GetStringAsync("/api/v1/customers?pageSize=1"));
        Assert.Equal(own.MaKhachHang,scoped.RootElement.GetProperty("items")[0].GetProperty("id").GetInt32());
        using var hidden=JsonDocument.Parse(await client.GetStringAsync("/api/v1/customers?search=Other"));
        Assert.Equal(0,hidden.RootElement.GetProperty("items").GetArrayLength());
        using var noInheritedScope=JsonDocument.Parse(await client.GetStringAsync($"/api/v1/recipients?customerId={own.MaKhachHang}"));
        Assert.Equal(0,noInheritedScope.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/customers",new {})).StatusCode);
        var cookies=login.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(cookies,x=>x.StartsWith("__Host-klcn-refresh=") && x.Contains("secure",StringComparison.OrdinalIgnoreCase) && x.Contains("httponly",StringComparison.OrdinalIgnoreCase));
        client.DefaultRequestHeaders.Add("Cookie",string.Join("; ",cookies.Select(x=>x.Split(';')[0])));
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/auth/refresh",new {clientType="web"})).StatusCode);
        client.DefaultRequestHeaders.Add("Origin","https://localhost");
        var csrf=cookies.Single(x=>x.StartsWith("__Host-klcn-csrf=")).Split(';')[0].Split('=',2)[1];
        client.DefaultRequestHeaders.Add("X-CSRF-Token",csrf);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/v1/auth/refresh",new {clientType="web"})).StatusCode);
        client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Remove("X-CSRF-Token");
        foreach(var title in new[]{"LeTan","Admin","BacSi","DieuDuong","ThuNgan","ThuKho"}) {
            var employee=new NhanVien { HoTen="Synthetic "+title,CCCD="000000000003",SoDienThoai="0900000000",Email="staff@example.invalid",BoPhan="Test",ChucDanh=title,TrangThai="Active" };
            db.Add(employee); await db.SaveChangesAsync();
            var staff=new TaiKhoan { MaNhanVien=employee.MaNhanVien,TenDangNhap="staff-"+title,NormalizedUsername=("staff-"+title).ToUpperInvariant(),
                AccountType="Employee",TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow };
            staff.MatKhauHash=new Microsoft.AspNetCore.Identity.PasswordHasher<TaiKhoan>().HashPassword(staff,"synthetic-staff-password-123");
            db.Add(staff); await db.SaveChangesAsync();
            var response=await client.PostAsJsonAsync("/api/v1/auth/login",new {identifier=staff.TenDangNhap,password="synthetic-staff-password-123",clientType="native"});
            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            using var staffTokens=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            client.DefaultRequestHeaders.Authorization=new("Bearer",staffTokens.RootElement.GetProperty("accessToken").GetString());
            Assert.Equal(title=="LeTan"?HttpStatusCode.OK:HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/customers")).StatusCode);
            if(title=="LeTan") {
                await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectProfileAudit ON SecurityAudit AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action='profiles.created') THROW 51009, 'Synthetic audit failure', 1; END;");
                var failed=await client.PostAsJsonAsync("/api/v1/customers",new {fullName="Synthetic Rollback",identityNumber="000000000099",phoneNumber="0900000000",email="rollback@example.invalid"});
                Assert.Equal(HttpStatusCode.InternalServerError,failed.StatusCode);
                Assert.False(await db.Customers.AnyAsync(x=>x.CCCD=="000000000099"));
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectProfileAudit;");
            }
            var create=await client.PostAsJsonAsync("/api/v1/customers",new {fullName="Synthetic Created",identityNumber="000000000010",phoneNumber="0900000000",email="created@example.invalid"});
            Assert.Equal(title=="LeTan"?HttpStatusCode.Created:HttpStatusCode.Forbidden,create.StatusCode);
            var put=await client.PutAsJsonAsync($"/api/v1/recipients/{recipient.MaNguoiDuocTiem}",new {fullName="Changed",identityNumber=(string?)null,dateOfBirth="2020-01-01",gender="Khác",relationshipToCustomer="Con"});
            Assert.Equal(title=="LeTan"?HttpStatusCode.Conflict:HttpStatusCode.Forbidden,put.StatusCode);
            if(title=="Admin") {
                var permission=new QuyenHan { Code="XemHoSo",TenQuyen="XemHoSo",MoTa="Synthetic" };
                db.Add(permission); await db.SaveChangesAsync();
                db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan=staff.MaTaiKhoan,MaQuyen=permission.MaQuyen }); await db.SaveChangesAsync();
                Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/customers")).StatusCode);
            }
            await db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==staff.MaTaiKhoan).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Locked"));
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/v1/auth/me")).StatusCode);
        }
        Assert.Equal("Synthetic Child",(await db.Recipients.AsNoTracking().SingleAsync()).HoTen);
    }
}
