namespace RcComercial.Application.Panel.Dtos;

public record VentaPorUsuarioDto(Guid UsuarioId, string UsuarioNombre, int NumeroVentas, decimal Total);

public record TopProductoDto(
    Guid ProductoId, string Nombre, decimal CantidadVendida, decimal MontoVendido,
    decimal? CostoTotal, decimal? Utilidad);

public record SesionCajaAbiertaDto(Guid Id, Guid UsuarioId, string UsuarioNombre, DateTimeOffset Apertura, decimal MontoInicial);

public record MontoPorMetodoPagoDto(string Metodo, decimal Monto);

public record PanelHoyDto(
    decimal TotalVendido,
    int NumeroVentas,
    decimal TicketPromedio,
    List<VentaPorUsuarioDto> VentasPorUsuario,
    int NumeroAnulaciones,
    decimal MontoAnulaciones,
    decimal MontoDescuentos,
    List<TopProductoDto> TopProductos,
    List<SesionCajaAbiertaDto> CajasAbiertas,
    List<MontoPorMetodoPagoDto> MontosPorMetodoPago);
