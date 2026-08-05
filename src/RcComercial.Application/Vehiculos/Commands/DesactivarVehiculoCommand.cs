using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Vehiculos.Commands;

public record DesactivarVehiculoCommand(Guid Id) : IRequest<bool>;

public class DesactivarVehiculoCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DesactivarVehiculoCommand, bool>
{
    public async Task<bool> Handle(DesactivarVehiculoCommand request, CancellationToken ct)
    {
        var vehiculo = await db.Vehiculos.FirstOrDefaultAsync(v => v.Id == request.Id, ct);
        if (vehiculo is null) return false;

        vehiculo.Activo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
