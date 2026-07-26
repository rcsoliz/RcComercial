using MediatR;

namespace RcComercial.Application.Productos.Commands.CambiarPrecio;

/// <summary>PresentacionId null = cambia el precio base del producto.</summary>
public record CambiarPrecioCommand(Guid ProductoId, Guid? PresentacionId, decimal NuevoPrecio) : IRequest<bool>;
