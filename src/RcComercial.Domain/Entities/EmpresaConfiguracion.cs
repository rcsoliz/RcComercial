namespace RcComercial.Domain.Entities;

/// <summary>Clave-valor por empresa: evolucionar sin ALTER TABLE.</summary>
public class EmpresaConfiguracion
{
    public Guid EmpresaId { get; set; }
    public string Clave { get; set; } = default!;
    public string Valor { get; set; } = default!; // JSONB en BD
    public DateTimeOffset ActualizadoEn { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ActualizadoPor { get; set; }
}
