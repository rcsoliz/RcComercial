namespace RcComercial.Domain.Common;

/// <summary>
/// Marca una entidad como perteneciente a una empresa (tenant).
/// El AppDbContext aplica automáticamente un query filter global
/// por EmpresaId: ninguna consulta cruza datos entre empresas.
/// </summary>
public interface ITenantEntity
{
    Guid EmpresaId { get; set; }
}
