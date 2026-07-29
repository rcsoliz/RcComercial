using RcComercial.Application.Usuarios;

namespace RcComercial.Application.Plataforma;

public record EmpresaPlataformaListItemDto(
    Guid Id, string Nombre, string? Nit, string RubroNombre, bool Activo, int NroUsuarios, DateTimeOffset? UltimaVenta);

/// <summary>PasswordTemporal se devuelve UNA sola vez: no se puede recuperar después (solo queda el hash).</summary>
public record AltaEmpresaResultDto(Guid EmpresaId, Guid SucursalId, UsuarioDto Dueno, string PasswordTemporal);
