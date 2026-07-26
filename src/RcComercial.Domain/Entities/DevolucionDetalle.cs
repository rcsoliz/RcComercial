using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class DevolucionDetalle : BaseEntity
{
    public Guid DevolucionId { get; set; }
    public Guid VentaDetalleId { get; set; }
    public decimal CantidadBase { get; set; }
    public bool ReingresaStock { get; set; } = true; // false si vuelve dañado
    public decimal Monto { get; set; }
}
