using MediatR;
using RcComercial.Application.Sync.Queries;

namespace RcComercial.Api.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sync");

        group.MapGet("/catalogo", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerCatalogoSyncQuery())))
            .RequireAuthorization();
    }
}
