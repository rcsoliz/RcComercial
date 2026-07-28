using FluentValidation;
using MediatR;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Clientes.Commands;

public record CrearClienteCommand(
    string Nombre, string? NitCi, string TipoDocumento, string? TelefonoWhatsapp, string? Email)
    : IRequest<ClienteDto>;

public class CrearClienteCommandValidator : AbstractValidator<CrearClienteCommand>
{
    public CrearClienteCommandValidator()
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

public class CrearClienteCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearClienteCommand, ClienteDto>
{
    public async Task<ClienteDto> Handle(CrearClienteCommand request, CancellationToken ct)
    {
        var cliente = new Cliente
        {
            EmpresaId = currentUser.EmpresaId!.Value,
            Nombre = request.Nombre,
            NitCi = request.NitCi,
            TipoDocumento = request.TipoDocumento,
            TelefonoWhatsapp = request.TelefonoWhatsapp,
            Email = request.Email,
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(ct);
        return new ClienteDto(
            cliente.Id, cliente.Nombre, cliente.NitCi, cliente.TipoDocumento,
            cliente.TelefonoWhatsapp, cliente.Email, cliente.Activo);
    }
}
