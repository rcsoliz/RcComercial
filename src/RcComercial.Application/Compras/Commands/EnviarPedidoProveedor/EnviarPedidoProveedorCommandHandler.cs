using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Notificaciones;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Compras.Commands.EnviarPedidoProveedor;

public class EnviarPedidoProveedorCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<EnviarPedidoProveedorCommand, bool>
{
    public async Task<bool> Handle(EnviarPedidoProveedorCommand request, CancellationToken ct)
    {
        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Id == request.ProveedorId, ct);
        if (proveedor is null || string.IsNullOrWhiteSpace(proveedor.TelefonoWhatsapp)) return false;

        var productoIds = request.Detalles.Select(d => d.ProductoId).ToList();
        var nombresPorProducto = await db.Productos
            .Where(p => productoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        var items = request.Detalles
            .Select(d => (nombresPorProducto.GetValueOrDefault(d.ProductoId, d.ProductoId.ToString()), d.Cantidad))
            .ToList();

        db.Notificaciones.Add(new Notificacion
        {
            EmpresaId = currentUser.EmpresaId!.Value,
            Tipo = TiposNotificacion.PedidoProveedor,
            Destinatario = proveedor.TelefonoWhatsapp,
            Contenido = NotificacionTemplates.PedidoProveedor(items),
            ReferenciaId = proveedor.Id,
        });

        await db.SaveChangesAsync(ct);
        return true;
    }
}
