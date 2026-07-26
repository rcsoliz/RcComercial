using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Ventas.Commands.AnularVenta;
using RcComercial.Application.Ventas.Commands.CrearVenta;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Ventas;

public class AnularVentaTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    [Fact]
    public async Task Anular_ReversaElStockExactoYRegistraElKardexDeDevolucion()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 10m);

        var ventaId = Uuid7.NewGuid();
        await EnviarComoAsync(ctx, ctx.Vendedor, null, new CrearVentaCommand(
            ventaId, null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 3m, 10m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 30m, null)],
            null));

        (await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple)).Should().Be(7m);

        var ventaAnulada = await EnviarComoAsync(
            ctx, ctx.Dueno, null, new AnularVentaCommand(ventaId, "Cliente se arrepintió"));

        ventaAnulada!.Estado.Should().Be(EstadosVenta.Anulada);
        (await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple)).Should().Be(10m, "debe reversar exactamente lo vendido");

        using var db = CrearContexto(new FakeCurrentUserService { EmpresaId = ctx.Empresa.Id });
        var kardexDevolucion = await db.MovimientosInventario.IgnoreQueryFilters()
            .Where(m => m.ReferenciaId == ventaId && m.Tipo == TiposMovimiento.Devolucion)
            .ToListAsync();

        kardexDevolucion.Should().ContainSingle();
        kardexDevolucion.Single().Cantidad.Should().Be(3m);
    }

    [Fact]
    public async Task Anular_ConProductoDeLotes_ReversaAlMismoLoteConsumido()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        var lote = await CrearLoteAsync(ctx.ProductoConLotes, "L-001", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)));
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoConLotes, 20m, lote);

        var ventaId = Uuid7.NewGuid();
        await EnviarComoAsync(ctx, ctx.Vendedor, null, new CrearVentaCommand(
            ventaId, null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoConLotes.Id, null, 6m, 2.5m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 15m, null)],
            null));

        await EnviarComoAsync(ctx, ctx.Dueno, null, new AnularVentaCommand(ventaId, "Error de digitación"));

        using var db = CrearContexto(new FakeCurrentUserService { EmpresaId = ctx.Empresa.Id });
        var stockDelLote = await db.Stocks.IgnoreQueryFilters()
            .Where(s => s.LoteId == lote.Id).Select(s => s.Cantidad).SingleAsync();

        stockDelLote.Should().Be(20m, "la reversa debe volver al mismo lote del que se descontó");
    }

    [Fact]
    public async Task Anular_EsIdempotente_SegundaLlamadaNoDuplicaLaReversaDeStock()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 10m);

        var ventaId = Uuid7.NewGuid();
        await EnviarComoAsync(ctx, ctx.Vendedor, null, new CrearVentaCommand(
            ventaId, null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 3m, 10m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 30m, null)],
            null));

        var primeraAnulacion = await EnviarComoAsync(ctx, ctx.Dueno, null, new AnularVentaCommand(ventaId, "Motivo 1"));
        var segundaAnulacion = await EnviarComoAsync(ctx, ctx.Dueno, null, new AnularVentaCommand(ventaId, "Motivo 2"));

        primeraAnulacion!.Estado.Should().Be(EstadosVenta.Anulada);
        segundaAnulacion!.Estado.Should().Be(EstadosVenta.Anulada);

        (await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple)).Should().Be(10m, "la segunda anulación no debe volver a sumar stock");

        using var db = CrearContexto(new FakeCurrentUserService { EmpresaId = ctx.Empresa.Id });
        var reversasEnKardex = await db.MovimientosInventario.IgnoreQueryFilters()
            .CountAsync(m => m.ReferenciaId == ventaId && m.Tipo == TiposMovimiento.Devolucion);

        reversasEnKardex.Should().Be(1, "la anulación idempotente no debe insertar un segundo movimiento de kardex");
    }
}
