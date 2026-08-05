using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RcComercial.Domain.Entities;

namespace RcComercial.Infrastructure.Persistence.Configurations;

public class VehiculoConfig : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> b)
    {
        b.ToTable("vehiculo");
        // No único: dos clientes podrían compartir una placa mal tipeada,
        // no conviene bloquear la carga de datos por eso.
        b.HasIndex(x => new { x.EmpresaId, x.Placa });
        b.HasIndex(x => x.ClienteId);
        b.Property(x => x.Placa).HasMaxLength(20);
        b.Property(x => x.Marca).HasMaxLength(60);
        b.Property(x => x.Modelo).HasMaxLength(60);
        b.Property(x => x.Color).HasMaxLength(40);
        b.Property(x => x.NumeroChasis).HasMaxLength(60);
    }
}
