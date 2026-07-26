using MediatR;
using RcComercial.Application.Panel.Dtos;

namespace RcComercial.Application.Panel.Queries.ObtenerPanelAlertas;

public record ObtenerPanelAlertasQuery(Guid? EmpresaId = null) : IRequest<PanelAlertasDto>;
