using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Categoria : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public string Nombre { get; set; } = default!;
    public Guid? PadreId { get; set; }
    public bool Activo { get; set; } = true;
}
