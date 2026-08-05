namespace RcComercial.Application.Vehiculos;

public record VehiculoDto(
    Guid Id, Guid ClienteId, string Placa, string? Marca, string? Modelo,
    short? Anio, string? Color, string? NumeroChasis, bool Activo);
