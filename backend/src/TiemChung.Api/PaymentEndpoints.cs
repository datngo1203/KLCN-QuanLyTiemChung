using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Domain.Rules;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class PaymentEndpoints
{
    private const decimal MaxMoney = RegistrationEndpoints.MaxMoney;

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

        group.MapGet("/payment/settings", async (HttpContext context, IdentityAuthorization auth, CancellationToken ct) => {
            await RequireCustomer(context, auth, false, ct);
            return Results.Ok(new PaymentSettingsDto("Ngân hàng VAC", "Trung tâm tiêm chủng VAC", "0000000402"));
        }).WithName("GetPaymentSettings").Produces<PaymentSettingsDto>();

        group.MapPost("/registrations/{id:int}/payment/online", async (int id, OnlinePaymentRequest req,
            HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireCustomer(context, auth, true, ct);
            var row = await LockOwnedRegistration(db, actor, id, ct);
            var before = await Detail(db, row, actor, ct);
            if (before.Payment is not null) throw new IdentityFailure(409, "paymentAlreadyRecorded");
            if (before.TotalDue == 0) throw new IdentityFailure(409, "paymentNotRequired");
            var now = DateTimeOffset.UtcNow;
            var payer = OnlinePaymentRules.NormalizePayer(req.Payer);
            if (payer is null || !req.TermsAccepted) throw new IdentityFailure(400, "validation");

            var (provider, holder, identifier, reference) = req switch {
                { Method: "TheQuocTe", InternationalCard: { } card, Domestic: null, Transfer: null }
                    when OnlinePaymentRules.ValidCard(card, now) =>
                    (card.CardBrand!, card.CardholderName!.Trim(), CounterPaymentRules.CardDigits(card.CardNumber!), (string?)null),
                { Method: "TheNoiDia", Domestic: { } atm, InternationalCard: null, Transfer: null }
                    when OnlinePaymentRules.ValidDomestic(atm) =>
                    (atm.BankName!.Trim(), atm.AccountHolderName!.Trim(), atm.CardOrAccountNumber!.Trim(), null),
                { Method: "ChuyenKhoanOnline", Transfer: { } transfer, InternationalCard: null, Domestic: null }
                    when OnlinePaymentRules.ValidTransfer(transfer) =>
                    (transfer.SourceBankOrWallet!.Trim(), transfer.AccountHolderName!.Trim(),
                        transfer.SourceAccountOrWalletId!.Trim(), transfer.TransferReference!.Trim()),
                _ => throw new IdentityFailure(400, "validation")
            };

            row.KenhThanhToanDuKien = "TrucTuyen";
            var payment = new ThanhToan {
                MaDangKy = id, SoTien = before.TotalDue, LoaiThanhToan = "ToanBo",
                PhuongThuc = req.Method!, TrangThai = req.Method == "ChuyenKhoanOnline" ? "ChoXacNhan" : "ThanhCong",
                ThoiDiem = now.UtcDateTime, MaGiaoDich = $"TT-{Guid.NewGuid():N}", DongYDieuKhoanLuc = now,
                TenNguoiThanhToan = payer.FullName, SoDienThoaiThanhToan = payer.PhoneNumber, EmailThanhToan = payer.Email,
                TinhThanhThanhToan = payer.ProvinceCity, PhuongXaThanhToan = payer.WardCommune, DiaChiThanhToan = payer.AddressLine,
                NhaCungCapThanhToan = provider, TenChuTaiKhoanThe = holder,
                SoDinhDanhMasked = CounterPaymentRules.Mask(identifier), ThamChieuThanhToan = reference
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);
            Audit(db, context, actor, payment, "payments.recorded");
            await db.SaveChangesAsync(ct);
            var dto = await Detail(db, row, actor, ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/registrations/{id}/payment", dto);
        }).WithName("RecordOnlinePayment").Produces<RegistrationPaymentDto>(201);

        group.MapPut("/registrations/{id:int}/payment/channel", async (int id, PaymentChannelRequest req,
            HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireCustomer(context, auth, true, ct);
            if (req.Channel != "TaiTrungTam") throw new IdentityFailure(400, "validation");
            var row = await LockOwnedRegistration(db, actor, id, ct);
            var detail = await Detail(db, row, actor, ct);
            if (detail.Payment is not null) throw new IdentityFailure(409, "paymentAlreadyRecorded");
            if (row.KenhThanhToanDuKien != req.Channel) {
                row.KenhThanhToanDuKien = req.Channel;
                var audit = IdentityTransactions.Audit(db, "payments.channelSelected", context.TraceIdentifier,
                    actor.Account.MaTaiKhoan, session: actor.Session.Id);
                audit.ResourceType = "Registration";
                audit.ResourceId = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct);
            return Results.Ok(detail with { ExpectedChannel = req.Channel });
        }).WithName("SelectPaymentChannel").Produces<RegistrationPaymentDto>();

        group.MapPost("/registrations/{id:int}/payment", async (int id, RecordPaymentRequest req,
            HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await auth.RequireAsync(context, true, ct);
            RequireCashier(actor, true);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            var row = await LockRegistration(db, id, ct);
            var before = await Detail(db, row, actor, ct);
            if (before.Payment is not null) throw new IdentityFailure(409, "paymentAlreadyRecorded");
            if (before.TotalDue == 0) throw new IdentityFailure(409, "paymentNotRequired");
            var now = DateTimeOffset.UtcNow;
            if (CounterPaymentRules.Validate(req, before.TotalDue, now) is not null)
                throw new IdentityFailure(400, "validation");
            var payer = CounterPaymentRules.NormalizePayer(req.Payer)!;
            var card = req.Card;
            var transfer = req.Transfer;
            var payment = new ThanhToan {
                MaDangKy = id, MaNhanVien = actor.Employee!.MaNhanVien, SoTien = before.TotalDue,
                LoaiThanhToan = "ToanBo", PhuongThuc = req.Method!, TrangThai = "ThanhCong", ThoiDiem = now.UtcDateTime,
                MaGiaoDich = $"TT-{Guid.NewGuid():N}",
                // SqlClient infers scale for decimal(18,0), so strip trailing fractional zeroes after validation.
                SoTienKhachDua = req.CashTendered is { } cash ? decimal.Truncate(cash) : null,
                TenNguoiThanhToan = payer.FullName, SoDienThoaiThanhToan = payer.PhoneNumber, EmailThanhToan = payer.Email,
                TinhThanhThanhToan = payer.ProvinceCity, PhuongXaThanhToan = payer.WardCommune, DiaChiThanhToan = payer.AddressLine,
                NhaCungCapThanhToan = card?.CardBrand ?? transfer?.SourceBankOrWallet?.Trim(),
                TenChuTaiKhoanThe = card?.CardholderName?.Trim() ?? transfer?.AccountHolderName?.Trim(),
                ThamChieuThanhToan = card?.AuthorizationReference?.Trim() ?? transfer?.TransferReference?.Trim(),
                SoDinhDanhMasked = card is not null ? CounterPaymentRules.Mask(CounterPaymentRules.CardDigits(card.CardNumber!))
                    : transfer is not null ? CounterPaymentRules.Mask(transfer.SourceAccountOrWalletId!.Trim()) : null
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);
            Audit(db, context, actor, payment, "payments.recorded");
            await db.SaveChangesAsync(ct);
            var dto = await Detail(db, row, actor, ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/registrations/{id}/payment", dto);
        }).WithName("RecordRegistrationPayment").Produces<RegistrationPaymentDto>(201);

        group.MapPost("/registrations/{id:int}/payment/confirm", async (int id, HttpContext context,
            IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await auth.RequireAsync(context, true, ct);
            RequireCashier(actor, true);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            var row = await LockRegistration(db, id, ct);
            var before = await Detail(db, row, actor, ct);
            if (!before.CanConfirmPendingPayment) throw new IdentityFailure(409, "paymentNotPending");

            var payment = await db.Payments.SingleAsync(p => p.MaDangKy == id, ct);
            payment.TrangThai = "ThanhCong";
            payment.MaNhanVien = actor.Employee!.MaNhanVien;
            payment.ThoiDiem = DateTime.UtcNow;
            Audit(db, context, actor, payment, "payments.confirmed");
            await db.SaveChangesAsync(ct);
            var dto = await Detail(db, row, actor, ct);
            await tx.CommitAsync(ct);
            return Results.Ok(dto);
        }).WithName("ConfirmRegistrationPayment").Produces<RegistrationPaymentDto>();
    }

    private static async Task<DangKyDichVu> LockRegistration(TiemChungDbContext db, int id, CancellationToken ct,
        bool customer = false) =>
        // All collectors lock the same registration after Identity account/session locks.
        await db.Registrations.FromSqlInterpolated(
            $"SELECT * FROM DangKyDichVu WITH (UPDLOCK,HOLDLOCK) WHERE MaDangKy={id}")
            .SingleOrDefaultAsync(ct) ?? throw (customer
                ? new IdentityFailure(403, "permissionDenied") : new IdentityFailure(404, "resourceNotFound"));

    private static async Task<DangKyDichVu> LockOwnedRegistration(TiemChungDbContext db, IdentityActor actor, int id,
        CancellationToken ct)
    {
        if (id <= 0) throw new IdentityFailure(400, "validation");
        var row = await LockRegistration(db, id, ct, customer: true);
        if (!await RegistrationEndpoints.Scope(db, actor).AnyAsync(r => r.MaDangKy == id, ct))
            throw new IdentityFailure(403, "permissionDenied");
        return row;
    }

    internal static async Task<IdentityActor> RequireCustomer(HttpContext context, IdentityAuthorization auth, bool write,
        CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, write, ct);
        if (!actor.IsCustomer) throw new IdentityFailure(403, "permissionDenied");
        return actor;
    }

    private static void Audit(TiemChungDbContext db, HttpContext context, IdentityActor actor, ThanhToan payment, string action)
    {
        var audit = IdentityTransactions.Audit(db, action, context.TraceIdentifier,
            actor.Account.MaTaiKhoan, session: actor.Session.Id);
        audit.ResourceType = "Payment";
        audit.ResourceId = payment.MaThanhToan.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void RequireCashier(IdentityActor actor, bool write)
    {
        if (actor.IsCustomer || actor.Employee?.ChucDanh != "ThuNgan" || !actor.Permissions.Contains("XemThanhToan") ||
            write && !actor.Permissions.Contains("GhiThanhToan")) throw new IdentityFailure(403, "permissionDenied");
    }

    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    internal static async Task<PaymentRegistrationSummaryDto[]> Summaries(TiemChungDbContext db, DangKyDichVu[] rows, CancellationToken ct, ThanhToan[]? paymentSnapshot = null)
    {
        var ids = rows.Select(r => r.MaDangKy).ToArray();
        var customerIds = rows.Select(r => r.MaKhachHang).ToArray();
        var recipientIds = rows.Select(r => r.MaNguoiDuocTiem).ToArray();
        var customers = await db.Customers.AsNoTracking().Where(c => customerIds.Contains(c.MaKhachHang)).ToDictionaryAsync(c => c.MaKhachHang, ct);
        var recipients = await db.Recipients.AsNoTracking().Where(p => recipientIds.Contains(p.MaNguoiDuocTiem)).ToDictionaryAsync(p => p.MaNguoiDuocTiem, ct);
        var singles = await db.RegistrationVaccineItems.AsNoTracking().Where(i => ids.Contains(i.MaDangKy)).ToArrayAsync(ct);
        var packages = await db.RegistrationPackageItems.AsNoTracking().Where(i => ids.Contains(i.MaDangKy)).ToArrayAsync(ct);
        var payments = paymentSnapshot ?? await db.Payments.AsNoTracking().Where(p => ids.Contains(p.MaDangKy)).ToArrayAsync(ct);
        return rows.Select(row => {
            if (row.TrangThai != "DaDangKy" || row.MaKhuyenMai is not null ||
                row.KenhThanhToanDuKien is not (null or "TaiTrungTam" or "TrucTuyen") ||
                !customers.TryGetValue(row.MaKhachHang, out var customer) ||
                !recipients.TryGetValue(row.MaNguoiDuocTiem, out var recipient) || recipient.MaKhachHang != row.MaKhachHang)
                throw new IdentityFailure(409, "paymentDataConflict");
            var lines = singles.Where(i => i.MaDangKy == row.MaDangKy).Select(i => new RegistrationLine(i.SoLuong, i.GiaTaiThoiDiemDangKy, false))
                .Concat(packages.Where(i => i.MaDangKy == row.MaDangKy).Select(i => new RegistrationLine(i.SoLuongGoi, i.GiaTaiThoiDiemDangKy, true))).ToArray();
            if (lines.Length is < 1 or > 100 || lines.Any(i => i.Quantity is < 1 or > 100 ||
                i.UnitPrice < 0 || i.UnitPrice > MaxMoney || decimal.Truncate(i.UnitPrice) != i.UnitPrice))
                throw new IdentityFailure(409, "paymentDataConflict");
            var subtotal = RegistrationRules.CalculateTotal(lines);
            var fee = row.PhiKhamTuVan ?? 0;
            var total = subtotal + fee;
            if (fee < 0 || decimal.Truncate(fee) != fee || total > MaxMoney) throw new IdentityFailure(409, "paymentDataConflict");
            var existing = payments.Where(p => p.MaDangKy == row.MaDangKy).ToArray();
            if (existing.Length > 1 || existing.Any(p => !ValidPayment(p, total)))
                throw new IdentityFailure(409, "paymentDataConflict");
            var successful = existing.Where(p => p.TrangThai == "ThanhCong").Select(p => p.SoTien).ToArray();
            var payment = existing.SingleOrDefault();
            return new PaymentRegistrationSummaryDto(row.MaDangKy, Utc(row.NgayDangKy), new(customer.MaKhachHang, customer.HoTen),
                new(recipient.MaNguoiDuocTiem, recipient.HoTen), subtotal, total, successful.Sum(),
                RegistrationRules.RemainingAmount(total, successful), payment?.MaThanhToan, "VND", row.PhiKhamTuVan,
                payment?.TrangThai ?? "ChuaThanhToan", row.KenhThanhToanDuKien);
        }).ToArray();
    }

    private static PaymentPayer Payer(ThanhToan payment) => new(payment.TenNguoiThanhToan, payment.SoDienThoaiThanhToan,
        payment.EmailThanhToan, payment.TinhThanhThanhToan, payment.PhuongXaThanhToan, payment.DiaChiThanhToan);

    private static bool ValidPayment(ThanhToan payment, decimal total)
    {
        if (payment.LoaiThanhToan != "ToanBo" || payment.SoTien <= 0 || payment.SoTien != total) return false;
        var success = payment.TrangThai == "ThanhCong" && payment.MaNhanVien is > 0;
        if (payment.PhuongThuc == "MoPhong") return success;
        if (!CounterPaymentRules.Text(payment.MaGiaoDich, 50) || CounterPaymentRules.NormalizePayer(Payer(payment)) is null) return false;
        if (payment.PhuongThuc == "TienMat")
            return success && CounterPaymentRules.IsCash(payment.SoTienKhachDua, total) &&
                payment.NhaCungCapThanhToan is null && payment.TenChuTaiKhoanThe is null &&
                payment.SoDinhDanhMasked is null && payment.ThamChieuThanhToan is null;

        if (payment.SoTienKhachDua is not null || !CounterPaymentRules.Text(payment.NhaCungCapThanhToan, 100) ||
            !CounterPaymentRules.Text(payment.TenChuTaiKhoanThe, 150) ||
            payment.SoDinhDanhMasked is not { Length: >= 4 and <= 8 } masked || !masked.StartsWith("****", StringComparison.Ordinal))
            return false;

        var online = payment.DongYDieuKhoanLuc is not null && OnlinePaymentRules.NormalizePayer(Payer(payment)) is not null;
        var onlineSuccess = online && payment.TrangThai == "ThanhCong" && payment.MaNhanVien is null &&
            payment.ThamChieuThanhToan is null && masked.Length == 8 && masked[4..].All(char.IsAsciiDigit);
        var reference = CounterPaymentRules.Text(payment.ThamChieuThanhToan, 100);
        return payment.PhuongThuc switch {
            "TheTaiQuay" => success && reference && CounterPaymentRules.IsCardBrand(payment.NhaCungCapThanhToan) &&
                masked.Length == 8 && masked[4..].All(char.IsAsciiDigit),
            "ChuyenKhoanQR" => success && reference && masked[4..].All(CounterPaymentRules.IsSourceCharacter),
            "TheQuocTe" => onlineSuccess && OnlinePaymentRules.IsInternationalBrand(payment.NhaCungCapThanhToan),
            "TheNoiDia" => onlineSuccess,
            "ChuyenKhoanOnline" => online && reference && (success || payment.TrangThai == "ChoXacNhan" && payment.MaNhanVien is null) &&
                masked[4..].All(CounterPaymentRules.IsSourceCharacter),
            _ => false
        };
    }

    internal static async Task<RegistrationPaymentDto> Detail(TiemChungDbContext db, DangKyDichVu row, IdentityActor actor, CancellationToken ct)
    {
        var payments = await db.Payments.AsNoTracking().Where(p => p.MaDangKy == row.MaDangKy).ToArrayAsync(ct);
        var summary = (await Summaries(db, [row], ct, payments)).Single();
        var registration = await RegistrationEndpoints.Detail(db, row, ct);
        var payment = payments.SingleOrDefault();
        var requiresPayment = summary.RemainingAmount > 0;
        var canWrite = !actor.IsCustomer && actor.Employee?.ChucDanh == "ThuNgan" &&
            actor.Permissions.Contains("XemThanhToan") && actor.Permissions.Contains("GhiThanhToan");
        var pending = summary.PaymentState == "ChoXacNhan";
        return new(row.MaDangKy, row.TrangThai, summary.Customer, summary.Recipient, registration.Items,
            summary.Subtotal, summary.TotalDue, summary.PaidAmount, summary.RemainingAmount, "VND", requiresPayment,
            canWrite && requiresPayment && payment is null, true,
            pending ? "Đang chờ Thu ngân xác nhận chuyển khoản mô phỏng."
                : payment is not null ? "Đã ghi nhận thanh toán mô phỏng toàn bộ; không xử lý tiền thật."
                : summary.TotalDue == 0 ? "Không phát sinh khoản thu." : "Chưa ghi nhận thanh toán mô phỏng; không xử lý tiền thật.",
            payment is null ? null : new(payment.MaThanhToan, payment.MaDangKy, payment.SoTien, payment.MaNhanVien,
                Utc(payment.ThoiDiem), payment.LoaiThanhToan, payment.PhuongThuc, payment.TrangThai, true,
                payment.MaGiaoDich, payment.PhuongThuc == "MoPhong" ? null : Payer(payment), payment.NhaCungCapThanhToan,
                payment.TenChuTaiKhoanThe, payment.SoDinhDanhMasked, payment.ThamChieuThanhToan,
                payment.SoTienKhachDua, payment.SoTienKhachDua - payment.SoTien), summary.ConsultationFee,
            summary.PaymentState, summary.ExpectedChannel, canWrite && pending, registration.Contact);
    }
}
