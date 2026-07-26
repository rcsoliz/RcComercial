namespace RcComercial.Domain.Entities;

/// <summary>
/// Numeración atómica por sucursal y tipo de documento.
/// Nunca usar MAX(numero)+1: genera duplicados con cajas concurrentes.
/// </summary>
public class Secuencia
{
    public Guid EmpresaId { get; set; }
    public Guid SucursalId { get; set; }
    public string TipoDocumento { get; set; } = default!;
    public string Prefijo { get; set; } = "";
    public long Siguiente { get; set; } = 1;
}
