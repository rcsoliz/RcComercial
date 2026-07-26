using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class VentaDetalle : BaseEntity
{
    public Guid VentaId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid? PresentacionId { get; set; } // NULL = unidad base
    public Guid? LoteId { get; set; }         // asignado por FEFO si maneja lote
    public decimal Cantidad { get; set; }     // en la presentación vendida
    public decimal CantidadBase { get; set; } // convertida a unidad base
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
}
