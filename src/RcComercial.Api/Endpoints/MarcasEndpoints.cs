using MediatR;
using RcComercial.Application.Marcas.Commands;
using RcComercial.Application.Marcas.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class MarcasEndpoints
{
    public static void MapMarcasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/marcas");

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarMarcasQuery())))
            .RequireAuthorization();

        group.MapPost("/", async (CrearMarcaCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.ProductosCrearEditar);

        group.MapPut("/{id:guid}", async (Guid id, ActualizarMarcaCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            return await mediator.Send(command) ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization(Permisos.ProductosCrearEditar);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
            await mediator.Send(new DesactivarMarcaCommand(id)) ? Results.Ok() : Results.NotFound())
            .RequireAuthorization(Permisos.ProductosCrearEditar);
    }
}
