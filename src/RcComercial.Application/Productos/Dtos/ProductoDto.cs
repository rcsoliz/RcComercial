namespace RcComercial.Application.Productos.Dtos;

public record PresentacionDto(
    Guid Id,
    string Nombre,
    decimal Factor,
    string? CodigoBarras,
    decimal Precio,
    decimal? PrecioMayorista,
    decimal? CantidadMinMayorista,
    bool EsPredeterminada);

public record FichaFarmaciaDto(
    string? PrincipioActivo,
    string? Concentracion,
    string? FormaFarmaceutica,
    string? Laboratorio,
    string? RegistroSanitario,
    string Clasificacion,
    bool RequiereCadenaFrio);

public record ProductoDto(
    Guid Id,
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
    bool Activo,
    int? CodigoProductoSin,
    int? CodigoUnidadSin,
    List<PresentacionDto> Presentaciones,
    FichaFarmaciaDto? FichaFarmacia);

public record ProductoListItemDto(
    Guid Id, string? Codigo, string? CodigoBarras, string Nombre, decimal PrecioBase, bool Activo,
    string? CategoriaNombre, string? MarcaNombre, decimal StockTotal, decimal StockMinimo, bool EsServicio);

public record ProductoMaestroDto(
    Guid Id, string CodigoBarras, string Nombre, string? Marca, string? Contenido, short? RubroId);

public record ProductoPorCodigoResult(bool EsSugerencia, ProductoDto? Producto, ProductoMaestroDto? Sugerencia);
