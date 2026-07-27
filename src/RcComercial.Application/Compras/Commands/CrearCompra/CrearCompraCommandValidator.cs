using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Compras.Commands.CrearCompra;

public class CrearCompraCommandValidator : AbstractValidator<CrearCompraCommand>
{
    public CrearCompraCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.Detalles).NotEmpty().WithMessage("La compra debe tener al menos un detalle.");

        RuleForEach(x => x.Detalles).ChildRules(d =>
        {
            d.RuleFor(x => x.Cantidad).GreaterThan(0);
            d.RuleFor(x => x.CostoUnitario).GreaterThanOrEqualTo(0);
        });

        RuleFor(x => x.ProveedorId)
            .MustAsync((proveedorId, ct) => db.Proveedores.AnyAsync(p => p.Id == proveedorId && p.Activo, ct))
            .WithMessage("El proveedor no existe.");

        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                var productoIds = cmd.Detalles.Select(d => d.ProductoId).Distinct().ToList();
                var idsConLote = await db.Productos
                    .Where(p => productoIds.Contains(p.Id) && p.ManejaLote)
                    .Select(p => p.Id)
                    .ToListAsync(ct);

                return cmd.Detalles
                    .Where(d => idsConLote.Contains(d.ProductoId))
                    .All(d => !string.IsNullOrWhiteSpace(d.NumeroLote) && d.FechaVencimiento is not null);
            })
            .WithMessage("Los productos que manejan lote requieren número de lote y fecha de vencimiento.");
    }
}
