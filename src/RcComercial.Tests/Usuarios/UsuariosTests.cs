using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Usuarios.Commands;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;
using RcComercial.Infrastructure.Auth;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Usuarios;

public class UsuariosTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    [Fact]
    public async Task CrearUsuario_GeneraPasswordTemporalQueFuncionaYQuedaPendienteDeCambiar()
    {
        var ctx = await CrearEmpresaDePruebaAsync();

        var resultado = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearUsuarioCommand("Ana Nueva", "ana.nueva", RolesSistema.Vendedor, ctx.Sucursal.Id, null));

        resultado.Usuario.DebeCambiarPassword.Should().BeTrue();
        resultado.PasswordTemporal.Should().NotBeNullOrWhiteSpace();

        using var db = CrearContextoComo(ctx, ctx.Dueno);
        var usuario = await db.Usuarios.FirstAsync(u => u.Id == resultado.Usuario.Id);
        new BCryptPasswordHasher().Verify(resultado.PasswordTemporal, usuario.PasswordHash)
            .Should().BeTrue("el hash guardado debe corresponder exactamente al temporal devuelto");
    }

    [Fact]
    public async Task EditarUsuario_CambiandoElRol_IncrementaPermisosVersion_PeroNoSiNoCambia()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        var creado = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearUsuarioCommand("Beto", "beto", RolesSistema.Vendedor, null, null));

        async Task<int> LeerPermisosVersionAsync()
        {
            using var db = CrearContextoComo(ctx, ctx.Dueno);
            return (await db.Usuarios.FirstAsync(u => u.Id == creado.Usuario.Id)).PermisosVersion;
        }

        (await LeerPermisosVersionAsync()).Should().Be(1);

        // Editar SIN tocar el rol: la versión no debe moverse (evita cerrar
        // sesiones activas por un cambio de nombre/whatsapp, por ejemplo).
        await EnviarComoAsync(ctx, ctx.Dueno, null,
            new EditarUsuarioCommand(creado.Usuario.Id, "Beto Editado", "beto", RolesSistema.Vendedor, null, null));
        (await LeerPermisosVersionAsync()).Should().Be(1);

        // Editar CAMBIANDO el rol: sí debe subir (invalida los JWT ya emitidos).
        await EnviarComoAsync(ctx, ctx.Dueno, null,
            new EditarUsuarioCommand(creado.Usuario.Id, "Beto Editado", "beto", RolesSistema.Encargado, null, null));
        (await LeerPermisosVersionAsync()).Should().Be(2);
    }

    [Fact]
    public async Task RestablecerContrasena_GeneraOtroTemporalYRevocaLasSesionesActivas()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        var creado = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearUsuarioCommand("Carla", "carla", RolesSistema.Vendedor, null, null));

        using (var db = CrearContextoComo(ctx, ctx.Dueno))
        {
            db.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = creado.Usuario.Id,
                TokenHash = "hash-de-prueba",
                ExpiraEn = DateTimeOffset.UtcNow.AddDays(7),
            });
            await db.SaveChangesAsync(default);
        }

        var restablecido = await EnviarComoAsync(ctx, ctx.Dueno, null, new RestablecerContrasenaCommand(creado.Usuario.Id));

        restablecido!.PasswordTemporal.Should().NotBe(creado.PasswordTemporal);
        restablecido.Usuario.DebeCambiarPassword.Should().BeTrue();

        using var dbFinal = CrearContextoComo(ctx, ctx.Dueno);
        var token = await dbFinal.RefreshTokens.FirstAsync(t => t.UsuarioId == creado.Usuario.Id);
        token.RevocadoEn.Should().NotBeNull("restablecer la contraseña debe cerrar cualquier sesión activa");
    }

    [Fact]
    public async Task CrearUsuario_ConUsuarioLoginRepetidoEnLaMismaEmpresa_EsRechazado()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearUsuarioCommand("Uno", "repetido", RolesSistema.Vendedor, null, null));

        var act = () => EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearUsuarioCommand("Dos", "repetido", RolesSistema.Vendedor, null, null));

        await act.Should().ThrowAsync<ValidationException>();
    }
}
