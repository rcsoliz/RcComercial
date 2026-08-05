using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Vehiculo : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public Guid ClienteId { get; set; }
    public string Placa { get; set; } = default!;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public short? Anio { get; set; }
    public string? Color { get; set; }
    public string? NumeroChasis { get; set; }
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}
