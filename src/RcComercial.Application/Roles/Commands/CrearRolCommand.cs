using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Roles.Commands;

public record CrearRolCommand(string Nombre, List<short> PermisoIds) : IRequest<RolDto>;

public class CrearRolCommandValidator : AbstractValidator<CrearRolCommand>
{
    public CrearRolCommandValidator(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(50)
            .MustAsync(async (nombre, ct) => !await db.Roles.IgnoreQueryFilters()
                .AnyAsync(r => r.EmpresaId == currentUser.EmpresaId && r.Nombre == nombre, ct))
            .WithMessage("Ya existe un rol con ese nombre.");
        RuleFor(x => x.PermisoIds)
            .MustAsync(async (ids, ct) => ids.Count == 0 || await db.Permisos.CountAsync(p => ids.Contains(p.Id), ct) == ids.Distinct().Count())
            .WithMessage("Uno o más permisos elegidos no existen.");
    }
}

public class CrearRolCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearRolCommand, RolDto>
{
    public async Task<RolDto> Handle(CrearRolCommand request, CancellationToken ct)
    {
        var rol = new Rol { EmpresaId = currentUser.EmpresaId!.Value, Nombre = request.Nombre, EsSistema = false };
        db.Roles.Add(rol);

        foreach (var permisoId in request.PermisoIds.Distinct())
            db.RolPermisos.Add(new RolPermiso { RolId = rol.Id, PermisoId = permisoId });

        await db.SaveChangesAsync(ct);
        return new RolDto(rol.Id, rol.Nombre, rol.EsSistema, rol.Activo, request.PermisoIds.Distinct().ToList());
    }
}
