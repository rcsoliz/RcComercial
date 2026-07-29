using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Plataforma.Commands;

/// <summary>Suspender/reactivar un tenant. Una empresa inactiva no puede loguear ningún usuario (ver AuthService).</summary>
public record CambiarActivoEmpresaCommand(Guid Id, bool Activo) : IRequest<bool>;

public class CambiarActivoEmpresaCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CambiarActivoEmpresaCommand, bool>
{
    public async Task<bool> Handle(CambiarActivoEmpresaCommand request, CancellationToken ct)
    {
        var empresa = await db.Empresas.FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        if (empresa is null) return false;

        empresa.Activo = request.Activo;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
