using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Cliente : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public string Nombre { get; set; } = default!;
    public string? NitCi { get; set; }
    public string TipoDocumento { get; set; } = "CI";
    public string? TelefonoWhatsapp { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ActualizadoEn { get; set; }
}
