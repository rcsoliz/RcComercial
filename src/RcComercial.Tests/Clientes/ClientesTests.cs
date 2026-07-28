using FluentAssertions;
using FluentValidation;
using RcComercial.Application.Clientes.Commands;
using RcComercial.Application.Clientes.Queries;
using RcComercial.Domain.Common;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Clientes;

public class ClientesTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    [Fact]
    public async Task BuscarClientes_PorTextoYFiltroDeEstado_DevuelveSoloLoEsperado()
    {
        var ctx = await CrearEmpresaDePruebaAsync();

        var juanPerez = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearClienteCommand("Juan Pérez", "12345678", TiposDocumentoCliente.Ci, "+59171234567", null));
        var mariaJuana = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearClienteCommand("María Juana Flores", "87654321", TiposDocumentoCliente.Ci, null, null));
        var clienteViejo = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearClienteCommand("Cliente Viejo", null, TiposDocumentoCliente.Ci, null, null));
        await EnviarComoAsync(ctx, ctx.Dueno, null, new DesactivarClienteCommand(clienteViejo.Id));

        // "Juan" matchea "Juan Pérez" y "María JUANa Flores" (substring, no solo prefijo).
        var porTexto = await EnviarComoAsync(ctx, ctx.Dueno, null, new BuscarClientesQuery("Juan", null, 1));
        porTexto.Select(c => c.Id).Should().BeEquivalentTo([juanPerez.Id, mariaJuana.Id]);

        // Filtro por defecto = solo activos: el desactivado no aparece.
        var soloActivos = await EnviarComoAsync(ctx, ctx.Dueno, null, new BuscarClientesQuery(null, null, 1));
        soloActivos.Select(c => c.Id).Should().NotContain(clienteViejo.Id);

        // Filtro "inactivos": únicamente el desactivado.
        var soloInactivos = await EnviarComoAsync(ctx, ctx.Dueno, null, new BuscarClientesQuery(null, "inactivos", 1));
        soloInactivos.Select(c => c.Id).Should().BeEquivalentTo([clienteViejo.Id]);

        // Filtro "todos": los tres.
        var todos = await EnviarComoAsync(ctx, ctx.Dueno, null, new BuscarClientesQuery(null, "todos", 1));
        todos.Should().HaveCount(3);

        // Búsqueda por NIT/CI exacto.
        var porNit = await EnviarComoAsync(ctx, ctx.Dueno, null, new BuscarClientesQuery("87654321", null, 1));
        porNit.Select(c => c.Id).Should().BeEquivalentTo([mariaJuana.Id]);
    }

    [Fact]
    public async Task CrearCliente_ConCiOWhatsappConFormatoInvalido_EsRechazado()
    {
        var ctx = await CrearEmpresaDePruebaAsync();

        var actCiInvalido = () => EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearClienteCommand("Cliente X", "abc", TiposDocumentoCliente.Ci, null, null));
        await actCiInvalido.Should().ThrowAsync<ValidationException>();

        // Falta el prefijo +591.
        var actWhatsappInvalido = () => EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearClienteCommand("Cliente Y", null, TiposDocumentoCliente.Ci, "71234567", null));
        await actWhatsappInvalido.Should().ThrowAsync<ValidationException>();
    }
}
