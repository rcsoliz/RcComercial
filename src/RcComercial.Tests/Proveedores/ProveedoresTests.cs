using FluentAssertions;
using FluentValidation;
using RcComercial.Application.Proveedores.Commands;
using RcComercial.Application.Proveedores.Queries;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Proveedores;

public class ProveedoresTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    [Fact]
    public async Task BuscarProveedores_PorTextoYFiltroDeEstado_DevuelveSoloLoEsperado()
    {
        var ctx = await CrearEmpresaDePruebaAsync();

        var andina = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearProveedorCommand("Distribuidora Andina", "1234567", "+59171234567", 30, 5));
        var otro = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearProveedorCommand("Insumos del Sur", "7654321", null, 0, 3));
        var proveedorViejo = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearProveedorCommand("Proveedor Viejo", null, null, 0, 3));
        await EnviarComoAsync(ctx, ctx.Dueno, null, new DesactivarProveedorCommand(proveedorViejo.Id));

        var porTexto = await EnviarComoAsync(ctx, ctx.Dueno, null, new BuscarProveedoresQuery("Andina", null, 1));
        porTexto.Select(p => p.Id).Should().BeEquivalentTo([andina.Id]);

        var soloActivos = await EnviarComoAsync(ctx, ctx.Dueno, null, new BuscarProveedoresQuery(null, null, 1));
        soloActivos.Select(p => p.Id).Should().NotContain(proveedorViejo.Id);
        soloActivos.Select(p => p.Id).Should().BeEquivalentTo([andina.Id, otro.Id]);

        var soloInactivos = await EnviarComoAsync(ctx, ctx.Dueno, null, new BuscarProveedoresQuery(null, "inactivos", 1));
        soloInactivos.Select(p => p.Id).Should().BeEquivalentTo([proveedorViejo.Id]);

        // Búsqueda por NIT exacto.
        var porNit = await EnviarComoAsync(ctx, ctx.Dueno, null, new BuscarProveedoresQuery("7654321", null, 1));
        porNit.Select(p => p.Id).Should().BeEquivalentTo([otro.Id]);
    }

    [Fact]
    public async Task CrearProveedor_ConNitOWhatsappConFormatoInvalido_EsRechazado()
    {
        var ctx = await CrearEmpresaDePruebaAsync();

        var actNitInvalido = () => EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearProveedorCommand("Proveedor X", "abc", null, 0, 3));
        await actNitInvalido.Should().ThrowAsync<ValidationException>();

        var actWhatsappInvalido = () => EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearProveedorCommand("Proveedor Y", null, "71234567", 0, 3)); // falta +591
        await actWhatsappInvalido.Should().ThrowAsync<ValidationException>();
    }
}
