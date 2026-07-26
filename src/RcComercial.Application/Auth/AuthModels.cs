namespace RcComercial.Application.Auth;

public enum LoginError
{
    Ninguno,
    CredencialesInvalidas,
    CuentaBloqueada,
}

public record LoginResult(
    bool Exitoso,
    LoginError Error,
    DateTimeOffset? BloqueadoHasta,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiraEn);

public enum RefreshError
{
    Ninguno,
    TokenInvalido,
    TokenExpirado,
    TokenReutilizado,
}

public record RefreshResult(
    bool Exitoso,
    RefreshError Error,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiraEn);
