using RcComercial.Domain.Entities;

namespace RcComercial.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiraEn) GenerarAccessToken(Usuario usuario, IEnumerable<string> permisos);

    /// <summary>Token opaco aleatorio (64 bytes). El caller guarda SOLO su hash.</summary>
    string GenerarRefreshToken();

    string HashRefreshToken(string refreshToken);

    TimeSpan DuracionRefreshToken { get; }
}
