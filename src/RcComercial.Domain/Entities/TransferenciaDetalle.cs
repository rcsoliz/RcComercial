using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class TransferenciaDetalle : BaseEntity
{
    public Guid TransferenciaId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid? LoteId { get; set; }
    public decimal CantidadBase { get; set; }
}
