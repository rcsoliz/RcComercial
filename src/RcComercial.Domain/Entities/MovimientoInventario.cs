using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>Kardex: única fuente de verdad. Todo entra/sale por aquí.</summary>
public class MovimientoInventario : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public Guid SucursalId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid? LoteId { get; set; }
    public string Tipo { get; set; } = default!;
    public decimal Cantidad { get; set; } // positiva entra, negativa sale
    public decimal? CostoUnitario { get; set; }
    public string? ReferenciaTipo { get; set; }
    public Guid? ReferenciaId { get; set; }
    public Guid UsuarioId { get; set; }
    public string? Observacion { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
}
