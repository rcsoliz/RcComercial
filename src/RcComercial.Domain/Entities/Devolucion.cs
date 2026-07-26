using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Devolucion : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public Guid SucursalId { get; set; }
    public Guid VentaId { get; set; }
    public string Numero { get; set; } = default!;
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    public string Motivo { get; set; } = default!;
    public decimal Total { get; set; }
    public Guid UsuarioId { get; set; }
    public string? CufNotaCredito { get; set; }
    public string EstadoSiat { get; set; } = "SIN_NOTA";

    public ICollection<DevolucionDetalle> Detalles { get; set; } = new List<DevolucionDetalle>();
}
