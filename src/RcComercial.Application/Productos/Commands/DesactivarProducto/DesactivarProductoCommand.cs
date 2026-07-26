using MediatR;

namespace RcComercial.Application.Productos.Commands.DesactivarProducto;

public record DesactivarProductoCommand(Guid Id) : IRequest<bool>;
