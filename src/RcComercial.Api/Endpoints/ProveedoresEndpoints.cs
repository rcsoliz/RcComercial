using MediatR;
using RcComercial.Application.Proveedores.Commands;
using RcComercial.Application.Proveedores.Queries;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class ProveedoresEndpoints
{
    public static void MapProveedoresEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/proveedores");

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarProveedoresQuery())))
            .RequireAuthorization();

        group.MapPost("/", async (CrearProveedorCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.ComprasCrear);
    }
}
