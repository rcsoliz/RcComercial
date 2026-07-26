using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using RcComercial.Api.Authorization;
using RcComercial.Application.Panel;
using RcComercial.Application.Ventas.Commands.CrearVenta;
using RcComercial.Domain.Common;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Autorizacion;

public class PermisosTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    [Fact]
    public async Task Panel_SinPermisoVerCostos_OcultaCostoYUtilidad()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 10m);
        await EnviarComoAsync(ctx, ctx.Vendedor, null, new CrearVentaCommand(
            Uuid7.NewGuid(), null, 0m,
            [new CrearVentaDetalleCommand(ctx.ProductoSimple.Id, null, 2m, 10m, 0m)],
            [new CrearVentaPagoCommand(MetodosPago.Efectivo, 20m, null)],
            null));

        using var db = CrearContexto(new FakeCurrentUserService());

        var panelSinCostos = await PanelHoyCalculator.CalcularAsync(db, ctx.Empresa.Id, null, incluirCostos: false, default);
        var panelConCostos = await PanelHoyCalculator.CalcularAsync(db, ctx.Empresa.Id, null, incluirCostos: true, default);

        panelSinCostos.TopProductos.Should().ContainSingle();
        panelSinCostos.TopProductos.Single().CostoTotal.Should().BeNull();
        panelSinCostos.TopProductos.Single().Utilidad.Should().BeNull();

        panelConCostos.TopProductos.Single().CostoTotal.Should().Be(12m); // 2 uds x costo promedio 6
        panelConCostos.TopProductos.Single().Utilidad.Should().Be(8m); // 20 vendido - 12 costo
    }

    [Fact]
    public async Task PolicyDePermiso_SinClaimDePermiso_NiegaLaAutorizacion()
    {
        var (resultadoSinPermiso, resultadoConPermiso) = await EvaluarPolicyVentasAnularAsync();

        resultadoSinPermiso.Succeeded.Should().BeFalse("un usuario sin el permiso 'ventas.anular' no debe pasar la policy");
        resultadoConPermiso.Succeeded.Should().BeTrue("un usuario con el claim 'permiso=ventas.anular' sí debe pasar");
    }

    private static async Task<(AuthorizationResult SinPermiso, AuthorizationResult ConPermiso)>
        EvaluarPolicyVentasAnularAsync()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddAuthorizationCore();
        await using var proveedor = servicios.BuildServiceProvider();
        var servicioAutorizacion = proveedor.GetRequiredService<IAuthorizationService>();

        var policyProvider = new PermisoPolicyProvider();
        var policy = await policyProvider.GetPolicyAsync(Permisos.VentasAnular);

        var vendedorSinPermiso = ArmarPrincipal(permisos: [Permisos.VentasCrear, Permisos.InventarioVer]);
        var duenoConPermiso = ArmarPrincipal(permisos: PermisosPorRol.Dueno);

        var resultadoSinPermiso = await servicioAutorizacion.AuthorizeAsync(vendedorSinPermiso, resource: null, policy!);
        var resultadoConPermiso = await servicioAutorizacion.AuthorizeAsync(duenoConPermiso, resource: null, policy!);

        return (resultadoSinPermiso, resultadoConPermiso);
    }

    private static ClaimsPrincipal ArmarPrincipal(IEnumerable<string> permisos)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("empresa_id", Guid.NewGuid().ToString()),
        };
        claims.AddRange(permisos.Select(p => new Claim("permiso", p)));

        var identidad = new ClaimsIdentity(claims, authenticationType: "PruebaAuth");
        return new ClaimsPrincipal(identidad);
    }
}
