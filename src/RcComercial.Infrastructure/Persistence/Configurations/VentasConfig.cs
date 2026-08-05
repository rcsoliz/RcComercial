using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Persistence.Configurations;

public class VentaConfig : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> b)
    {
        b.ToTable("venta");
        b.Property(x => x.Id).ValueGeneratedNever(); // el ID lo genera el cliente (offline)
        b.HasIndex(x => new { x.SucursalId, x.Numero }).IsUnique();
        b.HasIndex(x => new { x.EmpresaId, x.Fecha });
        b.HasIndex(x => new { x.EmpresaId, x.Estado, x.Fecha });
        b.HasIndex(x => x.VehiculoId);
        b.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.VentaId);
        b.HasMany(x => x.Pagos).WithOne().HasForeignKey(x => x.VentaId);
        b.Property(x => x.Numero).HasMaxLength(20);
        b.Property(x => x.Estado).HasMaxLength(15);
        b.Property(x => x.EstadoSiat).HasMaxLength(15);
    }
}

public class VentaDetalleConfig : IEntityTypeConfiguration<VentaDetalle>
{
    public void Configure(EntityTypeBuilder<VentaDetalle> b)
    {
        b.ToTable("venta_detalle");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.ProductoId);
        b.Property(x => x.Cantidad).HasPrecision(14, 3);
        b.Property(x => x.CantidadBase).HasPrecision(14, 3);
    }
}

public class PagoConfig : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> b)
    {
        b.ToTable("pago");
        b.HasIndex(x => x.VentaId);
        b.Property(x => x.Metodo).HasMaxLength(15);
    }
}

public class SesionCajaConfig : IEntityTypeConfiguration<SesionCaja>
{
    public void Configure(EntityTypeBuilder<SesionCaja> b)
    {
        b.ToTable("sesion_caja");
        b.HasIndex(x => new { x.SucursalId, x.Apertura });
        b.Property(x => x.Estado).HasMaxLength(10);
    }
}

public class ClienteConfig : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("cliente");
        b.HasIndex(x => new { x.EmpresaId, x.TelefonoWhatsapp });
        b.Property(x => x.Nombre).HasMaxLength(150);
    }
}

public class RecetaConfig : IEntityTypeConfiguration<Receta>
{
    public void Configure(EntityTypeBuilder<Receta> b)
    {
        b.ToTable("receta");
        b.HasIndex(x => x.VentaId);
    }
}

public class DevolucionConfig : IEntityTypeConfiguration<Devolucion>
{
    public void Configure(EntityTypeBuilder<Devolucion> b)
    {
        b.ToTable("devolucion");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.SucursalId, x.Numero }).IsUnique();
        b.HasIndex(x => x.VentaId);
        b.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.DevolucionId);
    }
}

public class DevolucionDetalleConfig : IEntityTypeConfiguration<DevolucionDetalle>
{
    public void Configure(EntityTypeBuilder<DevolucionDetalle> b)
    {
        b.ToTable("devolucion_detalle");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.CantidadBase).HasPrecision(14, 3);
    }
}

public class SecuenciaConfig : IEntityTypeConfiguration<Secuencia>
{
    public void Configure(EntityTypeBuilder<Secuencia> b)
    {
        b.ToTable("secuencia");
        b.HasKey(x => new { x.EmpresaId, x.SucursalId, x.TipoDocumento });
        b.Property(x => x.TipoDocumento).HasMaxLength(20);
        b.Property(x => x.Prefijo).HasMaxLength(10);
    }
}

public class NotificacionConfig : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> b)
    {
        b.ToTable("notificacion");
        b.HasIndex(x => new { x.Estado, x.CreadoEn });
        b.HasIndex(x => new { x.Estado, x.ProximoIntentoEn });
        b.Property(x => x.Contenido).HasMaxLength(4000);
        b.Property(x => x.Tipo).HasMaxLength(30);
        b.Property(x => x.EnlaceGenerado).HasMaxLength(4000);
    }
}
