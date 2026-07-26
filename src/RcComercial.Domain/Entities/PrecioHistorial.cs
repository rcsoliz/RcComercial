using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class PrecioHistorial : BaseEntity
{
    public Guid ProductoId { get; set; }
    public Guid? PresentacionId { get; set; }
    public decimal PrecioAnterior { get; set; }
    public decimal PrecioNuevo { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
}
