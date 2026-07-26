using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Api.Productos;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Productos.Commands.ActualizarProducto;
using RcComercial.Application.Productos.Commands.CambiarPrecio;
using RcComercial.Application.Productos.Commands.CrearProducto;
using RcComercial.Application.Productos.Commands.DesactivarProducto;
using RcComercial.Application.Productos.Commands.ImportarProductos;
using RcComercial.Application.Productos.Queries.BuscarProductos;
using RcComercial.Application.Productos.Queries.ObtenerProductoPorCodigoBarras;
using RcComercial.Domain.Common;
using RcComercial.Infrastructure.Persistence;

namespace RcComercial.Api.Endpoints;

public record CambiarPrecioRequest(Guid? PresentacionId, decimal NuevoPrecio);

public static class ProductosEndpoints
{
    public static void MapProductosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/productos");

        group.MapGet("/", async (string? buscar, int? pagina, IMediator mediator) =>
            Results.Ok(await mediator.Send(new BuscarProductosQuery(buscar, pagina ?? 1))))
            .RequireAuthorization();

        group.MapGet("/por-codigo/{codigoBarras}", async (string codigoBarras, IMediator mediator) =>
        {
            var resultado = await mediator.Send(new ObtenerProductoPorCodigoBarrasQuery(codigoBarras));
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization();

        group.MapPost("/", async (CrearProductoCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)))
            .RequireAuthorization(Permisos.ProductosCrearEditar);

        group.MapPut("/{id:guid}", async (Guid id, ActualizarProductoCommand command, IMediator mediator) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var resultado = await mediator.Send(command);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }).RequireAuthorization(Permisos.ProductosCrearEditar);

        group.MapPut("/{id:guid}/precio", async (Guid id, CambiarPrecioRequest request, IMediator mediator) =>
        {
            var ok = await mediator.Send(new CambiarPrecioCommand(id, request.PresentacionId, request.NuevoPrecio));
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization(Permisos.ProductosCambiarPrecios);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var ok = await mediator.Send(new DesactivarProductoCommand(id));
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization(Permisos.ProductosEliminar);

        group.MapPost("/importar", async (
            HttpRequest http, IMediator mediator, AppDbContext db, ICurrentUserService currentUser) =>
        {
            if (!http.HasFormContentType) return Results.BadRequest("Se esperaba multipart/form-data.");
            var form = await http.ReadFormAsync();
            var archivo = form.Files["archivo"];
            if (archivo is null || archivo.Length == 0) return Results.BadRequest("Falta el archivo 'archivo'.");

            Guid sucursalId;
            if (Guid.TryParse(http.Query["sucursalId"], out var sucursalIdQuery))
            {
                sucursalId = sucursalIdQuery;
            }
            else if (currentUser.SucursalId is { } sucursalUsuario)
            {
                sucursalId = sucursalUsuario;
            }
            else
            {
                var sucursales = await db.Sucursales.Where(s => s.Activo).Select(s => s.Id).ToListAsync();
                if (sucursales.Count != 1)
                    return Results.BadRequest(
                        "Especifique 'sucursalId': la empresa tiene más de una sucursal activa.");
                sucursalId = sucursales[0];
            }

            await using var stream = archivo.OpenReadStream();
            var parseo = await CsvProductoParser.ParsearAsync(stream);
            var resultado = await mediator.Send(
                new ImportarProductosCommand(sucursalId, parseo.Filas, parseo.Errores));
            return Results.Ok(resultado);
        }).RequireAuthorization(Permisos.ProductosCrearEditar);
    }
}
