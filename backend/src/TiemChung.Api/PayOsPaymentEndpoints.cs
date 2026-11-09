using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayOS.Exceptions;
using TiemChung.Api.Payments;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class PayOsPaymentEndpoints
{
    private sealed record Reservation(int PaymentId, long OrderCode, decimal Amount, string ReturnUrl,
        string CancelUrl, IdentityActor Actor, PayOsPaymentStatusDto Status);

    private sealed record ReconcileSnapshot(int PaymentId, long OrderCode, decimal Amount,
        string? PaymentLinkId, string Lifecycle);

    public static void MapPayOsPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Payment");
        group.AddEndpointFilter(async (invocation, next) => {
            invocation.HttpContext.Response.Headers.CacheControl = "no-store";
            try { return await next(invocation); }
            catch (DbUpdateException error) when (error.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
                sql.Number is 2601 or 2627 && sql.Message.Contains("IX_ThanhToan_MaDangKy", StringComparison.Ordinal)) {
                throw new IdentityFailure(409, "paymentAlreadyRecorded");
            }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });

        group.MapPost("/registrations/{id:int}/payment/payos", async (int id, PayOsPaymentRequest request,
            HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            IPayOsPaymentProvider provider, IOptions<PayOsPaymentOptions> options, ILogger<IdentityAuthorization> logger,
            CancellationToken ct) => {
            if (id <= 0) throw new IdentityFailure(400, "validation");
            var payer = OnlinePaymentRules.NormalizePayer(request.Payer);
            if (payer is null || !request.TermsAccepted) throw new IdentityFailure(400, "validation");
            var reserved = await ReserveAsync(id, payer, context, auth, db, provider, options.Value, ct);
            if (reserved.PaymentId == 0) return Results.Ok(reserved.Status);

            PayOsCreatePaymentResult created;
            try {
                created = await provider.CreateAsync(new(reserved.OrderCode, checked((long)reserved.Amount),
                    SafeDescription(reserved.OrderCode), reserved.ReturnUrl, reserved.CancelUrl), ct);
            } catch (ApiException error) when (IsDefinitiveRejection(error)) {
                await AbandonRejectedReservationAsync(reserved, context, db, CancellationToken.None);
                throw new IdentityFailure(502, "paymentProviderRejected");
            } catch (Exception error) when (IsProviderFailure(error)) {
                logger.LogWarning("payOS create outcome unknown. TraceId={TraceId}; ExceptionType={ExceptionType}",
                    context.TraceIdentifier, error.GetType().Name);
                return Results.Ok(await MarkOutcomeUnknownAsync(reserved, context, db, CancellationToken.None));
            }

            if (!ValidCreateResult(created, reserved)) {
                logger.LogWarning("payOS create response did not match the reserved intent. TraceId={TraceId}",
                    context.TraceIdentifier);
                return Results.Ok(await MarkOutcomeUnknownAsync(reserved, context, db, CancellationToken.None));
            }

            try {
                return Results.Ok(await PersistCreatedAsync(reserved, created, context, db, ct));
            } catch (Exception error) when (error is DbUpdateException or Microsoft.Data.SqlClient.SqlException or
                TimeoutException or OperationCanceledException) {
                logger.LogWarning("payOS create response persistence outcome unknown. TraceId={TraceId}; ExceptionType={ExceptionType}",
                    context.TraceIdentifier, error.GetType().Name);
                return Results.Ok(await MarkOutcomeUnknownAsync(reserved, context, db, CancellationToken.None));
            }
        }).WithName("CreateOrResumePayOsPayment").Produces<PayOsPaymentStatusDto>();

        group.MapGet("/registrations/{id:int}/payment/payos", async (int id, HttpContext context,
            IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await PaymentEndpoints.RequireCustomer(context, auth, false, ct);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            var row = await RegistrationEndpoints.Scope(db, actor).SingleOrDefaultAsync(r => r.MaDangKy == id, ct);
            if (row is null) throw new IdentityFailure(403, "permissionDenied");
            var detail = await PaymentEndpoints.Detail(db, row, actor, ct);
            if (detail.Payment is null) throw new IdentityFailure(404, "paymentIntentNotFound");
            if (detail.Payment.Method != "PayOsQr") throw new IdentityFailure(409, "paymentMethodMismatch");
            var payment = await db.Payments.AsNoTracking().SingleAsync(p => p.MaDangKy == id, ct);
            var result = Status(payment, detail.TotalDue);
            await tx.CommitAsync(ct);
            return Results.Ok(result);
        }).WithName("GetPayOsPaymentStatus").Produces<PayOsPaymentStatusDto>();

        group.MapPost("/registrations/{id:int}/payment/payos/reconcile", async (int id, HttpContext context,
            IdentityAuthorization auth, TiemChungDbContext db, IPayOsPaymentProvider provider,
            ILogger<IdentityAuthorization> logger, CancellationToken ct) => {
            var (actor, snapshot, current) = await CaptureAsync(id, context, auth, db, ct);
            if (current is not null) return Results.Ok(current);
            if (!provider.IsReady) throw new IdentityFailure(503, "paymentProviderUnavailable");

            PayOsPaymentSnapshot? remote;
            try {
                remote = await provider.ReadAsync(snapshot!.OrderCode, ct);
            } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                throw;
            } catch (Exception error) when (IsProviderFailure(error)) {
                logger.LogWarning("payOS reconcile read failed. TraceId={TraceId}; ExceptionType={ExceptionType}",
                    context.TraceIdentifier, error.GetType().Name);
                throw new IdentityFailure(503, "paymentProviderUnavailable");
            }

            if (remote is null) {
                await AbandonNotFoundAsync(id, actor, snapshot!, context, db, ct);
                throw new IdentityFailure(409, "paymentIntentNotFound");
            }
            if (remote.OrderCode != snapshot!.OrderCode || remote.Amount != snapshot.Amount ||
                !CounterPaymentRules.Text(remote.PaymentLinkId, 50) ||
                snapshot.PaymentLinkId is not null && snapshot.PaymentLinkId != remote.PaymentLinkId)
                throw new IdentityFailure(409, "paymentProviderConflict");

            db.ChangeTracker.Clear();
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var row = await PaymentEndpoints.LockOwnedRegistration(db, actor, id, ct);
            var payment = await db.Payments.SingleOrDefaultAsync(p => p.MaDangKy == id, ct);
            EnsureSameIntent(payment, snapshot);
            if (payment!.PayOsLifecycleState == "Paid") {
                var paid = Status(payment, payment.SoTien);
                await tx.CommitAsync(ct);
                return Results.Ok(paid);
            }

            var status = remote.Status.ToUpperInvariant();
            if (status is "PENDING" or "PROCESSING") {
                var changed = payment.PayOsLifecycleState == "CreateOutcomeUnknown" && payment.PayOsPaymentLinkId is null;
                if (changed) {
                    payment.PayOsPaymentLinkId = remote.PaymentLinkId;
                    PaymentEndpoints.Audit(db, context, actor, payment, "payments.payosReconciledPending");
                    await db.SaveChangesAsync(ct);
                }
                await tx.CommitAsync(ct);
                return Results.Ok(Status(payment, payment.SoTien));
            }

            if (status != "PAID" || !CounterPaymentRules.Text(remote.Reference, 100))
                throw new IdentityFailure(409, "paymentProviderStateUnsupported");

            payment.PayOsPaymentLinkId = remote.PaymentLinkId;
            payment.PayOsReference = remote.Reference;
            payment.PayOsLifecycleState = "Paid";
            payment.TrangThai = "ThanhCong";
            payment.MaNhanVien = null;
            payment.ThoiDiem = DateTime.UtcNow;
            PaymentEndpoints.Audit(db, context, actor, payment, "payments.payosReconciledPaid");
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(Status(payment, payment.SoTien));
        }).WithName("ReconcilePayOsPayment").Produces<PayOsPaymentStatusDto>();
    }

    private static async Task<Reservation> ReserveAsync(int id, PaymentPayer payer, HttpContext context,
        IdentityAuthorization auth, TiemChungDbContext db, IPayOsPaymentProvider provider,
        PayOsPaymentOptions options, CancellationToken ct)
    {
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var actor = await PaymentEndpoints.RequireCustomer(context, auth, true, ct);
        var row = await PaymentEndpoints.LockOwnedRegistration(db, actor, id, ct);
        var detail = await PaymentEndpoints.Detail(db, row, actor, ct);
        if (detail.Payment is { } existing) {
            if (existing.Method != "PayOsQr") throw new IdentityFailure(409, "paymentAlreadyRecorded");
            var existingPayment = await db.Payments.AsNoTracking().SingleAsync(p => p.MaDangKy == id, ct);
            return new(0, 0, 0, "", "", actor, Status(existingPayment, detail.TotalDue));
        }
        if (detail.TotalDue == 0) throw new IdentityFailure(409, "paymentNotRequired");
        if (!provider.IsReady || !options.IsReady) throw new IdentityFailure(503, "paymentProviderUnavailable");

        long orderCode = 0;
        for (var attempt = 0; attempt < 8; attempt++) {
            orderCode = NewOrderCode();
            if (!await db.Payments.AnyAsync(p => p.PayOsOrderCode == orderCode, ct)) break;
            orderCode = 0;
        }
        if (orderCode == 0) throw new IdentityFailure(503, "dependencyUnavailable");

        var now = DateTimeOffset.UtcNow;
        row.KenhThanhToanDuKien = "TrucTuyen";
        var payment = new ThanhToan {
            MaDangKy = id,
            SoTien = detail.TotalDue,
            LoaiThanhToan = "ToanBo",
            PhuongThuc = "PayOsQr",
            TrangThai = "ChoXacNhan",
            ThoiDiem = now.UtcDateTime,
            MaGiaoDich = "PAYOS-" + Guid.NewGuid().ToString("N"),
            TenNguoiThanhToan = payer.FullName,
            SoDienThoaiThanhToan = payer.PhoneNumber,
            EmailThanhToan = payer.Email,
            TinhThanhThanhToan = payer.ProvinceCity,
            PhuongXaThanhToan = payer.WardCommune,
            DiaChiThanhToan = payer.AddressLine,
            DongYDieuKhoanLuc = now,
            PayOsOrderCode = orderCode,
            PayOsLifecycleState = "Creating"
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        PaymentEndpoints.Audit(db, context, actor, payment, "payments.payosReserved");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(payment.MaThanhToan, orderCode, payment.SoTien, options.ReturnUrl!, options.CancelUrl!, actor,
            Status(payment, payment.SoTien));
    }

    private static async Task<PayOsPaymentStatusDto> PersistCreatedAsync(Reservation reserved,
        PayOsCreatePaymentResult created, HttpContext context, TiemChungDbContext db, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        await PaymentEndpoints.LockRegistration(db, await RegistrationIdAsync(db, reserved.PaymentId, ct), ct);
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.MaThanhToan == reserved.PaymentId, ct);
        EnsureReservation(payment, reserved);
        if (payment!.PayOsLifecycleState == "Paid") {
            await tx.CommitAsync(ct);
            return Status(payment, payment.SoTien);
        }
        if (payment.PayOsLifecycleState is not ("Creating" or "CreateOutcomeUnknown"))
            throw new IdentityFailure(409, "paymentDataConflict");
        payment.PayOsPaymentLinkId = created.PaymentLinkId;
        payment.PayOsCheckoutUrl = created.CheckoutUrl;
        payment.PayOsQrCode = created.QrCode;
        payment.PayOsLifecycleState = "Pending";
        PaymentEndpoints.Audit(db, context, reserved.Actor, payment, "payments.payosPending");
        await db.SaveChangesAsync(ct);
        var result = Status(payment, payment.SoTien);
        await tx.CommitAsync(ct);
        return result;
    }

    private static async Task<int> RegistrationIdAsync(TiemChungDbContext db, int paymentId, CancellationToken ct) =>
        await db.Payments.AsNoTracking().Where(p => p.MaThanhToan == paymentId)
            .Select(p => p.MaDangKy).SingleOrDefaultAsync(ct);

    private static async Task<PayOsPaymentStatusDto> MarkOutcomeUnknownAsync(Reservation reserved,
        HttpContext context, TiemChungDbContext db, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var registrationId = await RegistrationIdAsync(db, reserved.PaymentId, ct);
        await PaymentEndpoints.LockRegistration(db, registrationId, ct);
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.MaThanhToan == reserved.PaymentId, ct);
        EnsureReservation(payment, reserved);
        if (payment!.PayOsLifecycleState == "Creating") {
            payment.PayOsLifecycleState = "CreateOutcomeUnknown";
            PaymentEndpoints.Audit(db, context, reserved.Actor, payment, "payments.payosOutcomeUnknown");
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return Status(payment, payment.SoTien);
    }

    private static async Task AbandonRejectedReservationAsync(Reservation reserved, HttpContext context,
        TiemChungDbContext db, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var registrationId = await RegistrationIdAsync(db, reserved.PaymentId, ct);
        await PaymentEndpoints.LockRegistration(db, registrationId, ct);
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.MaThanhToan == reserved.PaymentId, ct);
        EnsureReservation(payment, reserved);
        if (payment!.PayOsLifecycleState == "Creating") {
            PaymentEndpoints.Audit(db, context, reserved.Actor, payment, "payments.payosRejected");
            db.Payments.Remove(payment);
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    private static async Task<(IdentityActor Actor, ReconcileSnapshot Snapshot, PayOsPaymentStatusDto? Current)>
        CaptureAsync(int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct)
    {
        if (id <= 0) throw new IdentityFailure(400, "validation");
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var actor = await PaymentEndpoints.RequireCustomer(context, auth, true, ct);
        var row = await PaymentEndpoints.LockOwnedRegistration(db, actor, id, ct);
        var detail = await PaymentEndpoints.Detail(db, row, actor, ct);
        if (detail.Payment is null) throw new IdentityFailure(404, "paymentIntentNotFound");
        if (detail.Payment.Method != "PayOsQr") throw new IdentityFailure(409, "paymentMethodMismatch");
        var payment = await db.Payments.AsNoTracking().SingleAsync(p => p.MaDangKy == id, ct);
        var snapshot = new ReconcileSnapshot(payment.MaThanhToan, payment.PayOsOrderCode!.Value,
            payment.SoTien, payment.PayOsPaymentLinkId, payment.PayOsLifecycleState!);
        var current = payment.PayOsLifecycleState == "Paid" ? Status(payment, detail.TotalDue) : null;
        await tx.CommitAsync(ct);
        return (actor, snapshot, current);
    }

    private static async Task AbandonNotFoundAsync(int registrationId, IdentityActor actor,
        ReconcileSnapshot snapshot, HttpContext context, TiemChungDbContext db, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var row = await PaymentEndpoints.LockOwnedRegistration(db, actor, registrationId, ct);
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.MaDangKy == registrationId, ct);
        EnsureSameIntent(payment, snapshot);
        if (payment!.PayOsLifecycleState != "CreateOutcomeUnknown")
            throw new IdentityFailure(409, "paymentProviderConflict");
        db.Payments.Remove(payment);
        PaymentEndpoints.Audit(db, context, actor, payment, "payments.payosNotFound");
        db.Payments.Remove(payment);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static void EnsureSameIntent(ThanhToan? payment, ReconcileSnapshot snapshot)
    {
        if (payment is null || payment.MaThanhToan != snapshot.PaymentId ||
            payment.PhuongThuc != "PayOsQr" || payment.PayOsOrderCode != snapshot.OrderCode ||
            payment.SoTien != snapshot.Amount ||
            snapshot.PaymentLinkId is not null && payment.PayOsPaymentLinkId != snapshot.PaymentLinkId)
            throw new IdentityFailure(409, "paymentProviderConflict");
    }

    private static void EnsureReservation(ThanhToan? payment, Reservation reserved)
    {
        if (payment is null || payment.MaThanhToan != reserved.PaymentId || payment.PhuongThuc != "PayOsQr" ||
            payment.PayOsOrderCode != reserved.OrderCode || payment.SoTien != reserved.Amount)
            throw new IdentityFailure(409, "paymentProviderConflict");
    }

    private static bool ValidCreateResult(PayOsCreatePaymentResult? created, Reservation reserved) =>
        created is not null && created.OrderCode == reserved.OrderCode &&
        created.Amount == checked((long)reserved.Amount) &&
        CounterPaymentRules.Text(created.PaymentLinkId, 50) &&
        string.Equals(created.Status, "PENDING", StringComparison.OrdinalIgnoreCase) &&
        PaymentEndpoints.SecurePayOsUrl(created.CheckoutUrl) && CounterPaymentRules.Text(created.QrCode, 2048);

    private static bool IsDefinitiveRejection(ApiException error) =>
        error.StatusCode is (int)HttpStatusCode.BadRequest or (int)HttpStatusCode.Unauthorized or
            (int)HttpStatusCode.Forbidden or 422;

    private static bool IsProviderFailure(Exception error) =>
        error is ApiException or HttpRequestException or OperationCanceledException or TimeoutException or
            System.Text.Json.JsonException or IOException;

    private static long NewOrderCode()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var code = BinaryPrimitives.ReadUInt64BigEndian(bytes) & 0x001FFFFFFFFFFFFFUL;
        return code == 0 ? 1 : (long)code;
    }

    private static string SafeDescription(long orderCode) =>
        "VC" + (orderCode % 10_000_000).ToString("D7", System.Globalization.CultureInfo.InvariantCulture);

    private static PayOsPaymentStatusDto Status(ThanhToan payment, decimal amount) =>
        new(payment.MaDangKy, amount, "VND", payment.PayOsLifecycleState switch {
            "Creating" => "Preparing",
            "CreateOutcomeUnknown" => "OutcomeUnknown",
            "Pending" => "Pending",
            "Paid" => "Paid",
            _ => throw new IdentityFailure(409, "paymentDataConflict")
        }, payment.PayOsLifecycleState == "Pending" ? payment.PayOsCheckoutUrl : null,
            payment.PayOsLifecycleState == "Pending" ? payment.PayOsQrCode : null,
            payment.PayOsLifecycleState != "Paid");
}
