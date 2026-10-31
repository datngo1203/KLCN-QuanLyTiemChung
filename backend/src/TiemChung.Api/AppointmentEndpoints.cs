using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static partial class AppointmentEndpoints
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex OffsetTime();

    private static DateTime Utc(string? value)
    {
        if (value is null || !OffsetTime().IsMatch(value) ||
            !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            throw new IdentityFailure(400, "validation");
        return time.UtcDateTime;
    }

    private static async Task<IdentityActor> RequireReception(HttpContext context, IdentityAuthorization auth, bool write, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, write, ct);
        if (actor.IsCustomer || actor.Employee?.ChucDanh != "LeTan" || !actor.Permissions.Contains("QuanLyLichHen"))
            throw new IdentityFailure(403, "permissionDenied");
        return actor;
    }

    private static async Task<IdentityActor> RequireReader(HttpContext context, IdentityAuthorization auth, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, false, ct);
        if (!actor.IsCustomer && (actor.Employee?.ChucDanh != "LeTan" || !actor.Permissions.Contains("QuanLyLichHen")))
            throw new IdentityFailure(403, "permissionDenied");
        return actor;
    }

    private static IQueryable<LichHenTiem> Scope(TiemChungDbContext db, IdentityActor actor)
    {
        var rows = db.Set<LichHenTiem>().AsNoTracking().Where(a => a.MaDangKy != null);
        if (!actor.IsCustomer) return rows;
        var registrations = RegistrationEndpoints.Scope(db, actor);
        return rows.Where(a => registrations.Any(r => r.MaDangKy == a.MaDangKy && r.MaNguoiDuocTiem == a.MaNguoiDuocTiem));
    }

    public static void MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/appointments").WithTags("Appointments");
        group.AddEndpointFilter(async (invocation, next) => {
            try { return await next(invocation); }
            catch (DbUpdateException e) when (e.InnerException is SqlException sql && sql.Number is 2601 or 2627 &&
                (sql.Message.Contains("IX_LichHenTiem_VaccineTime", StringComparison.Ordinal) ||
                 sql.Message.Contains("IX_LichHenTiem_PackageTime", StringComparison.Ordinal))) {
                throw new IdentityFailure(409, "appointmentAlreadyExists");
            }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });

        group.MapPost("", async (CreateAppointmentRequest req, HttpContext context, IdentityAuthorization auth,
            TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireReception(context, auth, true, ct);
            var time = Utc(req.ScheduledAt);
            if (req.RegistrationId <= 0 || req.RegistrationItemId <= 0 || req.ItemKind is not ("vaccine" or "package") ||
                time <= DateTime.UtcNow || string.IsNullOrWhiteSpace(req.Location) || req.Location.Trim().Length > 200)
                throw new IdentityFailure(400, "validation");
            // Serialize appointments for the same source; the unique indexes also protect direct writes.
            var registration = await db.Registrations.FromSqlInterpolated(
                $"SELECT * FROM DangKyDichVu WITH (UPDLOCK,HOLDLOCK) WHERE MaDangKy={req.RegistrationId}")
                .SingleOrDefaultAsync(ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            if (registration.TrangThai != "DaDangKy" || !await db.Recipients.AnyAsync(x =>
                x.MaNguoiDuocTiem == registration.MaNguoiDuocTiem && x.MaKhachHang == registration.MaKhachHang, ct))
                throw new IdentityFailure(409, "appointmentSourceConflict");
            var lineExists = req.ItemKind == "vaccine"
                ? await db.RegistrationVaccineItems.AnyAsync(x => x.MaDangKy == req.RegistrationId && x.MaChiTietVacXin == req.RegistrationItemId, ct)
                : await db.RegistrationPackageItems.AnyAsync(x => x.MaDangKy == req.RegistrationId && x.MaChiTietGoi == req.RegistrationItemId, ct);
            if (!lineExists) throw new IdentityFailure(400, "validation");
            if (await db.Set<LichHenTiem>().AnyAsync(x => x.MaDangKy == req.RegistrationId && x.ThoiGianHen == time &&
                (req.ItemKind == "vaccine" ? x.MaChiTietVacXin == req.RegistrationItemId : x.MaChiTietGoi == req.RegistrationItemId), ct))
                throw new IdentityFailure(409, "appointmentAlreadyExists");
            var row = new LichHenTiem {
                MaDangKy = registration.MaDangKy, MaNguoiDuocTiem = registration.MaNguoiDuocTiem,
                MaChiTietVacXin = req.ItemKind == "vaccine" ? req.RegistrationItemId : null,
                MaChiTietGoi = req.ItemKind == "package" ? req.RegistrationItemId : null,
                ThoiGianHen = time, DiemTiem = req.Location.Trim(), TrangThai = "DaXepLich"
            };
            db.Add(row); await db.SaveChangesAsync(ct);
            var audit = IdentityTransactions.Audit(db, "appointments.created", context.TraceIdentifier,
                actor.Account.MaTaiKhoan, session: actor.Session.Id);
            audit.ResourceType = "Appointment"; audit.ResourceId = row.MaLichHen.ToString(CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);
            var dto = await Query(db, db.Set<LichHenTiem>().Where(x => x.MaLichHen == row.MaLichHen)).SingleAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/appointments/{row.MaLichHen}", dto);
        }).WithName("CreateAppointment").Produces<AppointmentDto>(201);

        group.MapGet("", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            int? registrationId, int? recipientId, string? itemKind, int? registrationItemId, string? scheduledAt,
            int? page, int? pageSize, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireReader(context, auth, ct);
            if (registrationId is <= 0 || recipientId is <= 0 || registrationItemId is <= 0 ||
                itemKind is not (null or "vaccine" or "package") ||
                (registrationItemId.HasValue != (itemKind is not null)) || page is <= 0 || pageSize is <= 0 or > 100 ||
                (long)((page ?? 1) - 1) * (pageSize ?? 25) > int.MaxValue) throw new IdentityFailure(400, "validation");
            var time = scheduledAt is null ? (DateTime?)null : Utc(scheduledAt);
            var query = Scope(db, actor).Where(x =>
                (!registrationId.HasValue || x.MaDangKy == registrationId) &&
                (!recipientId.HasValue || x.MaNguoiDuocTiem == recipientId) &&
                (itemKind == null || (itemKind == "vaccine" ? x.MaChiTietVacXin == registrationItemId : x.MaChiTietGoi == registrationItemId)) &&
                (!time.HasValue || x.ThoiGianHen == time));
            var rows = await Query(db, query.OrderByDescending(x => x.MaLichHen).Skip(((page ?? 1) - 1) * (pageSize ?? 25))
                .Take(pageSize ?? 25)).ToArrayAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new AppointmentListResponse(rows));
        }).WithName("ListAppointments").Produces<AppointmentListResponse>();

        group.MapGet("/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireReader(context, auth, ct);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            var row = await Query(db, Scope(db, actor).Where(x => x.MaLichHen == id)).SingleOrDefaultAsync(ct)
                ?? throw new IdentityFailure(actor.IsCustomer ? 403 : 404, actor.IsCustomer ? "permissionDenied" : "resourceNotFound");
            await tx.CommitAsync(ct);
            return Results.Ok(row);
        }).WithName("GetAppointment").Produces<AppointmentDto>();
    }

    internal static IQueryable<AppointmentDto> Query(TiemChungDbContext db, IQueryable<LichHenTiem> rows) =>
        from a in rows.AsNoTracking()
        join r in db.Registrations on a.MaDangKy equals r.MaDangKy
        join c in db.Customers on r.MaKhachHang equals c.MaKhachHang
        join p in db.Recipients on a.MaNguoiDuocTiem equals p.MaNguoiDuocTiem
        join s in db.RegistrationVaccineItems on a.MaChiTietVacXin equals s.MaChiTietVacXin into singles
        from s in singles.DefaultIfEmpty()
        join v in db.Vaccines on s.MaVacXin equals v.MaVacXin into vaccines
        from v in vaccines.DefaultIfEmpty()
        join b in db.RegistrationPackageItems on a.MaChiTietGoi equals b.MaChiTietGoi into bundles
        from b in bundles.DefaultIfEmpty()
        join g in db.Set<GoiVacXin>() on b.MaGoi equals g.MaGoi into packages
        from g in packages.DefaultIfEmpty()
        select new AppointmentDto(a.MaLichHen, r.MaDangKy, new(c.MaKhachHang, c.HoTen), new(p.MaNguoiDuocTiem, p.HoTen),
            a.MaChiTietVacXin != null ? "vaccine" : "package", a.MaChiTietVacXin ?? a.MaChiTietGoi!.Value,
            a.MaChiTietVacXin != null ? s.MaVacXin : b.MaGoi, a.MaChiTietVacXin != null ? v.TenVacXin : g.TenGoi,
            new DateTimeOffset(DateTime.SpecifyKind(a.ThoiGianHen, DateTimeKind.Utc)), a.DiemTiem, a.TrangThai);
}
