using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Marcas.Commands;

public record DesactivarMarcaCommand(Guid Id) : IRequest<bool>;

public class DesactivarMarcaCommandHandler(IApplicationDbContext db) : IRequestHandler<DesactivarMarcaCommand, bool>
{
    public async Task<bool> Handle(DesactivarMarcaCommand request, CancellationToken ct)
    {
        var marca = await db.Marcas.FirstOrDefaultAsync(m => m.Id == request.Id, ct);
        if (marca is null) return false;

        marca.Activo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
