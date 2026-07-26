using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Marca : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public string Nombre { get; set; } = default!;
}
