using MediatR;
using RcComercial.Application.Caja.Commands;
using RcComercial.Application.Caja.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class CajaEndpoints
{
    public static void MapCajaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/caja");

        group.MapPost("/abrir", async (AbrirCajaCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.CajaAbrirCerrar);

        group.MapPost("/cerrar", async (CerrarCajaCommand command, IMediator mediator) =>
        {
            var resultado = await mediator.Send(command);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization(Permisos.CajaAbrirCerrar);

        group.MapGet("/abierta", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerSesionAbiertaQuery())))
            .RequireAuthorization();
    }
}
