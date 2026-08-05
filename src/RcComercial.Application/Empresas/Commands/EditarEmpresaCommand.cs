using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Empresas.Commands;

public record EditarEmpresaCommand(string Nombre, string? Nit, string? TelefonoWhatsapp) : IRequest<EmpresaActualDto?>;

public class EditarEmpresaCommandValidator : AbstractValidator<EditarEmpresaCommand>
{
    public EditarEmpresaCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Nit).Must(ValidacionesFormato.NitValido).WithMessage("El NIT debe tener entre 5 y 15 dígitos.");
        RuleFor(x => x.TelefonoWhatsapp).Must(ValidacionesFormato.TelefonoWhatsappValido)
            .WithMessage("El WhatsApp debe tener el formato +591 seguido de 8 dígitos (ej. +59171234567).");
    }
}

public class EditarEmpresaCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<EditarEmpresaCommand, EmpresaActualDto?>
{
    public async Task<EmpresaActualDto?> Handle(EditarEmpresaCommand request, CancellationToken ct)
    {
        var empresa = await db.Empresas.Include(e => e.Rubro).FirstOrDefaultAsync(e => e.Id == currentUser.EmpresaId, ct);
        if (empresa is null) return null;

        empresa.Nombre = request.Nombre;
        empresa.Nit = request.Nit;
        empresa.TelefonoWhatsapp = request.TelefonoWhatsapp;
        await db.SaveChangesAsync(ct);

        return new EmpresaActualDto(
            empresa.Id, empresa.Nombre, empresa.Nit, empresa.TelefonoWhatsapp, empresa.RubroId, empresa.Rubro.Nombre,
            empresa.Rubro.UsaLotesPorDefecto, empresa.Rubro.UsaControlados,
            empresa.Rubro.UsaFichaFarmacia, empresa.Rubro.UsaDecimalesPorDefecto, empresa.Rubro.EsTipoServicio);
    }
}
