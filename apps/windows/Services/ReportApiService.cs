using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed class ReportApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class ReportApiService(IdentityApiService? identity = null) : IReportService
{
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;

    public bool UseSampleData { get; set; } = false;

    private sealed record MonthlyRevenueItemResponse(
        int Year,
        int Month,
        string Label,
        decimal Revenue,
        int TransactionCount
    );

    private sealed record MonthlyRevenueReportResponse(
        DateOnly From,
        DateOnly To,
        decimal TotalRevenue,
        int TotalTransactions,
        bool IsSimulated,
        List<MonthlyRevenueItemResponse>? Items
    );

    private sealed record VaccineDosesItemResponse(
        int VaccineId,
        string VaccineName,
        int DoseCount
    );

    private sealed record VaccineDosesReportResponse(
        DateOnly From,
        DateOnly To,
        int TotalDoses,
        List<VaccineDosesItemResponse>? Items
    );

    private sealed record RegistrationStatusItemResponse(
        string Status,
        int RegistrationCount,
        double Percentage
    );

    private sealed record RegistrationStatusReportResponse(
        DateOnly From,
        DateOnly To,
        int TotalRegistrations,
        List<RegistrationStatusItemResponse>? Items
    );

    public async Task<MonthlyRevenueReport> GetMonthlyRevenueAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        if (UseSampleData)
        {
            return await new SampleReportService { UseSampleData = true }.GetMonthlyRevenueAsync(from, to, ct);
        }

        var query = new List<string>();
        if (from is not null) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to is not null) query.Add($"to={to.Value:yyyy-MM-dd}");
        var path = "api/v1/reports/monthly-revenue" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        using var response = await _identity.SendCatalogAsync(HttpMethod.Get, path);
        if (!response.IsSuccessStatusCode)
        {
            HandleError(response, ct);
        }

        var dto = await response.Content.ReadFromJsonAsync<MonthlyRevenueReportResponse>(cancellationToken: ct)
            ?? throw new JsonException("Invalid response payload from report API.");

        var items = (dto.Items ?? []).Select(x => new MonthlyRevenueItem(
            x.Year,
            x.Month,
            x.Label,
            x.Revenue,
            x.TransactionCount
        )).ToList();

        var totalTransactions = items.Sum(x => x.TransactionCount);

        return new MonthlyRevenueReport(
            dto.From,
            dto.To,
            dto.TotalRevenue,
            totalTransactions,
            dto.IsSimulated,
            items
        );
    }

    public async Task<VaccineDosesReport> GetVaccineDosesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        if (UseSampleData)
        {
            return await new SampleReportService { UseSampleData = true }.GetVaccineDosesAsync(from, to, ct);
        }

        var query = new List<string>();
        if (from is not null) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to is not null) query.Add($"to={to.Value:yyyy-MM-dd}");
        var path = "api/v1/reports/vaccine-doses" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        using var response = await _identity.SendCatalogAsync(HttpMethod.Get, path);
        if (!response.IsSuccessStatusCode)
        {
            HandleError(response, ct);
        }

        var dto = await response.Content.ReadFromJsonAsync<VaccineDosesReportResponse>(cancellationToken: ct)
            ?? throw new JsonException("Invalid response payload from vaccine doses report API.");

        var totalDoses = dto.TotalDoses;
        var items = (dto.Items ?? []).Select(x =>
        {
            var percentage = totalDoses > 0 ? (double)x.DoseCount * 100.0 / totalDoses : 0.0;
            return new VaccineDoseItem(
                x.VaccineId,
                x.VaccineName,
                x.DoseCount,
                percentage
            );
        }).ToList();

        return new VaccineDosesReport(
            dto.From,
            dto.To,
            totalDoses,
            items
        );
    }

    public async Task<RegistrationStatusReport> GetRegistrationStatusesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        if (UseSampleData)
        {
            return await new SampleReportService { UseSampleData = true }.GetRegistrationStatusesAsync(from, to, ct);
        }

        var query = new List<string>();
        if (from is not null) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to is not null) query.Add($"to={to.Value:yyyy-MM-dd}");
        var path = "api/v1/reports/registration-statuses" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        using var response = await _identity.SendCatalogAsync(HttpMethod.Get, path);
        if (!response.IsSuccessStatusCode)
        {
            HandleError(response, ct);
        }

        var dto = await response.Content.ReadFromJsonAsync<RegistrationStatusReportResponse>(cancellationToken: ct)
            ?? throw new JsonException("Invalid response payload from registration status report API.");

        var items = (dto.Items ?? []).Select(x => new RegistrationStatusItem(
            x.Status,
            x.RegistrationCount,
            x.Percentage
        )).ToList();

        return new RegistrationStatusReport(
            dto.From,
            dto.To,
            dto.TotalRegistrations,
            items
        );
    }

    private static void HandleError(HttpResponseMessage response, CancellationToken ct)
    {
        string? code = null;
        try
        {
            using var json = JsonDocument.Parse(response.Content.ReadAsStringAsync(ct).GetAwaiter().GetResult());
            if (json.RootElement.TryGetProperty("code", out var value))
                code = value.GetString();
        }
        catch (JsonException) { }

        code = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "invalidSession",
            HttpStatusCode.Forbidden => "permissionDenied",
            HttpStatusCode.BadRequest => "validation",
            _ => code ?? "requestFailed"
        };

        throw new ReportApiException(code, code switch
        {
            "invalidSession" => "Phiên đã hết hạn. Vui lòng đăng nhập lại.",
            "permissionDenied" => "Bạn không có quyền XemBaoCao để xem báo cáo thống kê.",
            "validation" => "Khoảng thời gian yêu cầu không hợp lệ.",
            _ => "Không thể tải báo cáo từ máy chủ."
        });
    }
}
