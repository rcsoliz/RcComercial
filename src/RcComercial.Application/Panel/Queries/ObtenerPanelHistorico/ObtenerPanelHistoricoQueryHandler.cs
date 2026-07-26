using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Panel.Dtos;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Panel.Queries.ObtenerPanelHistorico;

public class ObtenerPanelHistoricoQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ObtenerPanelHistoricoQuery, List<VentasPorDiaDto>>
{
    public async Task<List<VentasPorDiaDto>> Handle(ObtenerPanelHistoricoQuery request, CancellationToken ct)
    {
        var empresaId = currentUser.EmpresaId!.Value;
        var sucursalId = currentUser.SucursalId;

        var (inicio, _) = HoraBolivia.RangoDelDia(request.Desde);
        var (_, fin) = HoraBolivia.RangoDelDia(request.Hasta);

        IQueryable<Venta> query = db.Ventas.IgnoreQueryFilters()
            .Where(v => v.EmpresaId == empresaId && v.Estado == EstadosVenta.Completada
                && v.Fecha >= inicio && v.Fecha < fin);
        if (sucursalId is { } suc) query = query.Where(v => v.SucursalId == suc);

        var ventas = await query.Select(v => new { v.Fecha, v.Total }).ToListAsync(ct);

        return ventas
            .GroupBy(v => DateOnly.FromDateTime(v.Fecha.ToOffset(HoraBolivia.Offset).Date))
            .Select(g => new VentasPorDiaDto(g.Key, g.Count(), g.Sum(x => x.Total)))
            .OrderBy(x => x.Dia)
            .ToList();
    }
}
