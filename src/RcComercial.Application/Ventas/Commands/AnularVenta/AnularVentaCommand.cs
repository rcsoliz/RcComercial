using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
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

        foreach (var detalle in venta.Detalles)
        {
            var stock = await db.Stocks.FirstOrDefaultAsync(
                s => s.SucursalId == venta.SucursalId && s.ProductoId == detalle.ProductoId
                    && s.LoteId == detalle.LoteId, ct);
            if (stock is null)
            {
                stock = new Stock
                {
                    SucursalId = venta.SucursalId, ProductoId = detalle.ProductoId, LoteId = detalle.LoteId,
                };
                db.Stocks.Add(stock);
            }
            stock.Cantidad += detalle.CantidadBase;
            stock.ActualizadoEn = DateTimeOffset.UtcNow;

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
        venta.MotivoAnulacion = request.Motivo;
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
                Contenido = $"Venta N° {venta.Numero} anulada. Motivo: {request.Motivo}",
                ReferenciaId = venta.Id,
            });
        }

        await db.SaveChangesAsync(ct);

        return VentaMapper.ToDto(venta);
    }
}
