using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Persistence.Configurations;

public class ProveedorConfig : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> b)
    {
        b.ToTable("proveedor");
        b.Property(x => x.Nombre).HasMaxLength(150);
    }
}

public class CompraConfig : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> b)
    {
        b.ToTable("compra");
        b.HasIndex(x => new { x.ProveedorId, x.Fecha });
        b.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.CompraId);
        b.Property(x => x.Numero).HasMaxLength(20);
    }
}

public class CompraDetalleConfig : IEntityTypeConfiguration<CompraDetalle>
{
    public void Configure(EntityTypeBuilder<CompraDetalle> b)
    {
        b.ToTable("compra_detalle");
        b.Property(x => x.Cantidad).HasPrecision(14, 3);
        b.Property(x => x.CantidadBase).HasPrecision(14, 3);
        b.Property(x => x.CostoUnitario).HasPrecision(14, 4);
    }
}
