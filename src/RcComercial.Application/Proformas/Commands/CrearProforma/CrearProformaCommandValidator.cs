using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Proformas.Commands.CrearProforma;

public class CrearProformaCommandValidator : AbstractValidator<CrearProformaCommand>
{
    public CrearProformaCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.Detalles).NotEmpty().WithMessage("La proforma necesita al menos una línea.");
        RuleForEach(x => x.Detalles).ChildRules(d =>
        {
            d.RuleFor(x => x.Cantidad).GreaterThan(0);
            d.RuleFor(x => x.PrecioUnitario).GreaterThanOrEqualTo(0);
            d.RuleFor(x => x.Descuento).GreaterThanOrEqualTo(0);
        });

        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                if (cmd.VehiculoId is not { } vehiculoId || cmd.ClienteId is not { } clienteId) return true;
                return await db.Vehiculos.AnyAsync(v => v.Id == vehiculoId && v.ClienteId == clienteId, ct);
            })
            .WithMessage("El vehículo indicado no pertenece al cliente indicado.");
    }
}
