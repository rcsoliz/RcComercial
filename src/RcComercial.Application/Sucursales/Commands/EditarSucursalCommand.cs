using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Sucursales.Commands;

public record EditarSucursalCommand(Guid Id, string Nombre, string? Direccion) : IRequest<SucursalDto?>;

public class EditarSucursalCommandValidator : AbstractValidator<EditarSucursalCommand>
{
    public EditarSucursalCommandValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
}

public class EditarSucursalCommandHandler(IApplicationDbContext db)
    : IRequestHandler<EditarSucursalCommand, SucursalDto?>
{
    public async Task<SucursalDto?> Handle(EditarSucursalCommand request, CancellationToken ct)
    {
        var sucursal = await db.Sucursales.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (sucursal is null) return null;

        sucursal.Nombre = request.Nombre;
        sucursal.Direccion = request.Direccion;
        await db.SaveChangesAsync(ct);
        return new SucursalDto(sucursal.Id, sucursal.Nombre, sucursal.Direccion, sucursal.Activo);
    }
}
