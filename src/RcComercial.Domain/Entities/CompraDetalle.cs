using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class CompraDetalle : BaseEntity
{
    public Guid CompraId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid? PresentacionId { get; set; }
    public decimal Cantidad { get; set; }      // en la presentación comprada
    public decimal CantidadBase { get; set; }  // convertida a unidad base
    public decimal CostoUnitario { get; set; } // por unidad base
    public string? NumeroLote { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
}
