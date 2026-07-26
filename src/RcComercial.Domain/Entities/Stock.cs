using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>
/// Saldo materializado. La verdad histórica vive en MovimientoInventario;
/// stock y movimiento se actualizan SIEMPRE en la misma transacción.
/// </summary>
public class Stock : BaseEntity
{
    public Guid SucursalId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid? LoteId { get; set; }
    public decimal Cantidad { get; set; } // SIEMPRE en unidad base
    public DateTimeOffset ActualizadoEn { get; set; } = DateTimeOffset.UtcNow;
}
