namespace RcComercial.Application.Compras.Dtos;

public record CompraDetalleDto(
    Guid Id,
    Guid ProductoId,
    Guid? PresentacionId,
    decimal Cantidad,
    decimal CantidadBase,
    decimal CostoUnitario,
    string? NumeroLote,
    DateOnly? FechaVencimiento);

public record CompraDto(
    Guid Id,
    string Numero,
    DateTimeOffset Fecha,
    Guid ProveedorId,
    string? NroFacturaProv,
    string Estado,
    decimal Subtotal,
    decimal Total,
    List<CompraDetalleDto> Detalles);

public record CompraListItemDto(
    Guid Id, string Numero, DateTimeOffset Fecha, Guid ProveedorId, string ProveedorNombre,
    string? NroFacturaProv, string Estado, decimal Total);
