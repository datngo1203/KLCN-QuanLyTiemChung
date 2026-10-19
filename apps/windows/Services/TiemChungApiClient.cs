using System.Net.Http;
using System.Net.Http.Json;

namespace TiemChung.Desktop.Services;

public sealed class TiemChungApiClient
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri((Environment.GetEnvironmentVariable("TIEMCHUNG_API_BASE_URL") ?? "http://localhost:5080").TrimEnd('/') + "/"),
        Timeout = TimeSpan.FromSeconds(8)
    };

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        var result = await Client.GetFromJsonAsync<HealthResponse>("health", ct);
        return result?.Status == "ok";
    }

    private sealed record HealthResponse(string Status);
}
