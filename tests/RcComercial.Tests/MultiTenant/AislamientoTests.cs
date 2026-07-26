using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Panel;
using RcComercial.Application.Ventas.Commands.AnularVenta;
using RcComercial.Application.Ventas.Commands.CrearVenta;
using RcComercial.Domain.Common;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.MultiTenant;

public class AislamientoTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    [Fact]
    public async Task Productos_DeUnaEmpresa_NuncaAparecenParaOtra()
    {
        var empresaA = await CrearEmpresaDePruebaAsync();
        var empresaB = await CrearEmpresaDePruebaAsync();

        using var dbComoA = CrearContextoComo(empresaA, empresaA.Dueno);
        var productosVisiblesDesdeA = await dbComoA.Productos.Select(p => p.Id).ToListAsync();

        productosVisiblesDesdeA.Should().Contain(empresaA.ProductoSimple.Id);
        productosVisiblesDesdeA.Should().NotContain(empresaB.ProductoSimple.Id);
        productosVisiblesDesdeA.Should().HaveCount(4); // los 4 productos sembrados, solo los de A
    }

    [Fact]
    public async Task Ventas_DeUnaEmpresa_SonInvisiblesParaOtraYNoSePuedenAnular()
    {
        var empresaA = await CrearEmpresaDePruebaAsync();
        var empresaB = await CrearEmpresaDePruebaAsync();

        await AbrirCajaAsync(empresaA.Sucursal, empresaA.Vendedor);
        await AgregarStockAsync(empresaA.Sucursal, empresaA.ProductoSimple, 10m);

        var ventaId = Uuid7.NewGuid();
        await EnviarComoAsync(empresaA, empresaA.Vendedor, null, new CrearVentaCommand(
            ventaId, null, 0m,
            [new CrearVentaDetalleCommand(empresaA.ProductoSimple.Id, null, 2m, 10m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 20m, null)],
            null));

        // Empresa B no ve la venta de A en una consulta directa.
        using var dbComoB = CrearContextoComo(empresaB, empresaB.Dueno);
        var ventaVisibleDesdeB = await dbComoB.Ventas.AnyAsync(v => v.Id == ventaId);
        ventaVisibleDesdeB.Should().BeFalse();

        // Un intento de anular (IDOR) desde la empresa B no encuentra la venta ajena: el
        // query filter multi-tenant hace que la búsqueda por Id simplemente no la vea.
        var resultadoAnulacion = await EnviarComoAsync(
            empresaB, empresaB.Dueno, null, new AnularVentaCommand(ventaId, "Intento cruzado"));
        resultadoAnulacion.Should().BeNull();

        // La venta de A sigue intacta.
        using var dbComoA = CrearContextoComo(empresaA, empresaA.Dueno);
        var ventaOriginal = await dbComoA.Ventas.SingleAsync(v => v.Id == ventaId);
        ventaOriginal.Estado.Should().Be(EstadosVenta.Completada);
    }

    [Fact]
    public async Task Panel_DeUnaEmpresa_NuncaSumaVentasDeOtra()
    {
        var empresaA = await CrearEmpresaDePruebaAsync();
        var empresaB = await CrearEmpresaDePruebaAsync();

        await AbrirCajaAsync(empresaA.Sucursal, empresaA.Vendedor);
        await AgregarStockAsync(empresaA.Sucursal, empresaA.ProductoSimple, 10m);
        await EnviarComoAsync(empresaA, empresaA.Vendedor, null, new CrearVentaCommand(
            Uuid7.NewGuid(), null, 0m,
            [new CrearVentaDetalleCommand(empresaA.ProductoSimple.Id, null, 2m, 10m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 20m, null)],
            null));

        using var db = CrearContexto(new FakeCurrentUserService());

        var panelA = await PanelHoyCalculator.CalcularAsync(db, empresaA.Empresa.Id, null, incluirCostos: true, default);
        var panelB = await PanelHoyCalculator.CalcularAsync(db, empresaB.Empresa.Id, null, incluirCostos: true, default);

        panelA.TotalVendido.Should().Be(20m);
        panelB.TotalVendido.Should().Be(0m, "el panel de B jamás debe incluir ventas de A");
    }
}
