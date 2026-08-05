using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Proforma : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public Guid SucursalId { get; set; }
    public Guid? ClienteId { get; set; } // NULL = consumidor final, mismo criterio que Venta.ClienteId
    public Guid? VehiculoId { get; set; }
    public string Numero { get; set; } = default!;
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    public string Estado { get; set; } = EstadosProforma.Pendiente;
    public DateOnly? ValidaHasta { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid? VentaId { get; set; } // se llena solo al convertir
    public string? MotivoRechazo { get; set; }

    public ICollection<ProformaDetalle> Detalles { get; set; } = new List<ProformaDetalle>();
}
