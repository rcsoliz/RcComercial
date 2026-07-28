using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Persistence.Configurations;

public class RolConfig : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> b)
    {
        b.ToTable("rol");
        b.HasIndex(x => new { x.EmpresaId, x.Nombre }).IsUnique();
        b.Property(x => x.Nombre).HasMaxLength(50);
        b.HasMany(x => x.Permisos).WithOne().HasForeignKey(x => x.RolId);

        b.HasData(
            new { Id = RolesSistema.Dueno, Nombre = "Dueño", EsSistema = true, Activo = true },
            new { Id = RolesSistema.Encargado, Nombre = "Encargado", EsSistema = true, Activo = true },
            new { Id = RolesSistema.Vendedor, Nombre = "Vendedor", EsSistema = true, Activo = true });
    }
}

public class PermisoConfig : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> b)
    {
        b.ToTable("permiso");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.Codigo).IsUnique();
        b.Property(x => x.Codigo).HasMaxLength(50);

        b.HasData(
            new Permiso { Id = 10, Codigo = Permisos.VentasCrear, Modulo = "Ventas", Nombre = "Registrar ventas" },
            new Permiso { Id = 11, Codigo = Permisos.VentasAnular, Modulo = "Ventas", Nombre = "Anular ventas", EsSensible = true },
            new Permiso { Id = 12, Codigo = Permisos.VentasDescuento, Modulo = "Ventas", Nombre = "Aplicar descuentos", EsSensible = true },
            new Permiso { Id = 13, Codigo = Permisos.VentasVerHistorial, Modulo = "Ventas", Nombre = "Ver historial de ventas" },
            new Permiso { Id = 20, Codigo = Permisos.CajaAbrirCerrar, Modulo = "Caja", Nombre = "Abrir y cerrar caja" },
            new Permiso { Id = 21, Codigo = Permisos.CajaVerTodas, Modulo = "Caja", Nombre = "Ver cajas de otros usuarios" },
            new Permiso { Id = 30, Codigo = Permisos.InventarioVer, Modulo = "Inventario", Nombre = "Consultar stock" },
            new Permiso { Id = 31, Codigo = Permisos.InventarioAjustar, Modulo = "Inventario", Nombre = "Ajustes y mermas de inventario", EsSensible = true },
            new Permiso { Id = 32, Codigo = Permisos.InventarioVerCostos, Modulo = "Inventario", Nombre = "Ver costos y utilidades", EsSensible = true },
            new Permiso { Id = 40, Codigo = Permisos.ComprasCrear, Modulo = "Compras", Nombre = "Registrar compras" },
            new Permiso { Id = 41, Codigo = Permisos.ComprasAnular, Modulo = "Compras", Nombre = "Anular compras", EsSensible = true },
            new Permiso { Id = 50, Codigo = Permisos.ProductosCrearEditar, Modulo = "Productos", Nombre = "Crear y editar productos" },
            new Permiso { Id = 51, Codigo = Permisos.ProductosEliminar, Modulo = "Productos", Nombre = "Desactivar productos", EsSensible = true },
            new Permiso { Id = 52, Codigo = Permisos.ProductosCambiarPrecios, Modulo = "Productos", Nombre = "Modificar precios", EsSensible = true },
            new Permiso { Id = 80, Codigo = Permisos.ClientesCrearEditar, Modulo = "Clientes", Nombre = "Crear y editar clientes" },
            new Permiso { Id = 81, Codigo = Permisos.ClientesEliminar, Modulo = "Clientes", Nombre = "Desactivar clientes", EsSensible = true },
            new Permiso { Id = 90, Codigo = Permisos.ProveedoresCrearEditar, Modulo = "Proveedores", Nombre = "Crear y editar proveedores" },
            new Permiso { Id = 91, Codigo = Permisos.ProveedoresEliminar, Modulo = "Proveedores", Nombre = "Desactivar proveedores", EsSensible = true },
            new Permiso { Id = 60, Codigo = Permisos.ReportesVer, Modulo = "Reportes", Nombre = "Ver reportes y panel del negocio" },
            new Permiso { Id = 70, Codigo = Permisos.AdminUsuarios, Modulo = "Administración", Nombre = "Crear, editar y desactivar usuarios", EsSensible = true },
            new Permiso { Id = 71, Codigo = Permisos.AdminRoles, Modulo = "Administración", Nombre = "Configurar roles y permisos", EsSensible = true },
            new Permiso { Id = 72, Codigo = Permisos.AdminConfiguracion, Modulo = "Administración", Nombre = "Configuración del negocio y facturación", EsSensible = true },
            new Permiso { Id = 73, Codigo = Permisos.AdminSucursales, Modulo = "Administración", Nombre = "Gestionar sucursales", EsSensible = true });
    }
}

public class RolPermisoConfig : IEntityTypeConfiguration<RolPermiso>
{
    private static readonly short[] TodosLosPermisos = [10, 11, 12, 13, 20, 21, 30, 31, 32, 40, 41, 50, 51, 52, 60, 70, 71, 72, 73, 80, 81, 90, 91];
    private static readonly short[] PermisosEncargado = [10, 11, 12, 13, 20, 21, 30, 31, 32, 40, 41, 50, 51, 52, 60, 80, 81, 90, 91];
    // Vendedor solo puede crear/editar clientes (alta rápida en el cobro del
    // POS); desactivar clientes y todo lo de proveedores queda para roles de más confianza.
    private static readonly short[] PermisosVendedor = [10, 13, 20, 30, 80];

    public void Configure(EntityTypeBuilder<RolPermiso> b)
    {
        b.ToTable("rol_permiso");
        b.HasKey(x => new { x.RolId, x.PermisoId });
        b.HasOne(x => x.Permiso).WithMany().HasForeignKey(x => x.PermisoId);

        b.HasData(
            TodosLosPermisos.Select(p => new RolPermiso { RolId = RolesSistema.Dueno, PermisoId = p })
                .Concat(PermisosEncargado.Select(p => new RolPermiso { RolId = RolesSistema.Encargado, PermisoId = p }))
                .Concat(PermisosVendedor.Select(p => new RolPermiso { RolId = RolesSistema.Vendedor, PermisoId = p })));
    }
}

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_token");
        b.HasIndex(x => new { x.UsuarioId, x.ExpiraEn });
    }
}

public class AuditoriaConfig : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> b)
    {
        // Tabla particionada por rango de fecha: se crea vía SQL
        // (database/02_revision_dba.sql), EF solo la mapea.
        b.ToTable("auditoria", t => t.ExcludeFromMigrations());
        b.HasKey(x => new { x.Id, x.Fecha });
        b.Property(x => x.Id).ValueGeneratedOnAdd(); // clave compuesta: EF no lo infiere solo
        b.Property(x => x.Detalle).HasColumnType("jsonb");
    }
}
