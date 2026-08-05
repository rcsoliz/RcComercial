namespace RcComercial.Domain.Entities;

/// <summary>Catálogo global. Agregar un rubro nuevo = un INSERT, no una migración.</summary>
public class Rubro
{
    public short Id { get; set; }
    public string Codigo { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public bool UsaLotesPorDefecto { get; set; }
    public bool UsaControlados { get; set; }
    public bool UsaFichaFarmacia { get; set; }
    public bool UsaDecimalesPorDefecto { get; set; }
    // Negocios que venden principalmente servicios (talleres, consultorías):
    // el frontend usa esto para mostrar "Servicio" en vez de "Producto" en
    // menú/listado/formulario, y ocultar la columna de stock (no aplica).
    public bool EsTipoServicio { get; set; }
    public bool Activo { get; set; } = true;
}
