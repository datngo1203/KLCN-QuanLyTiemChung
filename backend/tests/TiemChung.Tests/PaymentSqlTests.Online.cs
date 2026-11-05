using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed partial class PaymentSqlTests
{
    private static readonly PaymentPayer OnlinePayer = new("Người trả tiền thử nghiệm", "0900000123",
        "payer@example.invalid", "Hà Nội", "Hoàn Kiếm", "Địa chỉ thanh toán");

    internal static OnlinePaymentRequest Online(string method) => method switch {
        "TheQuocTe" => new(OnlinePayer, method, true, InternationalCard: new("VISA", "Chủ thẻ", "4111 1111 1111 1111", 12, 2099, "683")),
        "TheNoiDia" => new(OnlinePayer, method, true, Domestic: new("Ngân hàng thử", "Chủ tài khoản", "9876543210", "246810")),
        _ => new(OnlinePayer, "ChuyenKhoanOnline", true, Transfer: new("Ngân hàng gửi", "Chủ tài khoản", "1122334455", "CK-TEST-111"))
    };

    [Theory]
    [InlineData("VISA", "4", 13, true)]
    [InlineData("VISA", "4", 16, true)]
    [InlineData("VISA", "4", 19, true)]
    [InlineData("VISA", "4", 14, false)]
    [InlineData("VISA", "5", 16, false)]
    [InlineData("MASTER", "5100", 16, true)]
    [InlineData("MASTER", "5599", 16, true)]
    [InlineData("MASTER", "2221", 16, true)]
    [InlineData("MASTER", "2720", 16, true)]
    [InlineData("MASTER", "2220", 16, false)]
    [InlineData("MASTER", "2721", 16, false)]
    [InlineData("MASTER", "5100", 17, false)]
    [InlineData("JCB", "3528", 16, true)]
    [InlineData("JCB", "3589", 19, true)]
    [InlineData("JCB", "3527", 16, false)]
    [InlineData("JCB", "3590", 16, false)]
    [InlineData("JCB", "3528", 15, false)]
    public void InternationalCardUsesBrandLengthPrefixLuhnAndVietnamExpiry(string brand, string prefix, int length, bool valid)
    {
        var now = new DateTimeOffset(2026, 11, 30, 17, 0, 0, TimeSpan.Zero);
        var stem = prefix.PadRight(length - 1, '0');
        var sum = stem.Reverse().Select((digit, index) => (digit - '0') * (index % 2 == 0 ? 2 : 1))
            .Sum(digit => digit > 9 ? digit - 9 : digit);
        var check = (10 - sum % 10) % 10;
        var card = new InternationalCard(brand, " Chủ thẻ ", stem + check, 12, 2026, "007");
        Assert.Equal(valid, OnlinePaymentRules.ValidCard(card, now));
        Assert.False(OnlinePaymentRules.ValidCard(card with { CardNumber = stem + (check + 1) % 10 }, now));
        Assert.False(OnlinePaymentRules.ValidCard(card with { ExpiryMonth = 11 }, now));
        Assert.False(OnlinePaymentRules.ValidCard(card with { Cvv = "٠٠٧" }, now));
        Assert.False(OnlinePaymentRules.ValidCard(card with { Cvv = "12" }, now));
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("TheQuocTe", "****1111")]
    [InlineData("TheNoiDia", "****3210")]
    [InlineData("ChuyenKhoanOnline", "****4455")]
    public async Task OnlineCreatesOnlySafeReceiptMetadataAndCountsOnlySuccess(string method, string masked)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(consultationFee: "50000"), async f => {
            var registration = await f.Register("single");
            var path = Path(registration.Id);
            var before = (await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(path))!;
            Assert.Equal(registration.Contact, before.RegistrationContact);
            var settings = await f.Customer.Client.GetFromJsonAsync<PaymentSettingsDto>("/api/v1/payment/settings");
            Assert.NotNull(settings);
            Assert.Equal("Đơn vị nhận chuyển khoản", settings.ReceiverBankName);
            Assert.Equal("Trung tâm tiêm chủng VNVC", settings.ReceiverAccountName);
            Assert.DoesNotContain("Ngân hàng thử nghiệm", settings.ReceiverBankName);
            Assert.Equal("0000000402", settings.ReceiverAccountNumber);
            Assert.Empty(await f.Db.Payments.ToArrayAsync());
            var pending = method == "ChuyenKhoanOnline";
            var request = Online(method);
            using var created = await f.Customer.Client.PostAsJsonAsync(path + "/online", request);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal(path, created.Headers.Location!.OriginalString);
            var saved = (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(path))!;
            Assert.Equal(registration.Subtotal + 50000, saved.TotalDue);
            Assert.Equal(registration.Contact, saved.RegistrationContact);
            Assert.Equal(OnlinePayer, saved.Payment!.Payer);
            Assert.Null(saved.Payment.EmployeeId);
            Assert.Equal(masked, saved.Payment.MaskedIdentifier);
            Assert.Equal(pending ? "ChoXacNhan" : "ThanhCong", saved.PaymentState);
            Assert.Equal(pending ? 0 : saved.TotalDue, saved.PaidAmount);
            Assert.Equal(pending ? saved.TotalDue : 0, saved.RemainingAmount);
            Assert.False(saved.CanRecordPayment);
            Assert.Equal(pending, saved.CanConfirmPendingPayment);
            Assert.Equal("TrucTuyen", saved.ExpectedChannel);
            Assert.Equal("TrucTuyen", await f.Db.Registrations.Where(r => r.MaDangKy == registration.Id).Select(r => r.KenhThanhToanDuKien).SingleAsync());
            var persisted = await f.Db.Payments.AsNoTracking().SingleAsync();
            Assert.NotNull(persisted.DongYDieuKhoanLuc);
            Assert.Equal(saved.Payment.Id, persisted.MaThanhToan);
            var sql = JsonSerializer.Serialize(persisted);
            foreach (var raw in new[] { "4111 1111 1111 1111", "4111111111111111", "9876543210", "246810", "1122334455" }) {
                Assert.DoesNotContain(raw, sql);
                Assert.DoesNotContain(raw, await created.Content.ReadAsStringAsync());
            }
            Assert.Equal(request.Transfer?.TransferReference, persisted.ThamChieuThanhToan);
            Assert.Equal(f.CustomerProfile.HoTen, await f.Db.Customers.Select(c => c.HoTen).SingleAsync());
            var audit = await f.Db.Set<SecurityAudit>().SingleAsync(a => a.Action == "payments.recorded");
            Assert.Equal(f.Customer.Account.MaTaiKhoan, audit.ActorAccountId);
            Assert.Equal(saved.Payment.Id.ToString(), audit.ResourceId);
            var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd");
            var report = $"/api/v1/reports/monthly-revenue?from={today[..7]}&to={today}";
            Assert.Equal(saved.PaidAmount, (await f.Base.Client.GetFromJsonAsync<MonthlyRevenueReportDto>(report))!.TotalRevenue);
            if (pending) {
                using var confirmed = await f.Cashier.Client.PostAsync(path + "/confirm", null);
                Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
                var receipt = (await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(path))!;
                Assert.Equal(saved.Payment.Id, receipt.Payment!.Id);
                Assert.Equal(saved.Payment.TransactionCode, receipt.Payment.TransactionCode);
                Assert.Equal(saved.Payment.Reference, receipt.Payment.Reference);
                Assert.Equal(persisted.DongYDieuKhoanLuc, await f.Db.Payments.Select(p => p.DongYDieuKhoanLuc).SingleAsync());
                Assert.Equal(saved.TotalDue, (await f.Base.Client.GetFromJsonAsync<MonthlyRevenueReportDto>(report))!.TotalRevenue);
            }
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task OnlineAndChannelDenyOtherActorsAndMalformedFinancialWrites()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register("single");
            var path = Path(registration.Id);
            var url = path + "/online";
            var center = new PaymentChannelRequest("TaiTrungTam");
            var card = Online("TheQuocTe");
            var other = await f.Actor(null);
            await Problem(await other.Client.PostAsJsonAsync(url, card), 403, "permissionDenied");
            await Problem(await other.Client.PostAsJsonAsync(Path(int.MaxValue) + "/online", card), 403, "permissionDenied");
            await Problem(await other.Client.PutAsJsonAsync(path + "/channel", center), 403, "permissionDenied");
            await Problem(await f.Cashier.Client.PostAsJsonAsync(url, card), 403, "permissionDenied");
            using var anonymous = f.Base.Factory.CreateClient();
            await Problem(await anonymous.PostAsJsonAsync(url, card), 401, "invalidSession");
            await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == other.Account.MaTaiKhoan)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.EmailVerifiedAt, (DateTimeOffset?)null));
            await Problem(await other.Client.PostAsJsonAsync(url, card), 403, "emailVerificationRequired");
            var atm = Online("TheNoiDia");
            foreach (var bad in new[] {
                card with { TermsAccepted = false },
                card with { Payer = OnlinePayer with { AddressLine = " " } },
                card with { InternationalCard = card.InternationalCard! with { Cvv = "1234" } },
                card with { InternationalCard = card.InternationalCard! with { ExpiryYear = 2000 } },
                card with { Domestic = atm.Domestic },
                atm with { Domestic = atm.Domestic! with { Otp = "12x456" } },
                atm with { Domestic = atm.Domestic! with { CardOrAccountNumber = "1234567" } },
            }) await Problem(await f.Customer.Client.PostAsJsonAsync(url, bad), 400, "validation");
            foreach (var field in new[] { "amount", "employeeId", "status", "type", "transactionCode", "occurredAt", "consentTimestamp" }) {
                using var payload = new StringContent($"{{\"method\":\"TheQuocTe\",\"{field}\":1}}", System.Text.Encoding.UTF8, "application/json");
                await Problem(await f.Customer.Client.PostAsync(url, payload), 400, "validation");
            }
            Assert.Empty(await f.Db.Payments.ToArrayAsync());
            for (var i = 0; i < 2; i++) {
                using var selected = await f.Customer.Client.PutAsJsonAsync(path + "/channel", center);
                Assert.Equal(HttpStatusCode.OK, selected.StatusCode);
                var detail = (await selected.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!;
                Assert.Equal("TaiTrungTam", detail.ExpectedChannel);
                Assert.Equal("ChuaThanhToan", detail.PaymentState);
                Assert.Null(detail.Payment);
            }
            Assert.Empty(await f.Db.Payments.ToArrayAsync());
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "payments.channelSelected"));
            Assert.Equal("TaiTrungTam", (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(path))!.ExpectedChannel);
            using var online = await f.Customer.Client.PostAsJsonAsync(url, card);
            Assert.Equal(HttpStatusCode.Created, online.StatusCode);
            Assert.Equal("TrucTuyen", (await online.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!.ExpectedChannel);
            await Problem(await f.Customer.Client.PutAsJsonAsync(path + "/channel", center), 409, "paymentAlreadyRecorded");
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("TheQuocTe", true)]
    [InlineData("ChuyenKhoanOnline", true)]
    [InlineData("ChuyenKhoanOnline", false)]
    public async Task OnlineAndCashierShareRegistrationLock(string method, bool onlineFirst)
    {
        var gate = new PaymentGate();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(gate), async f => {
            var registration = await f.Register();
            var path = Path(registration.Id);
            Task<HttpResponseMessage> Send(bool online) => online
                ? f.Customer.Client.PostAsJsonAsync(path + "/online", Online(method))
                : f.Cashier.Client.PostAsJsonAsync(path, Cash(registration.TotalBeforeDiscount));
            gate.Armed = true;
            var first = Send(onlineFirst);
            await gate.Locked.Task.WaitAsync(TimeSpan.FromSeconds(20));
            var second = Send(!onlineFirst);
            try {
                await gate.Contending.Task.WaitAsync(TimeSpan.FromSeconds(20));
                Assert.False(second.IsCompleted);
            } finally { gate.Release.TrySetResult(); }
            using var success = await first;
            Assert.Equal(HttpStatusCode.Created, success.StatusCode);
            await Problem(await second, 409, "paymentAlreadyRecorded");
            var row = await f.Db.Payments.AsNoTracking().SingleAsync();
            Assert.Equal(onlineFirst ? method : "TienMat", row.PhuongThuc);
            Assert.Equal(row.MaThanhToan, (await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(path))!.Payment!.Id);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ConcurrentOnlineSubmissionsProduceOnePayment()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register();
            var path = Path(registration.Id) + "/online";
            var results = await Task.WhenAll(
                f.Customer.Client.PostAsJsonAsync(path, Online("TheQuocTe")),
                f.Customer.Client.PostAsJsonAsync(path, Online("TheNoiDia")));
            using var created = Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created);
            await Problem(Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict), 409, "paymentAlreadyRecorded");
            Assert.Equal(1, await f.Db.Payments.CountAsync());
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "payments.recorded"));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task OnlineUsesExactMaxTotalAndFailsClosedForZeroAndPromotion()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(consultationFee: "50000"), async f => {
            var max = await f.Register("single");
            var zero = await f.Register("single");
            var discounted = await f.Register("single");
            await f.Db.RegistrationVaccineItems.Where(i => i.MaDangKy == max.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(i => i.SoLuong, 1).SetProperty(i => i.GiaTaiThoiDiemDangKy, CounterPaymentRules.MaxMoney - 50000m));
            await f.Db.RegistrationVaccineItems.Where(i => i.MaDangKy == zero.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(i => i.GiaTaiThoiDiemDangKy, 0m));
            await f.Db.Registrations.Where(r => r.MaDangKy == zero.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(r => r.PhiKhamTuVan, 0m));
            var promotion = new KhuyenMai { TenKhuyenMai = "Thử ưu đãi chưa chốt",
                ThoiGianBatDau = DateTime.UtcNow.AddDays(-1), ThoiGianKetThuc = DateTime.UtcNow.AddDays(1), MucUuDai = 1 };
            f.Db.Add(promotion);
            await f.Db.SaveChangesAsync();
            await f.Db.Registrations.Where(r => r.MaDangKy == discounted.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(r => r.MaKhuyenMai, promotion.MaKhuyenMai));
            Task<HttpResponseMessage> Pay(int id) => f.Customer.Client.PostAsJsonAsync(Path(id) + "/online", Online("TheQuocTe"));
            await Problem(await Pay(zero.Id), 409, "paymentNotRequired");
            await Problem(await Pay(discounted.Id), 409, "paymentDataConflict");
            using var paid = await Pay(max.Id);
            Assert.Equal(HttpStatusCode.Created, paid.StatusCode);
            var detail = (await paid.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!;
            Assert.Equal(CounterPaymentRules.MaxMoney, detail.TotalDue);
            Assert.Equal(CounterPaymentRules.MaxMoney, (await f.Db.Payments.SingleAsync()).SoTien);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task B2MigrationChecksOnlineCombinationsAndProtectsRollback()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            const string b1 = "20261104112618_RF04B1CashierPayment";
            const string b2 = "20261104134233_RF04B2WebCheckout";
            var legacy = await f.Register();
            var online = await f.Register();
            var migrator = f.Db.GetService<IMigrator>();
            await InsertLegacy(f.Db, f.Payment(legacy));
            var snapshot = await LegacySnapshot(f.Db);
            await migrator.MigrateAsync(b1);
            Assert.Equal(snapshot, await LegacySnapshot(f.Db));
            await migrator.MigrateAsync();
            Assert.False(f.Db.Database.HasPendingModelChanges());
            Assert.Equal(snapshot, await LegacySnapshot(f.Db));
            using var response = await f.Customer.Client.PostAsJsonAsync(Path(online.Id) + "/online", Online("ChuyenKhoanOnline"));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            foreach (var sql in new[] {
                "UPDATE dbo.ThanhToan SET DongYDieuKhoanLuc = NULL WHERE MaDangKy = {0}",
                "UPDATE dbo.ThanhToan SET MaNhanVien = (SELECT TOP 1 MaNhanVien FROM NhanVien) WHERE MaDangKy = {0}",
                "UPDATE dbo.ThanhToan SET TrangThai = 'ThanhCong' WHERE MaDangKy = {0}",
                "UPDATE dbo.ThanhToan SET PhuongThuc = 'Unknown' WHERE MaDangKy = {0}"
            }) {
                var error = await Assert.ThrowsAsync<SqlException>(() => f.Db.Database.ExecuteSqlRawAsync(
                    sql, online.Id));
                Assert.Contains("CK_ThanhToan_NhanVien", error.Message);
            }
            Assert.Equal(51006, (await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(b1))).Number);
            Assert.Contains(b2, await f.Db.Database.GetAppliedMigrationsAsync());
            Assert.Equal(2, await f.Db.Payments.CountAsync());
        });
    }
}
