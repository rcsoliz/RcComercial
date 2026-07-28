using MediatR;
using RcComercial.Application.Sync.Commands;
using RcComercial.Application.Sync.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sync");

        group.MapGet("/catalogo", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerCatalogoSyncQuery())))
            .RequireAuthorization();

        group.MapPost("/reservar-rango", async (ReservarRangoCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.VentasCrear);

        group.MapPost("/ventas", async (SincronizarVentasCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.VentasCrear);
    }
}
