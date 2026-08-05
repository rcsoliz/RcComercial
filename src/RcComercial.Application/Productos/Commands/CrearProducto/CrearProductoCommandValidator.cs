using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Productos.Commands.CrearProducto;

public class CrearProductoCommandValidator : AbstractValidator<CrearProductoCommand>
{
    public CrearProductoCommandValidator(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnidadBaseId).GreaterThan((short)0);
        RuleFor(x => x.PrecioBase).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockMinimo).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(c => !c.EsServicio || !c.ManejaLote)
            .WithMessage("Un servicio no puede manejar lote.");

        RuleForEach(x => x.Presentaciones).ChildRules(p =>
        {
            p.RuleFor(x => x.Nombre).NotEmpty();
            p.RuleFor(x => x.Factor).GreaterThan(0);
            p.RuleFor(x => x.Precio).GreaterThanOrEqualTo(0);
        });

        RuleFor(x => x.FichaFarmacia)
            .MustAsync(async (ficha, ct) =>
            {
                if (ficha is null) return true;
                return await db.Empresas
                    .Where(e => e.Id == currentUser.EmpresaId)
                    .Select(e => e.Rubro.UsaFichaFarmacia)
                    .FirstOrDefaultAsync(ct);
            })
            .WithMessage("El rubro de la empresa no admite ficha farmacéutica.");
    }
}
