using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Proveedor : BaseEntity, ITenantEntity, IAuditable
{
    public Guid EmpresaId { get; set; }
    public string Nombre { get; set; } = default!;
    public string? Nit { get; set; }
    public string? TelefonoWhatsapp { get; set; }
    public int DiasCredito { get; set; }
    public int LeadTimeDias { get; set; } = 3; // insumo del sugerido de compra
    public bool Activo { get; set; } = true;

    public DateTimeOffset CreadoEn { get; set; }
    public Guid? CreadoPor { get; set; }
    public DateTimeOffset? ActualizadoEn { get; set; }
    public Guid? ActualizadoPor { get; set; }
}
