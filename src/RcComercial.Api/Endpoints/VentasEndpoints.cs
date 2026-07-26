using MediatR;
using RcComercial.Application.Ventas.Commands.AnularVenta;
using RcComercial.Application.Ventas.Commands.CrearVenta;
using RcComercial.Application.Ventas.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public record AnularVentaRequest(string Motivo);

public static class VentasEndpoints
{
    public static void MapVentasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ventas");

        group.MapPost("/", async (CrearVentaCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.VentasCrear);

        group.MapPost("/{id:guid}/anular", async (Guid id, AnularVentaRequest request, IMediator mediator) =>
        {
            var resultado = await mediator.Send(new AnularVentaCommand(id, request.Motivo));
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization(Permisos.VentasAnular);

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var resultado = await mediator.Send(new ObtenerVentaQuery(id));
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization(Permisos.VentasVerHistorial);
    }
}
