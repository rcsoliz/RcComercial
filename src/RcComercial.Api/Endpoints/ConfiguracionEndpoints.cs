using MediatR;
using RcComercial.Application.Configuracion.Commands;
using RcComercial.Application.Configuracion.Queries;
using RcComercial.Application.Empresas.Commands;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class ConfiguracionEndpoints
{
    public static void MapConfiguracionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/configuracion").RequireAuthorization(Permisos.AdminConfiguracion);

        group.MapGet("/", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerConfiguracionQuery())));

        group.MapPut("/", async (ActualizarConfiguracionCommand command, IMediator mediator) =>
        {
            await mediator.Send(command);
            return Results.Ok();
        });

        group.MapPut("/empresa", async (EditarEmpresaCommand command, IMediator mediator) =>
        {
            var resultado = await mediator.Send(command);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        });
    }
}
