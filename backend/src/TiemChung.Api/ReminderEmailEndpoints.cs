using System.Globalization;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class ReminderEmailEndpoints
{
    private static async Task<IdentityActor> Reception(HttpContext context, IdentityAuthorization auth, bool write, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, write, ct);
        actor.RequirePermission("GuiNhacLich");
        return actor;
    }

    internal static IQueryable<ReminderEmail> Snapshot(TiemChungDbContext db, AppointmentDto a) =>
        db.Set<ReminderEmail>().Where(x => x.AppointmentId == a.Id && x.RegistrationId == a.RegistrationId &&
            x.RecipientId == a.Recipient.Id && x.ItemKind == a.ItemKind && x.RegistrationItemId == a.RegistrationItemId && x.ScheduledAt == a.ScheduledAt.UtcDateTime);

    internal static async Task<AppointmentDto> Source(TiemChungDbContext db, int id, CancellationToken ct) =>
        await AppointmentEndpoints.Query(db, ClinicalEndpoints.Sources(db).Where(x => x.MaLichHen == id)).SingleOrDefaultAsync(ct)
            ?? throw new IdentityFailure(409, "reminderSourceConflict");

    internal static Task<bool> Eligible(TiemChungDbContext db, AppointmentDto a, CancellationToken ct) =>
        Eligible(db, a, "Manual", DateTimeOffset.UtcNow, ct);

    internal static async Task<bool> Eligible(TiemChungDbContext db, AppointmentDto a, string kind, DateTimeOffset now, CancellationToken ct) =>
        a.Status != "DaXepLich" || !(kind switch {
            "Manual" or "TMinus48" or "TMinus24" => a.ScheduledAt > now,
            "DPlus3" => a.ScheduledAt.AddDays(3) <= now,
            "DPlus7" => a.ScheduledAt.AddDays(7) <= now,
            _ => false
        }) ? false : !await
        (from m in db.Set<MuiTiem>()
         join i in db.Set<ChiDinhTiem>() on m.MaChiDinh equals i.MaChiDinh
         join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
         where p.MaLichHen == a.Id && m.TrangThai == "DaTiem"
         select m.MaMuiTiem).AnyAsync(ct);

    internal static bool ValidEmail(string? email) => email?.Length is > 0 and <= 254 &&
        !email.Any(char.IsControl) && MailAddress.TryCreate(email, out var parsed) && parsed.Address == email && parsed.Host.Contains('.');

    internal static async Task<TaiKhoan?> Recipient(TiemChungDbContext db, AppointmentDto a, CancellationToken ct)
    {
        var customers = await db.Set<AccountProfileLink>().Where(l => l.State == "Active" && l.CustomerId == a.Customer.Id).Select(l => l.AccountId).Take(2).ToArrayAsync(ct);
        var recipients = await db.Set<AccountProfileLink>().Where(l => l.State == "Active" && l.RecipientId == a.Recipient.Id).Select(l => l.AccountId).Take(2).ToArrayAsync(ct);
        if (customers.Length != 1 || recipients.Length != 1 || customers[0] != recipients[0]) return null;
        var owner = await db.Set<TaiKhoan>().AsNoTracking().SingleOrDefaultAsync(x => x.MaTaiKhoan == customers[0] && x.AccountType == "Customer" && x.TrangThai == "Active", ct);
        return owner?.EmailVerifiedAt != null && ValidEmail(owner.LoginEmail) ? owner : null;
    }

    internal static string Mask(string email) => email[..1] + "***@" + email[(email.IndexOf('@') + 1)..];
    private static string Display(string name) => name.StartsWith("[DEMO] ", StringComparison.OrdinalIgnoreCase) ? name[7..] : name;
    internal static (string Subject, string Body) Content(AppointmentDto a, IConfiguration config, string kind = "Manual")
    {
        var hotline = config["Appointments:Hotline"];
        if (string.IsNullOrWhiteSpace(hotline)) throw new IdentityFailure(503, "dependencyUnavailable");
        var time = a.ScheduledAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm 'UTC+07:00 (Asia/Ho_Chi_Minh)'", CultureInfo.InvariantCulture);
        var recall = kind is "DPlus3" or "DPlus7";
        var intro = recall ? "Chúng tôi ghi nhận lịch hẹn đã qua nhưng chưa có thông tin mũi tiêm hoàn tất trong hệ thống.\nVui lòng liên hệ trung tâm để sắp xếp lại.\n\n" : "";
        var content = ($"[VNVC] Nhắc lịch tiêm chủng LH-{a.Id:0000}",
            $"Trung tâm tiêm chủng VNVC — VNVC Hoàng Văn Thụ\n\nKính chào Quý khách,\n\n{intro}Mã lịch hẹn: LH-{a.Id:0000}\nMã đăng ký: DK-{a.RegistrationId:0000}\nNgười được tiêm: {Display(a.Recipient.FullName)}\nDịch vụ: {Display(a.ServiceName)}\nNgày giờ hẹn: {time}\nĐịa điểm: {a.Location}\nHotline: {hotline}\n\nNếu cần thay đổi lịch, vui lòng liên hệ trung tâm qua hotline trên để được hỗ trợ.\nLịch hẹn không thay thế khám sàng lọc và chỉ định chuyên môn.\n\nTrân trọng,\nTrung tâm tiêm chủng VNVC");
        return (recall ? $"[VNVC] Liên hệ sắp xếp lịch LH-{a.Id:0000}" : content.Item1, content.Item2);
    }

    internal static bool Retryable(ReminderEmail row) => row.Status == "GuiLoi" &&
        row.LastErrorCode is "providerUnavailable" or "providerRejected" && row.NextAttemptAt <= DateTimeOffset.UtcNow;

    private static async Task<ReminderEmailDto> Result(TiemChungDbContext db, ReminderEmail row, CancellationToken ct)
    {
        var staff = row.CreatedByEmployeeId == null ? null : await db.Set<NhanVien>().AsNoTracking().SingleAsync(x => x.MaNhanVien == row.CreatedByEmployeeId, ct);
        return new(row.Id, row.AppointmentId, "Email", row.Status, row.Body, row.CreatedAt,
            staff == null ? null : new(staff.MaNhanVien, staff.HoTen), Mask(row.RecipientEmail), row.Subject, row.Attempts,
            row.DeliveredAt, row.NextAttemptAt, row.LastErrorCode, Retryable(row), row.Kind);
    }

    public static void MapReminderEmailEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/reminders/email-queue", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            string? search, int? page, int? pageSize, CancellationToken ct) => {
            await Reception(context, auth, false, ct);
            search = search?.Trim();
            if (search?.Length > 100 || page is <= 0 || pageSize is <= 0 or > 100 || (long)((page ?? 1) - 1) * (pageSize ?? 25) > int.MaxValue)
                throw new IdentityFailure(400, "validation");
            var now = DateTime.UtcNow;
            var shots = from m in db.Set<MuiTiem>() join i in db.Set<ChiDinhTiem>() on m.MaChiDinh equals i.MaChiDinh
                        join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham where m.TrangThai == "DaTiem" select p.MaLichHen;
            var rows = ClinicalEndpoints.Sources(db).Where(a =>
                (a.TrangThai == "DaXepLich" && a.ThoiGianHen > now && !shots.Contains(a.MaLichHen)) ||
                db.Set<ReminderEmail>().Any(r => r.AppointmentId == a.MaLichHen));
            if (!string.IsNullOrEmpty(search)) {
                int.TryParse(search.StartsWith("LH-", StringComparison.OrdinalIgnoreCase) ? search[3..] : search, out var id);
                rows = rows.Where(a => a.MaLichHen == id || db.Recipients.Any(p => p.MaNguoiDuocTiem == a.MaNguoiDuocTiem && p.HoTen.Contains(search)));
            }
            return Results.Ok(new AppointmentListResponse(await AppointmentEndpoints.Query(db, rows.OrderBy(a => a.ThoiGianHen).ThenBy(a => a.MaLichHen)
                .Skip(((page ?? 1) - 1) * (pageSize ?? 25)).Take(pageSize ?? 25)).ToArrayAsync(ct)));
        }).WithTags("Reminders");
        var group = app.MapGroup("/api/v1/appointments/{id:int}/email-reminder").WithTags("Reminders");
        group.AddEndpointFilter(async (invocation, next) => {
            try { return await next(invocation); }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });
        group.MapGet("", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, IConfiguration config, CancellationToken ct) => {
            await Reception(context, auth, false, ct);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            if (!await db.Set<LichHenTiem>().AnyAsync(x => x.MaLichHen == id, ct)) throw new IdentityFailure(404, "resourceNotFound");
            var a = await Source(db, id, ct);
            var row = await Snapshot(db, a).AsNoTracking().SingleOrDefaultAsync(r => r.Kind == "Manual", ct);
            var historyRows = await db.Set<ReminderEmail>().AsNoTracking().Where(r => r.AppointmentId == id && (row == null || r.Id != row.Id))
                .OrderByDescending(r => r.Id).ToArrayAsync(ct);
            var history = new List<ReminderEmailDto>();
            foreach (var old in historyRows) history.Add((await Result(db, old, ct)) with {
                CanRetry = Retryable(old) && await Eligible(db, a, old.Kind, DateTimeOffset.UtcNow, ct) &&
                    await Snapshot(db, a).AnyAsync(r => r.Id == old.Id, ct)
            });
            if (row != null) return Results.Ok(new ReminderEmailDetailDto(a, null, false,
                (await Result(db, row, ct)) with { CanRetry = await Eligible(db, a, ct) && Retryable(row) },
                Mask(row.RecipientEmail), row.Subject, null, history.ToArray()));
            var eligible = await Eligible(db, a, ct);
            var recipient = eligible ? await Recipient(db, a, ct) : null;
            var content = recipient == null ? default : Content(a, config);
            return Results.Ok(new ReminderEmailDetailDto(a, content.Body, recipient != null, null,
                recipient == null ? null : Mask(recipient.LoginEmail!), content.Subject, eligible ? recipient == null ? "reminderRecipientUnavailable" : null : "reminderSourceConflict", history.ToArray()));
        });
        group.MapPost("", async (int id, CreateReminderRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, IConfiguration config, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await Reception(context, auth, true, ct);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            _ = await db.Set<LichHenTiem>().FromSqlInterpolated($"SELECT * FROM LichHenTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaLichHen={id}")
                .SingleOrDefaultAsync(ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            var a = await Source(db, id, ct);
            // Replay returns the committed snapshot, including after a lost HTTP response.
            var row = await Snapshot(db, a).SingleOrDefaultAsync(r => r.Kind == "Manual", ct);
            if (row == null) {
                if (!await Eligible(db, a, ct)) throw new IdentityFailure(409, "reminderSourceConflict");
                var recipient = await Recipient(db, a, ct) ?? throw new IdentityFailure(409, "reminderRecipientUnavailable");
                var content = Content(a, config);
                row = new ReminderEmail { AppointmentId = id, RegistrationId = a.RegistrationId, CustomerId = a.Customer.Id,
                    RecipientId = a.Recipient.Id, ItemKind = a.ItemKind, RegistrationItemId = a.RegistrationItemId,
                    ScheduledAt = a.ScheduledAt.UtcDateTime, Location = a.Location, RecipientName = a.Recipient.FullName, ServiceName = a.ServiceName,
                    RecipientAccountId = recipient.MaTaiKhoan, RecipientEmail = recipient.LoginEmail!, Subject = content.Subject, Body = content.Body,
                    CreatedByAccountId = actor.Account.MaTaiKhoan, CreatedByEmployeeId = actor.Employee!.MaNhanVien, CreatedAt = DateTimeOffset.UtcNow };
                db.Add(row); await db.SaveChangesAsync(ct);
                var audit = IdentityTransactions.Audit(db, "reminders.enqueued", context.TraceIdentifier, actor.Account.MaTaiKhoan, session: actor.Session.Id);
                audit.ResourceType = "ReminderEmail"; audit.ResourceId = row.Id.ToString(CultureInfo.InvariantCulture);
                await db.SaveChangesAsync(ct);
            }
            var result = await Result(db, row, ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/appointments/{id}/email-reminder", result);
        });
        group.MapPost("/retry", async (int id, RetryReminderEmailRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await Reception(context, auth, true, ct);
            _ = await db.Set<LichHenTiem>().FromSqlInterpolated($"SELECT * FROM LichHenTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaLichHen={id}")
                .SingleOrDefaultAsync(ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            var a = await Source(db, id, ct);
            var row = await Snapshot(db, a).SingleOrDefaultAsync(r => req.DeliveryId == null ? r.Kind == "Manual" : r.Id == req.DeliveryId, ct)
                ?? throw new IdentityFailure(409, "reminderRetryUnavailable");
            // Expected attempt prevents a stale retry click from retrying a later failed attempt.
            if (!Retryable(row) || req.ExpectedAttempts != row.Attempts) throw new IdentityFailure(409, "reminderRetryUnavailable");
            if (!await Eligible(db, a, row.Kind, DateTimeOffset.UtcNow, ct)) throw new IdentityFailure(409, "reminderSourceConflict");
            var recipient = await Recipient(db, a, ct);
            if (recipient?.MaTaiKhoan != row.RecipientAccountId || recipient.LoginEmail != row.RecipientEmail)
                throw new IdentityFailure(409, "reminderRecipientUnavailable");
            row.Status = "ChoGui"; row.DispatchStarted = false; row.NextAttemptAt = null; row.LastErrorCode = null;
            var audit = IdentityTransactions.Audit(db, "reminders.retried", context.TraceIdentifier, actor.Account.MaTaiKhoan, session: actor.Session.Id);
            audit.ResourceType = "ReminderEmail"; audit.ResourceId = row.Id.ToString(CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct); var result = await Result(db, row, ct); await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/appointments/{id}/email-reminder", result);
        });
    }
}
