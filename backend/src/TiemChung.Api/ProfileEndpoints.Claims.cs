using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static partial class ProfileEndpoints
{
    private static void MapClaims(RouteGroupBuilder group)
    {
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
                : ["fullName", "dateOfBirth", "identityNumber"], ct);
            var name = IdentityEndpoints.Value(body, "fullName").Trim();
            var dob = ReadBirthDate(body);
            if (name.Length is 0 or > 150 || dob == DateOnly.MinValue || dob > ProfileRules.Today(DateTimeOffset.UtcNow))
                throw ClaimUnavailable();

            var accountId = actor.Account.MaTaiKhoan;
            var links = db.Set<AccountProfileLink>().Where(l => l.State == "Active");
            var customerId = await links.Where(l => l.AccountId == accountId && l.CustomerId != null)
                .Select(l => l.CustomerId).SingleOrDefaultAsync(ct);

            if (customer)
            {
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
                if (customerId is null) throw ClaimUnavailable();
                var number = OptionalIdentity(body)?.Trim();
                if (number?.Length > 0 && (number.Length != 12 || !number.All(char.IsAsciiDigit)))
                    throw ClaimUnavailable();
                var candidates = await db.Recipients.AsNoTracking().Where(r => r.MaKhachHang == customerId &&
                    r.NgaySinh == dob).ToListAsync(ct);
                var matches = candidates.Where(r => SameText(r.HoTen, name) &&
                    (string.IsNullOrEmpty(number) || r.CCCD?.Trim() == number)).Take(2).ToArray();
                if (matches.Length != 1) throw ClaimUnavailable();

                var match = matches[0];
                var linkedAccount = await links.Where(l => l.RecipientId == match.MaNguoiDuocTiem)
                    .Select(l => (int?)l.AccountId).SingleOrDefaultAsync(ct);
                if (linkedAccount is not null && linkedAccount != accountId) throw ClaimUnavailable();
                if (linkedAccount is null)
                    db.Add(new AccountProfileLink { AccountId = accountId, RecipientId = match.MaNguoiDuocTiem, CreatedAt = DateTimeOffset.UtcNow });
                profile = new RecipientDto(match.MaNguoiDuocTiem, match.MaKhachHang, match.HoTen, match.CCCD,
                    match.NgaySinh, match.GioiTinh, match.QuanHeVoiKhachHang);
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
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        if (failure is not null) throw failure;
        return Results.Ok(profile);
    }

    private static IdentityFailure ClaimUnavailable() => new(409, "profileClaimUnavailable");

    private static bool SameText(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
