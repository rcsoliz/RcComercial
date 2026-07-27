using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Panel;
using RcComercial.Domain.Common;

namespace RcComercial.Application.Caja.Queries;

public record ListarSesionesCajaQuery(int Pagina = 1) : IRequest<List<SesionCajaHistorialDto>>;

public class ListarSesionesCajaQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ListarSesionesCajaQuery, List<SesionCajaHistorialDto>>
{
    private const int TamanoPagina = 20;

    public async Task<List<SesionCajaHistorialDto>> Handle(ListarSesionesCajaQuery request, CancellationToken ct)
    {
        var empresaId = currentUser.EmpresaId!.Value;
        var pagina = Math.Max(1, request.Pagina);

        var sucursalIds = await db.Sucursales
            .Where(s => s.EmpresaId == empresaId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var query = db.SesionesCaja.Where(s => sucursalIds.Contains(s.SucursalId));
        if (!currentUser.TienePermiso(Permisos.CajaVerTodas))
            query = query.Where(s => s.UsuarioId == currentUser.UsuarioId);

        var sesiones = await query
            .OrderByDescending(s => s.Apertura)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .ToListAsync(ct);

        var nombres = await PanelHoyCalculator.ObtenerNombresAsync(
            db, empresaId, sesiones.Select(s => s.UsuarioId), ct);

        return sesiones
            .Select(s => new SesionCajaHistorialDto(
                s.Id, s.SucursalId, s.UsuarioId, nombres.GetValueOrDefault(s.UsuarioId, "?"),
                s.Apertura, s.Cierre, s.MontoInicial, s.MontoCierreDeclarado, s.MontoCierreCalculado,
                s.MontoCierreDeclarado is not null && s.MontoCierreCalculado is not null
                    ? s.MontoCierreDeclarado - s.MontoCierreCalculado
                    : null,
                s.Estado))
            .ToList();
    }
}
