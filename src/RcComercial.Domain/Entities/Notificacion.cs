using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>Cola/log de envíos WhatsApp (facturas, resúmenes, alertas).</summary>
public class Notificacion : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public string Tipo { get; set; } = default!;
    public string Destinatario { get; set; } = default!;
    public string Contenido { get; set; } = default!;
    public string Estado { get; set; } = "PENDIENTE";
    public Guid? ReferenciaId { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EnviadoEn { get; set; }
}
