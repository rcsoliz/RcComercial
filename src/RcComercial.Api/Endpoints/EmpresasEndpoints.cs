using MediatR;
using RcComercial.Application.Empresas.Queries.ObtenerEmpresaActual;

namespace RcComercial.Api.Endpoints;

public static class EmpresasEndpoints
{
    public static void MapEmpresasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/empresa");

        group.MapGet("/actual", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerEmpresaActualQuery())))
            .RequireAuthorization();
    }
}
