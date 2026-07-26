using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Productos.Commands.DesactivarProducto;

public class DesactivarProductoCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DesactivarProductoCommand, bool>
{
    public async Task<bool> Handle(DesactivarProductoCommand request, CancellationToken ct)
    {
        var producto = await db.Productos.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (producto is null) return false;

        producto.Activo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
