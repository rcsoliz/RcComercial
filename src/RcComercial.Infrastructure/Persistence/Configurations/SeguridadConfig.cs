using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
    }
}

public class RolPermisoConfig : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> b)
    {
        b.ToTable("rol_permiso");
        b.HasKey(x => new { x.RolId, x.PermisoId });
        b.HasOne(x => x.Permiso).WithMany().HasForeignKey(x => x.PermisoId);
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
        b.Property(x => x.Detalle).HasColumnType("jsonb");
    }
}
