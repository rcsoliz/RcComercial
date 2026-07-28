using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using RcComercial.Domain.Common;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Ventas;

/// <summary>
/// A diferencia del resto de la suite (EnviarComoAsync, que manda el comando
/// directo por MediatR), esto sí pega por HTTP contra el Program.cs real:
/// es la única forma honesta de probar que un endpoint descarta un campo del
/// body ANTES de llegar al handler — un test a nivel MediatR no puede
/// demostrar eso, porque ya bypasea al endpoint.
/// </summary>
public class CrearVentaHttpTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    private ApiWebFactory _factory = null!;
    private HttpClient _cliente = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = new ApiWebFactory(Fixture.ConnectionString);
        _cliente = _factory.CreateClient();
    }

    public override async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task PostVentas_IgnoraElNumeroEnviadoPorElCliente_SiempreAsignaPorSecuencia()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await AbrirCajaAsync(ctx.Sucursal, ctx.Vendedor);
        await AgregarStockAsync(ctx.Sucursal, ctx.ProductoSimple, 10m);

        var login = await _cliente.PostAsJsonAsync("/api/auth/login", new
        {
            usuarioLogin = ctx.Vendedor.UsuarioLogin,
            password = Contrasenas.Vendedor,
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("accessToken").GetString();
        _cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            id = Uuid7.NewGuid(),
            clienteId = (Guid?)null,
            descuento = 0m,
            detalles = new[]
            {
                new
                {
                    productoId = ctx.ProductoSimple.Id, presentacionId = (Guid?)null,
                    cantidad = 1m, precioUnitario = 10m, descuento = 0m,
                },
            },
            pagos = new[] { new { metodo = MetodosPago.Efectivo, monto = 10m, referenciaQr = (string?)null } },
            receta = (object?)null,
            numero = "99999999", // intento de numeración propia por el cliente: debe ignorarse
        };

        var respuesta = await _cliente.PostAsJsonAsync("/api/ventas", payload);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var venta = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var numeroAsignado = venta.GetProperty("numero").GetString();
        var creadoOffline = venta.GetProperty("creadoOffline").GetBoolean();

        numeroAsignado.Should().NotBe("99999999");
        numeroAsignado.Should().Be("00000001", "es la primera venta de esta sucursal: la secuencia empieza en 1");
        creadoOffline.Should().BeFalse("vino por el endpoint online, no por el lote de sincronización");
    }
}
