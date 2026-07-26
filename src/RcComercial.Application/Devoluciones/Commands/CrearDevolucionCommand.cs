using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Devoluciones.Commands;

public record CrearDevolucionDetalleCommand(Guid VentaDetalleId, decimal CantidadBase, bool ReingresaStock);

public record CrearDevolucionCommand(
    Guid VentaId, string Motivo, List<CrearDevolucionDetalleCommand> Detalles) : IRequest<DevolucionDto>;

public class CrearDevolucionCommandValidator : AbstractValidator<CrearDevolucionCommand>
{
    public CrearDevolucionCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Detalles).NotEmpty();
        RuleForEach(x => x.Detalles).ChildRules(d => d.RuleFor(x => x.CantidadBase).GreaterThan(0));

        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                var ventaDetalleIds = cmd.Detalles.Select(d => d.VentaDetalleId).Distinct().ToList();
                var ventaDetalles = await db.VentaDetalles
                    .Where(vd => ventaDetalleIds.Contains(vd.Id) && vd.VentaId == cmd.VentaId)
                    .ToDictionaryAsync(vd => vd.Id, ct);

                if (ventaDetalles.Count != ventaDetalleIds.Count) return false;

                foreach (var d in cmd.Detalles)
                {
                    var yaDevuelto = await db.DevolucionDetalles
                        .Where(dd => dd.VentaDetalleId == d.VentaDetalleId)
                        .SumAsync(dd => dd.CantidadBase, ct);
                    if (d.CantidadBase > ventaDetalles[d.VentaDetalleId].CantidadBase - yaDevuelto) return false;
                }
                return true;
            })
            .WithMessage("Alguna línea no pertenece a la venta o la cantidad a devolver excede lo disponible.");
    }
}
