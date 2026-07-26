using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiraEn { get; set; }
    public DateTimeOffset? RevocadoEn { get; set; }
    public Guid? ReemplazadoPor { get; set; }
    public string? IpCreacion { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}
