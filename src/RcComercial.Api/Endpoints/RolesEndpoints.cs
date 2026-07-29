using MediatR;
using RcComercial.Application.Roles.Commands;
using RcComercial.Application.Roles.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class RolesEndpoints
{
    public static void MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/roles").RequireAuthorization(Permisos.AdminRoles);

        group.MapGet("/", async (IMediator mediator) => Results.Ok(await mediator.Send(new ListarRolesQuery())));

        group.MapGet("/permisos", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarPermisosQuery())));

        group.MapPost("/", async (CrearRolCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)));

        group.MapPut("/{id:guid}", async (Guid id, EditarRolCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var resultado = await mediator.Send(command);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        });
    }
}
