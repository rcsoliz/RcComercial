using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Proveedores.Queries;

/// <summary>Estado: "activos" (default) | "inactivos" | "todos".</summary>
public record BuscarProveedoresQuery(string? Buscar, string? Estado, int Pagina = 1) : IRequest<List<ProveedorDto>>;

public class BuscarProveedoresQueryHandler(IApplicationDbContext db)
    : IRequestHandler<BuscarProveedoresQuery, List<ProveedorDto>>
{
    // Un negocio típico tiene decenas de proveedores, no miles (a diferencia
    // del catálogo de productos): una página generosa evita romper el <select>
    // de "elegir proveedor" en Compras, que carga todos los activos de una.
    private const int TamanoPagina = 100;

    public async Task<List<ProveedorDto>> Handle(BuscarProveedoresQuery request, CancellationToken ct)
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

        return await query
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(p => new ProveedorDto(p.Id, p.Nombre, p.Nit, p.TelefonoWhatsapp, p.DiasCredito, p.LeadTimeDias, p.Activo))
            .ToListAsync(ct);
    }
}
