using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserService currentUser) : DbContext(options), IApplicationDbContext
{
    private readonly ICurrentUserService _currentUser = currentUser;

    // Núcleo
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rubro> Rubros => Set<Rubro>();
    public DbSet<EmpresaConfiguracion> EmpresaConfiguraciones => Set<EmpresaConfiguracion>();
    // Seguridad
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    // Catálogo
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ProductoPresentacion> ProductoPresentaciones => Set<ProductoPresentacion>();
    public DbSet<ProductoFarmacia> ProductosFarmacia => Set<ProductoFarmacia>();
    public DbSet<ProductoMaestro> ProductosMaestro => Set<ProductoMaestro>();
    public DbSet<PrecioHistorial> PreciosHistorial => Set<PrecioHistorial>();
    // Inventario
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();
    public DbSet<TransferenciaDetalle> TransferenciaDetalles => Set<TransferenciaDetalle>();
    // Compras
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> CompraDetalles => Set<CompraDetalle>();
    // Ventas
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<SesionCaja> SesionesCaja => Set<SesionCaja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<DevolucionDetalle> DevolucionDetalles => Set<DevolucionDetalle>();
    public DbSet<Secuencia> Secuencias => Set<Secuencia>();
    // Notificaciones
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    public void Detach(object entity) => Entry(entity).State = EntityState.Detached;

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Precisión por defecto para montos; se sobreescribe donde toca (14,3 / 14,4)
        builder.Properties<decimal>().HavePrecision(14, 2);
        builder.Properties<string>().HaveMaxLength(200);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ============================================================
        // QUERY FILTER MULTI-TENANT GLOBAL
        // Toda entidad ITenantEntity queda filtrada por la empresa del
        // usuario autenticado. Nadie puede leer datos de otra empresa
        // aunque lo intente: la BD nunca recibe la consulta sin filtro.
        // ============================================================
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var empresaProp = Expression.Property(parameter, nameof(ITenantEntity.EmpresaId));
                var currentEmpresa = Expression.Property(
                    Expression.Field(Expression.Constant(this), nameof(_currentUser)),
                    nameof(ICurrentUserService.EmpresaId));
                var body = Expression.Equal(
                    Expression.Convert(empresaProp, typeof(Guid?)), currentEmpresa);
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(Expression.Lambda(body, parameter));
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
