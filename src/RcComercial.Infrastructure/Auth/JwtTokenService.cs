using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Auth;

public class JwtTokenService(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _settings = options.Value;

    public TimeSpan DuracionRefreshToken => TimeSpan.FromDays(_settings.RefreshTokenDias);

    public (string Token, DateTimeOffset ExpiraEn) GenerarAccessToken(Usuario usuario, IEnumerable<string> permisos)
    {
        var expiraEn = DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenMinutos);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new("empresa_id", usuario.EmpresaId.ToString()),
            new("permisos_version", usuario.PermisosVersion.ToString()),
        };
        if (usuario.SucursalId is { } sucursalId)
            claims.Add(new Claim("sucursal_id", sucursalId.ToString()));
        claims.AddRange(permisos.Select(p => new Claim("permiso", p)));

        var key = new SymmetricSecurityKey(Convert.FromBase64String(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiraEn.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }

    public string GenerarRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
