using MediatR;
using RcComercial.Application.Usuarios.Commands;
using RcComercial.Application.Usuarios.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class UsuariosEndpoints
{
    public static void MapUsuariosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/usuarios").RequireAuthorization(Permisos.AdminUsuarios);

        group.MapGet("/", async (string? buscar, string? estado, int? pagina, IMediator mediator) =>
            Results.Ok(await mediator.Send(new BuscarUsuariosQuery(buscar, estado, pagina ?? 1))));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var resultado = await mediator.Send(new ObtenerUsuarioQuery(id));
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        });

        group.MapPost("/", async (CrearUsuarioCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)));

        group.MapPut("/{id:guid}", async (Guid id, EditarUsuarioCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var resultado = await mediator.Send(command);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        });

        group.MapPost("/{id:guid}/restablecer-password", async (Guid id, IMediator mediator) =>
        {
            var resultado = await mediator.Send(new RestablecerContrasenaCommand(id));
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var ok = await mediator.Send(new DesactivarUsuarioCommand(id));
            return ok ? Results.Ok() : Results.NotFound();
        });
    }
}
