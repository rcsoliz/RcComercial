using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Productos.Commands.ImportarProductos;

public class ImportarProductosCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ImportarProductosCommand, ImportarProductosResult>
{
    // Sin columna de unidad en el CSV: se asume unidad base "Unidad" (seed id=1).
    private const short UnidadPorDefecto = 1;

    public async Task<ImportarProductosResult> Handle(ImportarProductosCommand request, CancellationToken ct)
    {
        var empresaId = currentUser.EmpresaId!.Value;
        var usuarioId = currentUser.UsuarioId!.Value;
        var errores = new List<string>(request.ErroresParseo);
        var creados = 0;

        foreach (var fila in request.FilasValidas)
        {
            var yaExiste = await db.Productos
                .AnyAsync(p => p.CodigoBarras == fila.CodigoBarras, ct);
            if (yaExiste)
            {
                errores.Add($"Código {fila.CodigoBarras}: ya existe un producto con ese código.");
                continue;
            }

            var producto = new Producto
            {
                EmpresaId = empresaId,
                CodigoBarras = fila.CodigoBarras,
                Nombre = fila.Nombre,
                UnidadBaseId = UnidadPorDefecto,
                PrecioBase = fila.Precio,
            };

            Stock? stock = null;
            MovimientoInventario? movimiento = null;

            db.Productos.Add(producto);
            if (fila.StockInicial > 0)
            {
                stock = new Stock
                {
                    SucursalId = request.SucursalId,
                    ProductoId = producto.Id,
                    Cantidad = fila.StockInicial,
                };
                movimiento = new MovimientoInventario
                {
                    EmpresaId = empresaId,
                    SucursalId = request.SucursalId,
                    ProductoId = producto.Id,
                    Tipo = TiposMovimiento.InventarioInicial,
                    Cantidad = fila.StockInicial,
                    UsuarioId = usuarioId,
                };
                db.Stocks.Add(stock);
                db.MovimientosInventario.Add(movimiento);
            }

            try
            {
                await db.SaveChangesAsync(ct);
                creados++;
            }
            catch (Exception ex)
            {
                db.Detach(producto);
                if (stock is not null) db.Detach(stock);
                if (movimiento is not null) db.Detach(movimiento);
                errores.Add($"Código {fila.CodigoBarras}: {ex.Message}");
            }
        }

        return new ImportarProductosResult(creados, errores);
    }
}
