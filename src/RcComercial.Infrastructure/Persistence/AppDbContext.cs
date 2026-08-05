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
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Proforma> Proformas => Set<Proforma>();
    public DbSet<ProformaDetalle> ProformaDetalles => Set<ProformaDetalle>();
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

    public void LimpiarSeguimiento() => ChangeTracker.Clear();

    public async Task<long> SiguienteNumeroAsync(
        Guid empresaId, Guid sucursalId, string tipoDocumento, CancellationToken ct = default)
    {
        var resultado = await Database.SqlQuery<long>($"""
            INSERT INTO secuencia (empresa_id, sucursal_id, tipo_documento, prefijo, siguiente)
            VALUES ({empresaId}, {sucursalId}, {tipoDocumento}, '', 2)
            ON CONFLICT (empresa_id, sucursal_id, tipo_documento)
            DO UPDATE SET siguiente = secuencia.siguiente + 1
            RETURNING siguiente - 1
            """).ToListAsync(ct);
        return resultado[0];
    }

    public async Task<long> ReservarRangoNumeroAsync(
        Guid empresaId, Guid sucursalId, string tipoDocumento, int tamano, CancellationToken ct = default)
    {
        var resultado = await Database.SqlQuery<long>($"""
            INSERT INTO secuencia (empresa_id, sucursal_id, tipo_documento, prefijo, siguiente)
            VALUES ({empresaId}, {sucursalId}, {tipoDocumento}, '', {tamano + 1})
            ON CONFLICT (empresa_id, sucursal_id, tipo_documento)
            DO UPDATE SET siguiente = secuencia.siguiente + {tamano}
            RETURNING siguiente - {tamano}
            """).ToListAsync(ct);
        return resultado[0];
    }

    public async Task<decimal?> AjustarStockAsync(
        Guid stockId, decimal delta, bool permiteNegativo, CancellationToken ct = default)
    {
        var resultado = await Database.SqlQuery<decimal>($"""
            UPDATE stock
            SET cantidad = cantidad + {delta}, actualizado_en = now()
            WHERE id = {stockId} AND ({permiteNegativo} OR cantidad + {delta} >= 0)
            RETURNING cantidad
            """).ToListAsync(ct);
        return resultado.Count > 0 ? resultado[0] : null;
    }

    public async Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default)
    {
        var estrategia = Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await Database.BeginTransactionAsync(ct);
            var resultado = await operacion(ct);
            await transaccion.CommitAsync(ct);
            return resultado;
        });
    }

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
