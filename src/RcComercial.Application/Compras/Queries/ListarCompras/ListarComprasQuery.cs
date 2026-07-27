using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Compras.Dtos;

namespace RcComercial.Application.Compras.Queries.ListarCompras;

public record ListarComprasQuery(int Pagina = 1) : IRequest<List<CompraListItemDto>>;

public class ListarComprasQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarComprasQuery, List<CompraListItemDto>>
{
    private const int TamanoPagina = 20;

    public async Task<List<CompraListItemDto>> Handle(ListarComprasQuery request, CancellationToken ct)
    {
        var pagina = Math.Max(1, request.Pagina);

        var compras = await db.Compras
            .OrderByDescending(c => c.Fecha)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(c => new { c.Id, c.Numero, c.Fecha, c.ProveedorId, c.NroFacturaProv, c.Estado, c.Total })
            .ToListAsync(ct);

        var proveedorIds = compras.Select(c => c.ProveedorId).Distinct().ToList();
        var nombresProveedor = await db.Proveedores
            .Where(p => proveedorIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        return compras
            .Select(c => new CompraListItemDto(
                c.Id, c.Numero, c.Fecha, c.ProveedorId, nombresProveedor.GetValueOrDefault(c.ProveedorId, "?"),
                c.NroFacturaProv, c.Estado, c.Total))
            .ToList();
    }
}
