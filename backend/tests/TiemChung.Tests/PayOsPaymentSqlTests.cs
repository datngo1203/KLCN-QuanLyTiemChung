using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using PayOS.Exceptions;
using TiemChung.Api.Payments;
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

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsCreateReservesOneServerOwnedIntentAndLocalGetDoesNotCallProvider()
    {
        var provider = new FakePayOsProvider { BlockCreate = true };
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            var firstTask = f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            try {
                await provider.CreateEntered.Task.WaitAsync(TimeSpan.FromSeconds(30));
                using var unknown = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
                Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
                Assert.Equal("Preparing", (await unknown.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);

                var cashierTask = f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), Cash(registration.TotalDue));
                var completed = await Task.WhenAny(cashierTask, Task.Delay(TimeSpan.FromSeconds(10)));
                Assert.Same(cashierTask, completed);
                await Problem(await cashierTask, 409, "paymentAlreadyRecorded");

                using var concurrent = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
                Assert.Equal("Preparing", (await concurrent.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
                Assert.Equal(1, provider.CreateCalls);
            } finally {
                provider.ReleaseCreate.TrySetResult();
            }

            using var first = await firstTask;
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var pending = (await first.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!;
            Assert.Equal("Pending", pending.State);
            Assert.Equal(registration.TotalDue, pending.Amount);
            Assert.Equal("VND", pending.Currency);
            Assert.Equal("https://example.invalid/checkout", pending.CheckoutUrl);
            Assert.Equal("qr-data", pending.QrCode);
            Assert.Equal(1, provider.CreateCalls);
            Assert.Equal(registration.TotalDue, provider.LastRequest!.Amount);
            Assert.DoesNotContain("payer", provider.LastRequest.Description, StringComparison.OrdinalIgnoreCase);

            var readsBeforeLocalGet = provider.ReadCalls;
            using var local = await f.Customer.Client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, local.StatusCode);
            Assert.Equal(pending, (await local.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!);
            Assert.Equal(readsBeforeLocalGet, provider.ReadCalls);

            using var unexpected = await f.Customer.Client.PostAsJsonAsync(path,
                new { payer = PayOsRequest.Payer, termsAccepted = true, amount = 1, orderCode = 4 });
            Assert.Equal(HttpStatusCode.BadRequest, unexpected.StatusCode);
            Assert.Equal(1, provider.CreateCalls);

            using var fresh = f.NewDb();
            var saved = await fresh.Payments.AsNoTracking().SingleAsync();
            Assert.Equal("PayOsQr", saved.PhuongThuc);
            Assert.Equal("Pending", saved.PayOsLifecycleState);
            Assert.Equal(registration.TotalDue, saved.SoTien);
            Assert.Equal(provider.LastRequest.OrderCode, saved.PayOsOrderCode);
            Assert.Equal("Success", (await fresh.Set<SecurityAudit>()
                .SingleAsync(a => a.Action == "payments.payosReserved")).Outcome);
            Assert.Equal("Success", (await fresh.Set<SecurityAudit>()
                .SingleAsync(a => a.Action == "payments.payosPending")).Outcome);
            Assert.Single(await fresh.Payments.ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ProviderNotFoundRetainsUnknownIdentityAndNeverAllowsAnotherCreate()
    {
        var provider = new FakePayOsProvider { CreateFailure = new TimeoutException("synthetic create timeout") };
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            using var response = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var unknown = (await response.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!;
            Assert.Equal("OutcomeUnknown", unknown.State);
            Assert.Null(unknown.CheckoutUrl);
            Assert.Null(unknown.QrCode);
            Assert.Equal(1, provider.CreateCalls);

            using var retry = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal("OutcomeUnknown", (await retry.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            Assert.Equal(1, provider.CreateCalls);

            provider.ReturnNotFound = true;
            using var notFound = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            Assert.Equal(HttpStatusCode.OK, notFound.StatusCode);
            var retained = (await notFound.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!;
            Assert.Equal("OutcomeUnknown", retained.State);
            Assert.True(retained.CanReconcile);
            using var retainedDb = f.NewDb();
            var intent = await retainedDb.Payments.AsNoTracking().SingleAsync();
            Assert.Equal(provider.LastRequest!.OrderCode, intent.PayOsOrderCode);
            Assert.Equal("CreateOutcomeUnknown", intent.PayOsLifecycleState);
            using var stillUnknown = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal("OutcomeUnknown",
                (await stillUnknown.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            Assert.Equal(1, provider.CreateCalls);
            provider.ReturnNotFound = false;

            provider.RemoteStatus = "PENDING";
            using var pending = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            Assert.Equal(HttpStatusCode.OK, pending.StatusCode);
            Assert.Equal("OutcomeUnknown", (await pending.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);

            await ReconcileAndAssertNoPaymentMutation(f, provider, path, registration, "wrong-order");
            await ReconcileAndAssertNoPaymentMutation(f, provider, path, registration, "wrong-link");
            await ReconcileAndAssertNoPaymentMutation(f, provider, path, registration, "paid-without-reference");

            provider.ReadFailure = new TimeoutException("synthetic read timeout");
            using var unavailable = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            await Problem(unavailable, 503, "paymentProviderUnavailable");
            Assert.Equal("OutcomeUnknown",
                (await f.Customer.Client.GetFromJsonAsync<PayOsPaymentStatusDto>(path))!.State);
            provider.ReadFailure = null;

            provider.RemoteAmountOffset = 1;
            using var mismatch = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            await Problem(mismatch, 409, "paymentProviderConflict");
            Assert.Equal("OutcomeUnknown",
                (await f.Customer.Client.GetFromJsonAsync<PayOsPaymentStatusDto>(path))!.State);
            provider.RemoteAmountOffset = 0;

            provider.RemoteStatus = "PAID";
            using var paidResponse = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            Assert.Equal(HttpStatusCode.OK, paidResponse.StatusCode);
            Assert.Equal("Paid", (await paidResponse.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            var detail = await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
            Assert.Equal(registration.TotalDue, detail!.PaidAmount);
            Assert.Equal(0, detail.RemainingAmount);
            Assert.Equal(8, provider.ReadCalls);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PendingNotFoundConflictsWithoutDowngradingAndPaidReconcileIsLocalOnly()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            using var create = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal("Pending", (await create.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            provider.ReturnNotFound = true;

            using var reconcile = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            await Problem(reconcile, 409, "paymentProviderConflict");
            using var fresh = f.NewDb();
            Assert.Equal("Pending", (await fresh.Payments.AsNoTracking().SingleAsync()).PayOsLifecycleState);
            Assert.Equal(1, provider.CreateCalls);

            provider.ReturnNotFound = false;
            provider.RemoteStatus = "PAID";
            provider.RemoteReference = "confirmed-reference";
            using var paid = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            Assert.Equal("Paid", (await paid.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            var readCalls = provider.ReadCalls;
            using var paidAgain = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            Assert.Equal("Paid", (await paidAgain.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            Assert.Equal(readCalls, provider.ReadCalls);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("wrong-order")]
    [InlineData("wrong-amount")]
    [InlineData("invalid-link")]
    [InlineData("invalid-checkout")]
    [InlineData("invalid-qr")]
    public async Task MismatchedCreateResponseStaysUnknownAndDoesNotRetry(string mismatch)
    {
        var provider = new FakePayOsProvider { CreateResponseMismatch = mismatch };
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            using var first = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal("OutcomeUnknown", (await first.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            using var second = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal("OutcomeUnknown", (await second.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            Assert.Equal(1, provider.CreateCalls);
            using var db = f.NewDb();
            var saved = await db.Payments.AsNoTracking().SingleAsync();
            Assert.Equal("CreateOutcomeUnknown", saved.PayOsLifecycleState);
            Assert.Null(saved.PayOsPaymentLinkId);
            Assert.Equal("ChoXacNhan", saved.TrangThai);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("rate-limit")]
    [InlineData("server-error")]
    [InlineData("invalid-signature")]
    [InlineData("bad-request")]
    [InlineData("unauthorized")]
    [InlineData("forbidden")]
    public async Task ProviderCreateFailuresAreConservativelyUnknownAndNeverRetried(string failure)
    {
        var provider = new FakePayOsProvider { CreateFailure = CreateFailure(failure) };
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            using var first = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal("OutcomeUnknown", (await first.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            using var second = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal("OutcomeUnknown", (await second.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            Assert.Equal(1, provider.CreateCalls);
            using var db = f.NewDb();
            Assert.Equal("CreateOutcomeUnknown", (await db.Payments.AsNoTracking().SingleAsync()).PayOsLifecycleState);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task InvalidSignatureDuringReconcileDoesNotMutateLocalPayment()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            using var created = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal("Pending", (await created.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);

            provider.ReadFailure = new InvalidSignatureException("invalid signature");
            using var reconcile = await f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            await Problem(reconcile, 503, "paymentProviderUnavailable");

            var detail = await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
            Assert.Equal("ChoXacNhan", detail!.PaymentState);
            Assert.Equal(0, detail.PaidAmount);
            Assert.Equal(registration.TotalDue, detail.RemainingAmount);
            Assert.Equal("Pending", (await f.Customer.Client.GetFromJsonAsync<PayOsPaymentStatusDto>(path))!.State);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ProviderCreateAmountUsesConsultationFeeAndPromotionSnapshot()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(consultationFee: "50000", payOsProvider: provider), async f => {
            var admin = await f.Actor("Admin", "QuanLyKhuyenMai");
            var promotionRequest = new UpsertPromotionRequest("Ưu đãi thử", "Synthetic",
                DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), 70000m, true);
            using var promotionResponse = await admin.Client.PostAsJsonAsync("/api/v1/promotions", promotionRequest);
            Assert.Equal(HttpStatusCode.Created, promotionResponse.StatusCode);
            var promotion = (await promotionResponse.Content.ReadFromJsonAsync<PromotionDto>())!;
            var registration = await f.Register("single", promotion.Id);
            Assert.Equal(registration.Subtotal - 70000m + 50000m, registration.TotalDue);

            using var response = await f.Customer.Client.PostAsJsonAsync(
                $"/api/v1/registrations/{registration.Id}/payment/payos", PayOsRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(registration.TotalDue, provider.LastRequest!.Amount);
            Assert.Equal(registration.TotalDue, (await response.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.Amount);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task UnverifiedCustomerCannotCreateOrReconcilePayOsPayment()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == f.Customer.Account.MaTaiKhoan)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.EmailVerifiedAt, (DateTimeOffset?)null));
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            await Problem(await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest), 403, "emailVerificationRequired");
            await Problem(await f.Customer.Client.PostAsync(path + "/reconcile", content: null),
                403, "emailVerificationRequired");
            Assert.Equal(0, provider.CreateCalls);
            Assert.Equal(0, provider.ReadCalls);
            using var db = f.NewDb();
            Assert.Empty(await db.Payments.ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsRoutesRequireOwnershipAndDoNotCreateForForeignRegistration()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var otherCustomer = await f.Actor(null);
            using var anonymousClient = f.Base.Factory.CreateClient();
            using var anonymous = await anonymousClient.PostAsJsonAsync(
                $"/api/v1/registrations/{registration.Id}/payment/payos", PayOsRequest);
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            using var foreign = await otherCustomer.Client.PostAsJsonAsync(
                $"/api/v1/registrations/{registration.Id}/payment/payos", PayOsRequest);
            Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
            using var foreignRead = await otherCustomer.Client.GetAsync(
                $"/api/v1/registrations/{registration.Id}/payment/payos");
            Assert.Equal(HttpStatusCode.Forbidden, foreignRead.StatusCode);
            provider.IsReady = false;
            using var disabled = await f.Customer.Client.PostAsJsonAsync(
                $"/api/v1/registrations/{registration.Id}/payment/payos", PayOsRequest);
            await Problem(disabled, 503, "paymentProviderUnavailable");
            using var fresh = f.NewDb();
            Assert.Empty(await fresh.Payments.ToArrayAsync());
            Assert.Equal(0, provider.CreateCalls);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("malformed")]
    [InlineData("invalid-signature")]
    [InlineData("oversized")]
    [InlineData("wrong-content-type")]
    public async Task PayOsWebhookRejectsInvalidInputBeforePaymentMutation(string input)
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            if (input == "invalid-signature") {
                using var create = await f.Customer.Client.PostAsJsonAsync(
                    $"/api/v1/registrations/{registration.Id}/payment/payos", PayOsRequest);
                Assert.Equal("Pending", (await create.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            }
            using var anonymous = f.AnonymousClient();
            using var response = input switch {
                "malformed" => await PostWebhookAsync(anonymous, "{"),
                "invalid-signature" => await PostWebhookAsync(anonymous,
                    """{"signature":"invalid-signature","data":{"accountNumber":"raw-sentinel"}}"""),
                "oversized" => await PostWebhookAsync(anonymous,
                    """{"signature":"valid-signature","data":" """ + new string('x', 65_540) + """ "}""", chunked: true),
                "wrong-content-type" => await PostWebhookAsync(anonymous, WebhookBody("raw-sentinel"), contentType: "text/plain"),
                _ => throw new ArgumentOutOfRangeException(nameof(input))
            };
            if (input == "wrong-content-type") {
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            } else {
                await Problem(response, 400, "paymentWebhookInvalid");
            }
            Assert.Equal(input == "malformed" || input == "invalid-signature" ? 1 : 0, provider.VerifyCalls);
            using var db = f.NewDb();
            if (input == "invalid-signature") {
                var unchanged = await db.Payments.SingleAsync();
                Assert.Equal("Pending", unchanged.PayOsLifecycleState);
                Assert.Equal("ChoXacNhan", unchanged.TrangThai);
            } else Assert.Empty(await db.Payments.ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsWebhookRequiresReadyProviderAndDoesNotMutateWhenUnavailable()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            using var create = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal("Pending", (await create.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            var payment = await f.Db.Payments.AsNoTracking().SingleAsync();
            provider.WebhookResult = Success(payment, "provider-reference");
            provider.IsReady = false;

            using var anonymous = f.AnonymousClient();
            await Problem(await PostWebhookAsync(anonymous, WebhookBody()), 503, "paymentProviderUnavailable");
            var unchanged = await f.Db.Payments.AsNoTracking().SingleAsync();
            Assert.Equal("Pending", unchanged.PayOsLifecycleState);
            Assert.Equal(0, provider.VerifyCalls);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("Pending")]
    [InlineData("Creating")]
    [InlineData("CreateOutcomeUnknown")]
    public async Task VerifiedPayOsWebhookCompletesAllowedLocalLifecycle(string lifecycle)
    {
        var provider = new FakePayOsProvider {
            BlockCreate = lifecycle == "Creating",
            CreateFailure = lifecycle == "CreateOutcomeUnknown" ? new TimeoutException("synthetic create timeout") : null
        };
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            Task<HttpResponseMessage>? createTask = null;
            if (lifecycle == "Creating") {
                createTask = f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
                await provider.CreateEntered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            } else {
                using var create = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
                Assert.Equal(lifecycle == "Pending" ? "Pending" : "OutcomeUnknown",
                    (await create.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            }

            var pending = await f.Db.Payments.AsNoTracking().SingleAsync();
            Assert.Equal(lifecycle, pending.PayOsLifecycleState);
            provider.WebhookResult = Success(pending, "provider-reference");
            var createCallsBeforeWebhook = provider.CreateCalls;
            var readCallsBeforeWebhook = provider.ReadCalls;
            using var anonymous = f.AnonymousClient();
            try {
                var rawBody = WebhookBody("raw-payload-sentinel");
                using var response = await PostWebhookAsync(anonymous, rawBody);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
                var responseText = await response.Content.ReadAsStringAsync();
                Assert.Contains("received", responseText);
                Assert.DoesNotContain("raw-payload-sentinel", responseText);
                Assert.DoesNotContain("provider-link-id", responseText);
                Assert.Equal(rawBody, provider.LastWebhookRawJson);
                Assert.Equal(createCallsBeforeWebhook, provider.CreateCalls);
                Assert.Equal(readCallsBeforeWebhook, provider.ReadCalls);
            } finally {
                provider.ReleaseCreate.TrySetResult();
            }
            if (createTask is not null) {
                using var created = await createTask;
                Assert.Equal("Paid", (await created.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            }

            var paid = await f.Db.Payments.AsNoTracking().SingleAsync();
            Assert.Equal("Paid", paid.PayOsLifecycleState);
            Assert.Equal("ThanhCong", paid.TrangThai);
            Assert.Null(paid.MaNhanVien);
            Assert.Equal(provider.WebhookResult!.PaymentLinkId, paid.PayOsPaymentLinkId);
            Assert.Equal(provider.WebhookResult.Reference, paid.PayOsReference);
            var audit = await f.Db.Set<SecurityAudit>().SingleAsync(a => a.Action == "payments.payosWebhookPaid");
            Assert.Equal("Payment", audit.ResourceType);
            Assert.Equal(paid.MaThanhToan.ToString(), audit.ResourceId);
            Assert.Null(audit.ActorAccountId);
            Assert.Null(audit.SessionId);
            Assert.Equal("Success", audit.Outcome);
            Assert.False(string.IsNullOrWhiteSpace(audit.TraceId));
            Assert.DoesNotContain("raw-payload-sentinel", audit.ChangedFields ?? "");
            Assert.DoesNotContain("raw-payload-sentinel", audit.TraceId);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsWebhookAcknowledgesUnknownAndUnsuccessfulEventsWithoutCreatingPayments()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            using var anonymous = f.AnonymousClient();
            provider.WebhookResult = new(987_654_321, (long)registration.TotalDue, "sample-link",
                "sample-reference", true, "00");
            using var unknown = await PostWebhookAsync(anonymous, WebhookBody());
            Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
            Assert.Empty(await f.Db.Payments.ToArrayAsync());

            using var create = await f.Customer.Client.PostAsJsonAsync(
                $"/api/v1/registrations/{registration.Id}/payment/payos", PayOsRequest);
            var pending = await f.Db.Payments.AsNoTracking().SingleAsync();
            provider.WebhookResult = Success(pending, null) with { IsSuccessful = false, ProviderCode = "21" };
            using var unsuccessful = await PostWebhookAsync(anonymous, WebhookBody());
            Assert.Equal(HttpStatusCode.OK, unsuccessful.StatusCode);
            var unchanged = await f.Db.Payments.AsNoTracking().SingleAsync();
            Assert.Equal("Pending", unchanged.PayOsLifecycleState);
            Assert.Equal("ChoXacNhan", unchanged.TrangThai);
            Assert.Empty(await f.Db.Set<SecurityAudit>().Where(a => a.Action == "payments.payosWebhookPaid").ToArrayAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("amount")]
    [InlineData("link")]
    [InlineData("reference")]
    public async Task PayOsWebhookRejectsSuccessfulEvidenceMismatch(string mismatch)
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            using var create = await f.Customer.Client.PostAsJsonAsync(
                $"/api/v1/registrations/{registration.Id}/payment/payos", PayOsRequest);
            var payment = await f.Db.Payments.AsNoTracking().SingleAsync();
            var evidence = Success(payment, "provider-reference");
            provider.WebhookResult = mismatch switch {
                "amount" => evidence with { Amount = evidence.Amount + 1 },
                "link" => evidence with { PaymentLinkId = "different-link" },
                "reference" => evidence with { Reference = " " },
                _ => throw new ArgumentOutOfRangeException(nameof(mismatch))
            };

            using var anonymous = f.AnonymousClient();
            await Problem(await PostWebhookAsync(anonymous, WebhookBody()), 409, "paymentProviderConflict");
            var unchanged = await f.Db.Payments.AsNoTracking().SingleAsync();
            Assert.Equal(payment.PayOsLifecycleState, unchanged.PayOsLifecycleState);
            Assert.Equal(payment.PayOsPaymentLinkId, unchanged.PayOsPaymentLinkId);
            Assert.Null(unchanged.PayOsReference);
            Assert.Equal("ChoXacNhan", unchanged.TrangThai);
            Assert.Empty(await f.Db.Set<SecurityAudit>().Where(a => a.Action == "payments.payosWebhookPaid").ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsWebhookDoesNotMutateCorruptNonPayOsRow()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            using var create = await f.Customer.Client.PostAsJsonAsync(
                $"/api/v1/registrations/{registration.Id}/payment/payos", PayOsRequest);
            var payment = await f.Db.Payments.AsNoTracking().SingleAsync();
            provider.WebhookResult = Success(payment, "provider-reference");
            await f.Db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE [ThanhToan] NOCHECK CONSTRAINT [CK_ThanhToan_PayOsQr]");
            await f.Db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE [ThanhToan] NOCHECK CONSTRAINT [CK_ThanhToan_PayOsFields]");
            try {
                await f.Db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [ThanhToan] SET [PhuongThuc] = {"ChuyenKhoanOnline"} WHERE [MaThanhToan] = {payment.MaThanhToan}");
                using var anonymous = f.AnonymousClient();
                await Problem(await PostWebhookAsync(anonymous, WebhookBody()), 409, "paymentDataConflict");
            } finally {
                await f.Db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [ThanhToan] SET [PhuongThuc] = {"PayOsQr"} WHERE [MaThanhToan] = {payment.MaThanhToan}");
                await f.Db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE [ThanhToan] WITH CHECK CHECK CONSTRAINT [CK_ThanhToan_PayOsQr]");
                await f.Db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE [ThanhToan] WITH CHECK CHECK CONSTRAINT [CK_ThanhToan_PayOsFields]");
            }
            Assert.Empty(await f.Db.Set<SecurityAudit>().Where(a => a.Action == "payments.payosWebhookPaid").ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsWebhookPaidFactDrivesPaymentAppointmentAndRevenueExactlyOnce()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            using var create = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            Assert.Equal("Pending", (await create.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            var reception = await f.Actor("LeTan", "QuanLyLichHen");
            var sourcePath = $"/api/v1/appointments/sources/{registration.Id}";
            Assert.Equal("pending",
                (await reception.Client.GetFromJsonAsync<AppointmentSourceDetailDto>(sourcePath))!.Source.ReadinessCode);
            var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd");
            var reportPath = $"/api/v1/reports/monthly-revenue?from={today[..7]}&to={today}";
            async Task<decimal> Revenue() =>
                (await f.Base.Client.GetFromJsonAsync<MonthlyRevenueReportDto>(reportPath))!.TotalRevenue;
            Assert.Equal(0, await Revenue());
            var appointmentRequest = new CreateAppointmentRequest(registration.Id,
                registration.Items[0].Kind, registration.Items[0].Id,
                DateTimeOffset.UtcNow.AddDays(2).ToString("O"));
            await Problem(await reception.Client.PostAsJsonAsync("/api/v1/appointments", appointmentRequest),
                409, "appointmentPaymentRequired");

            var payment = await f.Db.Payments.AsNoTracking().SingleAsync();
            using var anonymous = f.AnonymousClient();
            provider.WebhookResult = Success(payment, null) with {
                IsSuccessful = false,
                ProviderCode = "21"
            };
            using (var unsuccessful = await PostWebhookAsync(anonymous, WebhookBody()))
                Assert.Equal(HttpStatusCode.OK, unsuccessful.StatusCode);
            provider.WebhookResult = Success(payment, "provider-reference") with {
                Amount = (long)payment.SoTien + 1
            };
            await Problem(await PostWebhookAsync(anonymous, WebhookBody()), 409, "paymentProviderConflict");
            Assert.Equal(0, await Revenue());
            Assert.Equal("pending",
                (await reception.Client.GetFromJsonAsync<AppointmentSourceDetailDto>(sourcePath))!.Source.ReadinessCode);

            provider.WebhookResult = Success(payment, "provider-reference");
            using var webhook = await PostWebhookAsync(anonymous, WebhookBody());
            Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);

            var receipt = await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
            Assert.Equal("ThanhCong", receipt!.PaymentState);
            Assert.Equal(registration.TotalDue, receipt.PaidAmount);
            Assert.Equal(0, receipt.RemainingAmount);
            Assert.Equal("paid",
                (await reception.Client.GetFromJsonAsync<AppointmentSourceDetailDto>(sourcePath))!.Source.ReadinessCode);
            using var appointment = await reception.Client.PostAsJsonAsync("/api/v1/appointments", appointmentRequest);
            Assert.Equal(HttpStatusCode.Created, appointment.StatusCode);
            Assert.Equal(registration.TotalDue, await Revenue());
            var paid = await f.Db.Payments.AsNoTracking().SingleAsync();
            var occurredAt = paid.ThoiDiem;

            var duplicateResponses = await Task.WhenAll(
                PostWebhookAsync(anonymous, WebhookBody()), PostWebhookAsync(anonymous, WebhookBody()));
            foreach (var duplicate in duplicateResponses) {
                using (duplicate) Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
            }
            var afterDuplicate = await f.Db.Payments.AsNoTracking().SingleAsync();
            Assert.Equal(occurredAt, afterDuplicate.ThoiDiem);
            Assert.Equal(registration.TotalDue, await Revenue());
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "payments.payosWebhookPaid"));

            provider.WebhookResult = provider.WebhookResult with { Reference = "conflicting-reference" };
            await Problem(await PostWebhookAsync(anonymous, WebhookBody()), 409, "paymentProviderConflict");
            Assert.Equal(occurredAt, (await f.Db.Payments.AsNoTracking().SingleAsync()).ThoiDiem);
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "payments.payosWebhookPaid"));

            provider.WebhookResult = provider.WebhookResult with { IsSuccessful = false, ProviderCode = "21" };
            using var staleUnsuccessful = await PostWebhookAsync(anonymous, WebhookBody());
            Assert.Equal(HttpStatusCode.OK, staleUnsuccessful.StatusCode);
            Assert.Equal("Paid", (await f.Db.Payments.AsNoTracking().SingleAsync()).PayOsLifecycleState);
            Assert.Equal(registration.TotalDue, await Revenue());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PayOsWebhookSerializesWithReconcileWithoutDoublePaidAudit()
    {
        var provider = new FakePayOsProvider();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(payOsProvider: provider), async f => {
            var registration = await f.Register("single");
            var path = $"/api/v1/registrations/{registration.Id}/payment/payos";
            using var create = await f.Customer.Client.PostAsJsonAsync(path, PayOsRequest);
            var payment = await f.Db.Payments.AsNoTracking().SingleAsync();
            provider.RemoteStatus = "PAID";
            provider.RemoteReference = "provider-reference";
            provider.BlockRead = true;
            var reconcileTask = f.Customer.Client.PostAsync(path + "/reconcile", content: null);
            await provider.ReadEntered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            provider.WebhookResult = Success(payment, "provider-reference");
            using var anonymous = f.AnonymousClient();
            try {
                using var webhook = await PostWebhookAsync(anonymous, WebhookBody());
                Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
            } finally {
                provider.ReleaseRead.TrySetResult();
            }
            using var reconcile = await reconcileTask;
            Assert.Equal(HttpStatusCode.OK, reconcile.StatusCode);
            Assert.Equal("Paid", (await reconcile.Content.ReadFromJsonAsync<PayOsPaymentStatusDto>())!.State);
            Assert.Equal("Paid", (await f.Db.Payments.AsNoTracking().SingleAsync()).PayOsLifecycleState);
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a =>
                a.Action == "payments.payosWebhookPaid" || a.Action == "payments.payosReconciledPaid"));
        });
    }

    private static readonly PayOsPaymentRequest PayOsRequest = new(
        new("[DEMO] PayOS payer", "0900000107", "payos@example.invalid",
            "Tỉnh thử nghiệm", "Phường thử nghiệm", "Địa chỉ thử nghiệm"), true);

    private static Exception CreateFailure(string name)
    {
        var details = new Dictionary<string, object>();
        return name switch {
            "rate-limit" => new TooManyRequestsException("rate limited", "429", details),
            "server-error" => new InternalServerErrorException(500, "500", "provider failure", details),
            "invalid-signature" => new InvalidSignatureException("invalid signature"),
            "bad-request" => new BadRequestException("bad request", "400", details),
            "unauthorized" => new UnauthorizedException("unauthorized", "401", details),
            "forbidden" => new ForbiddenException("forbidden", "403", details),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
    }

    private static async Task ReconcileAndAssertNoPaymentMutation(Fixture f, FakePayOsProvider provider,
        string path, RegistrationDto registration, string mismatch)
    {
        provider.RemoteStatus = "PAID";
        provider.RemoteOrderCodeOffset = mismatch == "wrong-order" ? 1 : 0;
        provider.RemotePaymentLinkId = mismatch == "wrong-link" ? "different-link" : "provider-link-id";
        provider.RemoteReference = mismatch == "paid-without-reference" ? null : "provider-reference";
        var expectedCode = mismatch == "paid-without-reference"
            ? "paymentProviderStateUnsupported" : "paymentProviderConflict";
        await Problem(await f.Customer.Client.PostAsync(path + "/reconcile", content: null), 409, expectedCode);

        var detail = await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.Equal("ChoXacNhan", detail!.PaymentState);
        Assert.Equal(0, detail.PaidAmount);
        Assert.Equal(registration.TotalDue, detail.RemainingAmount);
        Assert.Equal("OutcomeUnknown",
            (await f.Customer.Client.GetFromJsonAsync<PayOsPaymentStatusDto>(path))!.State);
        provider.RemoteOrderCodeOffset = 0;
        provider.RemotePaymentLinkId = "provider-link-id";
        provider.RemoteReference = "provider-reference";
    }

    private static PayOsVerifiedWebhook Success(ThanhToan payment, string? reference) =>
        new(payment.PayOsOrderCode!.Value, (long)payment.SoTien, "provider-link-id", reference, true, "00");

    private static string WebhookBody(string sentinel = "synthetic-payload") =>
        "{\"signature\":\"valid-signature\",\"data\":{\"accountNumber\":\"" + sentinel +
        "\",\"counterAccountNumber\":\"000111222\"}}";

    private static async Task<HttpResponseMessage> PostWebhookAsync(HttpClient client, string raw,
        string contentType = "application/json", bool chunked = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payment/payos/webhook") {
            Content = chunked ? new UnknownLengthJsonContent(raw, contentType) :
                new StringContent(raw, Encoding.UTF8, contentType)
        };
        if (chunked) request.Headers.TransferEncodingChunked = true;
        return await client.SendAsync(request);
    }

    private sealed class UnknownLengthJsonContent : HttpContent
    {
        private readonly string _value;

        public UnknownLengthJsonContent(string value, string contentType)
        {
            _value = value;
            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context) =>
            stream.WriteAsync(Encoding.UTF8.GetBytes(_value)).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

    }

    private sealed class FakePayOsProvider : IPayOsPaymentProvider
    {
        public bool IsReady { get; set; } = true;
        public bool BlockCreate { get; set; }
        public bool BlockRead { get; set; }
        public Exception? CreateFailure { get; set; }
        public bool ReturnNotFound { get; set; }
        public string RemoteStatus { get; set; } = "PENDING";
        public long RemoteAmountOffset { get; set; }
        public long RemoteOrderCodeOffset { get; set; }
        public string RemotePaymentLinkId { get; set; } = "provider-link-id";
        public string? RemoteReference { get; set; } = "provider-reference";
        public string? CreateResponseMismatch { get; set; }
        public PayOsVerifiedWebhook? WebhookResult { get; set; }
        public Exception? ReadFailure { get; set; }
        public int CreateCalls { get; private set; }
        public int ReadCalls { get; private set; }
        public int VerifyCalls { get; private set; }
        public string? LastWebhookRawJson { get; private set; }
        public PayOsCreatePaymentRequest? LastRequest { get; private set; }
        public TaskCompletionSource CreateEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseCreate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReadEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<PayOsCreatePaymentResult> CreateAsync(PayOsCreatePaymentRequest request,
            CancellationToken cancellationToken)
        {
            CreateCalls++;
            LastRequest = request;
            CreateEntered.TrySetResult();
            if (BlockCreate) await ReleaseCreate.Task.WaitAsync(cancellationToken);
            if (CreateFailure is not null) throw CreateFailure;
            return CreateResponseMismatch switch {
                "wrong-order" => new(request.OrderCode + 1, request.Amount, "provider-link-id", "PENDING",
                    "https://example.invalid/checkout", "qr-data"),
                "wrong-amount" => new(request.OrderCode, request.Amount + 1, "provider-link-id", "PENDING",
                    "https://example.invalid/checkout", "qr-data"),
                "invalid-link" => new(request.OrderCode, request.Amount, "", "PENDING",
                    "https://example.invalid/checkout", "qr-data"),
                "invalid-checkout" => new(request.OrderCode, request.Amount, "provider-link-id", "PENDING",
                    "http://example.invalid/checkout", "qr-data"),
                "invalid-qr" => new(request.OrderCode, request.Amount, "provider-link-id", "PENDING",
                    "https://example.invalid/checkout", ""),
                _ => new(request.OrderCode, request.Amount, "provider-link-id", "PENDING",
                    "https://example.invalid/checkout", "qr-data")
            };
        }

        public async Task<PayOsPaymentSnapshot?> ReadAsync(long orderCode, CancellationToken cancellationToken)
        {
            ReadCalls++;
            ReadEntered.TrySetResult();
            if (BlockRead) await ReleaseRead.Task.WaitAsync(cancellationToken);
            if (ReadFailure is not null) throw ReadFailure;
            if (ReturnNotFound || LastRequest?.OrderCode != orderCode)
                return null;
            return new(orderCode + RemoteOrderCodeOffset,
                LastRequest.Amount + RemoteAmountOffset, RemotePaymentLinkId, RemoteStatus,
                RemoteStatus == "PAID" ? RemoteReference : null);
        }

        public Task<PayOsVerifiedWebhook?> VerifyWebhookAsync(PayOsWebhookEnvelope envelope,
            CancellationToken cancellationToken)
        {
            VerifyCalls++;
            LastWebhookRawJson = envelope.RawJson;
            try {
                using var json = JsonDocument.Parse(envelope.RawJson);
                if (json.RootElement.ValueKind != JsonValueKind.Object ||
                    !json.RootElement.TryGetProperty("signature", out var signature) ||
                    signature.GetString() != "valid-signature")
                    return Task.FromResult<PayOsVerifiedWebhook?>(null);
                return Task.FromResult(WebhookResult);
            } catch (JsonException) {
                return Task.FromResult<PayOsVerifiedWebhook?>(null);
            }
        }
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
