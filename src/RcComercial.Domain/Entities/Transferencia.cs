using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>Sale del origen al ENVIAR, entra al destino al RECIBIR.</summary>
public class Transferencia : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public Guid SucursalOrigenId { get; set; }
    public Guid SucursalDestinoId { get; set; }
    public string Numero { get; set; } = default!;
    public string Estado { get; set; } = "ENVIADA";
    public Guid EnviadoPor { get; set; }
    public Guid? RecibidoPor { get; set; }
    public DateTimeOffset FechaEnvio { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FechaRecepcion { get; set; }

    public ICollection<TransferenciaDetalle> Detalles { get; set; } = new List<TransferenciaDetalle>();
}
