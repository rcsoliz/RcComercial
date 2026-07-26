using MediatR;
using RcComercial.Application.Devoluciones.Commands;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class DevolucionesEndpoints
{
    public static void MapDevolucionesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/devoluciones")
            .MapPost("/", async (CrearDevolucionCommand command, IMediator mediator) =>
                Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.VentasAnular);
    }
}
