using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Devoluciones.Commands;

public class CrearDevolucionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearDevolucionCommand, DevolucionDto>
{
    public async Task<DevolucionDto> Handle(CrearDevolucionCommand request, CancellationToken ct)
    {
        var empresaId = currentUser.EmpresaId!.Value;
        var usuarioId = currentUser.UsuarioId!.Value;

        var venta = await db.Ventas.FirstOrDefaultAsync(v => v.Id == request.VentaId, ct)
            ?? throw new ValidationException("La venta no existe.");

        var ventaDetalleIds = request.Detalles.Select(d => d.VentaDetalleId).ToList();
        var ventaDetalles = await db.VentaDetalles
            .Where(vd => ventaDetalleIds.Contains(vd.Id) && vd.VentaId == request.VentaId)
            .ToDictionaryAsync(vd => vd.Id, ct);

        var numero = await db.SiguienteNumeroAsync(empresaId, venta.SucursalId, "DEVOLUCION", ct);

        var devolucion = new Devolucion
        {
            EmpresaId = empresaId,
            SucursalId = venta.SucursalId,
            VentaId = venta.Id,
            Numero = numero.ToString("00000000"),
            Motivo = request.Motivo,
            UsuarioId = usuarioId,
        };

        decimal total = 0;
        foreach (var d in request.Detalles)
        {
            if (!ventaDetalles.TryGetValue(d.VentaDetalleId, out var ventaDetalle))
                throw new ValidationException("Uno de los detalles no pertenece a la venta indicada.");

            var precioUnitarioBase = ventaDetalle.Total / ventaDetalle.CantidadBase;
            var monto = d.CantidadBase * precioUnitarioBase;
            total += monto;

            devolucion.Detalles.Add(new DevolucionDetalle
            {
                DevolucionId = devolucion.Id,
                VentaDetalleId = d.VentaDetalleId,
                CantidadBase = d.CantidadBase,
                ReingresaStock = d.ReingresaStock,
                Monto = monto,
            });

            if (d.ReingresaStock)
            {
                var stock = await db.Stocks.FirstOrDefaultAsync(
                    s => s.SucursalId == venta.SucursalId && s.ProductoId == ventaDetalle.ProductoId
                        && s.LoteId == ventaDetalle.LoteId, ct);
                if (stock is null)
                {
                    stock = new Stock
                    {
                        SucursalId = venta.SucursalId, ProductoId = ventaDetalle.ProductoId, LoteId = ventaDetalle.LoteId,
                    };
                    db.Stocks.Add(stock);
                }
                stock.Cantidad += d.CantidadBase;
                stock.ActualizadoEn = DateTimeOffset.UtcNow;

                db.MovimientosInventario.Add(new MovimientoInventario
                {
                    EmpresaId = empresaId,
                    SucursalId = venta.SucursalId,
                    ProductoId = ventaDetalle.ProductoId,
                    LoteId = ventaDetalle.LoteId,
                    Tipo = TiposMovimiento.Devolucion,
                    Cantidad = d.CantidadBase,
                    ReferenciaTipo = "DEVOLUCION",
                    ReferenciaId = devolucion.Id,
                    UsuarioId = usuarioId,
                });
            }
        }

        devolucion.Total = total;
        db.Devoluciones.Add(devolucion);
        await db.SaveChangesAsync(ct);

        return new DevolucionDto(devolucion.Id, devolucion.Numero, devolucion.Fecha, devolucion.Motivo, devolucion.Total,
            devolucion.Detalles.Select(x =>
                new DevolucionDetalleDto(x.Id, x.VentaDetalleId, x.CantidadBase, x.ReingresaStock, x.Monto)).ToList());
    }
}
