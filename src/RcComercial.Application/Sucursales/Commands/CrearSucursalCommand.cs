using FluentValidation;
using MediatR;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Sucursales.Commands;

public record CrearSucursalCommand(string Nombre, string? Direccion) : IRequest<SucursalDto>;

public class CrearSucursalCommandValidator : AbstractValidator<CrearSucursalCommand>
{
    public CrearSucursalCommandValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
}

public class CrearSucursalCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearSucursalCommand, SucursalDto>
{
    public async Task<SucursalDto> Handle(CrearSucursalCommand request, CancellationToken ct)
    {
        var sucursal = new Sucursal
        {
            EmpresaId = currentUser.EmpresaId!.Value,
            Nombre = request.Nombre,
            Direccion = request.Direccion,
        };
        db.Sucursales.Add(sucursal);
        await db.SaveChangesAsync(ct);
        return new SucursalDto(sucursal.Id, sucursal.Nombre, sucursal.Direccion, sucursal.Activo);
    }
}
