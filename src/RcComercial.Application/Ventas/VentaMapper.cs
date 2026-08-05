using RcComercial.Application.Ventas.Dtos;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Ventas;

internal static class VentaMapper
{
    public static VentaDto ToDto(Venta v) => new(
        v.Id, v.Numero, v.Fecha, v.Estado, v.ClienteId, v.VehiculoId, v.Subtotal, v.Descuento, v.Total,
        v.Detalles.Select(d => new VentaDetalleDto(
            d.Id, d.ProductoId, d.PresentacionId, d.LoteId, d.Cantidad, d.CantidadBase,
            d.PrecioUnitario, d.Descuento, d.Total)).ToList(),
        v.Pagos.Select(p => new PagoDto(p.Id, p.Metodo, p.Monto, p.ReferenciaQr)).ToList(),
        v.CreadoOffline);
}
