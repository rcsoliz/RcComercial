using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Auth;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;
using RcComercial.Infrastructure.Persistence;

namespace RcComercial.Infrastructure.Auth;

public class AuthService(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    private const int MaxIntentosFallidos = 5;
    private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> LoginAsync(
        string usuarioLogin, string password, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.IgnoreQueryFilters()
            .Include(u => u.Rol).ThenInclude(r => r.Permisos).ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u => u.UsuarioLogin == usuarioLogin && u.Activo, ct);

        if (usuario is null)
            return new LoginResult(false, LoginError.CredencialesInvalidas, null, null, null, null);

        var ahora = DateTimeOffset.UtcNow;

        if (usuario.BloqueadoHasta is { } bloqueadoHasta && bloqueadoHasta > ahora)
        {
            RegistrarAuditoria(usuario.EmpresaId, usuario.Id, "login.fallido", ip);
            await db.SaveChangesAsync(ct);
            return new LoginResult(false, LoginError.CuentaBloqueada, bloqueadoHasta, null, null, null);
        }

        if (!passwordHasher.Verify(password, usuario.PasswordHash))
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= MaxIntentosFallidos)
                usuario.BloqueadoHasta = ahora.Add(DuracionBloqueo);

            RegistrarAuditoria(usuario.EmpresaId, usuario.Id, "login.fallido", ip);
            await db.SaveChangesAsync(ct);
            return new LoginResult(false, LoginError.CredencialesInvalidas, usuario.BloqueadoHasta, null, null, null);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoLogin = ahora;

        var permisos = usuario.Rol.Permisos.Select(rp => rp.Permiso.Codigo).ToList();
        var (accessToken, expiraEn) = tokenService.GenerarAccessToken(usuario, permisos);
        var (_, rawRefreshToken) = CrearRefreshToken(usuario.Id, ip, userAgent);

        RegistrarAuditoria(usuario.EmpresaId, usuario.Id, "login.exitoso", ip);
        await db.SaveChangesAsync(ct);

        return new LoginResult(true, LoginError.Ninguno, null, accessToken, rawRefreshToken, expiraEn);
    }

    public async Task<LoginResult> CambiarPasswordObligatorioAsync(
        Guid usuarioId, string passwordActual, string passwordNueva, string? ip, string? userAgent,
        CancellationToken ct = default)
    {
        var usuario = await db.Usuarios
            .Include(u => u.Rol).ThenInclude(r => r.Permisos).ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo, ct);

        if (usuario is null)
            return new LoginResult(false, LoginError.CredencialesInvalidas, null, null, null, null);

        if (!passwordHasher.Verify(passwordActual, usuario.PasswordHash))
            return new LoginResult(false, LoginError.CredencialesInvalidas, null, null, null, null);

        usuario.PasswordHash = passwordHasher.Hash(passwordNueva);
        usuario.DebeCambiarPassword = false;

        var permisos = usuario.Rol.Permisos.Select(rp => rp.Permiso.Codigo).ToList();
        var (accessToken, expiraEn) = tokenService.GenerarAccessToken(usuario, permisos);
        var (_, rawRefreshToken) = CrearRefreshToken(usuario.Id, ip, userAgent);

        RegistrarAuditoria(usuario.EmpresaId, usuario.Id, "auth.password_cambiada", ip);
        await db.SaveChangesAsync(ct);

        return new LoginResult(true, LoginError.Ninguno, null, accessToken, rawRefreshToken, expiraEn);
    }

    public async Task<RefreshResult> RefreshAsync(
        string refreshToken, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var hash = tokenService.HashRefreshToken(refreshToken);
        var existente = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (existente is null)
            return new RefreshResult(false, RefreshError.TokenInvalido, null, null, null);

        var ahora = DateTimeOffset.UtcNow;

        if (existente.RevocadoEn is not null)
        {
            // Reuso de un token ya rotado: indicio de robo. Revocar toda la cadena del usuario.
            var activos = await db.RefreshTokens
                .Where(rt => rt.UsuarioId == existente.UsuarioId && rt.RevocadoEn == null)
                .ToListAsync(ct);
            foreach (var rt in activos) rt.RevocadoEn = ahora;

            var empresaId = await db.Usuarios.IgnoreQueryFilters()
                .Where(u => u.Id == existente.UsuarioId)
                .Select(u => (Guid?)u.EmpresaId)
                .FirstOrDefaultAsync(ct);
            RegistrarAuditoria(empresaId, existente.UsuarioId, "auth.refresh_reuso_detectado", ip);
            await db.SaveChangesAsync(ct);
            return new RefreshResult(false, RefreshError.TokenReutilizado, null, null, null);
        }

        if (existente.ExpiraEn < ahora)
            return new RefreshResult(false, RefreshError.TokenExpirado, null, null, null);

        var usuario = await db.Usuarios.IgnoreQueryFilters()
            .Include(u => u.Rol).ThenInclude(r => r.Permisos).ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(u => u.Id == existente.UsuarioId && u.Activo, ct);

        if (usuario is null)
            return new RefreshResult(false, RefreshError.TokenInvalido, null, null, null);

        var (nuevoRefreshId, nuevoRefreshRaw) = CrearRefreshToken(usuario.Id, ip, userAgent);
        existente.RevocadoEn = ahora;
        existente.ReemplazadoPor = nuevoRefreshId;

        var permisos = usuario.Rol.Permisos.Select(rp => rp.Permiso.Codigo).ToList();
        var (accessToken, expiraEn) = tokenService.GenerarAccessToken(usuario, permisos);

        await db.SaveChangesAsync(ct);

        return new RefreshResult(true, RefreshError.Ninguno, accessToken, nuevoRefreshRaw, expiraEn);
    }

    private (Guid Id, string RawToken) CrearRefreshToken(Guid usuarioId, string? ip, string? userAgent)
    {
        var raw = tokenService.GenerarRefreshToken();
        var entity = new RefreshToken
        {
            UsuarioId = usuarioId,
            TokenHash = tokenService.HashRefreshToken(raw),
            ExpiraEn = DateTimeOffset.UtcNow.Add(tokenService.DuracionRefreshToken),
            IpCreacion = ip,
            UserAgent = userAgent,
        };
        db.RefreshTokens.Add(entity);
        return (entity.Id, raw);
    }

    private void RegistrarAuditoria(Guid? empresaId, Guid? usuarioId, string accion, string? ip)
    {
        if (empresaId is null) return; // sin empresa conocida no se puede satisfacer el NOT NULL de auditoria
        db.Auditorias.Add(new Auditoria
        {
            EmpresaId = empresaId.Value,
            UsuarioId = usuarioId,
            Accion = accion,
            Ip = ip,
        });
    }
}
