using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Roles.Commands;

public record EditarRolCommand(Guid Id, string Nombre, List<short> PermisoIds) : IRequest<RolDto?>;

public class EditarRolCommandValidator : AbstractValidator<EditarRolCommand>
{
    public EditarRolCommandValidator(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(50)
            .MustAsync(async (cmd, nombre, ct) => !await db.Roles.IgnoreQueryFilters()
                .AnyAsync(r => r.EmpresaId == currentUser.EmpresaId && r.Nombre == nombre && r.Id != cmd.Id, ct))
            .WithMessage("Ya existe un rol con ese nombre.");
        RuleFor(x => x.PermisoIds)
            .MustAsync(async (ids, ct) => ids.Count == 0 || await db.Permisos.CountAsync(p => ids.Contains(p.Id), ct) == ids.Distinct().Count())
            .WithMessage("Uno o más permisos elegidos no existen.");
    }
}

public class EditarRolCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<EditarRolCommand, RolDto?>
{
    public async Task<RolDto?> Handle(EditarRolCommand request, CancellationToken ct)
    {
        // Rol no implementa ITenantEntity (los de sistema son EmpresaId null,
        // compartidos): sin este filtro explícito, cualquier empresa podría
        // editar el rol propio de OTRA con solo adivinarle el Guid.
        var rol = await db.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.Id && (r.EsSistema || r.EmpresaId == currentUser.EmpresaId), ct);
        if (rol is null) return null;
        // Los de sistema (EmpresaId null) son de solo lectura: ni siquiera el
        // dueño los edita, para que "Vendedor"/"Dueño"/"Encargado" signifiquen
        // siempre lo mismo en cualquier empresa.
        if (rol.EsSistema) throw new ValidationException("Los roles de sistema no se pueden editar.");

        rol.Nombre = request.Nombre;

        var actuales = await db.RolPermisos.Where(rp => rp.RolId == rol.Id).ToListAsync(ct);
        var nuevos = request.PermisoIds.Distinct().ToHashSet();

        foreach (var actual in actuales.Where(a => !nuevos.Contains(a.PermisoId)))
            db.RolPermisos.Remove(actual);

        var existentes = actuales.Select(a => a.PermisoId).ToHashSet();
        foreach (var permisoId in nuevos.Where(id => !existentes.Contains(id)))
            db.RolPermisos.Add(new RolPermiso { RolId = rol.Id, PermisoId = permisoId });

        await db.SaveChangesAsync(ct);
        return new RolDto(rol.Id, rol.Nombre, rol.EsSistema, rol.Activo, nuevos.ToList());
    }
}
