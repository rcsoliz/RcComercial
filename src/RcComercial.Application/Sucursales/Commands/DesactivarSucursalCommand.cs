using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Sucursales.Commands;

public record DesactivarSucursalCommand(Guid Id) : IRequest<bool>;

public class DesactivarSucursalCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DesactivarSucursalCommand, bool>
{
    public async Task<bool> Handle(DesactivarSucursalCommand request, CancellationToken ct)
    {
        var sucursal = await db.Sucursales.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (sucursal is null) return false;

        sucursal.Activo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
