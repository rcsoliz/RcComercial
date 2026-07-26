using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Persistence.Configurations;

public class EmpresaConfig : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.ToTable("empresa");
        b.Property(x => x.Nombre).HasMaxLength(150);
        b.Property(x => x.Nit).HasMaxLength(20);
        b.Property(x => x.TelefonoWhatsapp).HasMaxLength(20);
    }
}

public class SucursalConfig : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> b)
    {
        b.ToTable("sucursal");
        b.HasIndex(x => x.EmpresaId);
        b.Property(x => x.Nombre).HasMaxLength(100);
    }
}

public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuario");
        b.HasIndex(x => new { x.EmpresaId, x.UsuarioLogin }).IsUnique();
        b.Property(x => x.Nombre).HasMaxLength(100);
        b.Property(x => x.UsuarioLogin).HasMaxLength(50);
    }
}

public class RubroConfig : IEntityTypeConfiguration<Rubro>
{
    public void Configure(EntityTypeBuilder<Rubro> b)
    {
        b.ToTable("rubro");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.Codigo).IsUnique();
    }
}

public class EmpresaConfiguracionConfig : IEntityTypeConfiguration<EmpresaConfiguracion>
{
    public void Configure(EntityTypeBuilder<EmpresaConfiguracion> b)
    {
        b.ToTable("empresa_configuracion");
        b.HasKey(x => new { x.EmpresaId, x.Clave });
        b.Property(x => x.Clave).HasMaxLength(60);
        b.Property(x => x.Valor).HasColumnType("jsonb");
    }
}
