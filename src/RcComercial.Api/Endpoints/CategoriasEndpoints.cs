using MediatR;
using RcComercial.Application.Categorias.Commands;
using RcComercial.Application.Categorias.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class CategoriasEndpoints
{
    public static void MapCategoriasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categorias");

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarCategoriasQuery())))
            .RequireAuthorization();

        group.MapPost("/", async (CrearCategoriaCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.ProductosCrearEditar);

        group.MapPut("/{id:guid}", async (Guid id, ActualizarCategoriaCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            return await mediator.Send(command) ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization(Permisos.ProductosCrearEditar);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
            await mediator.Send(new DesactivarCategoriaCommand(id)) ? Results.Ok() : Results.NotFound())
            .RequireAuthorization(Permisos.ProductosCrearEditar);
    }
}
