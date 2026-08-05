using MediatR;
using RcComercial.Application.Clientes.Commands;
using RcComercial.Application.Clientes.Queries;
using RcComercial.Application.Clientes.Queries.ListarClientes;
using RcComercial.Application.Ventas.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class ClientesEndpoints
{
    public static void MapClientesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/clientes");

        group.MapGet("/", async (string? buscar, string? estado, int? pagina, IMediator mediator) =>
            Results.Ok(await mediator.Send(new BuscarClientesQuery(buscar, estado, pagina ?? 1))))
            .RequireAuthorization();

        group.MapGet("/listado", async (string? buscar, string? estado, int? pagina, IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarClientesQuery(buscar, pagina ?? 1, estado))))
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var resultado = await mediator.Send(new ObtenerClienteQuery(id));
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization();

        group.MapGet("/{id:guid}/ventas", async (Guid id, int? pagina, IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarVentasPorClienteQuery(id, pagina ?? 1))))
            .RequireAuthorization(Permisos.VentasVerHistorial);

        group.MapPost("/", async (CrearClienteCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.ClientesCrearEditar);

        group.MapPut("/{id:guid}", async (Guid id, EditarClienteCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var resultado = await mediator.Send(command);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization(Permisos.ClientesCrearEditar);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var ok = await mediator.Send(new DesactivarClienteCommand(id));
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization(Permisos.ClientesEliminar);
    }
}
