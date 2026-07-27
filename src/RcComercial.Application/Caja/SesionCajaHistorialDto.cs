namespace RcComercial.Application.Caja;

public record SesionCajaHistorialDto(
    Guid Id,
    Guid SucursalId,
    Guid UsuarioId,
    string UsuarioNombre,
    DateTimeOffset Apertura,
    DateTimeOffset? Cierre,
    decimal MontoInicial,
    decimal? MontoCierreDeclarado,
    decimal? MontoCierreCalculado,
    decimal? Diferencia,
    string Estado);
