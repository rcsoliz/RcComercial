using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Persistence.Configurations;

public class ProductoConfig : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.ToTable("producto");
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique();
        b.HasIndex(x => new { x.EmpresaId, x.CodigoBarras });
        b.HasIndex(x => new { x.EmpresaId, x.Activo, x.Nombre });
        b.Property(x => x.CostoPromedio).HasPrecision(14, 4);
        b.Property(x => x.StockMinimo).HasPrecision(14, 3);
        b.HasOne(x => x.FichaFarmacia).WithOne()
            .HasForeignKey<ProductoFarmacia>(x => x.ProductoId);
        b.HasMany(x => x.Presentaciones).WithOne()
            .HasForeignKey(x => x.ProductoId);
    }
}

public class ProductoPresentacionConfig : IEntityTypeConfiguration<ProductoPresentacion>
{
    public void Configure(EntityTypeBuilder<ProductoPresentacion> b)
    {
        b.ToTable("producto_presentacion");
        b.HasIndex(x => x.CodigoBarras);
        b.Property(x => x.Factor).HasPrecision(14, 4);
        b.Property(x => x.CantidadMinMayorista).HasPrecision(14, 3);
    }
}

public class ProductoFarmaciaConfig : IEntityTypeConfiguration<ProductoFarmacia>
{
    public void Configure(EntityTypeBuilder<ProductoFarmacia> b)
    {
        b.ToTable("producto_farmacia");
        b.HasKey(x => x.ProductoId);
        b.HasIndex(x => x.PrincipioActivo);
        b.Property(x => x.Clasificacion).HasMaxLength(20);
    }
}

public class ProductoMaestroConfig : IEntityTypeConfiguration<ProductoMaestro>
{
    public void Configure(EntityTypeBuilder<ProductoMaestro> b)
    {
        b.ToTable("producto_maestro");
        b.HasIndex(x => x.CodigoBarras).IsUnique();
    }
}

public class PrecioHistorialConfig : IEntityTypeConfiguration<PrecioHistorial>
{
    public void Configure(EntityTypeBuilder<PrecioHistorial> b)
    {
        b.ToTable("precio_historial");
        b.HasIndex(x => new { x.ProductoId, x.Fecha });
    }
}

public class CategoriaConfig : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> b)
    {
        b.ToTable("categoria");
        b.Property(x => x.Nombre).HasMaxLength(100);
    }
}

public class MarcaConfig : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> b)
    {
        b.ToTable("marca");
        b.Property(x => x.Nombre).HasMaxLength(100);
    }
}

public class UnidadMedidaConfig : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> b)
    {
        b.ToTable("unidad_medida");
        b.Property(x => x.Id).ValueGeneratedNever();
    }
}
