namespace TiemChung.Desktop.Services;

/// <summary>Sample-only password flow; never persists credentials or password material.</summary>
public sealed class SampleChangePasswordService : IChangePasswordService
{
    public bool UseSampleData { get; set; } = true;

    public Task<ChangePasswordResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (!UseSampleData)
            return Task.FromResult(new ChangePasswordResult(false, "Chưa có dịch vụ đổi mật khẩu."));

        if (currentPassword.Equals("sai", StringComparison.Ordinal))
            return Task.FromResult(new ChangePasswordResult(false, "Mật khẩu hiện tại không chính xác."));

        return Task.FromResult(new ChangePasswordResult(true,
            "Đã kiểm tra biểu mẫu mẫu. Backend chưa có xác thực hoặc đổi mật khẩu thật."));
    }
}
