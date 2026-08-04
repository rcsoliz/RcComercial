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
    List<SyncPresentacionDto> Presentaciones,
    // Stock total de la empresa (todas las sucursales) al momento de sincronizar:
    // referencia para el descuento OPTIMISTA local; el backend revalida el
    // stock real de todas formas al recibir la venta.
    decimal StockTotal,
    // Umbral para el badge de "stock bajo" en el grid del POS (Sesión Venta).
    decimal StockMinimo);

public record SyncCatalogoDto(string Version, List<SyncProductoDto> Productos);
