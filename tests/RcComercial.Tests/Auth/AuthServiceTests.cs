using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RcComercial.Application.Auth;
using RcComercial.Infrastructure.Auth;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Auth;

public class AuthServiceTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    private static AuthService CrearAuthService(RcComercial.Infrastructure.Persistence.AppDbContext db) =>
        new(db, new BCryptPasswordHasher(), new JwtTokenService(Options.Create(new JwtSettings
        {
            Secret = Convert.ToBase64String(new byte[32]), // clave dummy de 32 bytes, suficiente para HMAC-SHA256
            Issuer = "RcComercialTest",
            Audience = "RcComercialTestApi",
            AccessTokenMinutos = 15,
            RefreshTokenDias = 7,
        })));

    [Fact]
    public async Task Login_CincoIntentosFallidos_BloqueaLaCuentaQuinceMinutosYRechazaInclusoLaClaveCorrecta()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        using var db = CrearContexto(new FakeCurrentUserService());
        var auth = CrearAuthService(db);

        LoginResult? ultimoResultado = null;
        for (var i = 0; i < 5; i++)
            ultimoResultado = await auth.LoginAsync(ctx.Dueno.UsuarioLogin, "ClaveIncorrecta", "127.0.0.1", "xunit");

        // Comportamiento REAL y actual (ver hallazgo reportado): el intento que
        // dispara el bloqueo (el 5°) ya deja BloqueadoHasta seteado en BD, pero
        // el propio LoginResult de ESE intento todavía informa
        // CredencialesInvalidas, no CuentaBloqueada — AuthEndpoints.cs por lo
        // tanto responde 401 genérico en vez de 423 justo en el momento en que
        // la cuenta se bloquea. Solo el intento SIGUIENTE (el 6°, probado abajo)
        // pasa por la rama que sí devuelve CuentaBloqueada.
        ultimoResultado!.Exitoso.Should().BeFalse();
        ultimoResultado.Error.Should().Be(LoginError.CredencialesInvalidas);
        ultimoResultado.BloqueadoHasta.Should().NotBeNull("el 5° intento ya bloqueó la cuenta en BD aunque el Error no lo refleje");
        ultimoResultado.BloqueadoHasta!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(30));

        // Ni siquiera la contraseña correcta entra mientras dura el bloqueo.
        var intentoConClaveCorrecta = await auth.LoginAsync(ctx.Dueno.UsuarioLogin, Contrasenas.Dueno, "127.0.0.1", "xunit");
        intentoConClaveCorrecta.Exitoso.Should().BeFalse();
        intentoConClaveCorrecta.Error.Should().Be(LoginError.CuentaBloqueada);
    }

    [Fact]
    public async Task Refresh_RotaElToken_RevocaElAnteriorYLoEnlazaAlNuevo()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        using var db = CrearContexto(new FakeCurrentUserService());
        var auth = CrearAuthService(db);

        var login = await auth.LoginAsync(ctx.Dueno.UsuarioLogin, Contrasenas.Dueno, "127.0.0.1", "xunit");
        login.Exitoso.Should().BeTrue();

        var refresh = await auth.RefreshAsync(login.RefreshToken!, "127.0.0.1", "xunit");

        refresh.Exitoso.Should().BeTrue();
        refresh.RefreshToken.Should().NotBe(login.RefreshToken);

        var tokenService = new JwtTokenService(Options.Create(new JwtSettings { Secret = Convert.ToBase64String(new byte[32]), Issuer = "x", Audience = "y" }));
        var hashAnterior = tokenService.HashRefreshToken(login.RefreshToken!);

        using var dbVerificacion = CrearContexto(new FakeCurrentUserService());
        var tokenAnterior = await dbVerificacion.RefreshTokens.SingleAsync(rt => rt.TokenHash == hashAnterior);

        tokenAnterior.RevocadoEn.Should().NotBeNull();
        tokenAnterior.ReemplazadoPor.Should().NotBeNull();
    }

    [Fact]
    public async Task Refresh_ReusoDeUnTokenYaRotado_RevocaTodaLaCadena()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        using var db = CrearContexto(new FakeCurrentUserService());
        var auth = CrearAuthService(db);

        var login = await auth.LoginAsync(ctx.Dueno.UsuarioLogin, Contrasenas.Dueno, "127.0.0.1", "xunit");
        var primeraRotacion = await auth.RefreshAsync(login.RefreshToken!, "127.0.0.1", "xunit");
        primeraRotacion.Exitoso.Should().BeTrue();

        // Reusar el token YA rotado (login.RefreshToken) es la señal de robo.
        var reuso = await auth.RefreshAsync(login.RefreshToken!, "127.0.0.1", "atacante");
        reuso.Exitoso.Should().BeFalse();
        reuso.Error.Should().Be(RefreshError.TokenReutilizado);

        // La cadena entera queda revocada: el token "bueno" emitido por la
        // primera rotación (primeraRotacion.RefreshToken) también deja de servir.
        var intentoConElTokenBueno = await auth.RefreshAsync(primeraRotacion.RefreshToken!, "127.0.0.1", "xunit");
        intentoConElTokenBueno.Exitoso.Should().BeFalse("toda la cadena del usuario debe quedar revocada ante un reuso");
    }
}
