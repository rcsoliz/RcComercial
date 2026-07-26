using FluentValidation;

namespace RcComercial.Application.Productos.Commands.ActualizarProducto;

public class ActualizarProductoCommandValidator : AbstractValidator<ActualizarProductoCommand>
{
    public ActualizarProductoCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnidadBaseId).GreaterThan((short)0);
        RuleFor(x => x.StockMinimo).GreaterThanOrEqualTo(0);

        RuleForEach(x => x.Presentaciones).ChildRules(p =>
        {
            p.RuleFor(x => x.Nombre).NotEmpty();
            p.RuleFor(x => x.Factor).GreaterThan(0);
            p.RuleFor(x => x.Precio).GreaterThanOrEqualTo(0);
        });
    }
}
