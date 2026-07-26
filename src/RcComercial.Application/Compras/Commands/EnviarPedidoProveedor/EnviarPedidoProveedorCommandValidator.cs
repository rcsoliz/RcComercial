using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Compras.Commands.EnviarPedidoProveedor;

public class EnviarPedidoProveedorCommandValidator : AbstractValidator<EnviarPedidoProveedorCommand>
{
    public EnviarPedidoProveedorCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.Detalles).NotEmpty();
        RuleForEach(x => x.Detalles).ChildRules(d => d.RuleFor(x => x.Cantidad).GreaterThan(0));

        RuleFor(x => x.ProveedorId)
            .MustAsync((proveedorId, ct) => db.Proveedores
                .AnyAsync(p => p.Id == proveedorId && p.Activo && p.TelefonoWhatsapp != null, ct))
            .WithMessage("El proveedor no existe o no tiene WhatsApp configurado.");
    }
}
