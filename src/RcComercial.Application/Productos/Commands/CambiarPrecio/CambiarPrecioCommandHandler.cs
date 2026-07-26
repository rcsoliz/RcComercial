using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Productos.Commands.CambiarPrecio;

public class CambiarPrecioCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CambiarPrecioCommand, bool>
{
    public async Task<bool> Handle(CambiarPrecioCommand request, CancellationToken ct)
    {
        // Producto SIEMPRE se resuelve primero (tiene ITenantEntity: el filtro
        // global ya garantiza que es de la empresa actual). ProductoPresentacion
        // NO tiene ITenantEntity, así que sin este paso se podía tocar el precio
        // de una presentación de OTRA empresa con solo conocer su GUID.
        var producto = await db.Productos.FirstOrDefaultAsync(p => p.Id == request.ProductoId, ct);
        if (producto is null) return false;

        decimal precioAnterior;

        if (request.PresentacionId is { } presentacionId)
        {
            var presentacion = await db.ProductoPresentaciones
                .FirstOrDefaultAsync(p => p.Id == presentacionId && p.ProductoId == producto.Id, ct);
            if (presentacion is null) return false;

            precioAnterior = presentacion.Precio;
            presentacion.Precio = request.NuevoPrecio;
        }
        else
        {
            precioAnterior = producto.PrecioBase;
            producto.PrecioBase = request.NuevoPrecio;
        }

        db.PreciosHistorial.Add(new PrecioHistorial
        {
            ProductoId = request.ProductoId,
            PresentacionId = request.PresentacionId,
            PrecioAnterior = precioAnterior,
            PrecioNuevo = request.NuevoPrecio,
            UsuarioId = currentUser.UsuarioId!.Value,
        });

        await db.SaveChangesAsync(ct);
        return true;
    }
}
