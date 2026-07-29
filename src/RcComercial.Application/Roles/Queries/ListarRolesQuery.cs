using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Roles.Queries;

/// <summary>Roles de sistema (EmpresaId null, visibles para todas) + los propios de la empresa actual.</summary>
public record ListarRolesQuery : IRequest<List<RolDto>>;

public class ListarRolesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ListarRolesQuery, List<RolDto>>
{
    public async Task<List<RolDto>> Handle(ListarRolesQuery request, CancellationToken ct)
    {
        var roles = await db.Roles.IgnoreQueryFilters()
            .Where(r => r.Activo && (r.EmpresaId == null || r.EmpresaId == currentUser.EmpresaId))
            .OrderByDescending(r => r.EsSistema)
            .ThenBy(r => r.Nombre)
            .Select(r => new { r.Id, r.Nombre, r.EsSistema, r.Activo })
            .ToListAsync(ct);

        var permisosPorRol = await db.RolPermisos
            .Where(rp => roles.Select(r => r.Id).Contains(rp.RolId))
            .GroupBy(rp => rp.RolId)
            .Select(g => new { RolId = g.Key, PermisoIds = g.Select(rp => rp.PermisoId).ToList() })
            .ToDictionaryAsync(x => x.RolId, x => x.PermisoIds, ct);

        return roles
            .Select(r => new RolDto(r.Id, r.Nombre, r.EsSistema, r.Activo, permisosPorRol.GetValueOrDefault(r.Id, [])))
            .ToList();
    }
}
