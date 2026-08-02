using MediatR;
using RcComercial.Application.Productos.Dtos;

namespace RcComercial.Application.Productos.Queries.ListarProductos;

/// <summary>Página de resultados + total que matchea el filtro (para calcular cuántas páginas hay).</summary>
public record ListarProductosResultDto(List<ProductoListItemDto> Items, int Total, int Pagina, int TamanoPagina);

/// <summary>
/// Paralelo a BuscarProductosQuery (POS/Compras, sin tocar): este es
/// específico para el listado de ProductosView, con paginado real (con
/// total) y filtro de estado. Estado: "activos" (default) | "inactivos" | "todos".
/// </summary>
public record ListarProductosQuery(string? Buscar, int Pagina = 1, string? Estado = null)
    : IRequest<ListarProductosResultDto>;
