using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class ReminderEndpoints
{
    private const string Channel = "LogMoPhong";
    private static async Task<IdentityActor> RequireReception(HttpContext context, IdentityAuthorization auth, bool write, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, write, ct);
        if (actor.IsCustomer || actor.Employee?.ChucDanh != "LeTan" || !actor.Permissions.Contains("QuanLyLichHen"))
            throw new IdentityFailure(403, "permissionDenied");
        return actor;
    }
    private static Task<ReminderDto?> Result(TiemChungDbContext db, int id, CancellationToken ct) =>
        (from r in db.Set<NhacLich>().AsNoTracking()
         join n in db.Set<NhanVien>() on r.MaNhanVien equals n.MaNhanVien into staff
         from n in staff.DefaultIfEmpty()
         where r.MaLichHen == id && r.KenhGui == Channel
         select new ReminderDto(r.MaNhacLich, id, r.KenhGui, r.TrangThai, r.NoiDungMoPhong,
             new DateTimeOffset(DateTime.SpecifyKind(r.ThoiDiemGui, DateTimeKind.Utc)),
             n == null ? null : new RegistrationProfileDto(n.MaNhanVien, n.HoTen))).SingleOrDefaultAsync(ct);

    private static async Task<AppointmentDto> Source(TiemChungDbContext db, int id, CancellationToken ct) =>
        await AppointmentEndpoints.Query(db, ClinicalEndpoints.Sources(db).Where(a => a.MaLichHen == id)).SingleOrDefaultAsync(ct)
            ?? throw new IdentityFailure(409, "reminderSourceConflict");

    private static Task<bool> HasShot(TiemChungDbContext db, int id, CancellationToken ct) =>
        (from m in db.Set<MuiTiem>()
         join i in db.Set<ChiDinhTiem>() on m.MaChiDinh equals i.MaChiDinh
         join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
         where p.MaLichHen == id && m.TrangThai == "DaTiem"
         select m.MaMuiTiem).AnyAsync(ct);

    private static string Content(AppointmentDto a)
    {
        var time = a.ScheduledAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss 'UTC+07:00'", CultureInfo.InvariantCulture);
        var content = $"VAC — NHẮC LỊCH MÔ PHỎNG\n\nMã lịch hẹn: LH-{a.Id:0000}\nNgười được tiêm: {a.Recipient.FullName}\nDịch vụ: {a.ServiceName}\nThời gian hẹn: {time}\nĐịa điểm: {a.Location}\n\nĐây là nội dung mô phỏng nội bộ, chưa được gửi đến khách hàng.";
        if (content.Length > 1000) throw new IdentityFailure(400, "validation") {
            Errors = new() { ["content"] = ["Nội dung nhắc lịch vượt 1000 ký tự. Kiểm tra thông tin lịch nguồn."] }
        };
        return content;
    }
    public static void MapReminderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/appointments/{id:int}/reminders").WithTags("Reminders");
        group.AddEndpointFilter(async (invocation, next) => {
            try { return await next(invocation); }
            catch (DbUpdateException e) when (e.InnerException is SqlException sql && sql.Number is 2601 or 2627 &&
                sql.Message.Contains("IX_NhacLich_Simulation", StringComparison.Ordinal)) {
                throw new IdentityFailure(409, "reminderAlreadyRecorded");
            }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });
        group.MapGet("", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await RequireReception(context, auth, false, ct);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            if (!await db.Set<LichHenTiem>().AnyAsync(a => a.MaLichHen == id, ct)) throw new IdentityFailure(404, "resourceNotFound");
            var a = await Source(db, id, ct);
            var result = await Result(db, id, ct);
            var canRecord = result == null && a.Status == "DaXepLich" && a.ScheduledAt > DateTimeOffset.UtcNow && !await HasShot(db, id, ct);
            return Results.Ok(new ReminderDetailDto(a, result == null && canRecord ? Content(a) : null, canRecord, result));
        });
        group.MapPost("", async (int id, CreateReminderRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireReception(context, auth, true, ct);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            var row = await db.Set<LichHenTiem>().FromSqlInterpolated($"SELECT * FROM LichHenTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaLichHen={id}")
                .SingleOrDefaultAsync(ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            if (await db.Set<NhacLich>().AnyAsync(r => r.MaLichHen == id && r.KenhGui == Channel, ct)) throw new IdentityFailure(409, "reminderAlreadyRecorded");
            var source = await Source(db, id, ct);
            if (row.TrangThai != "DaXepLich" || row.ThoiGianHen <= DateTime.UtcNow || await HasShot(db, id, ct))
                throw new IdentityFailure(409, "reminderSourceConflict");
            var content = Content(source);
            var now = DateTime.UtcNow;
            if (row.ThoiGianHen <= now) throw new IdentityFailure(409, "reminderSourceConflict");
            var reminder = new NhacLich { MaLichHen = id, MaNhanVien = actor.Employee!.MaNhanVien, KenhGui = Channel,
                TrangThai = "DaGhiLogMoPhong", NoiDungMoPhong = content, ThoiDiemGui = now };
            db.Add(reminder); await db.SaveChangesAsync(ct);
            var audit = IdentityTransactions.Audit(db, "reminders.simulated", context.TraceIdentifier, actor.Account.MaTaiKhoan, session: actor.Session.Id);
            audit.ResourceType = "Reminder"; audit.ResourceId = reminder.MaNhacLich.ToString(CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);
            var result = await Result(db, id, ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/appointments/{id}/reminders", result);
        });
    }
}
