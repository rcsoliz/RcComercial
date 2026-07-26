using MediatR;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Panel.Dtos;
using RcComercial.Domain.Common;

namespace RcComercial.Application.Panel.Queries.ObtenerPanelHoy;

public class ObtenerPanelHoyQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ObtenerPanelHoyQuery, PanelHoyDto>
{
    public Task<PanelHoyDto> Handle(ObtenerPanelHoyQuery request, CancellationToken ct)
    {
        var empresaId = currentUser.EmpresaId!.Value;
        var sucursalId = currentUser.SucursalId;
        var incluirCostos = currentUser.TienePermiso(Permisos.InventarioVerCostos);

        return PanelHoyCalculator.CalcularAsync(db, empresaId, sucursalId, incluirCostos, ct);
    }
}
