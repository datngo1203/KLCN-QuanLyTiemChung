using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
namespace LocalAcceptance;
public static class Entry {
public static async Task Main(string[] args){
 var lines=await File.ReadAllLinesAsync(@"C:\Users\DAT\Documents\Codex\KLCN090-Coordination\TEST-ACCOUNTS.local.md");
 var actor=lines.First(x=>x.StartsWith("- Password synthetic dự kiến cho prompt Recover: `")).Split('`')[1];
 var staff=lines.First(x=>x.StartsWith("- Reset generation2:")).Split('`')[3];
 var cs=Environment.GetEnvironmentVariable("M1_BOUNDARY_APP_CONNECTION")!;
 await using(var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs).Options)){
  var row=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.TenDangNhap=="legacy-staff");
  if(new PasswordHasher<TaiKhoan>().VerifyHashedPassword(row,row.MatKhauHash,staff)==PasswordVerificationResult.Failed)throw new Exception("Recorded staff password does not match; stop.");
  Console.WriteLine("StaffRecordedPasswordHash=PASS");
 }
 Directory.SetCurrentDirectory(AppContext.BaseDirectory);
 var values=new Dictionary<string,string?>{["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))};
 await using var host=new WebApplicationFactory<global::Program>().WithWebHostBuilder(b=>b.UseEnvironment("Test").UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../backend/src/TiemChung.Api"))).UseSetting("ConnectionStrings:TiemChung",cs).UseSetting("Catalog:UseDemoData","false").ConfigureLogging(l=>l.ClearProviders()).ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(values)).ConfigureServices(s=>{s.RemoveAll<DbContextOptions<TiemChungDbContext>>();s.RemoveAll<TiemChungDbContext>();s.AddDbContext<TiemChungDbContext>(o=>o.UseSqlServer(cs));}));
 using var client=host.CreateClient();
 if(args.Contains("--hold-session")){
  var next=lines.First(x=>x.StartsWith("- Reset kiểm thu hồi phiên generation2 tiếp theo:")).Split('`')[1];
  await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs).Options);
  var before=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.TenDangNhap=="legacy-staff");
  var login=await client.PostAsJsonAsync("/api/v1/auth/login",new{identifier="legacy-staff",password=staff,clientType="native"});login.EnsureSuccessStatusCode();
  var session=await login.Content.ReadFromJsonAsync<JsonElement>();
  client.DefaultRequestHeaders.Authorization=new("Bearer",session.GetProperty("accessToken").GetString());
  if((await client.GetAsync("/api/v1/auth/me")).StatusCode!=HttpStatusCode.OK)throw new Exception("Pre-reset session not valid");
  Console.WriteLine("PRE_RESET_STAFF_SESSION_VALID=PASS; waiting for actual user CLI reset; no tokens emitted.");
  var until=DateTimeOffset.UtcNow.AddHours(12);
  TaiKhoan after;
    DateTimeOffset TokenExpiry(){using var payload=JsonDocument.Parse(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(session.GetProperty("accessToken").GetString()!.Split('.')[1]));return DateTimeOffset.FromUnixTimeSeconds(payload.RootElement.GetProperty("exp").GetInt64());}
  while(true){
   await Task.Delay(2000);after=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==before.MaTaiKhoan);
   if(after.SecurityVersion!=before.SecurityVersion)break;
   if(DateTimeOffset.UtcNow>until)throw new Exception("Timed out; no CLI revocation PASS.");
   if(TokenExpiry()<DateTimeOffset.UtcNow.AddSeconds(60)){
    var rotate=await client.PostAsJsonAsync("/api/v1/auth/refresh",new{clientType="native",refreshToken=session.GetProperty("refreshToken").GetString()});
    if(!rotate.IsSuccessStatusCode){after=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==before.MaTaiKhoan);if(after.SecurityVersion!=before.SecurityVersion)break;throw new Exception("Pre-reset session could not be kept valid");}
    session=await rotate.Content.ReadFromJsonAsync<JsonElement>();client.DefaultRequestHeaders.Authorization=new("Bearer",session.GetProperty("accessToken").GetString());
    if((await client.GetAsync("/api/v1/auth/me")).StatusCode!=HttpStatusCode.OK){after=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==before.MaTaiKhoan);if(after.SecurityVersion!=before.SecurityVersion)break;throw new Exception("Pre-reset refreshed access not valid");}
   }
  }
  if(TokenExpiry()<=DateTimeOffset.UtcNow.AddSeconds(10))throw new Exception("Access expiry too close; cannot attribute denial to reset.");
  if(after.SecurityVersion!=before.SecurityVersion+1)throw new Exception("Unexpected version change; inspect actual audit.");
  if((await client.GetAsync("/api/v1/auth/me")).StatusCode!=HttpStatusCode.Unauthorized)throw new Exception("Old access token still valid");
  if((await client.PostAsJsonAsync("/api/v1/auth/refresh",new{clientType="native",refreshToken=session.GetProperty("refreshToken").GetString()})).StatusCode!=HttpStatusCode.Unauthorized)throw new Exception("Old refresh still valid");
  var fresh=await client.PostAsJsonAsync("/api/v1/auth/login",new{identifier="legacy-staff",password=next,clientType="native"});fresh.EnsureSuccessStatusCode();
  var updated=await fresh.Content.ReadFromJsonAsync<JsonElement>();
  var end=await client.PostAsJsonAsync("/api/v1/auth/logout",new{clientType="native",refreshToken=updated.GetProperty("refreshToken").GetString()});if(end.StatusCode!=HttpStatusCode.NoContent)throw new Exception("New session logout failed");
  Console.WriteLine("CLI_RESET_REVOKES_PREEXISTING_ACCESS_AND_REFRESH=PASS; new recorded password HTTP login/logout=PASS");
  return;
 }
 foreach(var item in new[]{(Name:"old-admin",Password:actor),(Name:"legacy-staff",Password:staff)}){
  var login=await client.PostAsJsonAsync("/api/v1/auth/login",new{identifier=item.Name,password=item.Password,clientType="native"});
  if(login.StatusCode!=HttpStatusCode.OK)throw new Exception("HTTP login failed: "+item.Name+" "+(int)login.StatusCode);
  var body=await login.Content.ReadFromJsonAsync<JsonElement>();
  client.DefaultRequestHeaders.Authorization=new("Bearer",body.GetProperty("accessToken").GetString());
  var me=await client.GetAsync("/api/v1/auth/me");if(me.StatusCode!=HttpStatusCode.OK)throw new Exception("me failed");
  var refresh=await client.PostAsJsonAsync("/api/v1/auth/refresh",new{clientType="native",refreshToken=body.GetProperty("refreshToken").GetString()});refresh.EnsureSuccessStatusCode();body=await refresh.Content.ReadFromJsonAsync<JsonElement>();
  var logout=await client.PostAsJsonAsync("/api/v1/auth/logout",new{clientType="native",refreshToken=body.GetProperty("refreshToken").GetString()});if(logout.StatusCode!=HttpStatusCode.NoContent)throw new Exception("logout failed");
  if((await client.GetAsync("/api/v1/auth/me")).StatusCode!=HttpStatusCode.Unauthorized)throw new Exception("post logout auth not revoked");
  Console.WriteLine(item.Name+": real app SQL credential / HTTP host login-me-refresh-logout-revocation=PASS");
 }
}}
