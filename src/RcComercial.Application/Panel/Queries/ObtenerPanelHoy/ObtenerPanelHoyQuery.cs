using MediatR;
using RcComercial.Application.Panel.Dtos;

namespace RcComercial.Application.Panel.Queries.ObtenerPanelHoy;

/// <summary>
/// EmpresaId/IncluirCostos son SOLO para uso interno del job nocturno
/// (ResumenDiarioBackgroundService, sin JWT). El endpoint HTTP nunca los
/// expone: siempre se resuelven de ICurrentUserService/permisos del token.
/// </summary>
public record ObtenerPanelHoyQuery(Guid? EmpresaId = null, bool? IncluirCostos = null) : IRequest<PanelHoyDto>;
