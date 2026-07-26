using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Ventas.Commands.CrearVenta;
using RcComercial.Domain.Common;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Ventas;

public class CrearVentaTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    [Fact]
    public async Task ConversionDePresentaciones_DescuentaCantidadBaseSegunFactor()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoConPresentaciones, 100m);

        var comando = new CrearVentaCommand(
            Uuid7.NewGuid(), null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoConPresentaciones.Id, ctx.PresentacionCajaX10.Id, 2m, 35m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 70m, null)],
            null);

        var venta = await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);

        venta.Detalles.Single().Cantidad.Should().Be(2m);
        venta.Detalles.Single().CantidadBase.Should().Be(20m); // 2 cajas x factor 10
        venta.Total.Should().Be(70m);

        var stockFinal = await StockTotalAsync(ctx.Sucursal, ctx.ProductoConPresentaciones);
        stockFinal.Should().Be(80m); // 100 - 20
    }

    [Fact]
    public async Task Fefo_DescuentaDelLoteQueVencePrimero_SinPartirLaLinea()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);

        var loteProximo = await CrearLoteAsync(ctx.ProductoConLotes, "L-PROXIMO", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        var loteLejano = await CrearLoteAsync(ctx.ProductoConLotes, "L-LEJANO", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(180)));
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoConLotes, 10m, loteProximo);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoConLotes, 50m, loteLejano);

        var comando = new CrearVentaCommand(
            Uuid7.NewGuid(), null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoConLotes.Id, null, 8m, 2.5m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 20m, null)],
            null);

        var venta = await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);

        venta.Detalles.Single().LoteId.Should().Be(loteProximo.Id);

        using var db = CrearContexto(new FakeCurrentUserService { EmpresaId = ctx.Empresa.Id });
        var stockProximo = await db.Stocks.IgnoreQueryFilters()
            .Where(s => s.LoteId == loteProximo.Id).Select(s => s.Cantidad).SingleAsync();
        var stockLejano = await db.Stocks.IgnoreQueryFilters()
            .Where(s => s.LoteId == loteLejano.Id).Select(s => s.Cantidad).SingleAsync();

        stockProximo.Should().Be(2m); // 10 - 8
        stockLejano.Should().Be(50m); // sin tocar: el lote que vence antes alcanzaba solo
    }

    [Fact]
    public async Task Concurrencia_TaskWhenAll_NuncaVendeMasDelStockDisponible()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 10m);

        const int intentos = 15; // más que el stock disponible (10)

        var resultados = await Task.WhenAll(Enumerable.Range(0, intentos).Select(async _ =>
        {
            var comando = new CrearVentaCommand(
                Uuid7.NewGuid(), null, 0m,
                [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 1m, 10m, 0m)],
                [new CrearVentaPagoCommand(MetodosPago.Efectivo, 10m, null)],
                null);
            try
            {
                await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);
                return true;
            }
            catch (ValidationException)
            {
                return false;
            }
        }));

        resultados.Count(exito => exito).Should().Be(10, "el stock solo alcanza para 10 ventas de 1 unidad");
        resultados.Count(exito => !exito).Should().Be(5);

        var stockFinal = await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple);
        stockFinal.Should().Be(0m, "nunca debe quedar en negativo ni permitir sobreventa");
    }

    [Fact]
    public async Task Numeracion_ConcurrenteSinRepetir()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 1000m);

        const int concurrentes = 20;

        var numeros = await Task.WhenAll(Enumerable.Range(0, concurrentes).Select(async _ =>
        {
            var comando = new CrearVentaCommand(
                Uuid7.NewGuid(), null, 0m,
                [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 1m, 10m, 0m)],
                [new CrearVentaPagoCommand(MetodosPago.Efectivo, 10m, null)],
                null);
            var venta = await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);
            return venta.Numero;
        }));

        numeros.Should().OnlyHaveUniqueItems();
        numeros.Should().HaveCount(concurrentes);
    }

    [Fact]
    public async Task Idempotencia_ReenviarMismoId_NoDuplicaLaVenta()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 10m);

        var id = Uuid7.NewGuid();
        var comando = new CrearVentaCommand(
            id, null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 3m, 10m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 30m, null)],
            null);

        var primeraRespuesta = await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);
        var segundaRespuesta = await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);

        primeraRespuesta.Id.Should().Be(segundaRespuesta.Id);
        primeraRespuesta.Numero.Should().Be(segundaRespuesta.Numero);

        using var db = CrearContexto(new FakeCurrentUserService { EmpresaId = ctx.Empresa.Id });
        var conteoVentas = await db.Ventas.IgnoreQueryFilters().CountAsync(v => v.Id == id);
        conteoVentas.Should().Be(1);

        var stockFinal = await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple);
        stockFinal.Should().Be(7m, "el reenvío no debe descontar stock una segunda vez");
    }

    [Fact]
    public async Task ProductoControlado_SinReceta_EsRechazado()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoControlado, 20m);

        var comando = new CrearVentaCommand(
            Uuid7.NewGuid(), null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoControlado.Id, null, 1m, 8m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 8m, null)],
            null);

        var accion = async () => await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);

        (await accion.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("receta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProductoControlado_ConReceta_SePermite()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoControlado, 20m);

        var comando = new CrearVentaCommand(
            Uuid7.NewGuid(), null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoControlado.Id, null, 1m, 8m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 8m, null)],
            new CrearVentaRecetaCommand("Dr. Pérez", "MAT-123", "Juan Paciente", "1234567", DateOnly.FromDateTime(DateTime.UtcNow), null));

        var venta = await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);

        venta.Total.Should().Be(8m);
    }

    [Fact]
    public async Task StockInsuficiente_SinPermitirNegativo_EsRechazado()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 3m);

        var comando = new CrearVentaCommand(
            Uuid7.NewGuid(), null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 5m, 10m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 50m, null)],
            null);

        var accion = async () => await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);

        await accion.Should().ThrowAsync<ValidationException>();

        var stockFinal = await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple);
        stockFinal.Should().Be(3m, "un intento rechazado no debe alterar el stock");
    }

    [Fact]
    public async Task StockInsuficiente_ConPermiteStockNegativo_SePermiteYQuedaEnNegativo()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 3m);
        await EstablecerConfiguracionAsync(ctx.Empresa, "venta.permite_stock_negativo", "true");

        var comando = new CrearVentaCommand(
            Uuid7.NewGuid(), null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 5m, 10m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 50m, null)],
            null);

        var venta = await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);

        venta.Total.Should().Be(50m);
        var stockFinal = await StockTotalAsync(ctx.Sucursal, ctx.ProductoSimple);
        stockFinal.Should().Be(-2m); // 3 - 5
    }

    [Fact]
    public async Task Concurrencia_PrimeraVentaSinFilaDeStockAun_ConStockNegativoPermitido()
    {
        // A diferencia de los demás tests de concurrencia, aquí NO se siembra
        // ninguna fila de stock previa: el handler debe crearla (INSERT), no
        // ajustarla (UPDATE...RETURNING atómico). Ese camino de "primera venta
        // de un producto" no pasa por AjustarStockAsync.
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await EstablecerConfiguracionAsync(ctx.Empresa, "venta.permite_stock_negativo", "true");

        var resultados = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            var comando = new CrearVentaCommand(
                Uuid7.NewGuid(), null, 0m,
                [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 1m, 10m, 0m)],
                [new CrearVentaPagoCommand(MetodosPago.Efectivo, 10m, null)],
                null);
            try
            {
                await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);
                return (Ok: true, Excepcion: (Exception?)null);
            }
            catch (Exception ex)
            {
                return (Ok: false, Excepcion: ex);
            }
        }));

        resultados.Should().OnlyContain(r => r.Ok,
            "ambas ventas concurrentes de un producto sin stock previo deberían completarse " +
            "(con stock negativo permitido), no reventar por una violación de índice único");
    }

    [Fact]
    public async Task PagosDescuadrados_NoCoincidenConElTotal_SonRechazados()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 10m);

        var comando = new CrearVentaCommand(
            Uuid7.NewGuid(), null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 2m, 10m, 0m)], // total = 20
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 15m, null)], // pago insuficiente
            null);

        var accion = async () => await EnviarComoAsync(ctx, ctx.Vendedor, null, comando);

        (await accion.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("pagos", StringComparison.OrdinalIgnoreCase));
    }
}
