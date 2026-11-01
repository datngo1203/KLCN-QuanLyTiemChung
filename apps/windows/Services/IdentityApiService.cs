using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TiemChung.Desktop.Services;

// One process-local session. Nothing is written to disk.
public sealed class IdentityApiService : IAuthService, IChangePasswordService
{
    public static IdentityApiService Current { get; private set; } = new();
    public static void StartNewSession()
    {
        if (Current.IsSignedIn) throw new InvalidOperationException("Sign out before starting a new session.");
        Current = new();
    }
    private readonly HttpClient _client;
    private Tokens? _tokens;
    private Account? _account;
    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    public int SessionRevision { get; private set; }
    public event EventHandler? SessionCleared;
    private void ClearSession()
    {
        _tokens = null;
        _account = null;
        SessionRevision++;
        SessionCleared?.Invoke(this, EventArgs.Empty);
    }
    public bool CanReadProfiles => IsSignedIn && _account?.Capabilities?.ProfilesRead == true;
    public bool CanWriteProfiles => IsSignedIn && _account?.Capabilities is { ProfilesCreate: true, ProfilesUpdate: true };
    public bool CanUseRegistrations => IsSignedIn && _account is { AccountType: "employee", Employee.Title: "LeTan" };
    public bool CanUseClinical => IsSignedIn && _account is { AccountType: "employee", Employee.Title: "BacSi" };
    public bool CanUseVaccinations => IsSignedIn && _account is { AccountType: "employee", Employee.Title: "DieuDuong" };
    public bool CanUseAppointments => CanUseRegistrations && Permissions.Contains("QuanLyLichHen");
    public bool CanReadPayments => IsSignedIn && _account is { AccountType: "employee", Employee.Title: "ThuNgan" } && Permissions.Contains("XemThanhToan");
    public bool CanWritePayments => CanReadPayments && Permissions.Contains("GhiThanhToan");
    public IReadOnlyCollection<string> Permissions => IsSignedIn ? _account?.Permissions ?? [] : [];
    public bool UseSampleData { get; set; } = false;
    public bool IsSignedIn => _tokens is not null;
    public event EventHandler? PasswordChanged;

    public IdentityApiService(HttpClient? client = null) => _client = client ?? new HttpClient(new HttpClientHandler { UseCookies = false }) {
        BaseAddress = new Uri((Environment.GetEnvironmentVariable("TIEMCHUNG_API_BASE_URL") ?? "http://localhost:5080").TrimEnd('/') + "/"),
        Timeout = TimeSpan.FromSeconds(15)
    };

    public async Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        await _sessionGate.WaitAsync(cancellationToken);
        try {
            if (_tokens is not null) await RevokeAsync(_tokens.RefreshToken);
            ClearSession();
            using var response = await _client.PostAsJsonAsync("api/v1/auth/login", new { identifier = userName, password, clientType = "native" }, cancellationToken);
            if (!response.IsSuccessStatusCode) return new(false, "", await ErrorAsync(response));
            var tokens = await response.Content.ReadFromJsonAsync<Tokens>(cancellationToken) ?? throw new HttpRequestException("Invalid login response.");
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            using var me = await _client.SendAsync(request, cancellationToken);
            if (!me.IsSuccessStatusCode) {
                await RevokeAsync(tokens.RefreshToken);
                return new(false, "", await ErrorAsync(me));
            }
            var account = await me.Content.ReadFromJsonAsync<Account>(cancellationToken);
            if (account?.AccountType != "employee") {
                await RevokeAsync(tokens.RefreshToken);
                return new(false, "", "Ứng dụng Windows dành cho nhân viên.");
            }
            _tokens = tokens;
            _account = account;
            return new(true, userName, "Đăng nhập thành công.");
        }
        finally { _sessionGate.Release(); }
    }

    public async Task LogoutAsync()
    {
        await _sessionGate.WaitAsync();
        try {
            if (_tokens is not { } tokens) return;
            // Retain the session on network failure so the user can retry revocation.
            await RevokeAsync(tokens.RefreshToken);
            ClearSession();
        }
        finally { _sessionGate.Release(); }
    }

    private async Task RevokeAsync(string refreshToken)
    {
        using var response = await _client.PostAsJsonAsync("api/v1/auth/logout", new { clientType = "native", refreshToken });
        response.EnsureSuccessStatusCode();
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        await _sessionGate.WaitAsync();
        try {
            if (_tokens is not { } tokens) return new(false, "Phiên đã hết hạn. Vui lòng đăng nhập lại.");
            using var refresh = await _client.PostAsJsonAsync("api/v1/auth/refresh", new { clientType = "native", refreshToken = tokens.RefreshToken });
            if (!refresh.IsSuccessStatusCode) return new(false, await ErrorAsync(refresh));
            _tokens = await refresh.Content.ReadFromJsonAsync<Tokens>() ?? throw new HttpRequestException("Invalid refresh response.");
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/change-password") {
                Content = JsonContent.Create(new { currentPassword, newPassword })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
            using var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new(false, await ErrorAsync(response));
            ClearSession();
            PasswordChanged?.Invoke(this, EventArgs.Empty);
            return new(true, "Đã đổi mật khẩu. Vui lòng đăng nhập lại.");
        }
        finally { _sessionGate.Release(); }
    }

    public async Task<HttpResponseMessage> SendCatalogAsync(HttpMethod method, string path, object? body = null,
        bool retryUnauthorized = true, int? expectedSessionRevision = null)
    {
        await _sessionGate.WaitAsync();
        try {
            // A queued request from a previous screen must not use the next employee's token.
            if (expectedSessionRevision is { } revision && (revision != SessionRevision || !IsSignedIn))
                throw new OperationCanceledException();
            async Task<HttpResponseMessage> Send() {
                using var request = new HttpRequestMessage(method, path);
                if (body is not null) request.Content = JsonContent.Create(body);
                if (_tokens is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
                return await _client.SendAsync(request);
            }
            var response = await Send();
            if (!retryUnauthorized || response.StatusCode != System.Net.HttpStatusCode.Unauthorized || _tokens is null) return response;
            using var refresh = await _client.PostAsJsonAsync("api/v1/auth/refresh", new { clientType = "native", refreshToken = _tokens.RefreshToken });
            if (!refresh.IsSuccessStatusCode) return response;
            _tokens = await refresh.Content.ReadFromJsonAsync<Tokens>() ?? throw new HttpRequestException("Invalid refresh response.");
            response.Dispose();
            return await Send();
        }
        finally { _sessionGate.Release(); }
    }

    private static async Task<string> ErrorAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<Problem>();
        return problem?.Code switch {
            "invalidCredentials" => "Tên đăng nhập hoặc mật khẩu không đúng.",
            "invalidSession" => "Phiên đã hết hạn. Vui lòng đăng nhập lại.",
            "emailVerificationRequired" => "Vui lòng xác minh email trước khi đăng nhập.",
            "validation" => "Mật khẩu mới cần 12–128 ký tự và khác mật khẩu hiện tại.",
            "rateLimited" => "Thao tác quá nhiều lần. Vui lòng thử lại sau.",
            _ => $"Không thực hiện được yêu cầu (HTTP {(int)response.StatusCode})."
        };
    }

    private sealed record Tokens(string AccessToken, string RefreshToken);
    private sealed record Account(string AccountType, string[]? Permissions, Capabilities? Capabilities, Employee? Employee);
    private sealed record Employee(string Title);
    private sealed record Capabilities(bool ProfilesRead, bool ProfilesCreate, bool ProfilesUpdate);
    private sealed record Problem(string Code);
}
