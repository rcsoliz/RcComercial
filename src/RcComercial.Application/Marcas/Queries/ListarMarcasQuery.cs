using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Marcas.Queries;

public record ListarMarcasQuery : IRequest<List<MarcaDto>>;

public class ListarMarcasQueryHandler(IApplicationDbContext db) : IRequestHandler<ListarMarcasQuery, List<MarcaDto>>
{
    public async Task<List<MarcaDto>> Handle(ListarMarcasQuery request, CancellationToken ct) =>
        await db.Marcas
            .Where(m => m.Activo)
            .OrderBy(m => m.Nombre)
            .Select(m => new MarcaDto(m.Id, m.Nombre, m.Activo))
            .ToListAsync(ct);
}
