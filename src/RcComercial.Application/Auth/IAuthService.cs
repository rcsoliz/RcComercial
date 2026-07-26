namespace RcComercial.Application.Auth;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(
        string usuarioLogin, string password, string? ip, string? userAgent, CancellationToken ct = default);

    Task<RefreshResult> RefreshAsync(
        string refreshToken, string? ip, string? userAgent, CancellationToken ct = default);
}
