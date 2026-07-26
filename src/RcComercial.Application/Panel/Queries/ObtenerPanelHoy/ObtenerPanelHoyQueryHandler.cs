using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Panel.Dtos;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Panel.Queries.ObtenerPanelHoy;

public class ObtenerPanelHoyQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ObtenerPanelHoyQuery, PanelHoyDto>
{
    public async Task<PanelHoyDto> Handle(ObtenerPanelHoyQuery request, CancellationToken ct)
    {
        var empresaId = request.EmpresaId ?? currentUser.EmpresaId!.Value;
        var sucursalId = currentUser.SucursalId;
        var verCostos = request.IncluirCostos ?? currentUser.TienePermiso(Permisos.InventarioVerCostos);

        var (inicio, fin) = HoraBolivia.RangoDelDia(DateOnly.FromDateTime(HoraBolivia.AhoraLocal().Date));

        // Todas las queries usan IgnoreQueryFilters + filtro manual por empresaId:
        // el job nocturno invoca este handler sin JWT (currentUser.EmpresaId es
        // null ahí), por lo que el filtro multi-tenant automático del DbContext
        // no serviría; así el comportamiento es idéntico en ambos modos.
        IQueryable<Venta> ventasQuery = db.Ventas.IgnoreQueryFilters()
            .Where(v => v.EmpresaId == empresaId && v.Fecha >= inicio && v.Fecha < fin);
        if (sucursalId is { } suc) ventasQuery = ventasQuery.Where(v => v.SucursalId == suc);

        var completadas = ventasQuery.Where(v => v.Estado == EstadosVenta.Completada);
        var anuladas = ventasQuery.Where(v => v.Estado == EstadosVenta.Anulada);

        var totalVendido = await completadas.SumAsync(v => (decimal?)v.Total, ct) ?? 0m;
        var numeroVentas = await completadas.CountAsync(ct);
        var montoDescuentos = await completadas.SumAsync(v => (decimal?)v.Descuento, ct) ?? 0m;
        var numeroAnulaciones = await anuladas.CountAsync(ct);
        var montoAnulaciones = await anuladas.SumAsync(v => (decimal?)v.Total, ct) ?? 0m;
        var ticketPromedio = numeroVentas > 0 ? totalVendido / numeroVentas : 0m;

        var ventasPorUsuarioRaw = await completadas
            .GroupBy(v => v.UsuarioId)
            .Select(g => new { UsuarioId = g.Key, Numero = g.Count(), Total = g.Sum(v => v.Total) })
            .ToListAsync(ct);

        var nombresUsuarios = await ObtenerNombresAsync(
            db, ventasPorUsuarioRaw.Select(x => x.UsuarioId), ct);

        var ventasPorUsuario = ventasPorUsuarioRaw
            .Select(x => new VentaPorUsuarioDto(x.UsuarioId, nombresUsuarios.GetValueOrDefault(x.UsuarioId, "?"), x.Numero, x.Total))
            .OrderByDescending(x => x.Total)
            .ToList();

        var topProductosRaw = await (
            from vd in db.VentaDetalles
            join v in completadas on vd.VentaId equals v.Id
            group vd by vd.ProductoId into g
            select new { ProductoId = g.Key, Cantidad = g.Sum(x => x.CantidadBase), Monto = g.Sum(x => x.Total) })
            .OrderByDescending(x => x.Monto)
            .Take(5)
            .ToListAsync(ct);

        var productoIds = topProductosRaw.Select(x => x.ProductoId).ToList();
        var productosInfo = await db.Productos.IgnoreQueryFilters()
            .Where(p => productoIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nombre, p.CostoPromedio })
            .ToDictionaryAsync(p => p.Id, ct);

        var topProductos = topProductosRaw.Select(x =>
        {
            var info = productosInfo.GetValueOrDefault(x.ProductoId);
            decimal? costoTotal = verCostos ? x.Cantidad * (info?.CostoPromedio ?? 0) : null;
            decimal? utilidad = verCostos ? x.Monto - costoTotal!.Value : null;
            return new TopProductoDto(x.ProductoId, info?.Nombre ?? "?", x.Cantidad, x.Monto, costoTotal, utilidad);
        }).ToList();

        var sucursalIds = await ResolverSucursalIdsAsync(db, empresaId, sucursalId, ct);
        var cajasAbiertasRaw = await db.SesionesCaja
            .Where(s => sucursalIds.Contains(s.SucursalId) && s.Estado == "ABIERTA")
            .ToListAsync(ct);
        var nombresCajas = await ObtenerNombresAsync(db, cajasAbiertasRaw.Select(s => s.UsuarioId), ct);
        var cajasAbiertas = cajasAbiertasRaw
            .Select(s => new SesionCajaAbiertaDto(
                s.Id, s.UsuarioId, nombresCajas.GetValueOrDefault(s.UsuarioId, "?"), s.Apertura, s.MontoInicial))
            .ToList();

        return new PanelHoyDto(
            totalVendido, numeroVentas, ticketPromedio, ventasPorUsuario,
            numeroAnulaciones, montoAnulaciones, montoDescuentos, topProductos, cajasAbiertas);
    }

    internal static Task<Dictionary<Guid, string>> ObtenerNombresAsync(
        IApplicationDbContext db, IEnumerable<Guid> usuarioIds, CancellationToken ct)
    {
        var ids = usuarioIds.Distinct().ToList();
        return db.Usuarios.IgnoreQueryFilters().Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nombre, ct);
    }

    internal static async Task<List<Guid>> ResolverSucursalIdsAsync(
        IApplicationDbContext db, Guid empresaId, Guid? sucursalId, CancellationToken ct)
    {
        var query = db.Sucursales.IgnoreQueryFilters().Where(s => s.EmpresaId == empresaId);
        if (sucursalId is { } suc) query = query.Where(s => s.Id == suc);
        return await query.Select(s => s.Id).ToListAsync(ct);
    }
}
