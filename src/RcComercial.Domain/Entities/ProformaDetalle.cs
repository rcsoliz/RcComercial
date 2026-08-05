using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class ProformaDetalle : BaseEntity
{
    public Guid ProformaId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid? PresentacionId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
}
