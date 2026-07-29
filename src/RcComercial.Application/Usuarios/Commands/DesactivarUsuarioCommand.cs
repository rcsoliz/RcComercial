using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Usuarios.Commands;

public record DesactivarUsuarioCommand(Guid Id) : IRequest<bool>;

public class DesactivarUsuarioCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DesactivarUsuarioCommand, bool>
{
    public async Task<bool> Handle(DesactivarUsuarioCommand request, CancellationToken ct)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == request.Id, ct);
        if (usuario is null) return false;

        usuario.Activo = false;

        var tokensActivos = await db.RefreshTokens
            .Where(rt => rt.UsuarioId == usuario.Id && rt.RevocadoEn == null)
            .ToListAsync(ct);
        foreach (var token in tokensActivos) token.RevocadoEn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }
}
