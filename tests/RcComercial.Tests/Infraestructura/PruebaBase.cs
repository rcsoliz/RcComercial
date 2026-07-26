using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RcComercial.Application;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;
using RcComercial.Infrastructure.Auth;
using RcComercial.Infrastructure.Persistence;

namespace RcComercial.Tests.Infraestructura;

/// <summary>Contraseñas de los usuarios que arma CrearEmpresaDePruebaAsync (hash real vía BCrypt).</summary>
public static class Contrasenas
{
    public const string Dueno = "Dueno123!";
    public const string Vendedor = "Vendedor123!";
}

[Collection("BaseDatos")]
public abstract class PruebaBase(PostgresContainerFixture fixture) : IAsyncLifetime
{
    protected readonly PostgresContainerFixture Fixture = fixture;

    public Task InitializeAsync() => Fixture.ResetearAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected AppDbContext CrearContexto(FakeCurrentUserService usuario) =>
        FabricaContexto.Crear(Fixture.ConnectionString, usuario);

    protected AppDbContext CrearContextoComo(ContextoPrueba ctx, Usuario usuario, IEnumerable<string>? permisos = null) =>
        CrearContexto(ctx.ComoUsuario(usuario, permisos));

    /// <summary>
    /// Empresa + sucursal + usuarios (dueño y vendedor, con hash BCrypt real) +
    /// cuatro productos (simple, con lotes, controlado, con presentaciones).
    /// </summary>
    protected async Task<ContextoPrueba> CrearEmpresaDePruebaAsync()
    {
        var hasher = new BCryptPasswordHasher();
        using var db = CrearContexto(new FakeCurrentUserService());

        var empresa = new Empresa { Nombre = "Empresa Test", RubroId = 1, Activo = true };
        var sucursal = new Sucursal { EmpresaId = empresa.Id, Nombre = "Sucursal Central" };

        var dueno = new Usuario
        {
            EmpresaId = empresa.Id,
            SucursalId = sucursal.Id,
            Nombre = "Dueño Test",
            UsuarioLogin = "dueno",
            PasswordHash = hasher.Hash(Contrasenas.Dueno),
            RolId = RolesSistema.Dueno,
            DebeCambiarPassword = false,
        };
        var vendedor = new Usuario
        {
            EmpresaId = empresa.Id,
            SucursalId = sucursal.Id,
            Nombre = "Vendedor Test",
            UsuarioLogin = "vendedor",
            PasswordHash = hasher.Hash(Contrasenas.Vendedor),
            RolId = RolesSistema.Vendedor,
            DebeCambiarPassword = false,
        };

        var productoSimple = new Producto
        {
            EmpresaId = empresa.Id,
            Nombre = "Arroz 1kg",
            UnidadBaseId = 1,
            CostoPromedio = 6m,
            PrecioBase = 10m,
            StockMinimo = 5m,
        };
        var productoConLotes = new Producto
        {
            EmpresaId = empresa.Id,
            Nombre = "Amoxicilina 500mg",
            UnidadBaseId = 1,
            CostoPromedio = 1.2m,
            PrecioBase = 2.5m,
            StockMinimo = 20m,
            ManejaLote = true,
        };
        var productoControlado = new Producto
        {
            EmpresaId = empresa.Id,
            Nombre = "Clonazepam 2mg",
            UnidadBaseId = 1,
            CostoPromedio = 3m,
            PrecioBase = 8m,
            StockMinimo = 10m,
            EsControlado = true,
        };
        var productoConPresentaciones = new Producto
        {
            EmpresaId = empresa.Id,
            Nombre = "Gaseosa 500ml",
            UnidadBaseId = 1,
            CostoPromedio = 2m,
            PrecioBase = 4m,
            StockMinimo = 12m,
        };
        var presentacionCajaX10 = new ProductoPresentacion
        {
            ProductoId = productoConPresentaciones.Id,
            Nombre = "Caja x10",
            Factor = 10m,
            Precio = 35m,
        };

        db.Empresas.Add(empresa);
        db.Sucursales.Add(sucursal);
        db.Usuarios.AddRange(dueno, vendedor);
        db.Productos.AddRange(productoSimple, productoConLotes, productoControlado, productoConPresentaciones);
        db.ProductoPresentaciones.Add(presentacionCajaX10);

        await db.SaveChangesAsync();

        return new ContextoPrueba
        {
            Empresa = empresa,
            Sucursal = sucursal,
            Dueno = dueno,
            Vendedor = vendedor,
            ProductoSimple = productoSimple,
            ProductoConLotes = productoConLotes,
            ProductoControlado = productoControlado,
            ProductoConPresentaciones = productoConPresentaciones,
            PresentacionCajaX10 = presentacionCajaX10,
        };
    }

    protected async Task<Lote> CrearLoteAsync(Producto producto, string numero, DateOnly? vencimiento)
    {
        using var db = CrearContexto(new FakeCurrentUserService());
        var lote = new Lote { ProductoId = producto.Id, Numero = numero, FechaVencimiento = vencimiento };
        db.Lotes.Add(lote);
        await db.SaveChangesAsync();
        return lote;
    }

    protected async Task<Stock> AgregarStockAsync(Sucursal sucursal, Producto producto, decimal cantidad, Lote? lote = null)
    {
        using var db = CrearContexto(new FakeCurrentUserService());
        var stock = new Stock
        {
            SucursalId = sucursal.Id,
            ProductoId = producto.Id,
            LoteId = lote?.Id,
            Cantidad = cantidad,
        };
        db.Stocks.Add(stock);
        await db.SaveChangesAsync();
        return stock;
    }

    protected async Task<SesionCaja> AbrirCajaAsync(Sucursal sucursal, Usuario usuario, decimal montoInicial = 500m)
    {
        using var db = CrearContexto(new FakeCurrentUserService());
        var sesion = new SesionCaja
        {
            SucursalId = sucursal.Id,
            UsuarioId = usuario.Id,
            MontoInicial = montoInicial,
            Estado = "ABIERTA",
        };
        db.SesionesCaja.Add(sesion);
        await db.SaveChangesAsync();
        return sesion;
    }

    protected async Task EstablecerConfiguracionAsync(Empresa empresa, string clave, string valor)
    {
        using var db = CrearContexto(new FakeCurrentUserService());
        db.EmpresaConfiguraciones.Add(new EmpresaConfiguracion { EmpresaId = empresa.Id, Clave = clave, Valor = valor });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Envía un comando/query por el pipeline REAL de MediatR (AddApplication:
    /// MediatR + ValidationBehavior), como lo haría un endpoint. Necesario para
    /// los tests que dependen de FluentValidation (pagos descuadrados, receta
    /// de controlados, sesión de caja abierta): esas reglas viven en el
    /// validador, no en el handler, así que llamar al handler directo se las
    /// saltaría.
    /// </summary>
    protected async Task<TResponse> EnviarComoAsync<TResponse>(
        ContextoPrueba ctx, Usuario usuario, IEnumerable<string>? permisos, IRequest<TResponse> command)
    {
        var currentUser = ctx.ComoUsuario(usuario, permisos);

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<ICurrentUserService>(currentUser);
        services.AddScoped(_ => FabricaContexto.Crear(Fixture.ConnectionString, currentUser));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(command);
    }

    protected async Task<decimal> StockTotalAsync(Sucursal sucursal, Producto producto)
    {
        using var db = CrearContexto(new FakeCurrentUserService { EmpresaId = producto.EmpresaId });
        return await db.Stocks.IgnoreQueryFilters()
            .Where(s => s.SucursalId == sucursal.Id && s.ProductoId == producto.Id)
            .SumAsync(s => (decimal?)s.Cantidad, default) ?? 0m;
    }
}
