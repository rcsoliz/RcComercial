using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Usuarios.Commands;

public record RestablecerContrasenaCommand(Guid Id) : IRequest<UsuarioConPasswordTemporalDto?>;

public class RestablecerContrasenaCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    : IRequestHandler<RestablecerContrasenaCommand, UsuarioConPasswordTemporalDto?>
{
    public async Task<UsuarioConPasswordTemporalDto?> Handle(RestablecerContrasenaCommand request, CancellationToken ct)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == request.Id, ct);
        if (usuario is null) return null;

        var passwordTemporal = GeneradorPassword.Temporal();
        usuario.PasswordHash = hasher.Hash(passwordTemporal);
        usuario.DebeCambiarPassword = true;

        // Fuerza a que cualquier sesión activa vuelva a loguearse con la
        // contraseña nueva en vez de seguir renovando con el refresh viejo.
        var tokensActivos = await db.RefreshTokens
            .Where(rt => rt.UsuarioId == usuario.Id && rt.RevocadoEn == null)
            .ToListAsync(ct);
        foreach (var token in tokensActivos) token.RevocadoEn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        var dto = await UsuarioMapper.ObtenerDtoAsync(db, usuario.Id, ct);
        return new UsuarioConPasswordTemporalDto(dto!, passwordTemporal);
    }
}
