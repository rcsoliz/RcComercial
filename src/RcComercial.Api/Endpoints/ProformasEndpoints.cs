using MediatR;
using RcComercial.Application.Proformas.Commands.ConvertirProformaEnVenta;
using RcComercial.Application.Proformas.Commands.CrearProforma;
using RcComercial.Application.Proformas.Commands.RechazarProforma;
using RcComercial.Application.Proformas.Queries.ListarProformas;
using RcComercial.Application.Proformas.Queries.ObtenerProforma;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class ProformasEndpoints
{
    public static void MapProformasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/proformas");

        group.MapGet("/", async (string? buscar, string? estado, int? pagina, Guid? clienteId, IMediator mediator) =>
            Results.Ok(await mediator.Send(new ListarProformasQuery(buscar, pagina ?? 1, estado, clienteId))))
            .RequireAuthorization(Permisos.ProformasCrear);

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var resultado = await mediator.Send(new ObtenerProformaQuery(id));
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization(Permisos.ProformasCrear);

        group.MapPost("/", async (CrearProformaCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.ProformasCrear);

        group.MapPost("/{id:guid}/rechazar", async (Guid id, RechazarProformaCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var ok = await mediator.Send(command);
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization(Permisos.ProformasAnular);

        // Gateada con ventas.crear (no proformas.crear): el resultado es una
        // venta real, con las mismas reglas de negocio que crear una venta.
        group.MapPost("/{id:guid}/convertir", async (Guid id, ConvertirProformaEnVentaCommand command, IMediator mediator) =>
        {
            if (id != command.ProformaId) return Results.BadRequest();
            return Results.Ok(await mediator.Send(command));
        }).RequireAuthorization(Permisos.VentasCrear);
    }
}
