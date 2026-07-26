using MediatR;
using RcComercial.Application.Panel.Dtos;

namespace RcComercial.Application.Panel.Queries.ObtenerPanelAlertas;

/// <summary>
/// Sin parámetros: empresaId/sucursalId SIEMPRE salen de ICurrentUserService
/// (JWT). El job nocturno usa PanelAlertasCalculator directo, no este tipo.
/// </summary>
public record ObtenerPanelAlertasQuery : IRequest<PanelAlertasDto>;
