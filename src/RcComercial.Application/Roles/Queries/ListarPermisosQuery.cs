using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Roles.Queries;

/// <summary>Catálogo global de permisos: insumo para armar los checkboxes de RolesView, agrupados por módulo en el cliente.</summary>
public record ListarPermisosQuery : IRequest<List<PermisoDto>>;

public class ListarPermisosQueryHandler(IApplicationDbContext db) : IRequestHandler<ListarPermisosQuery, List<PermisoDto>>
{
    public async Task<List<PermisoDto>> Handle(ListarPermisosQuery request, CancellationToken ct) =>
        await db.Permisos
            .OrderBy(p => p.Modulo).ThenBy(p => p.Nombre)
            .Select(p => new PermisoDto(p.Id, p.Codigo, p.Modulo, p.Nombre, p.EsSensible))
            .ToListAsync(ct);
}
