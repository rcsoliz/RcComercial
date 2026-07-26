namespace RcComercial.Application.Panel.Dtos;

public record VentasPorDiaDto(DateOnly Dia, int NumeroVentas, decimal Total);
