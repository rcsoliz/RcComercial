using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Lote : BaseEntity
{
    public Guid ProductoId { get; set; }
    public string Numero { get; set; } = default!;
    public DateOnly? FechaVencimiento { get; set; }
}
