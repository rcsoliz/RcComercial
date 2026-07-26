namespace RcComercial.Application.Devoluciones;

public record DevolucionDetalleDto(Guid Id, Guid VentaDetalleId, decimal CantidadBase, bool ReingresaStock, decimal Monto);

public record DevolucionDto(
    Guid Id, string Numero, DateTimeOffset Fecha, string Motivo, decimal Total, List<DevolucionDetalleDto> Detalles);
