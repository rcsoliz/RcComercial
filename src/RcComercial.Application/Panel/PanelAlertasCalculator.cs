using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Panel.Dtos;

namespace RcComercial.Application.Panel;

/// <summary>
/// Misma razón de ser que PanelHoyCalculator: lógica compartida entre el
/// endpoint HTTP (empresaId del JWT) y el job nocturno (empresaId explícito),
/// sin pasar por un IRequest de MediatR con un campo EmpresaId bindeable.
/// </summary>
public static class PanelAlertasCalculator
{
    public static async Task<PanelAlertasDto> CalcularAsync(
        IApplicationDbContext db, Guid empresaId, Guid? sucursalId, CancellationToken ct)
    {
        var sucursalIds = await PanelHoyCalculator.ResolverSucursalIdsAsync(db, empresaId, sucursalId, ct);

        var productosBajoMinimo = await ObtenerProductosBajoMinimoAsync(db, empresaId, sucursalIds, ct);
        var (l30, l60, l90) = await ObtenerLotesPorVencerAsync(db, empresaId, sucursalIds, ct);
        var diferencias = await ObtenerDiferenciasCajaAsync(db, empresaId, sucursalIds, ct);

        return new PanelAlertasDto(productosBajoMinimo, l30, l60, l90, diferencias);
    }

    private static async Task<List<ProductoBajoMinimoDto>> ObtenerProductosBajoMinimoAsync(
        IApplicationDbContext db, Guid empresaId, List<Guid> sucursalIds, CancellationToken ct)
    {
        var stockPorProducto = await db.Stocks
            .Where(s => sucursalIds.Contains(s.SucursalId))
            .GroupBy(s => s.ProductoId)
            .Select(g => new { ProductoId = g.Key, Total = g.Sum(s => s.Cantidad) })
            .ToListAsync(ct);

        var productoIds = stockPorProducto.Select(x => x.ProductoId).ToList();
        var productos = await db.Productos.IgnoreQueryFilters()
            .Where(p => productoIds.Contains(p.Id) && p.EmpresaId == empresaId && p.Activo)
            .Select(p => new { p.Id, p.Nombre, p.StockMinimo })
            .ToDictionaryAsync(p => p.Id, ct);

        return stockPorProducto
            .Where(x => productos.ContainsKey(x.ProductoId) && x.Total <= productos[x.ProductoId].StockMinimo)
            .Select(x => new ProductoBajoMinimoDto(
                x.ProductoId, productos[x.ProductoId].Nombre, x.Total, productos[x.ProductoId].StockMinimo))
            .OrderBy(x => x.StockTotal)
            .ToList();
    }

    private static async Task<(List<LotePorVencerDto> D30, List<LotePorVencerDto> D60, List<LotePorVencerDto> D90)>
        ObtenerLotesPorVencerAsync(
            IApplicationDbContext db, Guid empresaId, List<Guid> sucursalIds, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(HoraBolivia.AhoraLocal().Date);

        var lotesConStock = await (
            from l in db.Lotes
            join s in db.Stocks on l.Id equals s.LoteId
            where sucursalIds.Contains(s.SucursalId) && l.FechaVencimiento != null && s.Cantidad > 0
            group s by new { l.Id, l.ProductoId, l.Numero, FechaVencimiento = l.FechaVencimiento!.Value } into g
            select new
            {
                g.Key.Id, g.Key.ProductoId, g.Key.Numero, g.Key.FechaVencimiento, Cantidad = g.Sum(s => s.Cantidad),
            })
            .Where(x => x.FechaVencimiento <= hoy.AddDays(90))
            .ToListAsync(ct);

        var productoIds = lotesConStock.Select(x => x.ProductoId).Distinct().ToList();
        var nombres = await db.Productos.IgnoreQueryFilters()
            .Where(p => productoIds.Contains(p.Id) && p.EmpresaId == empresaId)
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        List<LotePorVencerDto> Bucket(int dias) => lotesConStock
            .Where(x => x.FechaVencimiento <= hoy.AddDays(dias))
            .Select(x => new LotePorVencerDto(
                x.Id, x.ProductoId, nombres.GetValueOrDefault(x.ProductoId, "?"), x.Numero,
                x.FechaVencimiento, x.Cantidad, x.FechaVencimiento.DayNumber - hoy.DayNumber))
            .OrderBy(x => x.FechaVencimiento)
            .ToList();

        return (Bucket(30), Bucket(60), Bucket(90));
    }

    private static async Task<List<DiferenciaCajaDto>> ObtenerDiferenciasCajaAsync(
        IApplicationDbContext db, Guid empresaId, List<Guid> sucursalIds, CancellationToken ct)
    {
        const decimal tolerancia = 0.01m;
        var haceSieteDias = HoraBolivia.AhoraLocal().AddDays(-7).ToUniversalTime();

        var candidatas = await db.SesionesCaja
            .Where(s => sucursalIds.Contains(s.SucursalId) && s.Estado == "CERRADA"
                && s.Cierre != null && s.Cierre >= haceSieteDias
                && s.MontoCierreDeclarado != null && s.MontoCierreCalculado != null)
            .ToListAsync(ct);

        var diferencias = candidatas
            .Where(s => Math.Abs(s.MontoCierreDeclarado!.Value - s.MontoCierreCalculado!.Value) > tolerancia)
            .ToList();

        var nombres = await PanelHoyCalculator.ObtenerNombresAsync(
            db, empresaId, diferencias.Select(s => s.UsuarioId), ct);

        return diferencias
            .Select(s => new DiferenciaCajaDto(
                s.Id, s.UsuarioId, nombres.GetValueOrDefault(s.UsuarioId, "?"), s.Cierre!.Value,
                s.MontoCierreDeclarado!.Value, s.MontoCierreCalculado!.Value,
                s.MontoCierreDeclarado!.Value - s.MontoCierreCalculado!.Value))
            .OrderByDescending(x => x.Cierre)
            .ToList();
    }
}
