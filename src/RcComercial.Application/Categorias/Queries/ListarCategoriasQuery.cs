using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Categorias.Queries;

public record ListarCategoriasQuery : IRequest<List<CategoriaDto>>;

public class ListarCategoriasQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarCategoriasQuery, List<CategoriaDto>>
{
    public async Task<List<CategoriaDto>> Handle(ListarCategoriasQuery request, CancellationToken ct) =>
        await db.Categorias
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.Id, c.Nombre, c.PadreId, c.Activo))
            .ToListAsync(ct);
}
