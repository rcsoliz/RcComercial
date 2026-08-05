using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Persistence.Configurations;

public class ProformaConfig : IEntityTypeConfiguration<Proforma>
{
    public void Configure(EntityTypeBuilder<Proforma> b)
    {
        b.ToTable("proforma");
        b.HasIndex(x => new { x.SucursalId, x.Numero }).IsUnique();
        b.HasIndex(x => x.ClienteId);
        b.HasIndex(x => x.VehiculoId);
        b.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.ProformaId);
        b.Property(x => x.Numero).HasMaxLength(20);
        b.Property(x => x.Estado).HasMaxLength(15);
    }
}

public class ProformaDetalleConfig : IEntityTypeConfiguration<ProformaDetalle>
{
    public void Configure(EntityTypeBuilder<ProformaDetalle> b)
    {
        b.ToTable("proforma_detalle");
        b.Property(x => x.Cantidad).HasPrecision(14, 3);
        b.Property(x => x.CantidadBase).HasPrecision(14, 3);
        b.Property(x => x.PrecioUnitario).HasPrecision(14, 4);
    }
}
