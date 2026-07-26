using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Pago : BaseEntity
{
    public Guid VentaId { get; set; }
    public string Metodo { get; set; } = MetodosPago.Efectivo;
    public decimal Monto { get; set; }
    public string? ReferenciaQr { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
}
