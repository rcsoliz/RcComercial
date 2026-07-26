using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Persistence.Configurations;

public class LoteConfig : IEntityTypeConfiguration<Lote>
{
    public void Configure(EntityTypeBuilder<Lote> b)
    {
        b.ToTable("lote");
        b.HasIndex(x => new { x.ProductoId, x.Numero }).IsUnique();
        b.HasIndex(x => x.FechaVencimiento);
        b.Property(x => x.Numero).HasMaxLength(50);
    }
}

public class StockConfig : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> b)
    {
        b.ToTable("stock");
        b.HasIndex(x => new { x.SucursalId, x.ProductoId, x.LoteId }).IsUnique();
        b.HasIndex(x => new { x.ProductoId, x.SucursalId });
        b.Property(x => x.Cantidad).HasPrecision(14, 3);
    }
}

public class MovimientoInventarioConfig : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> b)
    {
        b.ToTable("movimiento_inventario");
        b.HasIndex(x => new { x.ProductoId, x.Fecha });
        b.HasIndex(x => new { x.SucursalId, x.Fecha });
        b.Property(x => x.Cantidad).HasPrecision(14, 3);
        b.Property(x => x.CostoUnitario).HasPrecision(14, 4);
        b.Property(x => x.Tipo).HasMaxLength(20);
    }
}

public class TransferenciaConfig : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> b)
    {
        b.ToTable("transferencia");
        b.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.TransferenciaId);
        b.Property(x => x.Numero).HasMaxLength(20);
    }
}

public class TransferenciaDetalleConfig : IEntityTypeConfiguration<TransferenciaDetalle>
{
    public void Configure(EntityTypeBuilder<TransferenciaDetalle> b)
    {
        b.ToTable("transferencia_detalle");
        b.Property(x => x.CantidadBase).HasPrecision(14, 3);
    }
}
