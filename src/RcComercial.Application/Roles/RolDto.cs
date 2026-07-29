namespace RcComercial.Application.Roles;

public record PermisoDto(short Id, string Codigo, string Modulo, string Nombre, bool EsSensible);

public record RolDto(Guid Id, string Nombre, bool EsSistema, bool Activo, List<short> PermisoIds);
