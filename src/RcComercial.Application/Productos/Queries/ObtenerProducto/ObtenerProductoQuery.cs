using MediatR;
using RcComercial.Application.Productos.Dtos;

namespace RcComercial.Application.Productos.Queries.ObtenerProducto;

public record ObtenerProductoQuery(Guid Id) : IRequest<ProductoDto?>;
