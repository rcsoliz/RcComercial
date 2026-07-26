using MediatR;
using RcComercial.Application.Compras.Commands.CrearCompra;
using RcComercial.Application.Compras.Commands.EnviarPedidoProveedor;
using RcComercial.Application.Compras.Queries.ObtenerSugeridoCompra;
using RcComercial.Domain.Common;

namespace RcComercial.Api.Endpoints;

public static class ComprasEndpoints
{
    public static void MapComprasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/compras");

        group.MapGet("/sugerido", async (Guid proveedorId, Guid? sucursalId, IMediator mediator) =>
            Results.Ok(await mediator.Send(new ObtenerSugeridoCompraQuery(proveedorId, sucursalId))))
            .RequireAuthorization(Permisos.ComprasCrear);

        group.MapPost("/", async (CrearCompraCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.ComprasCrear);

        group.MapPost("/pedido-proveedor", async (EnviarPedidoProveedorCommand command, IMediator mediator) =>
            await mediator.Send(command) ? Results.Ok() : Results.NotFound())
            .RequireAuthorization(Permisos.ComprasCrear);
    }
}
