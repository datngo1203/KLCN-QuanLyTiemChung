namespace TiemChung.Desktop.Services;

public sealed record AuthResult(bool Succeeded, string UserName, string Message);

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string userName, string password, CancellationToken cancellationToken = default);
}
