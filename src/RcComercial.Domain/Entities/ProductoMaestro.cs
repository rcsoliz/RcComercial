using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>
/// Catálogo maestro GLOBAL (compartido entre empresas): al escanear un
/// código no registrado, se ofrece el producto precargado.
/// </summary>
public class ProductoMaestro : BaseEntity
{
    public string CodigoBarras { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public string? Marca { get; set; }
    public string? Contenido { get; set; }
    public short? RubroId { get; set; }
    public string? PrincipioActivo { get; set; }
    public string? Concentracion { get; set; }
    public string? Laboratorio { get; set; }
    public bool Verificado { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}
