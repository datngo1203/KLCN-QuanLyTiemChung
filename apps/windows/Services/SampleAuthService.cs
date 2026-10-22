namespace TiemChung.Desktop.Services;

/// <summary>Local-only authentication fixture. Replace with the backend identity client when available.</summary>
public sealed class SampleAuthService : IAuthService
{
    public bool UseSampleData { get; set; } = true;

    public async Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        await Task.Delay(350, cancellationToken);
        if (!UseSampleData)
            return new(false, "", "Chưa có dịch vụ xác thực. Vui lòng thử lại sau.");
        if (userName.Equals("invalid", StringComparison.OrdinalIgnoreCase))
            return new(false, "", "Tên đăng nhập hoặc mật khẩu không đúng.");
        if (userName.Equals("locked", StringComparison.OrdinalIgnoreCase))
            return new(false, "", "Tài khoản đang bị khóa. Vui lòng liên hệ quản trị viên.");
        if (userName.Equals("offline", StringComparison.OrdinalIgnoreCase))
            return new(false, "", "Không kết nối được dịch vụ xác thực. Kiểm tra mạng rồi thử lại.");
        // Sample-only: accept non-empty credentials so the shell can be previewed without a real identity API.
        return new(true, userName, "Đăng nhập mẫu thành công. Chưa xác thực với máy chủ.");
    }
}
