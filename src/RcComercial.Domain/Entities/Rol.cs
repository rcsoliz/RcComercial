using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>EmpresaId NULL = rol de sistema (plantilla global, no editable).</summary>
public class Rol : BaseEntity
{
    public Guid? EmpresaId { get; set; }
    public string Nombre { get; set; } = default!;
    public bool EsSistema { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<RolPermiso> Permisos { get; set; } = new List<RolPermiso>();
}
