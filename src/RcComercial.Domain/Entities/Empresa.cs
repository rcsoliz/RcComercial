using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Empresa : BaseEntity
{
    public string Nombre { get; set; } = default!;
    public string? Nit { get; set; }
    public short RubroId { get; set; }
    public Rubro Rubro { get; set; } = default!;
    public string? TelefonoWhatsapp { get; set; }
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
}
