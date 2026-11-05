using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed partial class PaymentSqlTests
{
    private const string ListPath = "/api/v1/payment-registrations";
    private static string Path(int id) => $"/api/v1/registrations/{id}/payment";
    internal static RecordPaymentRequest Cash(decimal amount) =>
        new(new("[DEMO] Người thanh toán", "0900000107"), "TienMat", amount);

    internal static ThanhToan Pending(RegistrationDto registration) => new() {
        MaDangKy = registration.Id, SoTien = registration.TotalBeforeDiscount, LoaiThanhToan = "ToanBo",
        PhuongThuc = "ChuyenKhoanOnline", TrangThai = "ChoXacNhan", ThoiDiem = DateTime.UtcNow.AddMinutes(-10),
        MaGiaoDich = $"TT-{Guid.NewGuid():N}", TenNguoiThanhToan = "[DEMO] Người thanh toán", SoDienThoaiThanhToan = "0900000107",
        TinhThanhThanhToan = "Tỉnh thử nghiệm", PhuongXaThanhToan = "Phường thử nghiệm", DiaChiThanhToan = "Địa chỉ thử nghiệm",
        DongYDieuKhoanLuc = DateTimeOffset.UtcNow.AddMinutes(-10),
        NhaCungCapThanhToan = "Synthetic Bank", TenChuTaiKhoanThe = "[DEMO] Chủ tài khoản",
        SoDinhDanhMasked = "****0107", ThamChieuThanhToan = "DEMO-PENDING-107"
    };

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("single")]
    [InlineData("package")]
    [InlineData("mixed")]
    public async Task FullPaymentUsesSnapshotsAndPersistsWithAuditAndFreshGet(string kind)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var registration = await f.Register(kind);
        await f.Db.Vaccines.ExecuteUpdateAsync(s => s.SetProperty(x => x.Gia, 777000m));
        await f.Db.Set<GoiVacXin>().ExecuteUpdateAsync(s => s.SetProperty(x => x.GiaGoi, 999000m));
        var before = await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.Equal(registration.Subtotal, before!.TotalDue); Assert.True(before.CanRecordPayment);
        Assert.True(before.RequiresPayment); Assert.Null(before.Payment); Assert.True(before.IsSimulation);
        using var response = await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(Path(registration.Id), response.Headers.Location!.OriginalString);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var saved = (await response.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!;
        Assert.Equal(registration.Subtotal, saved.Subtotal); Assert.Equal(saved.Subtotal, saved.TotalDue);
        Assert.Equal(saved.TotalDue, saved.PaidAmount); Assert.Equal(0, saved.RemainingAmount);
        Assert.False(saved.RequiresPayment); Assert.False(saved.CanRecordPayment);
        Assert.NotNull(saved.Payment); Assert.Equal("ToanBo", saved.Payment.Type);
        Assert.Equal("TienMat", saved.Payment.Method); Assert.Equal("ThanhCong", saved.Payment.Status);
        Assert.Equal("ThanhCong", saved.PaymentState); Assert.Equal(saved.TotalDue, saved.Payment.CashTendered);
        Assert.Equal(0, saved.Payment.ChangeAmount); Assert.NotNull(saved.Payment.TransactionCode);
        Assert.True(saved.Payment.IsSimulation); Assert.Equal(f.Cashier.Employee!.MaNhanVien, saved.Payment.EmployeeId);
        Assert.Equal(TimeSpan.Zero, saved.Payment.OccurredAt.Offset);
        Assert.Equal(JsonSerializer.Serialize(registration.Items), JsonSerializer.Serialize(saved.Items));
        await using var fresh = f.NewDb();
        var payment = Assert.Single(await fresh.Payments.AsNoTracking().ToArrayAsync());
        Assert.Equal(saved.TotalDue, payment.SoTien); Assert.Equal(registration.Id, payment.MaDangKy);
        var audit = Assert.Single(await fresh.Set<SecurityAudit>().Where(a => a.Action == "payments.recorded").ToArrayAsync());
        Assert.Equal("Payment", audit.ResourceType); Assert.Equal(payment.MaThanhToan.ToString(), audit.ResourceId);
        Assert.Equal(f.Cashier.Account.MaTaiKhoan, audit.ActorAccountId); Assert.Equal(f.Cashier.Session.Id, audit.SessionId);
        Assert.Equal("Success", audit.Outcome);
        Assert.Equal("DaDangKy", (await fresh.Registrations.SingleAsync()).TrangThai);
        Assert.Empty(await fresh.Set<LichHenTiem>().ToArrayAsync());
        // Each HTTP request uses a new scoped DbContext, including the Customer verification GET.
        var reloaded = await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.Equal(JsonSerializer.Serialize(saved with { CanRecordPayment = false }), JsonSerializer.Serialize(reloaded));
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount)), 409, "paymentAlreadyRecorded");
        Assert.Equal(1, await fresh.Payments.CountAsync());
        Assert.Equal(1, await fresh.Set<SecurityAudit>().CountAsync(a => a.Action == "payments.recorded"));
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PromotionCapsAtServiceSubtotalAndPaymentUsesImmutableSnapshot(bool pendingTransfer)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(consultationFee: "50000"), async f => {
            var manager = await f.Actor("Admin", "QuanLyKhuyenMai");
            const string promotions = "/api/v1/promotions";
            var request = new UpsertPromotionRequest("Ưu đãi kiểm thử", "Chương trình tổng hợp",
                DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1),
                9_999_999_999_999_999m, true);
            using var forbidden = await f.Customer.Client.PostAsJsonAsync(promotions, request);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            using var created = await manager.Client.PostAsJsonAsync(promotions, request);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var promotion = (await created.Content.ReadFromJsonAsync<PromotionDto>())!;
            var offers = (await f.Customer.Client.GetFromJsonAsync<PromotionListResponse>(promotions + "/valid"))!;
            Assert.Equal(request.DiscountAmount, Assert.Single(offers.Items).DiscountAmount);

            var registration = await f.Register(promotionId: promotion.Id);
            Assert.True(registration.Subtotal > 0);
            Assert.Equal(registration.Subtotal, registration.DiscountAmount);
            Assert.Equal(50000m, registration.TotalDue);
            Assert.Equal(promotion.Name, registration.PromotionName);

            using var updated = await manager.Client.PutAsJsonAsync($"{promotions}/{promotion.Id}",
                request with { IsEnabled = false, DiscountAmount = 1, Name = "Tên đã đổi" });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Empty((await f.Customer.Client.GetFromJsonAsync<PromotionListResponse>(promotions + "/valid"))!.Items);
            var vaccine = registration.Items.First(i => i.Kind == "vaccine");
            var rejected = RegistrationSqlTests.SyntheticRequest(f.CustomerProfile.MaKhachHang, f.Recipient.MaNguoiDuocTiem,
                [new("vaccine", vaccine.ServiceId, 1, vaccine.UnitPrice)]) with { PromotionId = promotion.Id };
            using var conflict = await f.Customer.Client.PostAsJsonAsync("/api/v1/registrations", rejected);
            await Problem(conflict, 409, "promotionUnavailable");
            var path = Path(registration.Id);
            var before = (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(path))!;
            Assert.Equal(registration.DiscountAmount, before.DiscountAmount);
            Assert.Equal(50000m, before.TotalDue);
            var reception = await f.Actor("LeTan", "QuanLyLichHen");
            var sourcePath = $"/api/v1/appointments/sources/{registration.Id}";
            async Task<string> Readiness() =>
                (await reception.Client.GetFromJsonAsync<AppointmentSourceDetailDto>(sourcePath))!.Source.ReadinessCode;
            Assert.Equal("unpaid", await Readiness());
            var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd");
            var report = $"/api/v1/reports/monthly-revenue?from={today[..7]}&to={today}";
            async Task<decimal> Revenue() =>
                (await f.Base.Client.GetFromJsonAsync<MonthlyRevenueReportDto>(report))!.TotalRevenue;

            using var response = pendingTransfer
                ? await f.Customer.Client.PostAsJsonAsync(path + "/online", Online("ChuyenKhoanOnline"))
                : await f.Cashier.Client.PostAsJsonAsync(path, Cash(50000m));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var receipt = (await response.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!;
            Assert.Equal(pendingTransfer ? "Đang chờ Thu ngân xác nhận chuyển khoản." : "Đã ghi nhận thanh toán toàn bộ.", receipt.Message);
            var settings = (await f.Customer.Client.GetFromJsonAsync<PaymentSettingsDto>("/api/v1/payment/settings"))!;
            Assert.Equal("Trung tâm tiêm chủng VNVC", settings.ReceiverAccountName);
            Assert.Equal("Đơn vị nhận chuyển khoản", settings.ReceiverBankName);
            if (pendingTransfer) {
                Assert.Equal(50000m, receipt.RemainingAmount);
                Assert.Equal(0m, receipt.PaidAmount);
                Assert.Equal(0m, await Revenue());
                Assert.Equal("pending", await Readiness());
                using var confirmed = await f.Cashier.Client.PostAsync(path + "/confirm", null);
                Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
            }

            var journey = (await f.Customer.Client.GetFromJsonAsync<VaccinationJourneyDto>(
                $"/api/v1/vaccination-journeys/{registration.Id}"))!;
            Assert.Equal(receipt.Payment!.Id, journey.Payment.Payment!.Id);
            Assert.Equal(registration.DiscountAmount, journey.Payment.DiscountAmount);
            Assert.Equal(50000m, journey.Payment.PaidAmount);
            Assert.Equal(0m, journey.Payment.RemainingAmount);
            Assert.Equal("paid", await Readiness());
            Assert.Equal(50000m, await Revenue());

            await using var readback = f.NewDb();
            var snapshot = await readback.Registrations.AsNoTracking().SingleAsync();
            Assert.Equal(registration.PromotionName, snapshot.TenKhuyenMaiSnapshot);
            Assert.Equal(registration.DiscountAmount, snapshot.MucGiamSnapshot);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CashierRecordsServiceSubtotalPlusSnapshottedConsultationFee()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(consultationFee: "50000"), async f => {
            const decimal fee = 50000m;
            var registration = await f.Register();
            var subtotal = registration.Items.Sum(item => item.Quantity * item.UnitPrice);
            var totalDue = subtotal + fee;
            Assert.Equal(subtotal, registration.Subtotal);
            Assert.Equal(fee, registration.ConsultationFee);
            Assert.Equal(totalDue, registration.TotalBeforeDiscount);

            await using var fresh = f.NewDb();
            Assert.Equal(fee, (await fresh.Registrations.AsNoTracking().SingleAsync()).PhiKhamTuVan);

            var path = Path(registration.Id);
            var before = (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(path))!;
            Assert.Equal(subtotal, before.Subtotal);
            Assert.Equal(fee, before.ConsultationFee);
            Assert.Equal(totalDue, before.TotalDue);
            Assert.Equal(0, before.PaidAmount);
            Assert.Equal(totalDue, before.RemainingAmount);
            Assert.Null(before.Payment);

            using var response = await f.Cashier.Client.PostAsJsonAsync(path, Cash(totalDue));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var saved = (await response.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!;
            Assert.Equal(totalDue, saved.TotalDue);
            Assert.Equal(totalDue, saved.PaidAmount);
            Assert.Equal(0, saved.RemainingAmount);
            Assert.NotNull(saved.Payment);
            Assert.Equal(totalDue, saved.Payment.Amount);

            var payment = Assert.Single(await fresh.Payments.AsNoTracking().ToArrayAsync());
            Assert.Equal(registration.Id, payment.MaDangKy);
            Assert.Equal(saved.Payment.Id, payment.MaThanhToan);
            Assert.Equal(totalDue, payment.SoTien);
            Assert.Equal("TienMat", payment.PhuongThuc);
            Assert.Equal("ToanBo", payment.LoaiThanhToan);
            Assert.Equal("ThanhCong", payment.TrangThai);
            Assert.Equal(payment.PhuongThuc, saved.Payment.Method);
            Assert.Equal(payment.LoaiThanhToan, saved.Payment.Type);
            Assert.Equal(payment.TrangThai, saved.Payment.Status);

            var reloaded = await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(path);
            Assert.Equal(JsonSerializer.Serialize(saved), JsonSerializer.Serialize(reloaded));
            using var duplicate = await f.Cashier.Client.PostAsJsonAsync(path, Cash(totalDue));
            await Problem(duplicate, 409, "paymentAlreadyRecorded");
            Assert.Equal(1, await fresh.Payments.CountAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DifferentCashiersSerializeInBothOrders(bool secondCashierFirst)
    {
        var gate = new PaymentGate();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(gate), async f => {
        var registration = await f.Register();
        var other = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
        var firstActor = secondCashierFirst ? other : f.Cashier;
        var secondActor = secondCashierFirst ? f.Cashier : other;
        gate.Armed = true;
        var first = firstActor.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount));
        await gate.Locked.Task.WaitAsync(TimeSpan.FromSeconds(20));
        var second = secondActor.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount));
        try {
            await gate.Contending.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(second.IsCompleted);
        } finally { gate.Release.TrySetResult(); }
        using var success = await first;
        Assert.Equal(HttpStatusCode.Created, success.StatusCode);
        await Problem(await second, 409, "paymentAlreadyRecorded");
        await using var fresh = f.NewDb();
        var payment = Assert.Single(await fresh.Payments.ToArrayAsync());
        Assert.Equal(firstActor.Employee!.MaNhanVien, payment.MaNhanVien);
        var audit = Assert.Single(await fresh.Set<SecurityAudit>().Where(a => a.Action == "payments.recorded").ToArrayAsync());
        Assert.Equal(firstActor.Account.MaTaiKhoan, audit.ActorAccountId);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task EveryRouteEnforcesRoleAndCurrentIdentityState()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var registration = await f.Register();
        async Task Denied(HttpClient client, int expected) {
            await Problem(await client.GetAsync(ListPath), expected);
            await Problem(await client.GetAsync(Path(registration.Id)), expected);
            await Problem(await client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount)), expected);
            await Problem(await client.PostAsync(Path(registration.Id) + "/confirm", null), expected);
        }
        using var anonymous = f.Base.Factory.CreateClient();
        await Denied(anonymous, 401);
        // Admin's existing seed has every permission code, including both payment grants.
        await Denied(f.Base.Client, 403);
        foreach (var title in new[] { "LeTan", "BacSi", "DieuDuong", "ThuKho" }) {
            var actor = await f.Actor(title, "XemThanhToan", "GhiThanhToan");
            await Denied(actor.Client, 403);
        }
        var reader = await f.Actor("ThuNgan", "XemThanhToan");
        var view = await reader.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.True(view!.CanRecordPayment); Assert.True(view.RequiresPayment);
        Assert.Equal(HttpStatusCode.OK, (await reader.Client.GetAsync(ListPath)).StatusCode);
        Assert.True((await reader.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id)))!.CanRecordPayment);
        var writerOnly = await f.Actor("ThuNgan", "GhiThanhToan");
        Assert.Equal(HttpStatusCode.OK,(await writerOnly.Client.GetAsync(ListPath)).StatusCode);
        await f.Db.Set<TaiKhoan_QuyenHan>().Where(g => g.MaTaiKhoan == f.Cashier.Account.MaTaiKhoan).ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.OK,(await f.Cashier.Client.GetAsync(ListPath)).StatusCode);
        await f.Db.Set<NhanVien>().Where(e=>e.MaNhanVien==f.Cashier.Employee!.MaNhanVien).ExecuteUpdateAsync(s=>s.SetProperty(e=>e.ChucDanh,"LeTan"));
        await Denied(f.Cashier.Client, 403);
        var inactive = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
        await f.Db.Set<NhanVien>().Where(e => e.MaNhanVien == inactive.Employee!.MaNhanVien)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.TrangThai, "Inactive"));
        await Denied(inactive.Client, 401);
        var revoked = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
        await f.Db.Set<IdentitySession>().Where(s => s.Id == revoked.Session.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow));
        await Denied(revoked.Client, 401);
        var locked = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
        await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == locked.Account.MaTaiKhoan)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Locked"));
        await Denied(locked.Client, 401);
        Assert.Empty(await f.Db.Payments.ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CustomerReadRequiresBothCurrentLinksAndCorrectPair()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var registration = await f.Register();
        await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount));
        var own = await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.NotNull(own!.Payment); Assert.False(own.CanRecordPayment);
        await Problem(await f.Customer.Client.GetAsync(ListPath), 403, "permissionDenied");
        await Problem(await f.Customer.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount)), 403, "permissionDenied");
        var stranger = await f.Actor(null);
        foreach (var id in new[] { registration.Id, int.MaxValue }) {
            using var response = await stranger.Client.GetAsync(Path(id));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.DoesNotContain(f.CustomerProfile.HoTen, await response.Content.ReadAsStringAsync());
        }
        await Problem(await f.Customer.Client.GetAsync(Path(int.MaxValue)), 403, "permissionDenied");
        foreach (var link in await f.Db.Set<AccountProfileLink>().Where(l => l.AccountId == f.Customer.Account.MaTaiKhoan).ToArrayAsync()) {
            link.State = "Revoked"; await f.Db.SaveChangesAsync();
            await Problem(await f.Customer.Client.GetAsync(Path(registration.Id)), 403, "permissionDenied");
            link.State = "Active"; await f.Db.SaveChangesAsync();
        }
        var other = new KhachHang { HoTen = "[DEMO] G3 other", Email = "other@example.invalid" };
        f.Db.Add(other); await f.Db.SaveChangesAsync();
        await f.Db.Recipients.Where(p => p.MaNguoiDuocTiem == f.Recipient.MaNguoiDuocTiem)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.MaKhachHang, other.MaKhachHang));
        await Problem(await f.Customer.Client.GetAsync(Path(registration.Id)), 403, "permissionDenied");
        await f.Db.Recipients.Where(p => p.MaNguoiDuocTiem == f.Recipient.MaNguoiDuocTiem)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.MaKhachHang, f.CustomerProfile.MaKhachHang));
        await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == f.Customer.Account.MaTaiKhoan)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.EmailVerifiedAt, (DateTimeOffset?)null));
        await Problem(await f.Customer.Client.GetAsync(Path(registration.Id)), 403, "emailVerificationRequired");
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ZeroTotalHasNoPaymentAndCannotBeRecorded()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        await f.Db.Vaccines.ExecuteUpdateAsync(s => s.SetProperty(x => x.Gia, 0m));
        var registration = await f.Register("single");
        var detail = await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.Equal(0, detail!.Subtotal); Assert.Equal(0, detail.TotalDue); Assert.Equal(0, detail.PaidAmount);
        Assert.Equal(0, detail.RemainingAmount); Assert.False(detail.RequiresPayment); Assert.False(detail.CanRecordPayment);
        Assert.Null(detail.Payment); Assert.Equal("Không phát sinh khoản thu.", detail.Message);
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount)), 409, "paymentNotRequired");
        Assert.Empty(await f.Db.Payments.ToArrayAsync());
        Assert.False(await f.Db.Set<SecurityAudit>().AnyAsync(a => a.Action == "payments.recorded"));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task LargestG2VndAmountRemainsExactThroughSqlAndJson()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        const decimal limit = 9999999999999999m;
        await f.Db.Vaccines.ExecuteUpdateAsync(s => s.SetProperty(v => v.Gia, limit));
        var vaccine = await f.Db.Vaccines.AsNoTracking().FirstAsync();
        var request = RegistrationSqlTests.SyntheticRequest(f.CustomerProfile.MaKhachHang, f.Recipient.MaNguoiDuocTiem,
            [new("vaccine", vaccine.MaVacXin, 1, limit)]);
        using var created = await f.Customer.Client.PostAsJsonAsync("/api/v1/registrations", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var registration = (await created.Content.ReadFromJsonAsync<RegistrationDto>())!;
        using var response = await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!;
        Assert.Equal(limit, saved.TotalDue); Assert.Equal(limit, saved.Payment!.Amount);
        Assert.Equal(limit, saved.Payment.CashTendered); Assert.Equal(0, saved.Payment.ChangeAmount);
        await using var fresh = f.NewDb();
        Assert.Equal(limit, (await fresh.Payments.SingleAsync()).SoTien);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task UnrelatedUniqueViolationIsNotReportedAsDuplicatePayment()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var first = await f.Register(); var second = await f.Register();
        Assert.Equal(HttpStatusCode.Created, (await f.Cashier.Client.PostAsJsonAsync(Path(first.Id), Cash(first.TotalBeforeDiscount))).StatusCode);
        await f.Db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX G3FixtureOtherUnique ON dbo.ThanhToan(SoTien);");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(second.Id), Cash(second.TotalBeforeDiscount)), 503, "dependencyUnavailable");
        Assert.Equal(1, await f.Db.Payments.CountAsync());
        Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "payments.recorded"));
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("payment")]
    [InlineData("audit")]
    public async Task PaymentOrAuditFailureRollsBackBothAndLeavesRegistrationUnchanged(string failure)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var registration = await f.Register();
        await f.Db.Database.ExecuteSqlRawAsync(failure == "payment"
            ? "ALTER TABLE dbo.ThanhToan ADD CONSTRAINT G3FixtureFailure CHECK (SoTien < 0);"
            : "CREATE TRIGGER G3FixtureFailure ON dbo.SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='payments.recorded') THROW 51000, 'Synthetic G3 audit failure', 1;");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount)), 503, "dependencyUnavailable");
        await using var fresh = f.NewDb();
        Assert.Empty(await fresh.Payments.ToArrayAsync());
        Assert.False(await fresh.Set<SecurityAudit>().AnyAsync(a => a.Action == "payments.recorded"));
        Assert.Equal("DaDangKy", (await fresh.Registrations.SingleAsync()).TrangThai);
        await f.Db.Database.ExecuteSqlRawAsync(failure == "payment"
            ? "ALTER TABLE dbo.ThanhToan DROP CONSTRAINT G3FixtureFailure;" : "DROP TRIGGER dbo.G3FixtureFailure;");
        Assert.Equal(HttpStatusCode.Created, (await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount))).StatusCode);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task SearchPaginationFiltersAndValidationAreStableAndDoNotWrite()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var first = await f.Register("single"); var second = await f.Register("package");
        var all = (await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath))!.Items;
        Assert.Equal(new[] { second.Id, first.Id }, all.Select(x => x.Id));
        Assert.All(all, x => { Assert.Equal(x.Subtotal, x.TotalDue); Assert.Equal(x.TotalDue, x.RemainingAmount); Assert.Null(x.PaymentId); });
        var page = (await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?page=2&pageSize=1"))!.Items;
        Assert.Equal(first.Id, Assert.Single(page).Id);
        foreach (var search in new[] { first.Id.ToString(), $"DK-{first.Id:0000}" })
            Assert.Equal(first.Id, Assert.Single((await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?search=" + search))!.Items).Id);
        foreach (var search in new[] { f.CustomerProfile.HoTen, f.Recipient.HoTen })
            Assert.Equal(2, (await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?search=" + Uri.EscapeDataString(search)))!.Items.Length);
        Assert.Equal(2, (await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + $"?customerId={f.CustomerProfile.MaKhachHang}&recipientId={f.Recipient.MaNguoiDuocTiem}"))!.Items.Length);
        Assert.Empty((await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?recipientId=2147483647"))!.Items);
        Assert.Empty((await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?page=3&pageSize=1"))!.Items);
        foreach (var query in new[] { "?page=0", "?pageSize=101", "?page=2147483647&pageSize=100", "?customerId=-1", "?recipientId=0", "?page=abc", "?search=" + new string('x', 101) })
            await Problem(await f.Cashier.Client.GetAsync(ListPath + query), 400, "validation");
        foreach (var id in new[] { 0, -1 }) {
            await Problem(await f.Cashier.Client.GetAsync(Path(id)), 400, "validation");
            await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(id), new { }), 400, "validation");
        }
        await Problem(await f.Cashier.Client.GetAsync(Path(int.MaxValue)), 404, "resourceNotFound");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(int.MaxValue), new { }), 404, "resourceNotFound");
        foreach (var field in new[] { "amount", "employeeId", "transactionCode", "status", "type", "discount", "paidAt", "unexpected" })
            await Problem(await f.Cashier.Client.PostAsync(Path(first.Id), new StringContent("{\"" + field + "\":1}", Encoding.UTF8, "application/json")), 400, "validation");
        foreach (var json in new[] { "{", "null", "[]" })
            await Problem(await f.Cashier.Client.PostAsync(Path(first.Id), new StringContent(json, Encoding.UTF8, "application/json")), 400, "validation");
        Assert.Empty(await f.Db.Payments.ToArrayAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("status")]
    [InlineData("type")]
    [InlineData("method")]
    [InlineData("amount")]
    [InlineData("case")]
    [InlineData("space")]
    public async Task IncompatibleExistingPaymentsAreNeitherAcceptedNorOverwritten(string kind)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var registration = await f.Register();
        var payment = f.Payment(registration);
        if (kind == "status") await f.Db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE dbo.ThanhToan NOCHECK CONSTRAINT CK_ThanhToan_TrangThai, CK_ThanhToan_NhanVien");
        Corrupt(payment, kind); f.Db.Add(payment); await f.Db.SaveChangesAsync();
        var before = JsonSerializer.Serialize(await f.Db.Payments.AsNoTracking().SingleAsync());
        await Problem(await f.Cashier.Client.GetAsync(Path(registration.Id)), 409, "paymentDataConflict");
        await Problem(await f.Customer.Client.GetAsync(Path(registration.Id)), 409, "paymentDataConflict");
        await Problem(await f.Cashier.Client.GetAsync(ListPath), 409, "paymentDataConflict");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount)), 409, "paymentDataConflict");
        await using var fresh = f.NewDb();
        Assert.Equal(before, JsonSerializer.Serialize(await fresh.Payments.SingleAsync()));
        Assert.False(await fresh.Set<SecurityAudit>().AnyAsync(a => a.Action == "payments.recorded"));
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("overflow")]
    [InlineData("fraction")]
    [InlineData("quantity")]
    [InlineData("empty")]
    [InlineData("registrationStatus")]
    [InlineData("promotion")]
    public async Task InvalidStoredRegistrationMoneyFailsClosed(string kind)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var registration = await f.Register("single");
        switch (kind) {
            case "overflow": await f.Db.RegistrationVaccineItems.ExecuteUpdateAsync(s => s.SetProperty(x => x.GiaTaiThoiDiemDangKy, 9999999999999999m)); break;
            case "fraction": await f.Db.RegistrationVaccineItems.ExecuteUpdateAsync(s => s.SetProperty(x => x.GiaTaiThoiDiemDangKy, 1.5m)); break;
            case "quantity": await f.Db.RegistrationVaccineItems.ExecuteUpdateAsync(s => s.SetProperty(x => x.SoLuong, 101)); break;
            case "empty": await f.Db.RegistrationVaccineItems.ExecuteDeleteAsync(); break;
            case "registrationStatus": await f.Db.Registrations.ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Legacy")); break;
            case "promotion":
                var promo = new KhuyenMai { TenKhuyenMai = "[DEMO] not snapshotted", ThoiGianBatDau = DateTime.UtcNow, ThoiGianKetThuc = DateTime.UtcNow.AddDays(1), MucUuDai = 100 };
                f.Db.Add(promo); await f.Db.SaveChangesAsync();
                await f.Db.Registrations.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaKhuyenMai, promo.MaKhuyenMai)); break;
        }
        await Problem(await f.Cashier.Client.GetAsync(Path(registration.Id)), 409, "paymentDataConflict");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalBeforeDiscount)), 409, "paymentDataConflict");
        Assert.Empty(await f.Db.Payments.ToArrayAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("valid")]
    [InlineData("duplicate")]
    [InlineData("status")]
    [InlineData("type")]
    [InlineData("method")]
    [InlineData("amount")]
    [InlineData("case")]
    [InlineData("space")]
    public async Task MigrationPreservesCompatibleDataAndStopsBeforeDdlForConflicts(string kind)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(historicalCatalog: true), async f => {
        var registration = await f.Register();
        var migrator = f.Db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261028172727_G2RegistrationNullableFields");
        var payment = f.Payment(registration);
        if (kind is not ("valid" or "duplicate")) Corrupt(payment, kind);
        await InsertLegacy(f.Db, payment);
        if (kind == "duplicate") await InsertLegacy(f.Db, f.Payment(registration));
        var before = await LegacySnapshot(f.Db);
        if (kind == "valid") {
            await migrator.MigrateAsync(HistoricalCatalogFixture.Latest); await migrator.MigrateAsync(HistoricalCatalogFixture.Latest);
            Assert.Equal(1, await UniqueIndex(f.Db));
            await Assert.ThrowsAsync<SqlException>(() => InsertLegacy(f.Db, f.Payment(registration)));
        } else {
            var error = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(HistoricalCatalogFixture.Latest));
            Assert.Contains(kind == "duplicate" ? "duplicate registration payments" : "incompatible legacy payment", error.Message);
            Assert.Equal(0, await UniqueIndex(f.Db));
            Assert.DoesNotContain("20261029000847_G3SinglePayment", await f.Db.Database.GetAppliedMigrationsAsync());
        }
        Assert.Equal(before, await LegacySnapshot(f.Db));
        Assert.Equal(registration.Subtotal, (await f.Customer.Client.GetFromJsonAsync<RegistrationDto>("/api/v1/registrations/" + registration.Id))!.Subtotal);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task OpenApiHasOnlyApprovedPaymentRoutesAndActualDtos()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        using var json = JsonDocument.Parse(await f.Base.Client.GetStringAsync("/openapi/v1.json"));
        var paths = json.RootElement.GetProperty("paths");
        Assert.Equal(new[] { "get" }, paths.GetProperty(ListPath).EnumerateObject().Select(x => x.Name));
        var detail = paths.GetProperty("/api/v1/registrations/{id}/payment");
        Assert.Equal(new[] { "get", "post" }, detail.EnumerateObject().Select(x => x.Name).Order());
        Assert.Equal(new[] { "post" }, paths.GetProperty("/api/v1/registrations/{id}/payment/confirm").EnumerateObject().Select(x => x.Name));
        var online = paths.GetProperty("/api/v1/registrations/{id}/payment/online");
        Assert.Equal(new[] { "post" }, online.EnumerateObject().Select(x => x.Name));
        Assert.Equal(new[] { "put" }, paths.GetProperty("/api/v1/registrations/{id}/payment/channel").EnumerateObject().Select(x => x.Name));
        Assert.Equal(new[] { "get" }, paths.GetProperty("/api/v1/payment/settings").EnumerateObject().Select(x => x.Name));
        Assert.DoesNotContain(paths.EnumerateObject(), p => p.Name.StartsWith("/api/v1/payments"));
        var schemas = json.RootElement.GetProperty("components").GetProperty("schemas");
        var request = schemas.GetProperty("RecordPaymentRequest");
        Assert.Equal(new[] { "card", "cashTendered", "method", "payer", "transfer" },
            request.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order());
        Assert.False(request.GetProperty("additionalProperties").GetBoolean());
        var onlineRequest = schemas.GetProperty("OnlinePaymentRequest");
        Assert.Equal(new[] { "domestic", "internationalCard", "method", "payer", "termsAccepted", "transfer" },
            onlineRequest.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order());
        Assert.False(onlineRequest.GetProperty("additionalProperties").GetBoolean());
        var props = schemas.GetProperty("RegistrationPaymentDto").GetProperty("properties");
        foreach (var field in new[] { "registrationId", "registrationStatus", "customer", "recipient", "items", "subtotal", "totalDue", "paidAmount", "remainingAmount", "requiresPayment", "canRecordPayment", "isSimulation", "payment", "paymentState", "expectedChannel", "canConfirmPendingPayment" })
            Assert.True(props.TryGetProperty(field, out _), field);
        Assert.Contains("RegistrationPaymentDto", detail.GetProperty("post").GetProperty("responses").GetProperty("201").GetRawText());
        });
    }

    private static Task InsertLegacy(TiemChungDbContext db, ThanhToan p) => db.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT dbo.ThanhToan (MaDangKy, MaNhanVien, SoTien, PhuongThuc, ThoiDiem, LoaiThanhToan, TrangThai)
        VALUES ({p.MaDangKy}, {p.MaNhanVien}, {p.SoTien}, {p.PhuongThuc}, {p.ThoiDiem}, {p.LoaiThanhToan}, {p.TrangThai})
        """);

    private static Task<string> LegacySnapshot(TiemChungDbContext db) => db.Database.SqlQueryRaw<string>("""
        SELECT (SELECT MaThanhToan, MaDangKy, MaNhanVien, SoTien, PhuongThuc, ThoiDiem, LoaiThanhToan, TrangThai
            FROM dbo.ThanhToan ORDER BY MaThanhToan FOR JSON PATH) AS Value
        """).SingleAsync();

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("TienMat", null, null)]
    [InlineData("TheTaiQuay", "VISA", "****4242")]
    [InlineData("ChuyenKhoanQR", "Synthetic Bank", "****1234")]
    public async Task CounterMethodsPersistOnlyReceiptMetadataAndReload(string method, string? provider, string? masked)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register();
            var another = await f.Register();
            var total = registration.TotalBeforeDiscount;
            var payer = new PaymentPayer("  [DEMO] Payer khác liên hệ  ", "0900000107", "payer@example.invalid", "Tỉnh", "Phường", "Địa chỉ");
            var request = new RecordPaymentRequest(payer, method,
                method == "TienMat" ? total + 20000 : null,
                method == "TheTaiQuay" ? new("VISA", "[DEMO] Holder", "4242 4242 4242 4242", 12, 2099, "AUTH-107") : null,
                method == "ChuyenKhoanQR" ? new("Synthetic Bank", "[DEMO] Holder", "SYNTHETIC-1234", "QR-107") : null);
            var url = Path(registration.Id);
            using var post = await f.Cashier.Client.PostAsJsonAsync(url, request);
            Assert.Equal(HttpStatusCode.Created, post.StatusCode);
            var saved = (await post.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!;
            var receipt = saved.Payment!;
            Assert.Equal(payer with { FullName = payer.FullName!.Trim() }, receipt.Payer);
            Assert.Equal(total, receipt.Amount);
            Assert.Equal(f.Cashier.Employee!.MaNhanVien, receipt.EmployeeId);
            Assert.Equal(provider, receipt.Provider); Assert.Equal(masked, receipt.MaskedIdentifier);
            Assert.Equal(request.Card?.AuthorizationReference ?? request.Transfer?.TransferReference, receipt.Reference);
            Assert.Equal(method == "TienMat" ? 20000m : (decimal?)null, receipt.ChangeAmount);
            Assert.Equal(registration.Contact, saved.RegistrationContact);
            Assert.Equal(receipt, (await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(url))!.Payment);

            using var secondPost = await f.Cashier.Client.PostAsJsonAsync(Path(another.Id), Cash(another.TotalBeforeDiscount));
            Assert.Equal(HttpStatusCode.Created, secondPost.StatusCode);
            var second = (await secondPost.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!.Payment!;
            Assert.NotEqual(receipt.TransactionCode, second.TransactionCode);
            await using var fresh = f.NewDb();
            var stored = await fresh.Payments.SingleAsync(p => p.MaDangKy == registration.Id);
            Assert.Equal(receipt.TransactionCode, stored.MaGiaoDich); Assert.Equal(total, stored.SoTien);
            Assert.Equal(provider, stored.NhaCungCapThanhToan); Assert.Equal(masked, stored.SoDinhDanhMasked);
            Assert.Equal(receipt.Reference, stored.ThamChieuThanhToan);
            Assert.Equal(receipt.CashTendered, stored.SoTienKhachDua);
            Assert.Equal(f.CustomerProfile.HoTen, (await fresh.Customers.SingleAsync(c => c.MaKhachHang == f.CustomerProfile.MaKhachHang)).HoTen);
            var serialized = JsonSerializer.Serialize(stored);
            Assert.DoesNotContain("4242424242424242", serialized); Assert.DoesNotContain("4242 4242 4242 4242", serialized);
            Assert.DoesNotContain("SYNTHETIC-1234", serialized);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task InvalidCounterInputsLeavePaymentSlotAndAuditEmpty()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register();
            var total = registration.TotalBeforeDiscount;
            var cash = Cash(total);
            var card = new CounterCard("VISA", "[DEMO] Cardholder", "4242 4242 4242 4242", 12, 2099, "AUTH-107");
            var cardRequest = new RecordPaymentRequest(cash.Payer, "TheTaiQuay", Card: card);
            var transfer = new CounterTransfer("Synthetic Bank", "[DEMO] Holder", "ABC-1234", "QR-107");
            var transferRequest = new RecordPaymentRequest(cash.Payer, "ChuyenKhoanQR", Transfer: transfer);
            foreach (var invalid in new[] {
                cash with { CashTendered = total - 1 }, cash with { CashTendered = total + .5m },
                cash with { CashTendered = CounterPaymentRules.MaxMoney + 1 }, cash with { CashTendered = null },
                cash with { Payer = null }, cash with { Payer = cash.Payer! with { FullName = " " } },
                cash with { Payer = cash.Payer! with { PhoneNumber = "０900000107" } },
                cash with { Payer = cash.Payer! with { Email = "Display <payer@example.invalid>" } },
                cash with { Payer = cash.Payer! with { ProvinceCity = new('x', 101) } },
                cash with { Payer = cash.Payer! with { AddressLine = new('x', 251) } },
                cardRequest with { Card = card with { CardNumber = "4242 4242 4242 4241" } },
                cardRequest with { Card = card with { CardNumber = "4242.4242.4242.4242" } },
                cardRequest with { Card = card with { ExpiryYear = 2020 } },
                cardRequest with { Card = card with { ExpiryMonth = 13 } },
                cardRequest with { Card = card with { AuthorizationReference = new('x', 101) } },
                cardRequest with { Card = card with { CardBrand = "Other" } },
                cardRequest with { CashTendered = total }, cardRequest with { Card = null },
                transferRequest with { Transfer = transfer with { TransferReference = "" } },
                transferRequest with { Transfer = transfer with { SourceAccountOrWalletId = "A/B" } },
                transferRequest with { Transfer = transfer with { SourceBankOrWallet = null } },
                cash with { Method = "MoPhong" }, cash with { Method = "ChuyenKhoanOnline" }
            }) await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), invalid), 400, "validation");
            Assert.False(await f.Db.Payments.AnyAsync());
            Assert.False(await f.Db.Set<SecurityAudit>().AnyAsync(a => a.Action == "payments.recorded"));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PendingTransferIsUnpaidUntilOneCashierConfirmsSameRow()
    {
        var gate = new PaymentGate();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(gate), async f => {
            var direct = await f.Register();
            var registration = await f.Register();
            var url = Path(registration.Id);
            var confirm = url + "/confirm";
            await Problem(await f.Cashier.Client.PostAsync(confirm, null), 409, "paymentNotPending");
            using var recorded = await f.Cashier.Client.PostAsJsonAsync(Path(direct.Id), Cash(direct.TotalBeforeDiscount));
            Assert.Equal(HttpStatusCode.Created, recorded.StatusCode);
            var row = Pending(registration);
            f.Db.Add(row); await f.Db.SaveChangesAsync();
            await f.Db.Registrations.ExecuteUpdateAsync(update => update.SetProperty(r => r.KenhThanhToanDuKien, "TrucTuyen"));
            var pending = (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(url))!;
            Assert.Equal("ChoXacNhan", pending.PaymentState); Assert.Equal("TrucTuyen", pending.ExpectedChannel);
            Assert.Equal(0, pending.PaidAmount); Assert.Equal(pending.TotalDue, pending.RemainingAmount);
            Assert.Null(pending.Payment!.EmployeeId); Assert.False(pending.CanRecordPayment); Assert.True(pending.CanConfirmPendingPayment);
            var summary = (await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath))!.Items.Single(x => x.Id == registration.Id);
            Assert.Equal("ChoXacNhan", summary.PaymentState); Assert.Equal(0, summary.PaidAmount);
            Assert.Equal(pending.TotalDue, summary.RemainingAmount);
            Assert.Equal("TrucTuyen", (await f.Customer.Client.GetFromJsonAsync<RegistrationDto>($"/api/v1/registrations/{registration.Id}"))!.ExpectedChannel);
            Assert.False((await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(url))!.CanConfirmPendingPayment);
            await Problem(await f.Customer.Client.PostAsync(confirm, null), 403, "permissionDenied");
            var reader = await f.Actor("LeTan", "XemThanhToan");
            await Problem(await reader.Client.GetAsync(url),403,"permissionDenied");
            await Problem(await reader.Client.PostAsync(confirm, null), 403, "permissionDenied");
            await Problem(await f.Cashier.Client.PostAsJsonAsync(url, Cash(pending.TotalDue)), 409, "paymentAlreadyRecorded");

            var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var reportPath = $"/api/v1/reports/monthly-revenue?from={today[..7]}&to={today}";
            var before = (await f.Base.Client.GetFromJsonAsync<MonthlyRevenueReportDto>(reportPath))!;
            Assert.Equal(direct.TotalBeforeDiscount, before.TotalRevenue);
            Assert.Equal(1, Assert.Single(before.Items).TransactionCount);

            var other = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
            gate.Armed = true;
            var first = f.Cashier.Client.PostAsync(confirm, null);
            await gate.Locked.Task.WaitAsync(TimeSpan.FromSeconds(20));
            var second = other.Client.PostAsync(confirm, null);
            try {
                await gate.Contending.Task.WaitAsync(TimeSpan.FromSeconds(20));
                Assert.False(second.IsCompleted);
            } finally { gate.Release.TrySetResult(); }
            using var result = await first;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            await Problem(await second, 409, "paymentNotPending");
            var receipt = (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(url))!;
            Assert.Equal("ThanhCong", receipt.PaymentState); Assert.Equal(receipt.TotalDue, receipt.PaidAmount);
            Assert.Equal(0, receipt.RemainingAmount); Assert.False(receipt.CanConfirmPendingPayment);
            Assert.Equal(row.MaThanhToan, receipt.Payment!.Id); Assert.Equal(row.MaGiaoDich, receipt.Payment.TransactionCode);
            Assert.Equal(f.Cashier.Employee!.MaNhanVien, receipt.Payment.EmployeeId); Assert.True(receipt.Payment.OccurredAt.UtcDateTime > row.ThoiDiem);
            await using var fresh = f.NewDb();
            var saved = await fresh.Payments.SingleAsync(p => p.MaDangKy == registration.Id);
            Assert.Equal(row.MaThanhToan, saved.MaThanhToan); Assert.Equal("ThanhCong", saved.TrangThai);
            var audit = await fresh.Set<SecurityAudit>().SingleAsync(a => a.Action == "payments.confirmed");
            Assert.Equal(f.Cashier.Account.MaTaiKhoan, audit.ActorAccountId); Assert.Equal(saved.MaThanhToan.ToString(), audit.ResourceId);
            var after = (await f.Base.Client.GetFromJsonAsync<MonthlyRevenueReportDto>(reportPath))!;
            Assert.Equal(direct.TotalBeforeDiscount + pending.TotalDue, after.TotalRevenue);
            Assert.Equal(2, Assert.Single(after.Items).TransactionCount);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("amount")]
    [InlineData("provider")]
    [InlineData("mask")]
    public async Task CorruptPendingCannotBeReadOrConfirmed(string kind)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register();
            var payment = Pending(registration);
            switch (kind) {
                case "amount": payment.SoTien--; break;
                case "provider": payment.NhaCungCapThanhToan = null; break;
                case "mask": payment.SoDinhDanhMasked = "0901234567"; break;
            }
            f.Db.Add(payment); await f.Db.SaveChangesAsync();
            var url = Path(registration.Id);
            await Problem(await f.Cashier.Client.GetAsync(url), 409, "paymentDataConflict");
            await Problem(await f.Cashier.Client.PostAsync(url + "/confirm", null), 409, "paymentDataConflict");
            await using var fresh = f.NewDb();
            Assert.Null((await fresh.Payments.SingleAsync()).MaNhanVien);
            Assert.False(await fresh.Set<SecurityAudit>().AnyAsync(a => a.Action == "payments.confirmed"));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task B1MigrationRoundTripsLegacyAndProtectsNewRowsAndReceiptHistory()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            const string previous = "20261104094327_RF04ARegistrationSnapshot";
            const string current = "20261104112618_RF04B1CashierPayment";
            var first = await f.Register();
            var second = await f.Register();
            var pendingRegistration = await f.Register();
            var migrator = f.Db.GetService<IMigrator>();
            await migrator.MigrateAsync(previous);
            await InsertLegacy(f.Db, f.Payment(first));
            await InsertLegacy(f.Db, f.Payment(second));
            var before = await LegacySnapshot(f.Db);
            await migrator.MigrateAsync(current);
            Assert.Equal(before, await LegacySnapshot(f.Db));
            // Keep the B1 guard on its historical schema while enabling the current API readback.
            await f.Db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE dbo.NguoiDuocTiem ADD MaHoSo varchar(16) NOT NULL DEFAULT 'VAC-000000000000';
                ALTER TABLE dbo.DangKyDichVu ADD TenKhuyenMaiSnapshot nvarchar(150) NULL, MucGiamSnapshot decimal(18,0) NULL;
                """);
            var legacy = (await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(first.Id)))!.Payment!;
            Assert.Equal("MoPhong", legacy.Method); Assert.Equal(first.TotalBeforeDiscount, legacy.Amount);
            Assert.Null(legacy.TransactionCode); Assert.Null(legacy.Payer);
            Assert.Equal(2, await f.Db.Payments.CountAsync(p => p.MaGiaoDich == null));
            await migrator.MigrateAsync(previous);
            Assert.Equal(before, await LegacySnapshot(f.Db));
            await migrator.MigrateAsync(current);
            await migrator.MigrateAsync(current);
            Assert.False(f.Db.Database.HasPendingModelChanges());

            var pending = Pending(pendingRegistration);
            f.Db.Add(pending); await f.Db.SaveChangesAsync();
            Assert.Null((await f.Db.Payments.AsNoTracking().SingleAsync(p => p.MaDangKy == pendingRegistration.Id)).MaNhanVien);
            await using var fresh = f.NewDb();
            var duplicateCode = Pending(await f.Register());
            duplicateCode.MaGiaoDich = pending.MaGiaoDich;
            fresh.Add(duplicateCode);
            var duplicate = Assert.IsType<SqlException>((await Assert.ThrowsAsync<DbUpdateException>(() => fresh.SaveChangesAsync())).InnerException);
            Assert.Contains("IX_ThanhToan_MaGiaoDich", duplicate.Message);
            var tender = Assert.IsType<SqlException>(await Record.ExceptionAsync(() => f.Db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.ThanhToan SET SoTienKhachDua=1 WHERE MaThanhToan={pending.MaThanhToan}")));
            Assert.Contains("CK_ThanhToan_SoTienKhachDua", tender.Message);
            Assert.Equal(51004, (await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(previous))).Number);
            Assert.Contains(current, await f.Db.Database.GetAppliedMigrationsAsync());
            using var confirmed = await f.Cashier.Client.PostAsync(Path(pendingRegistration.Id) + "/confirm", null);
            Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
            Assert.Equal(51005, (await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(previous))).Number);
            Assert.Equal(3, await f.Db.Payments.CountAsync());
        });
    }

    private static Task<int> UniqueIndex(TiemChungDbContext db) => db.Database.SqlQueryRaw<int>(
        "SELECT CAST(is_unique AS int) AS Value FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.ThanhToan') AND name='IX_ThanhToan_MaDangKy'").SingleAsync();

    private static void Corrupt(ThanhToan payment, string kind)
    {
        switch (kind) {
            case "status": payment.TrangThai = "DangXuLy"; break;
            case "type": payment.LoaiThanhToan = "DatCoc"; break;
            case "method": payment.PhuongThuc = "TienMat"; break;
            case "amount": payment.SoTien -= 1; break;
            case "case": payment.TrangThai = "thanhcong"; break;
            case "space": payment.PhuongThuc = "MoPhong "; break;
        }
    }

    private static async Task Problem(HttpResponseMessage response, int status, string? code = null)
    {
        using (response) {
            Assert.Equal((HttpStatusCode)status, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            var text = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(text);
            if (code is not null) Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
            Assert.True(json.RootElement.TryGetProperty("traceId", out _));
            Assert.DoesNotContain("SqlException", text); Assert.DoesNotContain("ConnectionStrings", text);
        }
    }

    private sealed class PaymentGate : DbCommandInterceptor
    {
        public bool Armed;
        private int _count;
        private Guid _first;
        public TaskCompletionSource Locked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Contending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data,
            InterceptionResult<DbDataReader> result, CancellationToken ct = default)
        {
            if (Armed && command.CommandText.Contains("DangKyDichVu WITH (UPDLOCK,HOLDLOCK)")) {
                if (Interlocked.Increment(ref _count) == 1) _first = data.CommandId;
                else Contending.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData data,
            DbDataReader result, CancellationToken ct = default)
        {
            if (Armed && data.CommandId == _first) {
                Locked.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
            }
            return result;
        }
    }

    private sealed record TestActor(TaiKhoan Account, NhanVien? Employee, IdentitySession Session, HttpClient Client);

    private sealed class Fixture : IAsyncDisposable
    {
        public required CatalogSqlTests.Fixture Base { get; init; }
        public TiemChungDbContext Db => Base.Db;
        public required KhachHang CustomerProfile { get; init; }
        public required NguoiDuocTiem Recipient { get; init; }
        public TestActor Customer { get; private set; } = null!;
        public TestActor Cashier { get; private set; } = null!;
        private WebApplicationFactory<Program>? _factory;
        private readonly List<HttpClient> _clients = [];
        public TiemChungDbContext NewDb() => new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(Db.Database.GetConnectionString()).Options);
        public ThanhToan Payment(RegistrationDto registration) => new() {
            MaDangKy = registration.Id, MaNhanVien = Cashier.Employee!.MaNhanVien, SoTien = registration.Subtotal,
            PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "ThanhCong", ThoiDiem = DateTime.UtcNow
        };
        public async Task<TestActor> Actor(string? title, params string[] permissions)
        {
            var suffix = Guid.NewGuid().ToString("N");
            NhanVien? employee = null;
            if (title is not null) {
                employee = new() { HoTen = "[DEMO] G3 " + title, ChucDanh = title, TrangThai = "Active" };
                Db.Add(employee); await Db.SaveChangesAsync();
            }
            var account = new TaiKhoan {
                AccountType = title is null ? "Customer" : "Employee", MaNhanVien = employee?.MaNhanVien,
                TenDangNhap = "g3-" + suffix, NormalizedUsername = title is null ? null : "G3-" + suffix.ToUpperInvariant(),
                LoginEmail = title is null ? suffix + "@example.invalid" : null,
                NormalizedEmail = title is null ? suffix.ToUpperInvariant() + "@EXAMPLE.INVALID" : null,
                EmailVerifiedAt = DateTimeOffset.UtcNow, TrangThai = "Active", CreatedAt = DateTimeOffset.UtcNow
            };
            account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, Guid.NewGuid().ToString("N"));
            Db.Add(account); await Db.SaveChangesAsync();
            foreach (var code in permissions) Db.Add(new TaiKhoan_QuyenHan {
                MaTaiKhoan = account.MaTaiKhoan, MaQuyen = (await Db.Set<QuyenHan>().SingleAsync(p => p.Code == code)).MaQuyen
            });
            var session = new IdentitySession { Id = Guid.NewGuid(), AccountId = account.MaTaiKhoan,
                SecurityVersion = account.SecurityVersion, CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };
            Db.Add(session); await Db.SaveChangesAsync();
            var client = (_factory ?? Base.Factory).CreateClient(); _clients.Add(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", new IdentityTokens(Base.Config).Issue(account.MaTaiKhoan, session.Id, DateTimeOffset.UtcNow));
            return new(account, employee, session, client);
        }
        public async Task<RegistrationDto> Register(string kind = "mixed", int? promotionId = null)
        {
            var vaccine = await Db.Vaccines.AsNoTracking().OrderBy(v => v.MaVacXin).FirstAsync();
            var package = await Db.Set<GoiVacXin>().AsNoTracking().SingleAsync();
            var components = await Db.Set<GoiVacXin_VacXin>().Select(x => new RegistrationComponentRequest(x.MaVacXin, x.SoLuong)).ToArrayAsync();
            var single = new RegistrationItemRequest("vaccine", vaccine.MaVacXin, 2, vaccine.Gia);
            var bundled = new RegistrationItemRequest("package", package.MaGoi, 2, package.GiaGoi, components);
            var request = RegistrationSqlTests.SyntheticRequest(CustomerProfile.MaKhachHang, Recipient.MaNguoiDuocTiem,
                kind == "single" ? [single] : kind == "package" ? [bundled] : [single, bundled]);
            using var response = await Customer.Client.PostAsJsonAsync("/api/v1/registrations", request with { PromotionId = promotionId });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<RegistrationDto>())!;
        }
        public static async Task<Fixture> Create(PaymentGate? gate = null, bool historicalCatalog = false, string? consultationFee = null)
        {
            var b = await CatalogSqlTests.Fixture.Create(historicalCatalog: historicalCatalog);
            Fixture? partial = null;
            try {
            await b.Login();
            if (historicalCatalog) await HistoricalCatalogFixture.SeedAsync(b.Db); else await SyntheticCatalogFixture.RunAsync(b.Db, new SeedEnvironment());
            var customer = new KhachHang { HoTen = "[DEMO] G3 Customer", Email = "g3@example.invalid" };
            b.Db.Add(customer); await b.Db.SaveChangesAsync();
            var recipient = new NguoiDuocTiem { HoTen = "[DEMO] G3 Recipient", MaKhachHang = customer.MaKhachHang,
                NgaySinh = new(2020, 1, 1), GioiTinh = "Khác", QuanHeVoiKhachHang = "Con" };
            b.Db.Add(recipient); await b.Db.SaveChangesAsync();
            var f = new Fixture { Base = b, CustomerProfile = customer, Recipient = recipient };
            partial = f;
            if (gate is not null) f._factory = b.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
                services.RemoveAll<DbContextOptions<TiemChungDbContext>>(); services.RemoveAll<TiemChungDbContext>();
                services.AddDbContext<TiemChungDbContext>(o => o.UseSqlServer(b.Db.Database.GetConnectionString()).AddInterceptors(gate));
            }));
            if (consultationFee is not null) {
                f._factory = b.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?> { ["Registration:ConsultationFeeVnd"] = consultationFee })));
            }
            f.Customer = await f.Actor(null); f.Cashier = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
            b.Db.AddRange(new AccountProfileLink { AccountId = f.Customer.Account.MaTaiKhoan, CustomerId = customer.MaKhachHang },
                new AccountProfileLink { AccountId = f.Customer.Account.MaTaiKhoan, RecipientId = recipient.MaNguoiDuocTiem });
            await b.Db.SaveChangesAsync(); return f;
            } catch (Exception failure) {
                await SqlFixtureLifecycle.CleanupAfterFailureAsync(failure, () => partial is null ? b.DisposeAsync() : partial.DisposeAsync());
                throw;
            }
        }
        public ValueTask DisposeAsync() => SqlFixtureLifecycle.CleanupAsync(
            () => SqlFixtureLifecycle.CleanupAsync(_clients.Select<HttpClient, Func<ValueTask>>(client => () => {
                client.Dispose(); return ValueTask.CompletedTask;
            }).ToArray()),
            () => _factory?.DisposeAsync() ?? ValueTask.CompletedTask,
            Base.DisposeAsync);
    }

    private sealed class SeedEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "G3Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
