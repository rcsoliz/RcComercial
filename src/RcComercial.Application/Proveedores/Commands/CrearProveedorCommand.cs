using FluentValidation;
using MediatR;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Proveedores.Commands;

public record CrearProveedorCommand(
    string Nombre, string? Nit, string? TelefonoWhatsapp, int DiasCredito, int LeadTimeDias)
    : IRequest<ProveedorDto>;

public class CrearProveedorCommandValidator : AbstractValidator<CrearProveedorCommand>
{
    public CrearProveedorCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DiasCredito).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LeadTimeDias).GreaterThanOrEqualTo(0);
    }
}

public class CrearProveedorCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearProveedorCommand, ProveedorDto>
{
    public async Task<ProveedorDto> Handle(CrearProveedorCommand request, CancellationToken ct)
    {
        var proveedor = new Proveedor
        {
            EmpresaId = currentUser.EmpresaId!.Value,
            Nombre = request.Nombre,
            Nit = request.Nit,
            TelefonoWhatsapp = request.TelefonoWhatsapp,
            DiasCredito = request.DiasCredito,
            LeadTimeDias = request.LeadTimeDias,
        };
        db.Proveedores.Add(proveedor);
        await db.SaveChangesAsync(ct);
        return new ProveedorDto(
            proveedor.Id, proveedor.Nombre, proveedor.Nit, proveedor.TelefonoWhatsapp,
            proveedor.DiasCredito, proveedor.LeadTimeDias, proveedor.Activo);
    }
}
