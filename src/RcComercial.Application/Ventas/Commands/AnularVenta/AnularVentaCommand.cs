using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Notificaciones;
using RcComercial.Application.Ventas.Dtos;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Ventas.Commands.AnularVenta;

public record AnularVentaCommand(Guid Id, string Motivo) : IRequest<VentaDto?>;

public class AnularVentaCommandValidator : AbstractValidator<AnularVentaCommand>
{
    public AnularVentaCommandValidator() => RuleFor(x => x.Motivo).NotEmpty().MaximumLength(300);
}

public class AnularVentaCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<AnularVentaCommand, VentaDto?>
{
    public async Task<VentaDto?> Handle(AnularVentaCommand request, CancellationToken ct)
    {
        var venta = await db.Ventas
            .Include(v => v.Detalles)
            .Include(v => v.Pagos)
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);
        if (venta is null) return null;

        if (venta.Estado == EstadosVenta.Anulada) return VentaMapper.ToDto(venta); // idempotente

        // AjustarStockAsync es SQL crudo que se ejecuta de inmediato: sin esta
        // transacción explícita, la reversión de stock podría quedar
        // confirmada aunque el resto de la anulación falle después.
        return await db.EjecutarEnTransaccionAsync(async ct => await AnularAsync(venta, request.Motivo, ct), ct);
    }

    private async Task<VentaDto> AnularAsync(Venta venta, string motivo, CancellationToken ct)
    {
        foreach (var detalle in venta.Detalles)
        {
            var stock = await db.Stocks.FirstOrDefaultAsync(
                s => s.SucursalId == venta.SucursalId && s.ProductoId == detalle.ProductoId
                    && s.LoteId == detalle.LoteId, ct);
            if (stock is null)
                db.Stocks.Add(new Stock
                {
                    SucursalId = venta.SucursalId, ProductoId = detalle.ProductoId, LoteId = detalle.LoteId,
                    Cantidad = detalle.CantidadBase,
                });
            else
                await db.AjustarStockAsync(stock.Id, detalle.CantidadBase, permiteNegativo: true, ct);

            db.MovimientosInventario.Add(new MovimientoInventario
            {
                EmpresaId = venta.EmpresaId,
                SucursalId = venta.SucursalId,
                ProductoId = detalle.ProductoId,
                LoteId = detalle.LoteId,
                Tipo = TiposMovimiento.Devolucion,
                Cantidad = detalle.CantidadBase,
                ReferenciaTipo = "VENTA",
                ReferenciaId = venta.Id,
                UsuarioId = currentUser.UsuarioId!.Value,
            });
        }

        venta.Estado = EstadosVenta.Anulada;
        venta.MotivoAnulacion = motivo;
        venta.AnuladaPor = currentUser.UsuarioId;

        var telefono = await db.Empresas
            .Where(e => e.Id == venta.EmpresaId)
            .Select(e => e.TelefonoWhatsapp)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(telefono))
        {
            db.Notificaciones.Add(new Notificacion
            {
                EmpresaId = venta.EmpresaId,
                Tipo = TiposNotificacion.Anulacion,
                Destinatario = telefono,
                Contenido = NotificacionTemplates.Anulacion(venta.Numero, motivo),
                ReferenciaId = venta.Id,
            });
        }

        await db.SaveChangesAsync(ct);

        return VentaMapper.ToDto(venta);
    }
}
