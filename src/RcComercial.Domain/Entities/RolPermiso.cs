namespace RcComercial.Domain.Entities;

public class RolPermiso
{
    public Guid RolId { get; set; }
    public short PermisoId { get; set; }
    public Permiso Permiso { get; set; } = default!;
}
