using Microsoft.EntityFrameworkCore;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Common.Interfaces;

/// <summary>
/// Puerto hacia el DbContext para los handlers de MediatR en Application,
/// sin acoplarlos a Infrastructure/EF. Implementado por AppDbContext.
/// Expone solo lo que los casos de uso de esta fase necesitan.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Empresa> Empresas { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<Rol> Roles { get; }
    DbSet<Rubro> Rubros { get; }
    DbSet<Permiso> Permisos { get; }
    DbSet<RolPermiso> RolPermisos { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Categoria> Categorias { get; }
    DbSet<Marca> Marcas { get; }
    DbSet<UnidadMedida> UnidadesMedida { get; }
    DbSet<Producto> Productos { get; }
    DbSet<ProductoPresentacion> ProductoPresentaciones { get; }
    DbSet<ProductoMaestro> ProductosMaestro { get; }
    DbSet<PrecioHistorial> PreciosHistorial { get; }
    DbSet<Stock> Stocks { get; }
    DbSet<MovimientoInventario> MovimientosInventario { get; }
    DbSet<Lote> Lotes { get; }
    DbSet<Sucursal> Sucursales { get; }
    DbSet<Cliente> Clientes { get; }
    DbSet<SesionCaja> SesionesCaja { get; }
    DbSet<Venta> Ventas { get; }
    DbSet<VentaDetalle> VentaDetalles { get; }
    DbSet<Pago> Pagos { get; }
    DbSet<Receta> Recetas { get; }
    DbSet<Devolucion> Devoluciones { get; }
    DbSet<DevolucionDetalle> DevolucionDetalles { get; }
    DbSet<Notificacion> Notificaciones { get; }
    DbSet<EmpresaConfiguracion> EmpresaConfiguraciones { get; }
    DbSet<Proveedor> Proveedores { get; }
    DbSet<Compra> Compras { get; }
    DbSet<CompraDetalle> CompraDetalles { get; }
    DbSet<Vehiculo> Vehiculos { get; }
    DbSet<Proforma> Proformas { get; }
    DbSet<ProformaDetalle> ProformaDetalles { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Deja de trackear una entidad (usado para descartar cambios de una fila fallida en imports por lote).</summary>
    void Detach(object entity);

    /// <summary>
    /// Limpia TODO el change tracker (ChangeTracker.Clear()). Para lotes que
    /// reusan un command handler ítem por ítem (p. ej. sync de ventas offline):
    /// si un ítem falla a mitad de camino, sus entidades ya trackeadas
    /// (detalles, pagos, stock nuevo...) no son alcanzables desde fuera del
    /// handler reusado para Detach()-earlas una por una: sin este limpiado,
    /// SaveChangesAsync del SIGUIENTE ítem exitoso las volvería a insertar.
    /// </summary>
    void LimpiarSeguimiento();

    /// <summary>
    /// Incremento atómico de secuencia.numero (INSERT ... ON CONFLICT DO UPDATE RETURNING),
    /// sin SELECT-then-UPDATE: dos ventas concurrentes nunca reciben el mismo número.
    /// </summary>
    Task<long> SiguienteNumeroAsync(Guid empresaId, Guid sucursalId, string tipoDocumento, CancellationToken ct = default);

    /// <summary>
    /// Reserva un BLOQUE atómico de <paramref name="tamano"/> números de la
    /// misma secuencia (mismo INSERT ... ON CONFLICT DO UPDATE RETURNING,
    /// incrementando por el tamaño del bloque en vez de 1). Para numeración
    /// offline por dispositivo: el bloque devuelto (inicio..inicio+tamano-1)
    /// jamás se solapa con otro dispositivo ni con ventas online, porque
    /// ambos consumen la MISMA secuencia compartida.
    /// </summary>
    Task<long> ReservarRangoNumeroAsync(
        Guid empresaId, Guid sucursalId, string tipoDocumento, int tamano, CancellationToken ct = default);

    /// <summary>
    /// Ajuste atómico de stock.cantidad (UPDATE ... RETURNING condicional, sin
    /// lectura previa): evita el "lost update" de dos ventas/compras
    /// concurrentes sobre el mismo producto/lote. Si <paramref name="permiteNegativo"/>
    /// es false y el ajuste dejaría el stock negativo, no aplica nada y
    /// devuelve null (el caller lo trata como stock insuficiente).
    /// </summary>
    Task<decimal?> AjustarStockAsync(
        Guid stockId, decimal delta, bool permiteNegativo, CancellationToken ct = default);

    /// <summary>
    /// Envuelve <paramref name="operacion"/> en una transacción explícita de BD.
    /// Necesario porque AjustarStockAsync/SiguienteNumeroAsync son SQL crudo que
    /// se ejecuta de inmediato (no participan del batch diferido de
    /// SaveChangesAsync): sin esto, un ajuste de stock podría quedar confirmado
    /// aunque el resto de la operación (venta/compra/kardex) falle después,
    /// violando "stock y kardex en la misma transacción, siempre".
    /// </summary>
    Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default);
}
