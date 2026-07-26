using FluentValidation;
using MediatR;
using RcComercial.Application.Panel.Dtos;

namespace RcComercial.Application.Panel.Queries.ObtenerPanelHistorico;

public record ObtenerPanelHistoricoQuery(DateOnly Desde, DateOnly Hasta) : IRequest<List<VentasPorDiaDto>>;

public class ObtenerPanelHistoricoQueryValidator : AbstractValidator<ObtenerPanelHistoricoQuery>
{
    public ObtenerPanelHistoricoQueryValidator()
    {
        RuleFor(x => x.Hasta).GreaterThanOrEqualTo(x => x.Desde);
        RuleFor(x => x)
            .Must(x => x.Hasta.DayNumber - x.Desde.DayNumber <= 366)
            .WithMessage("El rango máximo es de 366 días.");
    }
}
