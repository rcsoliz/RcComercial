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
    }
}
