using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Compra : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public Guid SucursalId { get; set; }
    public Guid ProveedorId { get; set; }
    public string Numero { get; set; } = default!;
    public string? NroFacturaProv { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    public string Estado { get; set; } = "RECIBIDA";
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public Guid UsuarioId { get; set; }

    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
}
