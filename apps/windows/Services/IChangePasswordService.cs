namespace TiemChung.Desktop.Services;

public sealed record ChangePasswordResult(bool Succeeded, string Message);

public interface IChangePasswordService
{
    bool UseSampleData { get; set; }
    Task<ChangePasswordResult> ChangePasswordAsync(string currentPassword, string newPassword);
}
