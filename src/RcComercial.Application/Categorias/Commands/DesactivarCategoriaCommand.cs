using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Categorias.Commands;

public record DesactivarCategoriaCommand(Guid Id) : IRequest<bool>;

public class DesactivarCategoriaCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DesactivarCategoriaCommand, bool>
{
    public async Task<bool> Handle(DesactivarCategoriaCommand request, CancellationToken ct)
    {
        var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (categoria is null) return false;

        categoria.Activo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
