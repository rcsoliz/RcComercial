namespace RcComercial.Application.Caja;

public record SesionCajaDto(
    Guid Id,
    Guid SucursalId,
    DateTimeOffset Apertura,
    DateTimeOffset? Cierre,
    decimal MontoInicial,
    decimal? MontoCierreDeclarado,
    decimal? MontoCierreCalculado,
    string Estado);
