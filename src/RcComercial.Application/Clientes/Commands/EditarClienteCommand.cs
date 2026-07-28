using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;

namespace RcComercial.Application.Clientes.Commands;

public record EditarClienteCommand(
    Guid Id, string Nombre, string? NitCi, string TipoDocumento, string? TelefonoWhatsapp, string? Email)
    : IRequest<ClienteDto?>;

public class EditarClienteCommandValidator : AbstractValidator<EditarClienteCommand>
{
    public EditarClienteCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TipoDocumento).Must(t => TiposDocumentoCliente.Todos.Contains(t))
            .WithMessage($"Tipo de documento inválido: debe ser {string.Join('/', TiposDocumentoCliente.Todos)}.");
        RuleFor(x => x.NitCi)
            .Must((cmd, nitCi) => ValidacionesFormato.NitCiValido(nitCi, cmd.TipoDocumento))
            .WithMessage("El NIT/CI no tiene un formato válido para el tipo de documento elegido.");
        RuleFor(x => x.TelefonoWhatsapp)
            .Must(ValidacionesFormato.TelefonoWhatsappValido)
            .WithMessage("El WhatsApp debe tener el formato +591 seguido de 8 dígitos (ej. +59171234567).");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class EditarClienteCommandHandler(IApplicationDbContext db)
    : IRequestHandler<EditarClienteCommand, ClienteDto?>
{
    public async Task<ClienteDto?> Handle(EditarClienteCommand request, CancellationToken ct)
    {
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (cliente is null) return null;

        cliente.Nombre = request.Nombre;
        cliente.NitCi = request.NitCi;
        cliente.TipoDocumento = request.TipoDocumento;
        cliente.TelefonoWhatsapp = request.TelefonoWhatsapp;
        cliente.Email = request.Email;
        await db.SaveChangesAsync(ct);

        return new ClienteDto(
            cliente.Id, cliente.Nombre, cliente.NitCi, cliente.TipoDocumento,
            cliente.TelefonoWhatsapp, cliente.Email, cliente.Activo);
    }
}
