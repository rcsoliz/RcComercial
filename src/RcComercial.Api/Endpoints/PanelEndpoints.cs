using MediatR;
using RcComercial.Application.Panel.Queries.ObtenerPanelAlertas;
using RcComercial.Application.Panel.Queries.ObtenerPanelHistorico;
using RcComercial.Application.Panel.Queries.ObtenerPanelHoy;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class PanelEndpoints
{
    public static void MapPanelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/panel").RequireAuthorization(Permisos.ReportesVer);

        group.MapGet("/hoy", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerPanelHoyQuery())));

        group.MapGet("/alertas", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerPanelAlertasQuery())));

        group.MapGet("/historico", async (DateOnly desde, DateOnly hasta, IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerPanelHistoricoQuery(desde, hasta))));
    }
}
