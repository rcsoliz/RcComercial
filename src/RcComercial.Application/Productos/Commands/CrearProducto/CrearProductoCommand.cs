using MediatR;
using RcComercial.Application.Productos.Dtos;

namespace RcComercial.Application.Productos.Commands.CrearProducto;

public record CrearProductoPresentacion(
    string Nombre,
    decimal Factor,
    string? CodigoBarras,
    decimal Precio,
    decimal? PrecioMayorista,
    decimal? CantidadMinMayorista,
    bool EsPredeterminada);

public record CrearProductoFichaFarmacia(
    string? PrincipioActivo,
    string? Concentracion,
    string? FormaFarmaceutica,
    string? Laboratorio,
    string? RegistroSanitario,
    string Clasificacion,
    bool RequiereCadenaFrio);

public record CrearProductoCommand(
    string? Codigo,
    string? CodigoBarras,
    string Nombre,
    Guid? CategoriaId,
    Guid? MarcaId,
    short UnidadBaseId,
    decimal PrecioBase,
    decimal StockMinimo,
    bool ManejaLote,
    bool EsControlado,
    bool PermiteDecimales,
    bool EsServicio,
    int? CodigoProductoSin,
    int? CodigoUnidadSin,
    List<CrearProductoPresentacion> Presentaciones,
    CrearProductoFichaFarmacia? FichaFarmacia) : IRequest<ProductoDto>;
