using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Sucursales.Queries;

/// <summary>Estado: "activos" (default) | "inactivos" | "todos".</summary>
public record ListarSucursalesQuery(string? Estado = null) : IRequest<List<SucursalDto>>;

public class ListarSucursalesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarSucursalesQuery, List<SucursalDto>>
{
    public async Task<List<SucursalDto>> Handle(ListarSucursalesQuery request, CancellationToken ct)
    {
        IQueryable<Sucursal> query = request.Estado?.Trim().ToUpperInvariant() switch
        {
            "INACTIVOS" => db.Sucursales.Where(s => !s.Activo),
            "TODOS" => db.Sucursales,
            _ => db.Sucursales.Where(s => s.Activo),
        };

        return await query
            .OrderBy(s => s.Nombre)
            .Select(s => new SucursalDto(s.Id, s.Nombre, s.Direccion, s.Activo))
            .ToListAsync(ct);
    }
}
