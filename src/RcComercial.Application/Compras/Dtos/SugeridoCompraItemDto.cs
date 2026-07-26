namespace RcComercial.Application.Compras.Dtos;

public record SugeridoCompraItemDto(
    Guid ProductoId, string ProductoNombre, decimal VentaDiaria, decimal StockActual, decimal CantidadSugerida);
