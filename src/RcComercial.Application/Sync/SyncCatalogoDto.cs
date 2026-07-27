namespace RcComercial.Application.Sync;

// Deliberadamente SIN costo/utilidad: este catálogo se cachea en el
// dispositivo (IndexedDB) para que el POS busque sin red — nunca debe
// llevarse costo_promedio a un dispositivo que puede perderse o compartirse.
public record SyncPresentacionDto(
    Guid Id, string Nombre, decimal Factor, string? CodigoBarras,
    decimal Precio, decimal? PrecioMayorista, decimal? CantidadMinMayorista, bool EsPredeterminada);

public record SyncProductoDto(
    Guid Id, string? Codigo, string? CodigoBarras, string Nombre,
    decimal PrecioBase, bool ManejaLote, bool EsControlado, bool PermiteDecimales,
    List<SyncPresentacionDto> Presentaciones);

public record SyncCatalogoDto(string Version, List<SyncProductoDto> Productos);
