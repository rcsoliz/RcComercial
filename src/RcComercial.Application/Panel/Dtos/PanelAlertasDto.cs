namespace RcComercial.Application.Panel.Dtos;

public record ProductoBajoMinimoDto(Guid ProductoId, string Nombre, decimal StockTotal, decimal StockMinimo);

public record LotePorVencerDto(
    Guid LoteId, Guid ProductoId, string ProductoNombre, string LoteNumero,
    DateOnly FechaVencimiento, decimal Cantidad, int DiasParaVencer);

public record DiferenciaCajaDto(
    Guid SesionId, Guid UsuarioId, string UsuarioNombre, DateTimeOffset Cierre,
    decimal MontoDeclarado, decimal MontoCalculado, decimal Diferencia);

public record PanelAlertasDto(
    List<ProductoBajoMinimoDto> ProductosBajoMinimo,
    List<LotePorVencerDto> LotesPorVencer30,
    List<LotePorVencerDto> LotesPorVencer60,
    List<LotePorVencerDto> LotesPorVencer90,
    List<DiferenciaCajaDto> DiferenciasCaja);
