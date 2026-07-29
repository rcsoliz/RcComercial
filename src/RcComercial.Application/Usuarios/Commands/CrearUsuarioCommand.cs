using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Usuarios.Commands;

public record CrearUsuarioCommand(
    string Nombre, string UsuarioLogin, Guid RolId, Guid? SucursalId, string? TelefonoWhatsapp)
    : IRequest<UsuarioConPasswordTemporalDto>;

public class CrearUsuarioCommandValidator : AbstractValidator<CrearUsuarioCommand>
{
    public CrearUsuarioCommandValidator(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UsuarioLogin).NotEmpty().MaximumLength(50)
            .MustAsync(async (login, ct) => !await db.Usuarios.IgnoreQueryFilters()
                .AnyAsync(u => u.EmpresaId == currentUser.EmpresaId && u.UsuarioLogin == login, ct))
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

public class CrearUsuarioCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IPasswordHasher hasher)
    : IRequestHandler<CrearUsuarioCommand, UsuarioConPasswordTemporalDto>
{
    public async Task<UsuarioConPasswordTemporalDto> Handle(CrearUsuarioCommand request, CancellationToken ct)
    {
        var passwordTemporal = GeneradorPassword.Temporal();

        var usuario = new Usuario
        {
            EmpresaId = currentUser.EmpresaId!.Value,
            Nombre = request.Nombre,
            UsuarioLogin = request.UsuarioLogin,
            PasswordHash = hasher.Hash(passwordTemporal),
            RolId = request.RolId,
            SucursalId = request.SucursalId,
            TelefonoWhatsapp = request.TelefonoWhatsapp,
            DebeCambiarPassword = true,
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);

        var dto = await UsuarioMapper.ObtenerDtoAsync(db, usuario.Id, ct);
        return new UsuarioConPasswordTemporalDto(dto!, passwordTemporal);
    }
}
