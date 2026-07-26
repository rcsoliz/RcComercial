namespace RcComercial.Domain.Entities;

/// <summary>Catálogo global: UND, KG, GR, M, CM, LT, ML, CJA, TAB...</summary>
public class UnidadMedida
{
    public short Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Abreviatura { get; set; } = default!;
}
