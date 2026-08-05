using MediatR;
using RcComercial.Application.Vehiculos.Commands;
using RcComercial.Application.Vehiculos.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class VehiculosEndpoints
{
    public static void MapVehiculosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/vehiculos");

        group.MapPost("/", async (CrearVehiculoCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.VehiculosCrearEditar);

        group.MapPut("/{id:guid}", async (Guid id, EditarVehiculoCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var resultado = await mediator.Send(command);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization(Permisos.VehiculosCrearEditar);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var ok = await mediator.Send(new DesactivarVehiculoCommand(id));
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization(Permisos.VehiculosEliminar);

        group.MapGet("/{id:guid}/historial", async (Guid id, IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerHistorialVehiculoQuery(id))))
            .RequireAuthorization(Permisos.VentasVerHistorial);
    }
}
