using MediatR;
using RcComercial.Application.Compras.Dtos;

namespace RcComercial.Application.Compras.Commands.CrearCompra;

public record CrearCompraDetalleCommand(
    Guid ProductoId,
    Guid? PresentacionId,
    decimal Cantidad,
    decimal CostoUnitario,
    string? NumeroLote,
    DateOnly? FechaVencimiento);

public record CrearCompraCommand(
    Guid ProveedorId,
    string? NroFacturaProv,
    Guid? SucursalId,
    List<CrearCompraDetalleCommand> Detalles) : IRequest<CompraDto>;
