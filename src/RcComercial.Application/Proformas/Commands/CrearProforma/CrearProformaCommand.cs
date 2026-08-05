using MediatR;
using RcComercial.Application.Proformas.Dtos;

namespace RcComercial.Application.Proformas.Commands.CrearProforma;

public record CrearProformaDetalleCommand(
    Guid ProductoId, Guid? PresentacionId, decimal Cantidad, decimal PrecioUnitario, decimal Descuento);

public record CrearProformaCommand(
    Guid? ClienteId,
    Guid? VehiculoId,
    DateOnly? ValidaHasta,
    Guid? SucursalId,
    List<CrearProformaDetalleCommand> Detalles) : IRequest<ProformaDto>;
