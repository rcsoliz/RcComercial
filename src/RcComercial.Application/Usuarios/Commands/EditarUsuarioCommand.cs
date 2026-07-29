using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Usuarios.Commands;

public record EditarUsuarioCommand(
    Guid Id, string Nombre, string UsuarioLogin, Guid RolId, Guid? SucursalId, string? TelefonoWhatsapp)
    : IRequest<UsuarioDto?>;

public class EditarUsuarioCommandValidator : AbstractValidator<EditarUsuarioCommand>
{
    public EditarUsuarioCommandValidator(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UsuarioLogin).NotEmpty().MaximumLength(50)
            .MustAsync(async (cmd, login, ct) => !await db.Usuarios.IgnoreQueryFilters()
                .AnyAsync(u => u.EmpresaId == currentUser.EmpresaId && u.UsuarioLogin == login && u.Id != cmd.Id, ct))
            .WithMessage("Ya existe un usuario con ese nombre de usuario.");
        RuleFor(x => x.TelefonoWhatsapp).Must(ValidacionesFormato.TelefonoWhatsappValido)
            .WithMessage("El WhatsApp debe tener el formato +591 seguido de 8 dígitos (ej. +59171234567).");
        RuleFor(x => x.RolId)
            .MustAsync(async (rolId, ct) => await db.Roles.IgnoreQueryFilters()
                .AnyAsync(r => r.Id == rolId && r.Activo && (r.EmpresaId == null || r.EmpresaId == currentUser.EmpresaId), ct))
            .WithMessage("El rol elegido no existe o no pertenece a tu empresa.");
        RuleFor(x => x.SucursalId)
            .MustAsync(async (sucursalId, ct) => sucursalId is null || await db.Sucursales.AnyAsync(s => s.Id == sucursalId, ct))
            .WithMessage("La sucursal elegida no existe.");
    }
}

public class EditarUsuarioCommandHandler(IApplicationDbContext db)
    : IRequestHandler<EditarUsuarioCommand, UsuarioDto?>
{
    public async Task<UsuarioDto?> Handle(EditarUsuarioCommand request, CancellationToken ct)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == request.Id, ct);
        if (usuario is null) return null;

        // El rol determina los permisos del JWT: si cambia, los tokens ya
        // emitidos deben dejar de servir en minutos (ver OnTokenValidated en
        // Program.cs, que compara esta versión contra el claim del token).
        if (usuario.RolId != request.RolId) usuario.PermisosVersion++;

        usuario.Nombre = request.Nombre;
        usuario.UsuarioLogin = request.UsuarioLogin;
        usuario.RolId = request.RolId;
        usuario.SucursalId = request.SucursalId;
        usuario.TelefonoWhatsapp = request.TelefonoWhatsapp;
        await db.SaveChangesAsync(ct);

        return await UsuarioMapper.ObtenerDtoAsync(db, usuario.Id, ct);
    }
}
