using MediatR;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Panel.Dtos;

namespace RcComercial.Application.Panel.Queries.ObtenerPanelAlertas;

public class ObtenerPanelAlertasQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ObtenerPanelAlertasQuery, PanelAlertasDto>
{
    public Task<PanelAlertasDto> Handle(ObtenerPanelAlertasQuery request, CancellationToken ct) =>
        PanelAlertasCalculator.CalcularAsync(db, currentUser.EmpresaId!.Value, currentUser.SucursalId, ct);
}
