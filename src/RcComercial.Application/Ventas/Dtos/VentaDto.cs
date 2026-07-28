namespace RcComercial.Application.Ventas.Dtos;

public record VentaDetalleDto(
    Guid Id,
    Guid ProductoId,
    Guid? PresentacionId,
    Guid? LoteId,
    decimal Cantidad,
    decimal CantidadBase,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal Total);

public record PagoDto(Guid Id, string Metodo, decimal Monto, string? ReferenciaQr);

public record VentaDto(
    Guid Id,
    string Numero,
    DateTimeOffset Fecha,
    string Estado,
    Guid? ClienteId,
    decimal Subtotal,
    decimal Descuento,
    decimal Total,
    List<VentaDetalleDto> Detalles,
    List<PagoDto> Pagos,
    bool CreadoOffline);
