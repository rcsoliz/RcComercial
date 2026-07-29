namespace RcComercial.Application.Usuarios;

public record UsuarioDto(
    Guid Id, string Nombre, string UsuarioLogin, Guid RolId, string RolNombre,
    Guid? SucursalId, string? SucursalNombre, string? TelefonoWhatsapp, bool Activo,
    DateTimeOffset? UltimoLogin, bool DebeCambiarPassword);

/// <summary>El temporal se devuelve UNA sola vez (no se puede recuperar después: solo queda el hash).</summary>
public record UsuarioConPasswordTemporalDto(UsuarioDto Usuario, string PasswordTemporal);
