namespace RcComercial.Domain.Entities;

/// <summary>Extensión 1:1, solo existe si el producto es farmacéutico.</summary>
public class ProductoFarmacia
{
    public Guid ProductoId { get; set; }
    public string? PrincipioActivo { get; set; }
    public string? Concentracion { get; set; }
    public string? FormaFarmaceutica { get; set; }
    public string? Laboratorio { get; set; }
    public string? RegistroSanitario { get; set; }
    public string Clasificacion { get; set; } = Common.ClasificacionesFarmacia.Libre;
    public bool RequiereCadenaFrio { get; set; }
}
