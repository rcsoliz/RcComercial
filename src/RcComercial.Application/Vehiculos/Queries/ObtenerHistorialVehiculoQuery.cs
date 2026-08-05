using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Vehiculos.Queries;

public record HistorialVehiculoLineaDto(Guid ProductoId, string ProductoNombre, decimal Cantidad, decimal PrecioUnitario);

public record HistorialVehiculoItemDto(
    Guid Id, string TipoDocumento, string Numero, DateTimeOffset Fecha, string Estado,
    decimal Total, List<HistorialVehiculoLineaDto> Lineas);

public record ObtenerHistorialVehiculoQuery(Guid VehiculoId) : IRequest<List<HistorialVehiculoItemDto>>;

/// <summary>
/// Historial de servicios de un vehículo: une Ventas y Proformas (incluye
/// cotizaciones nunca convertidas — contexto útil para el taller). El
/// filtro ITenantEntity de Vehiculo ya garantiza que solo se puede pedir el
/// historial de un vehículo de la empresa actual.
/// </summary>
public class ObtenerHistorialVehiculoQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ObtenerHistorialVehiculoQuery, List<HistorialVehiculoItemDto>>
{
    public async Task<List<HistorialVehiculoItemDto>> Handle(ObtenerHistorialVehiculoQuery request, CancellationToken ct)
    {
        var existe = await db.Vehiculos.AnyAsync(v => v.Id == request.VehiculoId, ct);
        if (!existe) return [];

        var ventas = await db.Ventas
            .Where(v => v.VehiculoId == request.VehiculoId)
            .Include(v => v.Detalles)
            .ToListAsync(ct);

        var proformas = await db.Proformas
            .Where(p => p.VehiculoId == request.VehiculoId)
            .Include(p => p.Detalles)
            .ToListAsync(ct);

        var productoIds = ventas.SelectMany(v => v.Detalles.Select(d => d.ProductoId))
            .Concat(proformas.SelectMany(p => p.Detalles.Select(d => d.ProductoId)))
            .Distinct().ToList();
        var nombresProducto = await db.Productos
            .Where(p => productoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        var itemsVenta = ventas.Select(v => new HistorialVehiculoItemDto(
            v.Id, "VENTA", v.Numero, v.Fecha, v.Estado, v.Total,
            v.Detalles.Select(d => new HistorialVehiculoLineaDto(
                d.ProductoId, nombresProducto.GetValueOrDefault(d.ProductoId, "—"), d.Cantidad, d.PrecioUnitario))
                .ToList()));

        var itemsProforma = proformas.Select(p => new HistorialVehiculoItemDto(
            p.Id, "PROFORMA", p.Numero, p.Fecha, p.Estado, p.Total,
            p.Detalles.Select(d => new HistorialVehiculoLineaDto(
                d.ProductoId, nombresProducto.GetValueOrDefault(d.ProductoId, "—"), d.Cantidad, d.PrecioUnitario))
                .ToList()));

        return itemsVenta.Concat(itemsProforma).OrderByDescending(i => i.Fecha).ToList();
    }
}
