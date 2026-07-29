using FluentAssertions;
using FluentValidation;
using RcComercial.Application.Roles.Commands;
using RcComercial.Application.Roles.Queries;
using RcComercial.Domain.Common;
using RcComercial.Tests.Infraestructura;

namespace RcComercial.Tests.Roles;

public class RolesTests(PostgresContainerFixture fixture) : PruebaBase(fixture)
{
    private async Task<short> IdDePermisoAsync(ContextoPrueba ctx, string codigo)
    {
        var catalogo = await EnviarComoAsync(ctx, ctx.Dueno, null, new ListarPermisosQuery());
        return catalogo.Single(p => p.Codigo == codigo).Id;
    }

    [Fact]
    public async Task EditarRol_DeSistema_EsRechazado()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        var adminUsuariosId = await IdDePermisoAsync(ctx, Permisos.AdminUsuarios);

        var act = () => EnviarComoAsync(ctx, ctx.Dueno, null,
            new EditarRolCommand(RolesSistema.Vendedor, "Vendedor hackeado", [adminUsuariosId]));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*roles de sistema*", "el mensaje debe explicar por qué, no solo fallar en silencio");
    }

    [Fact]
    public async Task CrearRol_PropioConPermisosElegidos_QuedaListoParaAsignarAUnUsuario()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        var ventasCrearId = await IdDePermisoAsync(ctx, Permisos.VentasCrear);
        var ventasAnularId = await IdDePermisoAsync(ctx, Permisos.VentasAnular);

        var rol = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearRolCommand("Cajero senior", [ventasCrearId, ventasAnularId]));

        rol.EsSistema.Should().BeFalse();
        rol.PermisoIds.Should().BeEquivalentTo([ventasCrearId, ventasAnularId]);

        var listado = await EnviarComoAsync(ctx, ctx.Dueno, null, new ListarRolesQuery());
        listado.Should().Contain(r => r.Id == rol.Id && !r.EsSistema);
    }

    [Fact]
    public async Task EditarRol_Propio_ReemplazaElConjuntoDePermisos()
    {
        var ctx = await CrearEmpresaDePruebaAsync();
        var inventarioVerId = await IdDePermisoAsync(ctx, Permisos.InventarioVer);
        var inventarioAjustarId = await IdDePermisoAsync(ctx, Permisos.InventarioAjustar);

        var rol = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new CrearRolCommand("Almacenero", [inventarioVerId, inventarioAjustarId]));

        var editado = await EnviarComoAsync(ctx, ctx.Dueno, null,
            new EditarRolCommand(rol.Id, "Almacenero", [inventarioVerId]));

        editado!.PermisoIds.Should().BeEquivalentTo([inventarioVerId]);
    }
}
