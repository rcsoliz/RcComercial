using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>
/// El Id lo genera el CLIENTE (Uuid7) para soportar modo offline:
/// la venta creada sin internet sube con su ID definitivo.
/// </summary>
public class Venta : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public Guid SucursalId { get; set; }
    public Guid? SesionCajaId { get; set; }
    public Guid? ClienteId { get; set; } // NULL = consumidor final S/N
    public Guid? VehiculoId { get; set; } // taller mecánico: qué auto se atendió
    public string Numero { get; set; } = default!;
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    public string Estado { get; set; } = EstadosVenta.Completada;
    public string? MotivoAnulacion { get; set; }
    public Guid? AnuladaPor { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public Guid UsuarioId { get; set; }

    public string? Cuf { get; set; }
    public string? Cufd { get; set; }
    public string EstadoSiat { get; set; } = EstadosSiat.SinFactura;

    public bool EnviadaWhatsapp { get; set; }
    public bool CreadoOffline { get; set; }
    public DateTimeOffset? SincronizadoEn { get; set; }

    public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
