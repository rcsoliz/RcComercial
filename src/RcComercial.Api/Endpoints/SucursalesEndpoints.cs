using MediatR;
using RcComercial.Application.Sucursales.Commands;
using RcComercial.Application.Sucursales.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class SucursalesEndpoints
{
    public static void MapSucursalesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sucursales").RequireAuthorization(Permisos.AdminSucursales);

        group.MapGet("/", async (string? estado, IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarSucursalesQuery(estado))));

        group.MapPost("/", async (CrearSucursalCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)));

        group.MapPut("/{id:guid}", async (Guid id, EditarSucursalCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var resultado = await mediator.Send(command);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var ok = await mediator.Send(new DesactivarSucursalCommand(id));
            return ok ? Results.Ok() : Results.NotFound();
        });
    }
}
