using MediatR;
using RcComercial.Application.Productos.Dtos;

namespace RcComercial.Application.Productos.Queries.BuscarProductos;

public record BuscarProductosQuery(string? Buscar, int Pagina = 1) : IRequest<List<ProductoListItemDto>>;
