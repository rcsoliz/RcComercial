using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;
using RcComercial.Infrastructure.Auth;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Plataforma;

/// <summary>
/// Pega por HTTP real (Program.cs completo) porque 7C es, en esencia, sobre
/// autenticación y autorización — policy "SoloPlataforma", claim
/// es_superadmin, bloqueo por empresa suspendida — nada de eso lo ejercita
/// EnviarComoAsync, que manda el comando directo por MediatR sin pasar por
/// el pipeline de auth. Ver CrearVentaHttpTests.cs (Sesión 8.2) para el
/// mismo razonamiento.
/// </summary>
public class PlataformaHttpTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
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

    private async Task<(string Login, string Password)> SembrarSuperadminAsync()
    {
        var login = "superadmin." + Uuid7.NewGuid().ToString("N")[..8];
        const string password = "SuperClave123!";

        using var db = CrearContexto(new FakeCurrentUserService());
        var empresaPlataforma = new Empresa { Nombre = "Plataforma Test", RubroId = 1, Activo = true };
        db.Empresas.Add(empresaPlataforma);
        db.Usuarios.Add(new Usuario
        {
            EmpresaId = empresaPlataforma.Id,
            Nombre = "Superadmin Test",
            UsuarioLogin = login,
            PasswordHash = new BCryptPasswordHasher().Hash(password),
            RolId = RolesSistema.Dueno,
            DebeCambiarPassword = false,
            EsSuperadmin = true,
        });
        await db.SaveChangesAsync(default);
        return (login, password);
    }

    private async Task<HttpResponseMessage> LoginRawAsync(string login, string password) =>
        await _cliente.PostAsJsonAsync("/api/auth/login", new { usuarioLogin = login, password });

    private async Task<string> LoginAsync(string login, string password)
    {
        var resp = await LoginRawAsync(login, password);
        resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private void UsarToken(string token) =>
        _cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static JsonElement DecodificarClaims(string jwt)
    {
        var payload = jwt.Split('.')[1];
        payload = payload.Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        return JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(payload));
    }

    [Fact]
    public async Task PostUsuarios_ConEsSuperadminInyectadoEnElBody_JamasLoActiva()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        UsarToken(await LoginAsync(ctx.Dueno.UsuarioLogin, Contrasenas.Dueno));

        var payload = new
        {
            nombre = "Cajero Nuevo",
            usuarioLogin = "cajero.nuevo",
            rolId = RolesSistema.Vendedor,
            sucursalId = (Guid?)null,
            telefonoWhatsapp = (string?)null,
            esSuperadmin = true, // CrearUsuarioCommand no tiene esta propiedad: se ignora al bindear
        };
        var resp = await _cliente.PostAsJsonAsync("/api/usuarios", payload);
        resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var usuarioId = body.GetProperty("usuario").GetProperty("id").GetGuid();

        using var db = CrearContextoComo(ctx, ctx.Dueno);
        var creado = await db.Usuarios.FirstAsync(u => u.Id == usuarioId);
        creado.EsSuperadmin.Should().BeFalse();
    }

    [Fact]
    public async Task SuperadminCreaEmpresaCompleta_ElDuenoEntraCambiaPasswordYCreaUnCajero()
    {
        var (loginSuperadmin, passwordSuperadmin) = await SembrarSuperadminAsync();
        UsarToken(await LoginAsync(loginSuperadmin, passwordSuperadmin));

        var respAlta = await _cliente.PostAsJsonAsync("/api/plataforma/empresas", new
        {
            nombreEmpresa = "Cliente Nuevo SRL",
            nit = "1234567",
            rubroId = (short)1,
            telefonoWhatsapp = (string?)null,
            nombreSucursal = "Sucursal Principal",
            nombreDueno = "Dueño Cliente",
            usuarioLoginDueno = "dueno.clientenuevo",
            telefonoWhatsappDueno = (string?)null,
        });
        respAlta.StatusCode.Should().Be(HttpStatusCode.OK, await respAlta.Content.ReadAsStringAsync());
        var alta = await respAlta.Content.ReadFromJsonAsync<JsonElement>();
        var passwordTemporalDueno = alta.GetProperty("passwordTemporal").GetString()!;
        var loginDueno = alta.GetProperty("dueno").GetProperty("usuarioLogin").GetString()!;

        // El dueño entra con la temporal: el JWT debe traer debe_cambiar_password=true.
        var tokenTempDueno = await LoginAsync(loginDueno, passwordTemporalDueno);
        DecodificarClaims(tokenTempDueno).GetProperty("debe_cambiar_password").GetString().Should().Be("true");

        // Con esa temporal, cualquier otro endpoint debe estar bloqueado.
        UsarToken(tokenTempDueno);
        (await _cliente.GetAsync("/api/productos")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var respCambio = await _cliente.PostAsJsonAsync("/api/auth/cambiar-password-obligatorio",
            new { passwordActual = passwordTemporalDueno, passwordNueva = "NuevaClaveDueno123" });
        respCambio.StatusCode.Should().Be(HttpStatusCode.OK, await respCambio.Content.ReadAsStringAsync());
        var cambio = await respCambio.Content.ReadFromJsonAsync<JsonElement>();
        var tokenDueno = cambio.GetProperty("accessToken").GetString()!;

        // Ya con la contraseña propia, crea un cajero para su empresa nueva.
        UsarToken(tokenDueno);
        var respCajero = await _cliente.PostAsJsonAsync("/api/usuarios", new
        {
            nombre = "Cajero Uno", usuarioLogin = "cajero.uno", rolId = RolesSistema.Vendedor,
            sucursalId = (Guid?)null, telefonoWhatsapp = (string?)null,
        });
        respCajero.StatusCode.Should().Be(HttpStatusCode.OK, await respCajero.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UsuarioNormal_InclusoDueno_Recibe403EnTodoApiPlataforma()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        UsarToken(await LoginAsync(ctx.Dueno.UsuarioLogin, Contrasenas.Dueno));

        (await _cliente.GetAsync("/api/plataforma/empresas")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var respAlta = await _cliente.PostAsJsonAsync("/api/plataforma/empresas", new
        {
            nombreEmpresa = "X", nit = (string?)null, rubroId = (short)1, telefonoWhatsapp = (string?)null,
            nombreSucursal = "X", nombreDueno = "X", usuarioLoginDueno = "x", telefonoWhatsappDueno = (string?)null,
        });
        respAlta.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var respPatch = await _cliente.PatchAsync(
            $"/api/plataforma/empresas/{ctx.Empresa.Id}/activo", JsonContent.Create(new { activo = false }));
        respPatch.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EmpresaSuspendida_NingunUsuarioPuedeLoguear()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        using (var db = CrearContextoComo(ctx, ctx.Dueno))
        {
            (await db.Empresas.FirstAsync(e => e.Id == ctx.Empresa.Id)).Activo = false;
            await db.SaveChangesAsync(default);
        }

        var resp = await LoginRawAsync(ctx.Dueno.UsuarioLogin, Contrasenas.Dueno);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Be("empresa_suspendida");

        // El vendedor de esa misma empresa tampoco puede, aunque su clave esté bien.
        var respVendedor = await LoginRawAsync(ctx.Vendedor.UsuarioLogin, Contrasenas.Vendedor);
        respVendedor.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EmpresaCreadaPorSuperadmin_QuedaAisladaDeLasDemas()
    {
        await CrearEmpresaDePruebaAsync(); // otra empresa ya con productos sembrados

        var (loginSuperadmin, passwordSuperadmin) = await SembrarSuperadminAsync();
        UsarToken(await LoginAsync(loginSuperadmin, passwordSuperadmin));

        var respAlta = await _cliente.PostAsJsonAsync("/api/plataforma/empresas", new
        {
            nombreEmpresa = "Tenant Aislado SRL",
            nit = (string?)null,
            rubroId = (short)1,
            telefonoWhatsapp = (string?)null,
            nombreSucursal = "Sucursal Única",
            nombreDueno = "Dueño Aislado",
            usuarioLoginDueno = "dueno.aislado",
            telefonoWhatsappDueno = (string?)null,
        });
        var alta = await respAlta.Content.ReadFromJsonAsync<JsonElement>();
        var passwordTemp = alta.GetProperty("passwordTemporal").GetString()!;

        var tokenTemp = await LoginAsync("dueno.aislado", passwordTemp);
        UsarToken(tokenTemp);
        var cambio = await _cliente.PostAsJsonAsync("/api/auth/cambiar-password-obligatorio",
            new { passwordActual = passwordTemp, passwordNueva = "OtraClave123" });
        var tokenDueno = (await cambio.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;

        UsarToken(tokenDueno);
        var respProductos = await _cliente.GetAsync("/api/productos");
        respProductos.StatusCode.Should().Be(HttpStatusCode.OK);
        var productos = await respProductos.Content.ReadFromJsonAsync<JsonElement>();
        productos.GetArrayLength().Should().Be(0, "el tenant nuevo arranca vacío: no debe ver productos de otra empresa");
    }
}
