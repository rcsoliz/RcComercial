using MediatR;
using RcComercial.Application.Proveedores.Commands;
using RcComercial.Application.Proveedores.Queries;
using RcComercial.Application.Proveedores.Queries.ListarProveedores;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class ProveedoresEndpoints
{
    public static void MapProveedoresEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/proveedores");

        group.MapGet("/", async (string? buscar, string? estado, int? pagina, IMediator mediator) =>
            Results.Ok(await mediator.Send(new BuscarProveedoresQuery(buscar, estado, pagina ?? 1))))
            .RequireAuthorization();

        group.MapGet("/listado", async (string? buscar, string? estado, int? pagina, IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarProveedoresQuery(buscar, pagina ?? 1, estado))))
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var resultado = await mediator.Send(new ObtenerProveedorQuery(id));
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization();

        group.MapPost("/", async (CrearProveedorCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.ProveedoresCrearEditar);

        group.MapPut("/{id:guid}", async (Guid id, EditarProveedorCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var resultado = await mediator.Send(command);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization(Permisos.ProveedoresCrearEditar);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var ok = await mediator.Send(new DesactivarProveedorCommand(id));
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization(Permisos.ProveedoresEliminar);
    }
}
