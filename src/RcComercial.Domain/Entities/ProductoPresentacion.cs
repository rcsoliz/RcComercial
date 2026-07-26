using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>
/// Cómo se compra/vende: 'Caja x 100' (factor 100), 'Blíster x 10',
/// 'Rollo 50 m'. Resuelve fraccionamiento de farmacia, multi-unidad
/// de ferretería y precio mayorista por cantidad.
/// </summary>
public class ProductoPresentacion : BaseEntity, IAuditable
{
    public Guid ProductoId { get; set; }
    public string Nombre { get; set; } = default!;
    public decimal Factor { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal Precio { get; set; }
    public decimal? PrecioMayorista { get; set; }
    public decimal? CantidadMinMayorista { get; set; }
    public bool EsPredeterminada { get; set; }
    public bool Activo { get; set; } = true;

    public DateTimeOffset CreadoEn { get; set; }
    public Guid? CreadoPor { get; set; }
    public DateTimeOffset? ActualizadoEn { get; set; }
    public Guid? ActualizadoPor { get; set; }
}
