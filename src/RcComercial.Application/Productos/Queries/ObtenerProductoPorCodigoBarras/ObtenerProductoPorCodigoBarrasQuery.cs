using MediatR;
using RcComercial.Application.Productos.Dtos;

namespace RcComercial.Application.Productos.Queries.ObtenerProductoPorCodigoBarras;

public record ObtenerProductoPorCodigoBarrasQuery(string CodigoBarras) : IRequest<ProductoPorCodigoResult?>;
