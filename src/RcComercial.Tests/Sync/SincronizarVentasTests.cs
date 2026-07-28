using FluentAssertions;
using RcComercial.Application.Sync.Commands;
using RcComercial.Application.Ventas.Commands.CrearVenta;
using RcComercial.Domain.Common;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Sync;

public class SincronizarVentasTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    private static SyncVentaItem VentaDeUnaUnidad(Guid ventaId, string numero, Guid productoId, decimal cantidad, decimal precio) =>
        new(
            ventaId, numero, null, 0m,
            [new CrearVentaDetalleCommand(productoId, null, cantidad, precio, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, cantidad * precio, null)],
            null);

    [Fact]
    public async Task SincronizarElMismoLoteDosVeces_NoDuplicaNada()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 100m);

        var lote = new SincronizarVentasCommand([
            VentaDeUnaUnidad(Uuid7.NewGuid(), "OFF-00000001", ctx.ProductoSimple.Id, 2m, 10m),
            VentaDeUnaUnidad(Uuid7.NewGuid(), "OFF-00000002", ctx.ProductoSimple.Id, 3m, 10m),
        ]);

        var primeraVez = await EnviarComoAsync(ctx, ctx.Vendedor, null, lote);
        primeraVez.Should().OnlyContain(r => r.Estado == "aceptada");

        var stockTrasPrimerEnvio = await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple);
        stockTrasPrimerEnvio.Should().Be(95m); // 100 - 2 - 3

        // Reenviar EXACTAMENTE el mismo lote (mismos Id de venta): reintento de
        // sincronización típico tras un corte de red a mitad de la respuesta.
        var segundaVez = await EnviarComoAsync(ctx, ctx.Vendedor, null, lote);
        segundaVez.Should().OnlyContain(r => r.Estado == "duplicada");

        var stockTrasSegundoEnvio = await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple);
        stockTrasSegundoEnvio.Should().Be(95m, "reenviar el lote no debe volver a descontar stock");
    }

    [Fact]
    public async Task VentaConStockInsuficienteEnElLote_SeRechazaSinFrenarLasDemas()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 5m);

        var idA = Uuid7.NewGuid();
        var idB = Uuid7.NewGuid();
        var idC = Uuid7.NewGuid();

        // B pide 10 pero tras A solo quedan 3: debe rechazarse SIN frenar a C
        // (que llega justo después en el mismo lote).
        var lote = new SincronizarVentasCommand([
            VentaDeUnaUnidad(idA, "OFF-00000010", ctx.ProductoSimple.Id, 2m, 10m),
            VentaDeUnaUnidad(idB, "OFF-00000011", ctx.ProductoSimple.Id, 10m, 10m),
            VentaDeUnaUnidad(idC, "OFF-00000012", ctx.ProductoSimple.Id, 1m, 10m),
        ]);

        var resultados = await EnviarComoAsync(ctx, ctx.Vendedor, null, lote);

        resultados.Should().HaveCount(3);
        resultados.Single(r => r.Id == idA).Estado.Should().Be("aceptada");
        resultados.Single(r => r.Id == idC).Estado.Should().Be("aceptada");

        var rechazada = resultados.Single(r => r.Id == idB);
        rechazada.Estado.Should().Be("rechazada");
        rechazada.Motivo.Should().Contain("Stock insuficiente");
        rechazada.Venta.Should().BeNull();

        var stockFinal = await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple);
        stockFinal.Should().Be(2m, "5 - 2 (A) - 1 (C); B no debe haber tocado el stock"); // 5-2-1
    }

    [Fact]
    public async Task ReservarRango_DosLlamadasSeguidas_DevuelveBloquesSinSolape()
    {
        var ctx = await CrearEmpresaDePruebaAsync();

        var comando1 = new ReservarRangoCommand(ctx.Sucursal.Id, "dispositivo-A", 500);
        var comando2 = new ReservarRangoCommand(ctx.Sucursal.Id, "dispositivo-B", 500);

        var rango1 = await EnviarComoAsync(ctx, ctx.Dueno, null, comando1);
        var rango2 = await EnviarComoAsync(ctx, ctx.Dueno, null, comando2);

        (rango1.Fin - rango1.Inicio + 1).Should().Be(500);
        rango2.Inicio.Should().BeGreaterThan(rango1.Fin, "el segundo bloque debe empezar después de donde terminó el primero");
    }
}
