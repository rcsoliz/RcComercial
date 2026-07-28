using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Proveedores.Commands;

public record DesactivarProveedorCommand(Guid Id) : IRequest<bool>;

public class DesactivarProveedorCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DesactivarProveedorCommand, bool>
{
    public async Task<bool> Handle(DesactivarProveedorCommand request, CancellationToken ct)
    {
        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (proveedor is null) return false;

        proveedor.Activo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
