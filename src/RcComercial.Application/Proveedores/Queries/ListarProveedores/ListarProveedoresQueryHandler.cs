using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Proveedores.Queries.ListarProveedores;

/// <summary>
/// Handler nuevo y paralelo a BuscarProveedoresQueryHandler (que sigue igual,
/// lo usa el &lt;select&gt; de "elegir proveedor" en Compras): misma
/// búsqueda/filtro, pero devolviendo el total para paginar de verdad en
/// ProveedoresView.
/// </summary>
public class ListarProveedoresQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarProveedoresQuery, ListarProveedoresResultDto>
{
    private const int TamanoPagina = 8;

    public async Task<ListarProveedoresResultDto> Handle(ListarProveedoresQuery request, CancellationToken ct)
    {
        var texto = request.Buscar?.Trim();
        var pagina = Math.Max(1, request.Pagina);

        IQueryable<Proveedor> query = request.Estado?.Trim().ToUpperInvariant() switch
        {
            "INACTIVOS" => db.Proveedores.Where(p => !p.Activo),
            "TODOS" => db.Proveedores,
            _ => db.Proveedores.Where(p => p.Activo),
        };

        if (!string.IsNullOrWhiteSpace(texto))
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.Nombre, $"%{texto}%") ||
                (p.Nit != null && EF.Functions.ILike(p.Nit, $"%{texto}%")) ||
                (p.TelefonoWhatsapp != null && EF.Functions.ILike(p.TelefonoWhatsapp, $"%{texto}%")));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(p => new ProveedorDto(p.Id, p.Nombre, p.Nit, p.TelefonoWhatsapp, p.DiasCredito, p.LeadTimeDias, p.Activo))
            .ToListAsync(ct);

        return new ListarProveedoresResultDto(items, total, pagina, TamanoPagina);
    }
}
