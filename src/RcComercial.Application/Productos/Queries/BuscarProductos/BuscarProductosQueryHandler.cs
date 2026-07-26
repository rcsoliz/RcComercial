using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Productos.Dtos;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Productos.Queries.BuscarProductos;

public class BuscarProductosQueryHandler(IApplicationDbContext db)
    : IRequestHandler<BuscarProductosQuery, List<ProductoListItemDto>>
{
    private const int TamanoPagina = 20;

    // Calibrado contra Postgres real: 'para 500' vs 'Paracetamol 500mg' da
    // similarity()=0.35; un par no relacionado da 0.0. 0.15 separa limpio.
    private const double UmbralSimilitud = 0.15;

    public async Task<List<ProductoListItemDto>> Handle(BuscarProductosQuery request, CancellationToken ct)
    {
        var texto = request.Buscar?.Trim();
        var pagina = Math.Max(1, request.Pagina);

        IQueryable<Producto> query = db.Productos.Where(p => p.Activo);

        if (string.IsNullOrWhiteSpace(texto))
        {
            query = query.OrderBy(p => p.Nombre);
        }
        else
        {
            query = query
                .Where(p => EF.Functions.ILike(p.Nombre, $"%{texto}%")
                    || EF.Functions.TrigramsSimilarity(p.Nombre, texto) > UmbralSimilitud)
                .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Nombre, texto));
        }

        return await query
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(p => new ProductoListItemDto(p.Id, p.Codigo, p.CodigoBarras, p.Nombre, p.PrecioBase, p.Activo))
            .ToListAsync(ct);
    }
}
