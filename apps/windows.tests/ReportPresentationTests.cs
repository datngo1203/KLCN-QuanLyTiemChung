using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class ReportPresentationTests
{
    [Fact]
    public async Task RevenueDisclaimerReflectsCurrentSimulationState()
    {
        var service = new ReportService { IsSimulated = true };
        var viewModel = new ReportViewModel(service);

        await viewModel.GenerateAsync();
        Assert.Contains("các giao dịch thanh toán thành công trong hệ thống", viewModel.RevenueDisclaimer);
        Assert.Contains("chưa phải số liệu đối soát ngân hàng", viewModel.RevenueDisclaimer);
        Assert.DoesNotContain("G3", viewModel.RevenueDisclaimer);

        service.IsSimulated = false;
        await viewModel.GenerateAsync();
        Assert.EndsWith("trong hệ thống.", viewModel.RevenueDisclaimer);
        Assert.DoesNotContain("đối soát ngân hàng", viewModel.RevenueDisclaimer);
    }

    private sealed class ReportService : IReportService
    {
        public bool UseSampleData { get; set; } = true;
        public bool IsSimulated { get; set; }

        public Task<MonthlyRevenueReport> GetMonthlyRevenueAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
            Task.FromResult(new MonthlyRevenueReport(from ?? DateOnly.FromDateTime(DateTime.Today),
                to ?? DateOnly.FromDateTime(DateTime.Today), 0, 0, IsSimulated, []));

        public Task<VaccineDosesReport> GetVaccineDosesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
            Task.FromResult(VaccineDosesReport.Empty);

        public Task<RegistrationStatusReport> GetRegistrationStatusesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
            Task.FromResult(RegistrationStatusReport.Empty);
    }
}
