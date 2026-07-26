using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Compras.Dtos;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Compras.Commands.CrearCompra;

public class CrearCompraCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearCompraCommand, CompraDto>
{
    public async Task<CompraDto> Handle(CrearCompraCommand request, CancellationToken ct)
    {
        var empresaId = currentUser.EmpresaId!.Value;
        var usuarioId = currentUser.UsuarioId!.Value;

        var sucursalId = await SucursalResolver.ResolverAsync(db, currentUser, request.SucursalId, ct)
            ?? throw new ValidationException("No se pudo determinar la sucursal: especifique 'sucursalId'.");

        var productoIds = request.Detalles.Select(d => d.ProductoId).Distinct().ToList();
        var productos = await db.Productos
            .Include(p => p.Presentaciones)
            .Where(p => productoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var numero = await db.SiguienteNumeroAsync(empresaId, sucursalId, "COMPRA", ct);

        var compra = new Compra
        {
            EmpresaId = empresaId,
            SucursalId = sucursalId,
            ProveedorId = request.ProveedorId,
            Numero = numero.ToString("00000000"),
            NroFacturaProv = request.NroFacturaProv,
            UsuarioId = usuarioId,
        };

        // Acumula stock+costo dentro de ESTA compra por producto, para que el
        // costo promedio ponderado sea correcto si el mismo producto aparece
        // en más de una línea (ej. dos lotes distintos).
        var acumuladoPorProducto = new Dictionary<Guid, (decimal Stock, decimal Costo)>();

        foreach (var d in request.Detalles)
        {
            if (!productos.TryGetValue(d.ProductoId, out var producto))
                throw new ValidationException($"El producto {d.ProductoId} no existe.");

            var factor = 1m;
            if (d.PresentacionId is { } presentacionId)
            {
                var presentacion = producto.Presentaciones.FirstOrDefault(p => p.Id == presentacionId)
                    ?? throw new ValidationException(
                        $"La presentación indicada no pertenece a '{producto.Nombre}'.");
                factor = presentacion.Factor;
            }
            var cantidadBase = d.Cantidad * factor;

            Guid? loteId = null;
            if (producto.ManejaLote && !string.IsNullOrWhiteSpace(d.NumeroLote))
            {
                var lote = await db.Lotes.FirstOrDefaultAsync(
                    l => l.ProductoId == producto.Id && l.Numero == d.NumeroLote, ct);
                if (lote is null)
                {
                    lote = new Lote
                    {
                        ProductoId = producto.Id, Numero = d.NumeroLote, FechaVencimiento = d.FechaVencimiento,
                    };
                    db.Lotes.Add(lote);
                }
                loteId = lote.Id;
            }

            var stock = await db.Stocks.FirstOrDefaultAsync(
                s => s.SucursalId == sucursalId && s.ProductoId == producto.Id && s.LoteId == loteId, ct);
            if (stock is null)
            {
                stock = new Stock { SucursalId = sucursalId, ProductoId = producto.Id, LoteId = loteId };
                db.Stocks.Add(stock);
            }
            stock.Cantidad += cantidadBase;
            stock.ActualizadoEn = DateTimeOffset.UtcNow;

            db.MovimientosInventario.Add(new MovimientoInventario
            {
                EmpresaId = empresaId,
                SucursalId = sucursalId,
                ProductoId = producto.Id,
                LoteId = loteId,
                Tipo = TiposMovimiento.Compra,
                Cantidad = cantidadBase,
                CostoUnitario = d.CostoUnitario,
                ReferenciaTipo = "COMPRA",
                ReferenciaId = compra.Id,
                UsuarioId = usuarioId,
            });

            if (!acumuladoPorProducto.TryGetValue(producto.Id, out var acumulado))
            {
                var stockTotalPrevio = await db.Stocks
                    .Where(s => s.ProductoId == producto.Id)
                    .SumAsync(s => s.Cantidad, ct);
                acumulado = (stockTotalPrevio, producto.CostoPromedio);
            }

            var nuevoStockTotal = acumulado.Stock + cantidadBase;
            var nuevoCosto = nuevoStockTotal == 0
                ? d.CostoUnitario
                : ((acumulado.Stock * acumulado.Costo) + (cantidadBase * d.CostoUnitario)) / nuevoStockTotal;

            acumuladoPorProducto[producto.Id] = (nuevoStockTotal, nuevoCosto);
            producto.CostoPromedio = nuevoCosto;

            compra.Detalles.Add(new CompraDetalle
            {
                CompraId = compra.Id,
                ProductoId = producto.Id,
                PresentacionId = d.PresentacionId,
                Cantidad = d.Cantidad,
                CantidadBase = cantidadBase,
                CostoUnitario = d.CostoUnitario,
                NumeroLote = d.NumeroLote,
                FechaVencimiento = d.FechaVencimiento,
            });
        }

        compra.Subtotal = compra.Detalles.Sum(d => d.CantidadBase * d.CostoUnitario);
        compra.Total = compra.Subtotal;

        db.Compras.Add(compra);
        await db.SaveChangesAsync(ct);

        return new CompraDto(
            compra.Id, compra.Numero, compra.Fecha, compra.ProveedorId, compra.NroFacturaProv, compra.Estado,
            compra.Subtotal, compra.Total,
            compra.Detalles.Select(d => new CompraDetalleDto(
                d.Id, d.ProductoId, d.PresentacionId, d.Cantidad, d.CantidadBase, d.CostoUnitario,
                d.NumeroLote, d.FechaVencimiento)).ToList());
    }
}
