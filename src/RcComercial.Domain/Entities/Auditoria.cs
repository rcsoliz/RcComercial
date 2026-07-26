namespace RcComercial.Domain.Entities;

public class Auditoria
{
    public long Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Accion { get; set; } = default!;
    public string? Entidad { get; set; }
    public Guid? EntidadId { get; set; }
    public string? Detalle { get; set; } // JSONB
    public string? Ip { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
}
