using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Productos.Dtos;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Productos.Queries.ListarProductos;

/// <summary>
/// Handler nuevo y paralelo a BuscarProductosQueryHandler (que sigue igual,
/// lo usan VentaView/ComprasView): misma búsqueda por trigramas, pero con
/// filtro de estado y devolviendo el total para paginar de verdad.
/// </summary>
public class ListarProductosQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarProductosQuery, ListarProductosResultDto>
{
    private const int TamanoPagina = 10;

    // Mismo umbral calibrado que BuscarProductosQueryHandler (ver ese archivo).
    private const double UmbralSimilitud = 0.15;

    public async Task<ListarProductosResultDto> Handle(ListarProductosQuery request, CancellationToken ct)
    {
        var texto = request.Buscar?.Trim();
        var pagina = Math.Max(1, request.Pagina);

        IQueryable<Producto> query = request.Estado?.Trim().ToUpperInvariant() switch
        {
            "INACTIVOS" => db.Productos.Where(p => !p.Activo),
            "TODOS" => db.Productos,
            _ => db.Productos.Where(p => p.Activo),
        };

        if (!string.IsNullOrWhiteSpace(texto))
        {
            query = query.Where(p => EF.Functions.ILike(p.Nombre, $"%{texto}%")
                || EF.Functions.TrigramsSimilarity(p.Nombre, texto) > UmbralSimilitud);
        }

        // El total se cuenta ANTES de ordenar (el ORDER BY no afecta el conteo,
        // y así evita que EF arrastre la expresión de similitud a un COUNT).
        var total = await query.CountAsync(ct);

        query = string.IsNullOrWhiteSpace(texto)
            ? query.OrderBy(p => p.Nombre)
            : query.OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Nombre, texto));

        var productos = await query
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(p => new
            {
                p.Id, p.Codigo, p.CodigoBarras, p.Nombre, p.PrecioBase, p.Activo,
                p.CategoriaId, p.MarcaId, p.StockMinimo,
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

        var items = productos
            .Select(p => new ProductoListItemDto(
                p.Id, p.Codigo, p.CodigoBarras, p.Nombre, p.PrecioBase, p.Activo,
                p.CategoriaId is { } catId ? categorias.GetValueOrDefault(catId) : null,
                p.MarcaId is { } marId ? marcas.GetValueOrDefault(marId) : null,
                stockPorProducto.GetValueOrDefault(p.Id), p.StockMinimo))
            .ToList();

        return new ListarProductosResultDto(items, total, pagina, TamanoPagina);
    }
}
