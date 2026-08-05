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

        var productos = await query
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(p => new
            {
                p.Id, p.Codigo, p.CodigoBarras, p.Nombre, p.PrecioBase, p.Activo,
                p.CategoriaId, p.MarcaId, p.StockMinimo, p.EsServicio,
            })
            .ToListAsync(ct);

        var productoIds = productos.Select(p => p.Id).ToList();
        var categoriaIds = productos.Where(p => p.CategoriaId is not null).Select(p => p.CategoriaId!.Value).ToList();
        var marcaIds = productos.Where(p => p.MarcaId is not null).Select(p => p.MarcaId!.Value).ToList();

        var stockPorProducto = await db.Stocks
            .Where(s => productoIds.Contains(s.ProductoId))
            .GroupBy(s => s.ProductoId)
            .Select(g => new { ProductoId = g.Key, Total = g.Sum(s => s.Cantidad) })
            .ToDictionaryAsync(x => x.ProductoId, x => x.Total, ct);

        var categorias = await db.Categorias
            .Where(c => categoriaIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);

        var marcas = await db.Marcas
            .Where(m => marcaIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Nombre, ct);

        return productos
            .Select(p => new ProductoListItemDto(
                p.Id, p.Codigo, p.CodigoBarras, p.Nombre, p.PrecioBase, p.Activo,
                p.CategoriaId is { } catId ? categorias.GetValueOrDefault(catId) : null,
                p.MarcaId is { } marId ? marcas.GetValueOrDefault(marId) : null,
                stockPorProducto.GetValueOrDefault(p.Id), p.StockMinimo, p.EsServicio))
            .ToList();
    }
}
