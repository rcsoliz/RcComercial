using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Plataforma.Queries;

/// <summary>Estado: "activos" (default) | "inactivos" | "todos".</summary>
public record ListarEmpresasPlataformaQuery(string? Estado) : IRequest<List<EmpresaPlataformaListItemDto>>;

public class ListarEmpresasPlataformaQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarEmpresasPlataformaQuery, List<EmpresaPlataformaListItemDto>>
{
    public async Task<List<EmpresaPlataformaListItemDto>> Handle(ListarEmpresasPlataformaQuery request, CancellationToken ct)
    {
        // Empresa no implementa ITenantEntity (es el tenant, no algo que le
        // pertenezca a uno): no hay filtro que ignorar acá. Usuarios y Ventas
        // sí lo implementan, y este listado necesita cruzar TODAS las
        // empresas — por eso IgnoreQueryFilters() en esos dos.
        IQueryable<Empresa> query = request.Estado?.Trim().ToUpperInvariant() switch
        {
            "INACTIVOS" => db.Empresas.Where(e => !e.Activo),
            "TODOS" => db.Empresas,
            _ => db.Empresas.Where(e => e.Activo),
        };

        var empresas = await query
            .Include(e => e.Rubro)
            .OrderBy(e => e.Nombre)
            .Select(e => new { e.Id, e.Nombre, e.Nit, RubroNombre = e.Rubro.Nombre, e.Activo })
            .ToListAsync(ct);

        var empresaIds = empresas.Select(e => e.Id).ToList();

        var usuariosPorEmpresa = await db.Usuarios.IgnoreQueryFilters()
            .Where(u => empresaIds.Contains(u.EmpresaId) && u.Activo)
            .GroupBy(u => u.EmpresaId)
            .Select(g => new { EmpresaId = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.EmpresaId, x => x.Cantidad, ct);

        var ultimaVentaPorEmpresa = await db.Ventas.IgnoreQueryFilters()
            .Where(v => empresaIds.Contains(v.EmpresaId))
            .GroupBy(v => v.EmpresaId)
            .Select(g => new { EmpresaId = g.Key, Ultima = g.Max(v => v.Fecha) })
            .ToDictionaryAsync(x => x.EmpresaId, x => (DateTimeOffset?)x.Ultima, ct);

        return empresas
            .Select(e => new EmpresaPlataformaListItemDto(
                e.Id, e.Nombre, e.Nit, e.RubroNombre, e.Activo,
                usuariosPorEmpresa.GetValueOrDefault(e.Id),
                ultimaVentaPorEmpresa.GetValueOrDefault(e.Id)))
            .ToList();
    }
}
