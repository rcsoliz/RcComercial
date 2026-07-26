using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>Registro obligatorio para venta de controlados (farmacia).</summary>
public class Receta : BaseEntity
{
    public Guid VentaId { get; set; }
    public string MedicoNombre { get; set; } = default!;
    public string MedicoMatricula { get; set; } = default!;
    public string PacienteNombre { get; set; } = default!;
    public string? PacienteCi { get; set; }
    public DateOnly FechaReceta { get; set; }
    public string? ImagenUrl { get; set; }
}
