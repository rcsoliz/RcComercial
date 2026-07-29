namespace RcComercial.Application.Auth;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(
        string usuarioLogin, string password, string? ip, string? userAgent, CancellationToken ct = default);

    Task<RefreshResult> RefreshAsync(
        string refreshToken, string? ip, string? userAgent, CancellationToken ct = default);

    /// <summary>
    /// Flujo de "debe_cambiar_password" (primer ingreso / tras restablecer):
    /// exige la contraseña actual (temporal) y devuelve tokens nuevos con el
    /// claim ya en false, para no forzar un segundo login.
    /// </summary>
    Task<LoginResult> CambiarPasswordObligatorioAsync(
        Guid usuarioId, string passwordActual, string passwordNueva, string? ip, string? userAgent,
        CancellationToken ct = default);
}
