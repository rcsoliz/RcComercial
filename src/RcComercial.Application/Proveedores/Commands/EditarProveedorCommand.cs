using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Proveedores.Commands;

public record EditarProveedorCommand(
    Guid Id, string Nombre, string? Nit, string? TelefonoWhatsapp, int DiasCredito, int LeadTimeDias)
    : IRequest<ProveedorDto?>;

public class EditarProveedorCommandValidator : AbstractValidator<EditarProveedorCommand>
{
    public EditarProveedorCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Nit).Must(ValidacionesFormato.NitValido)
            .WithMessage("El NIT debe tener entre 5 y 15 dígitos.");
        RuleFor(x => x.TelefonoWhatsapp).Must(ValidacionesFormato.TelefonoWhatsappValido)
            .WithMessage("El WhatsApp debe tener el formato +591 seguido de 8 dígitos (ej. +59171234567).");
        RuleFor(x => x.DiasCredito).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LeadTimeDias).GreaterThanOrEqualTo(0);
    }
}

public class EditarProveedorCommandHandler(IApplicationDbContext db)
    : IRequestHandler<EditarProveedorCommand, ProveedorDto?>
{
    public async Task<ProveedorDto?> Handle(EditarProveedorCommand request, CancellationToken ct)
    {
        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (proveedor is null) return null;

        proveedor.Nombre = request.Nombre;
        proveedor.Nit = request.Nit;
        proveedor.TelefonoWhatsapp = request.TelefonoWhatsapp;
        proveedor.DiasCredito = request.DiasCredito;
        proveedor.LeadTimeDias = request.LeadTimeDias;
        await db.SaveChangesAsync(ct);

        return new ProveedorDto(
            proveedor.Id, proveedor.Nombre, proveedor.Nit, proveedor.TelefonoWhatsapp,
            proveedor.DiasCredito, proveedor.LeadTimeDias, proveedor.Activo);
    }
}
