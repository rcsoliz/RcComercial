using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>
/// REGLA DE ORO: todo stock, movimiento y cantidad_base se expresa
/// en la unidad base (la tableta, el metro, el kilo, la unidad).
/// </summary>
public class Producto : BaseEntity, ITenantEntity, IAuditable
{
    public Guid EmpresaId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = default!;
    public Guid? CategoriaId { get; set; }
    public Guid? MarcaId { get; set; }
    public short UnidadBaseId { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal StockMinimo { get; set; }
    public bool ManejaLote { get; set; }
    public bool EsControlado { get; set; }
    public bool PermiteDecimales { get; set; }
    public bool Activo { get; set; } = true;
    public Guid? ProductoMaestroId { get; set; }

    // Facturación (Fase 9, facturador-by-rc): nullable a propósito, solo se
    // exigen al facturar; la tienda que no factura no está obligada a llenarlos.
    public int? CodigoProductoSin { get; set; }
    public int? CodigoUnidadSin { get; set; }

    public ProductoFarmacia? FichaFarmacia { get; set; }
    public ICollection<ProductoPresentacion> Presentaciones { get; set; } = new List<ProductoPresentacion>();

    public DateTimeOffset CreadoEn { get; set; }
    public Guid? CreadoPor { get; set; }
    public DateTimeOffset? ActualizadoEn { get; set; }
    public Guid? ActualizadoPor { get; set; }
}
