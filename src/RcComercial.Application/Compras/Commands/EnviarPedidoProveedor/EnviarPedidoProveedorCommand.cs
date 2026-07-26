using MediatR;

namespace RcComercial.Application.Compras.Commands.EnviarPedidoProveedor;

public record EnviarPedidoProveedorDetalle(Guid ProductoId, decimal Cantidad);

public record EnviarPedidoProveedorCommand(
    Guid ProveedorId, List<EnviarPedidoProveedorDetalle> Detalles) : IRequest<bool>;
