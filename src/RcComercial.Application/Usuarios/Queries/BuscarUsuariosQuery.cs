using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Usuarios.Queries;

/// <summary>Estado: "activos" (default) | "inactivos" | "todos".</summary>
public record BuscarUsuariosQuery(string? Buscar, string? Estado, int Pagina = 1) : IRequest<List<UsuarioDto>>;

public class BuscarUsuariosQueryHandler(IApplicationDbContext db)
    : IRequestHandler<BuscarUsuariosQuery, List<UsuarioDto>>
{
    private const int TamanoPagina = 50; // un negocio típico tiene pocos usuarios, no miles

    public async Task<List<UsuarioDto>> Handle(BuscarUsuariosQuery request, CancellationToken ct)
    {
        var texto = request.Buscar?.Trim();
        var pagina = Math.Max(1, request.Pagina);

        IQueryable<Usuario> query = request.Estado?.Trim().ToUpperInvariant() switch
        {
            "INACTIVOS" => db.Usuarios.Where(u => !u.Activo),
            "TODOS" => db.Usuarios,
            _ => db.Usuarios.Where(u => u.Activo),
        };

        if (!string.IsNullOrWhiteSpace(texto))
        {
            query = query.Where(u =>
                EF.Functions.ILike(u.Nombre, $"%{texto}%") || EF.Functions.ILike(u.UsuarioLogin, $"%{texto}%"));
        }

        var usuarioIds = await query
            .OrderBy(u => u.Nombre)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var dtos = await (
            from u in db.Usuarios
            join r in db.Roles on u.RolId equals r.Id
            join s in db.Sucursales on u.SucursalId equals s.Id into sucursales
            from s in sucursales.DefaultIfEmpty()
            where usuarioIds.Contains(u.Id)
            select new UsuarioDto(
                u.Id, u.Nombre, u.UsuarioLogin, u.RolId, r.Nombre,
                u.SucursalId, s != null ? s.Nombre : null, u.TelefonoWhatsapp, u.Activo,
                u.UltimoLogin, u.DebeCambiarPassword)
        ).ToListAsync(ct);

        // El JOIN no conserva el orden por nombre del query original: se reordena en memoria.
        var ordenPorId = usuarioIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        return dtos.OrderBy(d => ordenPorId[d.Id]).ToList();
    }
}
