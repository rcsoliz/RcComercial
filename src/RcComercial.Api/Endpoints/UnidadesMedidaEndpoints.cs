using MediatR;
using RcComercial.Application.UnidadesMedida.Queries;

namespace RcComercial.Api.Endpoints;

public static class UnidadesMedidaEndpoints
{
    public static void MapUnidadesMedidaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/unidades-medida", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarUnidadesMedidaQuery())))
            .RequireAuthorization();
    }
}
