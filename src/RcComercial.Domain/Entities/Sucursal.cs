using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Sucursal : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public string Nombre { get; set; } = default!;
    public string? Direccion { get; set; }
    public int? CodigoSucursalSiat { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Código CAEB registrado (Fase 9, facturador-by-rc). Nullable: solo se exige al facturar.</summary>
    public string? ActividadEconomica { get; set; }
}
