using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Notificaciones;
using RcComercial.Application.Ventas.Dtos;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Ventas.Commands.CrearVenta;

public class CrearVentaCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearVentaCommand, VentaDto>
{
    public async Task<VentaDto> Handle(CrearVentaCommand request, CancellationToken ct)
    {
        // Idempotencia: reenviar el mismo Id (offline/reintentos) no duplica.
        var existente = await db.Ventas
            .Include(v => v.Detalles)
            .Include(v => v.Pagos)
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);
        if (existente is not null) return VentaMapper.ToDto(existente);

        var empresaId = currentUser.EmpresaId!.Value;
        var usuarioId = currentUser.UsuarioId!.Value;

        var sesion = await db.SesionesCaja
            .FirstAsync(s => s.UsuarioId == usuarioId && s.Estado == "ABIERTA", ct);

        var permiteStockNegativo = await ObtenerPermiteStockNegativoAsync(db, empresaId, ct);

        var productoIds = request.Detalles.Select(d => d.ProductoId).Distinct().ToList();
        var productos = await db.Productos
            .Include(p => p.Presentaciones)
            .Where(p => productoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var numero = await db.SiguienteNumeroAsync(empresaId, sesion.SucursalId, "VENTA", ct);

        var venta = new Venta
        {
            Id = request.Id,
            EmpresaId = empresaId,
            SucursalId = sesion.SucursalId,
            SesionCajaId = sesion.Id,
            ClienteId = request.ClienteId,
            Numero = numero.ToString("00000000"),
            UsuarioId = usuarioId,
        };

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
            if (producto.ManejaLote)
            {
                var stockLote = await BuscarStockLoteFefoAsync(db, sesion.SucursalId, producto.Id, cantidadBase, ct);
                if (stockLote is null && !permiteStockNegativo)
                    throw new ValidationException($"No hay stock/lote registrado para '{producto.Nombre}'.");
                if (stockLote is not null)
                {
                    if (stockLote.Cantidad < cantidadBase && !permiteStockNegativo)
                        throw new ValidationException($"Stock insuficiente para '{producto.Nombre}'.");
                    loteId = stockLote.LoteId;
                    stockLote.Cantidad -= cantidadBase;
                    stockLote.ActualizadoEn = DateTimeOffset.UtcNow;
                }
            }
            else
            {
                var stock = await db.Stocks.FirstOrDefaultAsync(
                    s => s.SucursalId == sesion.SucursalId && s.ProductoId == producto.Id && s.LoteId == null, ct);
                if (stock is null)
                {
                    if (!permiteStockNegativo)
                        throw new ValidationException($"Stock insuficiente para '{producto.Nombre}'.");
                    stock = new Stock { SucursalId = sesion.SucursalId, ProductoId = producto.Id };
                    db.Stocks.Add(stock);
                }
                else if (stock.Cantidad < cantidadBase && !permiteStockNegativo)
                {
                    throw new ValidationException($"Stock insuficiente para '{producto.Nombre}'.");
                }
                stock.Cantidad -= cantidadBase;
                stock.ActualizadoEn = DateTimeOffset.UtcNow;
            }

            venta.Detalles.Add(new VentaDetalle
            {
                VentaId = venta.Id,
                ProductoId = producto.Id,
                PresentacionId = d.PresentacionId,
                LoteId = loteId,
                Cantidad = d.Cantidad,
                CantidadBase = cantidadBase,
                PrecioUnitario = d.PrecioUnitario,
                Descuento = d.Descuento,
                Total = (d.Cantidad * d.PrecioUnitario) - d.Descuento,
            });

            db.MovimientosInventario.Add(new MovimientoInventario
            {
                EmpresaId = empresaId,
                SucursalId = sesion.SucursalId,
                ProductoId = producto.Id,
                LoteId = loteId,
                Tipo = TiposMovimiento.Venta,
                Cantidad = -cantidadBase,
                CostoUnitario = producto.CostoPromedio,
                ReferenciaTipo = "VENTA",
                ReferenciaId = venta.Id,
                UsuarioId = usuarioId,
            });
        }

        foreach (var p in request.Pagos)
            venta.Pagos.Add(new Pago { VentaId = venta.Id, Metodo = p.Metodo, Monto = p.Monto, ReferenciaQr = p.ReferenciaQr });

        if (request.Receta is { } r)
        {
            db.Recetas.Add(new Receta
            {
                VentaId = venta.Id,
                MedicoNombre = r.MedicoNombre,
                MedicoMatricula = r.MedicoMatricula,
                PacienteNombre = r.PacienteNombre,
                PacienteCi = r.PacienteCi,
                FechaReceta = r.FechaReceta,
                ImagenUrl = r.ImagenUrl,
            });
        }

        venta.Subtotal = venta.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        venta.Descuento = request.Descuento + venta.Detalles.Sum(d => d.Descuento);
        venta.Total = venta.Subtotal - venta.Descuento;

        db.Ventas.Add(venta);

        if (request.ClienteId is { } clienteId)
        {
            var telefono = await db.Clientes
                .Where(c => c.Id == clienteId)
                .Select(c => c.TelefonoWhatsapp)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(telefono))
            {
                db.Notificaciones.Add(new Notificacion
                {
                    EmpresaId = empresaId,
                    Tipo = TiposNotificacion.FacturaCliente,
                    Destinatario = telefono,
                    Contenido = NotificacionTemplates.ReciboVenta(venta.Numero, venta.Total),
                    ReferenciaId = venta.Id,
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return VentaMapper.ToDto(venta);
    }

    private static async Task<bool> ObtenerPermiteStockNegativoAsync(
        IApplicationDbContext db, Guid empresaId, CancellationToken ct)
    {
        var valor = await db.EmpresaConfiguraciones
            .Where(c => c.EmpresaId == empresaId && c.Clave == "venta.permite_stock_negativo")
            .Select(c => c.Valor)
            .FirstOrDefaultAsync(ct);
        return valor is not null && bool.TryParse(valor, out var b) && b;
    }

    /// <summary>FEFO: lote más próximo a vencer cuyo stock alcance para toda la cantidad; sin partir la línea.</summary>
    private static async Task<Stock?> BuscarStockLoteFefoAsync(
        IApplicationDbContext db, Guid sucursalId, Guid productoId, decimal cantidadNecesaria, CancellationToken ct)
    {
        var candidatos = await (
            from s in db.Stocks
            join l in db.Lotes on s.LoteId equals l.Id
            where s.SucursalId == sucursalId && s.ProductoId == productoId
            orderby l.FechaVencimiento
            select s
        ).ToListAsync(ct);

        return candidatos.FirstOrDefault(s => s.Cantidad >= cantidadNecesaria) ?? candidatos.FirstOrDefault();
    }
}
