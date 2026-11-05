using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text.Json;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static partial class ProfileEndpoints
{
    private static void MapClaims(RouteGroupBuilder group)
    {
        group.MapPost("/recipients/find", async (HttpContext context, IdentityRateLimits limits,
            TiemChungDbContext db, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            limits.CheckProfileClaimIp(context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            var body = await IdentityEndpoints.Payload(context,
                ["mode", "profileCode", "phoneNumber", "fullName", "dateOfBirth"], ct);
            var matches = await FindRecipients(db, body, null, ct);
            // Existence only: ownership and access are checked again by authenticated claim.
            return Results.Ok(new { matched = matches.Length == 1 });
        });
        group.MapPost("/customers/claim", (HttpContext context, IdentityAuthorization auth, IdentityRateLimits limits,
            TiemChungDbContext db, CancellationToken ct) => ClaimAsync(context, auth, limits, db, customer: true, ct));
        group.MapPost("/recipients/claim", (HttpContext context, IdentityAuthorization auth, IdentityRateLimits limits,
            TiemChungDbContext db, CancellationToken ct) => ClaimAsync(context, auth, limits, db, customer: false, ct));
    }

    private static async Task<IResult> ClaimAsync(HttpContext context, IdentityAuthorization auth, IdentityRateLimits limits,
        TiemChungDbContext db, bool customer, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        limits.CheckProfileClaimIp(context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var actor = await auth.RequireAsync(context, true, ct);
        IdentityFailure? failure = null;
        object? profile = null;
        try
        {
            limits.CheckProfileClaimAccount(actor.Account.MaTaiKhoan);
            if (!actor.IsCustomer) throw new IdentityFailure(403, "permissionDenied");

            var body = await IdentityEndpoints.Payload(context, customer
                ? ["fullName", "dateOfBirth", "phoneNumber"]
                : ["mode", "profileCode", "phoneNumber", "fullName", "dateOfBirth"], ct);
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource=N'KLCN.Profiles.Claim',
                    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 51001, 'Profile claim lock unavailable', 1;
                """, ct);
            var accountId = actor.Account.MaTaiKhoan;
            var links = db.Set<AccountProfileLink>().Where(l => l.State == "Active");
            var customerId = await links.Where(l => l.AccountId == accountId && l.CustomerId != null)
                .Select(l => l.CustomerId).SingleOrDefaultAsync(ct);

            if (customer)
            {
                var name = IdentityEndpoints.Value(body, "fullName").Trim();
                var dob = ReadBirthDate(body);
                if (name.Length is 0 or > 150 || dob == DateOnly.MinValue || dob > ProfileRules.Today(DateTimeOffset.UtcNow))
                    throw ClaimUnavailable();
                var phone = IdentityEndpoints.Value(body, "phoneNumber").Trim();
                if (customerId is not null || actor.Account.LoginEmail is not string email ||
                    phone.Length is < 10 or > 11 || !phone.All(char.IsAsciiDigit))
                    throw ClaimUnavailable();

                var candidates = await db.Customers.AsNoTracking()
                    .Where(c => c.NgaySinh == dob && c.SoDienThoai.Trim() == phone).ToListAsync(ct);
                var matches = candidates.Where(c => c.SoDienThoai.Trim() == phone &&
                    SameText(c.HoTen, name) && SameText(c.Email, email)).Take(2).ToArray();
                if (matches.Length != 1 || await links.AnyAsync(l => l.CustomerId == matches[0].MaKhachHang, ct))
                    throw ClaimUnavailable();

                var match = matches[0];
                db.Add(new AccountProfileLink { AccountId = accountId, CustomerId = match.MaKhachHang, CreatedAt = DateTimeOffset.UtcNow });
                profile = ToCustomerDto(match);
            }
            else
            {
                var matches = await FindRecipients(db, body, customerId, ct);
                if (matches.Length != 1) throw ClaimUnavailable();
                var match = matches[0];
                var owner = await db.Customers.AsNoTracking().SingleAsync(c => c.MaKhachHang == match.MaKhachHang, ct);
                if (customerId is null && await links.AnyAsync(l => l.CustomerId == owner.MaKhachHang, ct))
                    throw ClaimUnavailable();
                var linkedAccount = await links.Where(l => l.RecipientId == match.MaNguoiDuocTiem)
                    .Select(l => (int?)l.AccountId).SingleOrDefaultAsync(ct);
                if (linkedAccount is not null && linkedAccount != accountId) throw ClaimUnavailable();

                var now = DateTimeOffset.UtcNow;
                if (customerId is null)
                    db.Add(new AccountProfileLink { AccountId = accountId, CustomerId = owner.MaKhachHang, CreatedAt = now });
                if (linkedAccount is null)
                    db.Add(new AccountProfileLink { AccountId = accountId, RecipientId = match.MaNguoiDuocTiem, CreatedAt = now });
                profile = new
                {
                    recipient = new RecipientDto(match.MaNguoiDuocTiem, match.MaKhachHang, match.HoTen, match.CCCD,
                        match.NgaySinh, match.GioiTinh, match.QuanHeVoiKhachHang, match.MaHoSo),
                    customer = ToCustomerDto(owner)
                };
            }
        }
        catch (IdentityFailure error)
        {
            failure = error;
        }

        var resource = customer ? "Customer" : "Recipient";
        IdentityTransactions.Audit(db, "profiles.claimAttempt", context.TraceIdentifier, actor.Account.MaTaiKhoan,
            session: actor.Session.Id).ResourceType = resource;
        var outcome = failure is null ? "Success" : failure.Status == 429 ? "RateLimited" : "Denied";
        IdentityTransactions.Audit(db, "profiles.claimOutcome", context.TraceIdentifier, actor.Account.MaTaiKhoan,
            session: actor.Session.Id, outcome: outcome).ResourceType = resource;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException error) when (!customer && failure is null
            && error.Entries.Any(entry => entry.Entity is AccountProfileLink)
            && error.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            foreach (var link in db.ChangeTracker.Entries<AccountProfileLink>().Where(x => x.State == EntityState.Added).ToArray())
                link.State = EntityState.Detached;
            var audit = db.ChangeTracker.Entries<SecurityAudit>().Single(x => x.Entity.Action == "profiles.claimOutcome");
            audit.Entity.Outcome = "Denied";
            await db.SaveChangesAsync(ct);
            failure = ClaimUnavailable();
        }
        await tx.CommitAsync(ct);

        if (failure is not null) throw failure;
        return Results.Ok(profile);
    }

    private static async Task<NguoiDuocTiem[]> FindRecipients(TiemChungDbContext db, JsonElement body,
        int? customerId, CancellationToken ct)
    {
        var mode = IdentityEndpoints.Value(body, "mode");
        var phone = IdentityEndpoints.Value(body, "phoneNumber").Trim();
        if (phone.Length is < 10 or > 11 || !phone.All(char.IsAsciiDigit))
            throw new IdentityFailure(400, "validation");
        var candidates = from recipient in db.Recipients.AsNoTracking()
                         join owner in db.Customers.AsNoTracking() on recipient.MaKhachHang equals owner.MaKhachHang
                         where (customerId == null || owner.MaKhachHang == customerId) && owner.SoDienThoai.Trim() == phone
                         select new { Recipient = recipient, Phone = owner.SoDienThoai };
        string? name = null;
        if (mode == "profileCode")
        {
            var code = IdentityEndpoints.Value(body, "profileCode").Trim().ToUpperInvariant();
            if (body.TryGetProperty("fullName", out _) || body.TryGetProperty("dateOfBirth", out _)
                || !Regex.IsMatch(code, @"\AVAC-[0-9A-F]{12}\z"))
                throw new IdentityFailure(400, "validation");
            candidates = candidates.Where(x => x.Recipient.MaHoSo == code);
        }
        else if (mode == "personalInfo")
        {
            name = IdentityEndpoints.Value(body, "fullName").Trim();
            var dob = ReadBirthDate(body);
            if (body.TryGetProperty("profileCode", out _) || name.Length is 0 or > 150
                || dob == DateOnly.MinValue || dob > ProfileRules.Today(DateTimeOffset.UtcNow))
                throw new IdentityFailure(400, "validation");
            candidates = candidates.Where(x => x.Recipient.NgaySinh == dob);
        }
        else throw new IdentityFailure(400, "validation");
        var rows = await candidates.ToListAsync(ct);
        return rows.Where(x => x.Phone.Trim() == phone && (name is null || SameText(x.Recipient.HoTen, name)))
            .Select(x => x.Recipient).Take(2).ToArray();
    }

    private static IdentityFailure ClaimUnavailable() => new(409, "profileClaimUnavailable");

    private static bool SameText(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
