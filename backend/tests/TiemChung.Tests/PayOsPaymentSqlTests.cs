using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed partial class PaymentSqlTests
{
    private const string PayOsMigration = "20261109011825_PAYR1PayOsPaymentPersistence";
    private const string PreviousPayOsMigration = "20261107065839_RegimenR1CalendarIntervals";

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsPendingAndPaidRowsAreReadWithoutCashierConfirmation()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var pendingRegistration = await f.Register();
            var paidRegistration = await f.Register();
            f.Db.AddRange(PayOsPayment(pendingRegistration, "Pending"), PayOsPayment(paidRegistration, "Paid"));
            await f.Db.SaveChangesAsync();

            var pendingUrl = Path(pendingRegistration.Id);
            var pending = (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(pendingUrl))!;
            Assert.Equal("ChoXacNhan", pending.PaymentState);
            Assert.Equal(0, pending.PaidAmount);
            Assert.Equal(pending.TotalDue, pending.RemainingAmount);
            Assert.Equal("PayOsQr", pending.Payment!.Method);
            Assert.False(pending.Payment.IsSimulation);
            Assert.Null(pending.Payment.EmployeeId);
            Assert.False(pending.CanConfirmPendingPayment);
            await Problem(await f.Cashier.Client.PostAsync(pendingUrl + "/confirm", null), 409, "paymentNotPending");

            var paid = (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(paidRegistration.Id)))!;
            Assert.Equal("ThanhCong", paid.PaymentState);
            Assert.Equal(paid.TotalDue, paid.PaidAmount);
            Assert.Equal(0, paid.RemainingAmount);
            Assert.Equal("PayOsQr", paid.Payment!.Method);
            Assert.False(paid.Payment.IsSimulation);
            Assert.Null(paid.Payment.EmployeeId);
            Assert.False(paid.CanConfirmPendingPayment);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("Pending")]
    [InlineData("Paid")]
    public async Task PaymentReadRejectsPayOsRowsMissingRequiredPaymentLinkId(string lifecycle)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register();
            var payment = PayOsPayment(registration, lifecycle);
            var paymentLinkId = payment.PayOsPaymentLinkId!;
            f.Db.Add(payment);
            await f.Db.SaveChangesAsync();

            await f.Db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE [ThanhToan] NOCHECK CONSTRAINT [CK_ThanhToan_PayOsQr]");
            try {
                await f.Db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [ThanhToan] SET [PayOsPaymentLinkId] = NULL WHERE [MaThanhToan] = {payment.MaThanhToan}");
                await Problem(await f.Cashier.Client.GetAsync(Path(registration.Id)), 409, "paymentDataConflict");
            } finally {
                await f.Db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [ThanhToan] SET [PayOsPaymentLinkId] = {paymentLinkId} WHERE [MaThanhToan] = {payment.MaThanhToan}");
                await f.Db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE [ThanhToan] WITH CHECK CHECK CONSTRAINT [CK_ThanhToan_PayOsQr]");
            }
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("order-code")]
    [InlineData("payment-link-id")]
    public async Task PayOsIdentifiersMustBeUnique(string duplicate)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var firstRegistration = await f.Register();
            var secondRegistration = await f.Register();
            var first = PayOsPayment(firstRegistration, "Pending");
            var second = PayOsPayment(secondRegistration, "Pending");
            if (duplicate == "order-code") second.PayOsOrderCode = first.PayOsOrderCode;
            else second.PayOsPaymentLinkId = first.PayOsPaymentLinkId;

            f.Db.Add(first);
            await f.Db.SaveChangesAsync();
            await using var db = f.NewDb();
            db.Add(second);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("provider-fields-on-transfer")]
    [InlineData("employee-on-payos")]
    [InlineData("invalid-lifecycle")]
    [InlineData("missing-paid-reference")]
    public async Task PayOsConstraintsRejectInconsistentRows(string invalidCase)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register();
            var payment = PayOsPayment(registration, "Pending");
            switch (invalidCase) {
                case "provider-fields-on-transfer":
                    payment.PhuongThuc = "ChuyenKhoanOnline";
                    payment.PayOsOrderCode = 12_345;
                    break;
                case "employee-on-payos":
                    payment.MaNhanVien = f.Cashier.Employee!.MaNhanVien;
                    break;
                case "invalid-lifecycle":
                    payment.PayOsLifecycleState = "Failed";
                    break;
                case "missing-paid-reference":
                    payment = PayOsPayment(registration, "Paid");
                    payment.PayOsReference = null;
                    break;
            }
            f.Db.Add(payment);
            await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsMigrationIsRepeatableAndPreservesLegacyPayments()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register();
            var legacyPayment = f.Payment(registration);
            f.Db.Add(legacyPayment);
            await f.Db.SaveChangesAsync();
            var migrator = f.Db.GetService<IMigrator>();

            await migrator.MigrateAsync(PreviousPayOsMigration);
            await migrator.MigrateAsync(PayOsMigration);
            await migrator.MigrateAsync(PayOsMigration);

            await using var fresh = f.NewDb();
            var saved = await fresh.Payments.SingleAsync();
            Assert.Equal(legacyPayment.MaThanhToan, saved.MaThanhToan);
            Assert.Null(saved.PayOsOrderCode);
            Assert.Null(saved.PayOsPaymentLinkId);
            Assert.Equal("MoPhong", saved.PhuongThuc);
            Assert.False(fresh.Database.HasPendingModelChanges());
            Assert.Empty(await fresh.Database.GetPendingMigrationsAsync());
            var receipt = (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id)))!;
            Assert.Equal(receipt.TotalDue, receipt.PaidAmount);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsMigrationRefusesDownWhenProviderHistoryExists()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register();
            f.Db.Add(PayOsPayment(registration, "Pending"));
            await f.Db.SaveChangesAsync();
            var migrator = f.Db.GetService<IMigrator>();

            var error = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(PreviousPayOsMigration));

            Assert.Equal(51006, error.Number);
            Assert.Contains(PayOsMigration, await f.Db.Database.GetAppliedMigrationsAsync());
        });
    }

    private static ThanhToan PayOsPayment(RegistrationDto registration, string lifecycle)
    {
        var linkId = "link-" + Guid.NewGuid().ToString("N");
        var isPaid = lifecycle == "Paid";
        return new() {
            MaDangKy = registration.Id,
            SoTien = registration.TotalBeforeDiscount,
            LoaiThanhToan = "ToanBo",
            PhuongThuc = "PayOsQr",
            TrangThai = isPaid ? "ThanhCong" : "ChoXacNhan",
            ThoiDiem = DateTime.UtcNow,
            MaGiaoDich = "PAYOS-" + Guid.NewGuid().ToString("N"),
            TenNguoiThanhToan = "[DEMO] Người thanh toán",
            SoDienThoaiThanhToan = "0900000107",
            TinhThanhThanhToan = "Tỉnh thử nghiệm",
            PhuongXaThanhToan = "Phường thử nghiệm",
            DiaChiThanhToan = "Địa chỉ thử nghiệm",
            DongYDieuKhoanLuc = DateTimeOffset.UtcNow,
            PayOsOrderCode = 1_000_000L + registration.Id,
            PayOsPaymentLinkId = linkId,
            PayOsLifecycleState = lifecycle,
            PayOsCheckoutUrl = isPaid ? null : "https://pay.payos.vn/web/" + linkId,
            PayOsQrCode = isPaid ? null : "synthetic-qr-payload",
            PayOsReference = isPaid ? "PAYOS-REF-" + registration.Id : null
        };
    }
}
