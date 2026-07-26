using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;

namespace RcComercial.Application.Ventas.Commands.CrearVenta;

public class CrearVentaCommandValidator : AbstractValidator<CrearVentaCommand>
{
    private const decimal ToleranciaPagos = 0.01m;

    public CrearVentaCommandValidator(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Detalles).NotEmpty().WithMessage("La venta debe tener al menos un detalle.");
        RuleFor(x => x.Pagos).NotEmpty().WithMessage("La venta debe tener al menos un pago.");
        RuleFor(x => x.Descuento).GreaterThanOrEqualTo(0);

        RuleForEach(x => x.Detalles).ChildRules(d =>
        {
            d.RuleFor(x => x.Cantidad).GreaterThan(0);
            d.RuleFor(x => x.PrecioUnitario).GreaterThanOrEqualTo(0);
            d.RuleFor(x => x.Descuento).GreaterThanOrEqualTo(0);
        });

        RuleForEach(x => x.Pagos).ChildRules(p => p.RuleFor(x => x.Monto).GreaterThan(0));

        // Sesión de caja abierta del usuario: precondición para vender.
        RuleFor(x => x)
            .MustAsync(async (_, ct) =>
            {
                var usuarioId = currentUser.UsuarioId!.Value;
                return await db.SesionesCaja.AnyAsync(s => s.UsuarioId == usuarioId && s.Estado == "ABIERTA", ct);
            })
            .WithMessage("No hay una sesión de caja abierta para este usuario.");

        // Descuento requiere permiso explícito.
        RuleFor(x => x)
            .Must(cmd => (cmd.Descuento <= 0 && cmd.Detalles.All(d => d.Descuento <= 0))
                || currentUser.TienePermiso(Permisos.VentasDescuento))
            .WithMessage("No tiene permiso para aplicar descuentos.");

        // Producto controlado exige receta (criterio: rechazar con 422 si falta).
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                if (cmd.Receta is not null) return true;
                var productoIds = cmd.Detalles.Select(d => d.ProductoId).Distinct().ToList();
                var hayControlado = await db.Productos
                    .AnyAsync(p => productoIds.Contains(p.Id) && p.EsControlado, ct);
                return !hayControlado;
            })
            .WithMessage("Uno o más productos requieren receta (es_controlado).");

        // Los pagos deben cubrir el total calculado desde las líneas.
        RuleFor(x => x)
            .Must(cmd =>
            {
                var subtotal = cmd.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
                var descuentoTotal = cmd.Descuento + cmd.Detalles.Sum(d => d.Descuento);
                var total = subtotal - descuentoTotal;
                var pagado = cmd.Pagos.Sum(p => p.Monto);
                return Math.Abs(pagado - total) <= ToleranciaPagos;
            })
            .WithMessage("La suma de los pagos no coincide con el total de la venta.");
    }
}
