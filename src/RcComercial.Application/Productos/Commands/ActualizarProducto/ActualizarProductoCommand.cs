using MediatR;
using RcComercial.Application.Productos.Commands.CrearProducto;
using RcComercial.Application.Productos.Dtos;

namespace RcComercial.Application.Productos.Commands.ActualizarProducto;

/// <summary>
/// El precio NO se edita aquí: siempre pasa por CambiarPrecioCommand para
/// garantizar que quede rastro en precio_historial.
/// </summary>
public record ActualizarProductoCommand(
    Guid Id,
    string? Codigo,
    string? CodigoBarras,
    string Nombre,
    Guid? CategoriaId,
    Guid? MarcaId,
    short UnidadBaseId,
    decimal StockMinimo,
    bool ManejaLote,
    bool EsControlado,
    bool PermiteDecimales,
    List<CrearProductoPresentacion> Presentaciones) : IRequest<ProductoDto?>;
