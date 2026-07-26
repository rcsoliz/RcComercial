namespace RcComercial.Domain.Common;

/// <summary>
/// Campos de auditoría llenados automáticamente por el
/// AuditableEntityInterceptor en cada SaveChanges.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreadoEn { get; set; }
    Guid? CreadoPor { get; set; }
    DateTimeOffset? ActualizadoEn { get; set; }
    Guid? ActualizadoPor { get; set; }
}
