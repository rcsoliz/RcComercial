using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class Usuario : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public Guid? SucursalId { get; set; }
    public string Nombre { get; set; } = default!;
    public string UsuarioLogin { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public Guid RolId { get; set; }
    public Rol Rol { get; set; } = default!;
    public string? TelefonoWhatsapp { get; set; }
    public bool Activo { get; set; } = true;
    public short IntentosFallidos { get; set; }
    public DateTimeOffset? BloqueadoHasta { get; set; }
    public bool DebeCambiarPassword { get; set; } = true;
    public int PermisosVersion { get; set; } = 1;
    public DateTimeOffset? UltimoLogin { get; set; }

    /// <summary>
    /// Acceso al back-office de la plataforma (/api/plataforma), fuera del
    /// sistema de roles/permisos por empresa. Deliberadamente SIN setter
    /// expuesto en ningún comando (CrearUsuarioCommand/EditarUsuarioCommand
    /// no tienen este campo): solo se escribe por seed o SQL manual.
    /// </summary>
    public bool EsSuperadmin { get; set; }
}
