namespace RcComercial.Domain.Entities;

/// <summary>Catálogo global definido por el sistema. Convención: modulo.accion.</summary>
public class Permiso
{
    public short Id { get; set; }
    public string Codigo { get; set; } = default!;
    public string Modulo { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public bool EsSensible { get; set; }
}
