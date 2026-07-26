using FluentValidation;

namespace RcComercial.Application.Productos.Commands.CambiarPrecio;

public class CambiarPrecioCommandValidator : AbstractValidator<CambiarPrecioCommand>
{
    public CambiarPrecioCommandValidator()
    {
        RuleFor(x => x.NuevoPrecio).GreaterThanOrEqualTo(0);
    }
}
