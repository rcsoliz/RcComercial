namespace RcComercial.Application.Proformas.Dtos;

public record ProformaDetalleDto(
    Guid Id, Guid ProductoId, Guid? PresentacionId, decimal Cantidad,
    decimal CantidadBase, decimal PrecioUnitario, decimal Descuento, decimal Total);

public record ProformaDto(
    Guid Id, string Numero, DateTimeOffset Fecha, Guid? ClienteId, Guid? VehiculoId,
    string Estado, DateOnly? ValidaHasta, decimal Subtotal, decimal Descuento, decimal Total,
    Guid? VentaId, string? MotivoRechazo, List<ProformaDetalleDto> Detalles);

public record ProformaListItemDto(
    Guid Id, string Numero, DateTimeOffset Fecha, Guid? ClienteId, string? ClienteNombre,
    string Estado, decimal Total);
