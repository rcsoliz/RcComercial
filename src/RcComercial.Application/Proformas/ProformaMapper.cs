using RcComercial.Application.Proformas.Dtos;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Proformas;

internal static class ProformaMapper
{
    public static ProformaDto ToDto(Proforma p) => new(
        p.Id, p.Numero, p.Fecha, p.ClienteId, p.VehiculoId, p.Estado, p.ValidaHasta,
        p.Subtotal, p.Descuento, p.Total, p.VentaId, p.MotivoRechazo,
        p.Detalles.Select(d => new ProformaDetalleDto(
            d.Id, d.ProductoId, d.PresentacionId, d.Cantidad, d.CantidadBase,
            d.PrecioUnitario, d.Descuento, d.Total)).ToList());
}
