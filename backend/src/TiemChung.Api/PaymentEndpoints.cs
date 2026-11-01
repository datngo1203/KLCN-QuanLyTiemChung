using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Domain.Rules;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class PaymentEndpoints
{
    private const decimal MaxMoney = 9999999999999999m;

    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Payment");
        group.AddEndpointFilter(async (invocation, next) => {
            invocation.HttpContext.Response.Headers.CacheControl = "no-store";
            try { return await next(invocation); }
            catch (DbUpdateException error) when (error.InnerException is SqlException sql &&
                sql.Number is 2601 or 2627 && sql.Message.Contains("IX_ThanhToan_MaDangKy", StringComparison.Ordinal)) {
                throw new IdentityFailure(409, "paymentAlreadyRecorded");
            }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });

        group.MapGet("/payment-registrations", async (HttpContext context, IdentityAuthorization auth,
            TiemChungDbContext db, string? search, int? customerId, int? recipientId, int? page, int? pageSize, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            RequireCashier(await auth.RequireAsync(context, false, ct), false);
            search = search?.Trim() ?? "";
            if (search.Length > 100 || customerId is <= 0 || recipientId is <= 0 || page is <= 0 ||
                pageSize is <= 0 or > 100 || (long)((page ?? 1) - 1) * (pageSize ?? 25) > int.MaxValue)
                throw new IdentityFailure(400, "validation");
            var code = search.StartsWith("DK-", StringComparison.OrdinalIgnoreCase) ? search[3..] : search;
            _ = int.TryParse(code, out var searchId);
            var rows = await db.Registrations.AsNoTracking().Where(r =>
                (!customerId.HasValue || r.MaKhachHang == customerId) &&
                (!recipientId.HasValue || r.MaNguoiDuocTiem == recipientId) &&
                (search == "" || r.MaDangKy == searchId ||
                    db.Customers.Any(c => c.MaKhachHang == r.MaKhachHang && c.HoTen.Contains(search)) ||
                    db.Recipients.Any(p => p.MaNguoiDuocTiem == r.MaNguoiDuocTiem && p.HoTen.Contains(search))))
                .OrderByDescending(r => r.MaDangKy).Skip(((page ?? 1) - 1) * (pageSize ?? 25))
                .Take(pageSize ?? 25).ToArrayAsync(ct);
            var items = await Summaries(db, rows, ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new PaymentRegistrationListResponse(items));
        }).WithName("ListPaymentRegistrations").Produces<PaymentRegistrationListResponse>();

        group.MapGet("/registrations/{id:int}/payment", async (int id, HttpContext context, IdentityAuthorization auth,
            TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await auth.RequireAsync(context, false, ct);
            if (!actor.IsCustomer) RequireCashier(actor, false);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            var row = await RegistrationEndpoints.Scope(db, actor).SingleOrDefaultAsync(r => r.MaDangKy == id, ct);
            if (row is null) throw new IdentityFailure(actor.IsCustomer ? 403 : 404,
                actor.IsCustomer ? "permissionDenied" : "resourceNotFound");
            var dto = await Detail(db, row, actor, ct);
            await tx.CommitAsync(ct);
            return Results.Ok(dto);
        }).WithName("GetRegistrationPayment").Produces<RegistrationPaymentDto>();

        group.MapPost("/registrations/{id:int}/payment", async (int id, RecordPaymentRequest req,
            HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await auth.RequireAsync(context, true, ct);
            RequireCashier(actor, true);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            // All collectors lock the same registration after Identity account/session locks.
            var row = await db.Registrations.FromSqlInterpolated(
                $"SELECT * FROM DangKyDichVu WITH (UPDLOCK,HOLDLOCK) WHERE MaDangKy={id}")
                .SingleOrDefaultAsync(ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            var before = await Detail(db, row, actor, ct);
            if (before.Payment is not null) throw new IdentityFailure(409, "paymentAlreadyRecorded");
            if (before.TotalDue == 0) throw new IdentityFailure(409, "paymentNotRequired");
            var payment = new ThanhToan {
                MaDangKy = id, MaNhanVien = actor.Employee!.MaNhanVien, SoTien = before.TotalDue,
                LoaiThanhToan = "ToanBo", PhuongThuc = "MoPhong", TrangThai = "ThanhCong", ThoiDiem = DateTime.UtcNow
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);
            var audit = IdentityTransactions.Audit(db, "payments.recorded", context.TraceIdentifier,
                actor.Account.MaTaiKhoan, session: actor.Session.Id);
            audit.ResourceType = "Payment";
            audit.ResourceId = payment.MaThanhToan.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);
            var dto = await Detail(db, row, actor, ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/registrations/{id}/payment", dto);
        }).WithName("RecordRegistrationPayment").Produces<RegistrationPaymentDto>(201);
    }

    private static void RequireCashier(IdentityActor actor, bool write)
    {
        if (actor.IsCustomer || actor.Employee?.ChucDanh != "ThuNgan" || !actor.Permissions.Contains("XemThanhToan") ||
            write && !actor.Permissions.Contains("GhiThanhToan")) throw new IdentityFailure(403, "permissionDenied");
    }

    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    internal static async Task<PaymentRegistrationSummaryDto[]> Summaries(TiemChungDbContext db, DangKyDichVu[] rows, CancellationToken ct)
    {
        var ids = rows.Select(r => r.MaDangKy).ToArray();
        var customerIds = rows.Select(r => r.MaKhachHang).ToArray();
        var recipientIds = rows.Select(r => r.MaNguoiDuocTiem).ToArray();
        var customers = await db.Customers.AsNoTracking().Where(c => customerIds.Contains(c.MaKhachHang)).ToDictionaryAsync(c => c.MaKhachHang, ct);
        var recipients = await db.Recipients.AsNoTracking().Where(p => recipientIds.Contains(p.MaNguoiDuocTiem)).ToDictionaryAsync(p => p.MaNguoiDuocTiem, ct);
        var singles = await db.RegistrationVaccineItems.AsNoTracking().Where(i => ids.Contains(i.MaDangKy)).ToArrayAsync(ct);
        var packages = await db.RegistrationPackageItems.AsNoTracking().Where(i => ids.Contains(i.MaDangKy)).ToArrayAsync(ct);
        var payments = await db.Payments.AsNoTracking().Where(p => ids.Contains(p.MaDangKy)).ToArrayAsync(ct);
        return rows.Select(row => {
            if (row.TrangThai != "DaDangKy" || row.MaKhuyenMai is not null ||
                !customers.TryGetValue(row.MaKhachHang, out var customer) ||
                !recipients.TryGetValue(row.MaNguoiDuocTiem, out var recipient) || recipient.MaKhachHang != row.MaKhachHang)
                throw new IdentityFailure(409, "paymentDataConflict");
            var lines = singles.Where(i => i.MaDangKy == row.MaDangKy).Select(i => new RegistrationLine(i.SoLuong, i.GiaTaiThoiDiemDangKy, false))
                .Concat(packages.Where(i => i.MaDangKy == row.MaDangKy).Select(i => new RegistrationLine(i.SoLuongGoi, i.GiaTaiThoiDiemDangKy, true))).ToArray();
            if (lines.Length is < 1 or > 100 || lines.Any(i => i.Quantity is < 1 or > 100 ||
                i.UnitPrice < 0 || i.UnitPrice > MaxMoney || decimal.Truncate(i.UnitPrice) != i.UnitPrice))
                throw new IdentityFailure(409, "paymentDataConflict");
            var total = RegistrationRules.CalculateTotal(lines);
            if (total > MaxMoney) throw new IdentityFailure(409, "paymentDataConflict");
            var existing = payments.Where(p => p.MaDangKy == row.MaDangKy).ToArray();
            if (existing.Length > 1 || existing.Any(p => p.LoaiThanhToan != "ToanBo" || p.PhuongThuc != "MoPhong" ||
                p.TrangThai != "ThanhCong" || p.SoTien <= 0 || p.SoTien != total))
                throw new IdentityFailure(409, "paymentDataConflict");
            var paid = existing.Sum(p => p.SoTien);
            return new PaymentRegistrationSummaryDto(row.MaDangKy, Utc(row.NgayDangKy), new(customer.MaKhachHang, customer.HoTen),
                new(recipient.MaNguoiDuocTiem, recipient.HoTen), total, total, paid,
                RegistrationRules.RemainingAmount(total, existing.Select(p => p.SoTien)), existing.SingleOrDefault()?.MaThanhToan, "VND");
        }).ToArray();
    }

    private static async Task<RegistrationPaymentDto> Detail(TiemChungDbContext db, DangKyDichVu row, IdentityActor actor, CancellationToken ct)
    {
        var summary = (await Summaries(db, [row], ct)).Single();
        var registration = await RegistrationEndpoints.Detail(db, row, ct);
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.MaDangKy == row.MaDangKy, ct);
        var requiresPayment = summary.RemainingAmount > 0;
        var canRecord = requiresPayment && !actor.IsCustomer && actor.Employee?.ChucDanh == "ThuNgan" &&
            actor.Permissions.Contains("XemThanhToan") && actor.Permissions.Contains("GhiThanhToan");
        return new(row.MaDangKy, row.TrangThai, summary.Customer, summary.Recipient, registration.Items,
            summary.Subtotal, summary.TotalDue, summary.PaidAmount, summary.RemainingAmount, "VND", requiresPayment, canRecord, true,
            payment is not null ? "Đã ghi nhận thanh toán mô phỏng toàn bộ; không xử lý tiền thật."
                : summary.TotalDue == 0 ? "Không phát sinh khoản thu." : "Chưa ghi nhận thanh toán mô phỏng; không xử lý tiền thật.",
            payment is null ? null : new(payment.MaThanhToan, payment.MaDangKy, payment.SoTien, payment.MaNhanVien,
                Utc(payment.ThoiDiem), payment.LoaiThanhToan, payment.PhuongThuc, payment.TrangThai, true));
    }
}
