using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>Cola/log de envíos WhatsApp (facturas, resúmenes, alertas).</summary>
public class Notificacion : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public string Tipo { get; set; } = default!;
    public string Destinatario { get; set; } = default!;
    public string Contenido { get; set; } = default!;
    public string Estado { get; set; } = EstadosNotificacion.Pendiente;
    public Guid? ReferenciaId { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EnviadoEn { get; set; }
    public short Intentos { get; set; }
    public DateTimeOffset? ProximoIntentoEn { get; set; }

    /// <summary>Enlace wa.me generado por WaLinkSender (el cajero lo toca para enviar).</summary>
    public string? EnlaceGenerado { get; set; }
}
