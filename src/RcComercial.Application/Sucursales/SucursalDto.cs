namespace RcComercial.Application.Sucursales;

public record SucursalDto(Guid Id, string Nombre, string? Direccion, bool Activo);
