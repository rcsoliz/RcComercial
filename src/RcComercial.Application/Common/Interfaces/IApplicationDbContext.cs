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
    DbSet<Categoria> Categorias { get; }
    DbSet<Marca> Marcas { get; }
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

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Deja de trackear una entidad (usado para descartar cambios de una fila fallida en imports por lote).</summary>
    void Detach(object entity);

    /// <summary>
    /// Incremento atómico de secuencia.numero (INSERT ... ON CONFLICT DO UPDATE RETURNING),
    /// sin SELECT-then-UPDATE: dos ventas concurrentes nunca reciben el mismo número.
    /// </summary>
    Task<long> SiguienteNumeroAsync(Guid empresaId, Guid sucursalId, string tipoDocumento, CancellationToken ct = default);
}
