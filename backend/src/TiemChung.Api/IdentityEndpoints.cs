using System.Text.Json;

namespace TiemChung.Api;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/v1/auth");
        g.MapPost("/register", async (HttpContext c, IdentityRegistration s, CancellationToken ct) => {
            var p = await Payload(c, ["email","password"], ct);
            Limit(c,"register",Value(p,"email"));
            await s.RegisterAsync(Value(p,"email"), Value(p,"password"), c.TraceIdentifier, ct);
            return Results.Json(new { status = "verificationPending" }, statusCode: 202);
        });
        g.MapPost("/email-verifications/resend", async (HttpContext c, IdentityRegistration s, CancellationToken ct) => {
            var p = await Payload(c, ["email"], ct);
            Limit(c,"resend",Value(p,"email"));
            await s.ResendAsync(Value(p,"email"), c.TraceIdentifier, ct);
            return Results.Json(new { status = "verificationPending" }, statusCode: 202);
        });
        g.MapPost("/email-verifications/confirm", async (HttpContext c, IdentityRegistration s, CancellationToken ct) => {
            var p = await Payload(c, ["token"], ct);
            Limit(c,"confirm","");
            await s.ConfirmAsync(Value(p,"token"), c.TraceIdentifier, ct);
            return Results.NoContent();
        });
        g.MapPost("/login", async (HttpContext c, IdentitySessions s, CancellationToken ct) => {
            var p = await Payload(c, ["identifier","password","clientType"], ct);
            Limit(c,"login",Value(p,"identifier"));
            var type = Value(p,"clientType");
            var result = await s.LoginAsync(Value(p,"identifier"), Value(p,"password"), type, c.TraceIdentifier, ct);
            return Envelope(c, result, type);
        });
        g.MapPost("/refresh", async (HttpContext c, IdentitySessions s, IConfiguration config, CancellationToken ct) => {
            var (refresh,type,csrf) = await Transport(c,config,ct);
            return Envelope(c, await s.RefreshAsync(refresh,type,csrf,c.TraceIdentifier,ct), type);
        });
        g.MapPost("/logout", async (HttpContext c, IdentitySessions s, IConfiguration config, CancellationToken ct) => {
            var (refresh,type,csrf) = await Transport(c,config,ct);
            await s.LogoutAsync(refresh,type,csrf,c.TraceIdentifier,ct);
            if (type == "web") {
                c.Response.Cookies.Delete("__Host-klcn-refresh", Cookie(true));
                c.Response.Cookies.Delete("__Host-klcn-csrf", Cookie(false));
            }
            return Results.NoContent();
        });
        g.MapGet("/me", async (HttpContext c, IdentityAuthorization auth, CancellationToken ct) => {
            var a = await auth.RequireAsync(c,false,ct);
            var db = c.RequestServices.GetRequiredService<TiemChung.Infrastructure.Data.TiemChungDbContext>();
            var linked = a.IsCustomer && await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                db.Set<TiemChung.Domain.Entities.AccountProfileLink>().Where(x => x.AccountId == a.Account.MaTaiKhoan && x.State == "Active"),ct);
            return Results.Ok(new {
                accountId=a.Account.MaTaiKhoan, accountType=a.IsCustomer?"customer":"employee",
                emailVerified=a.Account.EmailVerifiedAt is not null,
                profileState=a.IsCustomer?(linked?"linked":"unlinked"):"notApplicable",
                employee=a.Employee is null?null:new { id=a.Employee.MaNhanVien,title=a.Employee.ChucDanh,administrativeDuty=a.Account.AdministrativeDuty },
                permissions=a.Permissions,
                capabilities=new { profilesRead=a.IsCustomer || a.CanReadProfiles, profilesCreate=a.CanCreateProfiles,
                    profilesUpdate=false,selfClaim=false,representatives=false }
            });
        });
    }

    internal static async Task<JsonElement> Payload(HttpContext c, string[] fields, CancellationToken ct)
    {
        if (c.Request.ContentLength > 16384) throw new IdentityFailure(400,"validation");
        using var stream = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count=await c.Request.Body.ReadAsync(buffer,ct))>0) {
            if (stream.Length+count>16384) throw new IdentityFailure(400,"validation");
            await stream.WriteAsync(buffer.AsMemory(0,count),ct);
        }
        try {
            using var doc=JsonDocument.Parse(stream.ToArray());
            var root=doc.RootElement;
            if (root.ValueKind!=JsonValueKind.Object) throw new IdentityFailure(400,"validation");
            var seen=new HashSet<string>();
            foreach(var p in root.EnumerateObject())
                if(!fields.Contains(p.Name) || !seen.Add(p.Name)) throw new IdentityFailure(400,"unknownFields");
            return root.Clone();
        } catch(JsonException) { throw new IdentityFailure(400,"validation"); }
    }
    private static void Limit(HttpContext c,string action,string identifier) => c.RequestServices.GetRequiredService<IdentityRateLimits>()
        .Check(action,identifier,c.Connection.RemoteIpAddress?.ToString()??"unknown");
    internal static string Value(JsonElement p,string key) => p.TryGetProperty(key,out var v) && v.ValueKind==JsonValueKind.String
        ? v.GetString()! : throw new IdentityFailure(400,"validation");
    private static CookieOptions Cookie(bool httpOnly) => new() { HttpOnly=httpOnly,Secure=true,SameSite=SameSiteMode.Strict,Path="/" };
    private static IResult Envelope(HttpContext c,SessionTokens t,string type)
    {
        if(type=="web") {
            var refresh=Cookie(true); refresh.MaxAge=TimeSpan.FromDays(7);
            var csrf=Cookie(false); csrf.MaxAge=TimeSpan.FromDays(7);
            c.Response.Cookies.Append("__Host-klcn-refresh",t.RefreshToken,refresh);
            c.Response.Cookies.Append("__Host-klcn-csrf",t.CsrfToken!,csrf);
            return Results.Ok(new { accessToken=t.AccessToken,tokenType="Bearer",expiresIn=300,sessionId=t.SessionId });
        }
        return Results.Ok(new { accessToken=t.AccessToken,tokenType="Bearer",expiresIn=300,sessionId=t.SessionId,refreshToken=t.RefreshToken });
    }
    private static async Task<(string Refresh,string Type,string? Csrf)> Transport(HttpContext c,IConfiguration config,CancellationToken ct)
    {
        var p=await Payload(c,["clientType","refreshToken"],ct);
        var type=Value(p,"clientType");
        var hasCookie=c.Request.Cookies.TryGetValue("__Host-klcn-refresh",out var cookie);
        if(type=="native") {
            if(hasCookie) throw new IdentityFailure(400,"validation");
            return (Value(p,"refreshToken"),type,null);
        }
        if(type!="web" || p.TryGetProperty("refreshToken",out _)) throw new IdentityFailure(400,"validation");
        var origin=c.Request.Headers.Origin.ToString();
        var allowed=config.GetSection("Cors:Origins").Get<string[]>()??[];
        var csrf=c.Request.Headers["X-CSRF-Token"].ToString();
        if(!allowed.Contains(origin,StringComparer.Ordinal) || string.IsNullOrEmpty(csrf) ||
            !c.Request.Cookies.TryGetValue("__Host-klcn-csrf",out var csrfCookie) || csrf!=csrfCookie)
            throw new IdentityFailure(403,"csrfFailed");
        return (cookie??"",type,csrf);
    }
}
