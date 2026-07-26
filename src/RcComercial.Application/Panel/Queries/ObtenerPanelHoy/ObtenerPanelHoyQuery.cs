using MediatR;
using RcComercial.Application.Panel.Dtos;

namespace RcComercial.Application.Panel.Queries.ObtenerPanelHoy;

/// <summary>
/// Sin parámetros: empresaId/sucursalId/permiso de costos SIEMPRE salen de
/// ICurrentUserService (JWT). El job nocturno NO usa este tipo — llama
/// directo a PanelHoyCalculator con el empresaId que está iterando, así no
/// existe ningún mensaje con un EmpresaId seteable alcanzable desde HTTP.
/// </summary>
public record ObtenerPanelHoyQuery : IRequest<PanelHoyDto>;
