using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Usuarios;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Plataforma.Commands;

/// <summary>
/// Alta completa de un tenant nuevo: empresa + sucursal inicial + usuario
/// Dueño con rol de sistema, en una sola operación. Reusa el mismo patrón de
/// contraseña temporal + debe_cambiar_password que CrearUsuarioCommand
/// (Sesión 7B) — no se llama a ese command porque deriva EmpresaId del
/// currentUser, que acá sería el de la empresa del superadmin, no el del
/// tenant nuevo que se está creando.
/// </summary>
public record CrearEmpresaPlataformaCommand(
    string NombreEmpresa, string? Nit, short RubroId, string? TelefonoWhatsapp,
    string NombreSucursal,
    string NombreDueno, string UsuarioLoginDueno, string? TelefonoWhatsappDueno)
    : IRequest<AltaEmpresaResultDto>;

public class CrearEmpresaPlataformaCommandValidator : AbstractValidator<CrearEmpresaPlataformaCommand>
{
    public CrearEmpresaPlataformaCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.NombreEmpresa).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Nit).Must(ValidacionesFormato.NitValido).WithMessage("El NIT debe tener entre 5 y 15 dígitos.");
        RuleFor(x => x.TelefonoWhatsapp).Must(ValidacionesFormato.TelefonoWhatsappValido)
            .WithMessage("El WhatsApp debe tener el formato +591 seguido de 8 dígitos (ej. +59171234567).");
        RuleFor(x => x.RubroId)
            .MustAsync(async (id, ct) => await db.Rubros.AnyAsync(r => r.Id == id && r.Activo, ct))
            .WithMessage("El rubro elegido no existe.");
        RuleFor(x => x.NombreSucursal).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NombreDueno).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UsuarioLoginDueno).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TelefonoWhatsappDueno).Must(ValidacionesFormato.TelefonoWhatsappValido)
            .WithMessage("El WhatsApp debe tener el formato +591 seguido de 8 dígitos (ej. +59171234567).");
    }
}

public class CrearEmpresaPlataformaCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    : IRequestHandler<CrearEmpresaPlataformaCommand, AltaEmpresaResultDto>
{
    public async Task<AltaEmpresaResultDto> Handle(CrearEmpresaPlataformaCommand request, CancellationToken ct)
    {
        var empresa = new Empresa
        {
            Nombre = request.NombreEmpresa,
            Nit = request.Nit,
            RubroId = request.RubroId,
            TelefonoWhatsapp = request.TelefonoWhatsapp,
        };
        var sucursal = new Sucursal { EmpresaId = empresa.Id, Nombre = request.NombreSucursal };

        var passwordTemporal = GeneradorPassword.Temporal();
        var dueno = new Usuario
        {
            EmpresaId = empresa.Id,
            SucursalId = null, // el Dueño ve todas las sucursales de su empresa
            Nombre = request.NombreDueno,
            UsuarioLogin = request.UsuarioLoginDueno,
            PasswordHash = hasher.Hash(passwordTemporal),
            RolId = RolesSistema.Dueno,
            TelefonoWhatsapp = request.TelefonoWhatsappDueno,
            DebeCambiarPassword = true,
        };

        db.Empresas.Add(empresa);
        db.Sucursales.Add(sucursal);
        db.Usuarios.Add(dueno);
        await db.SaveChangesAsync(ct);

        // Se arma a mano (no vía UsuarioMapper): ese helper consulta db.Usuarios,
        // que sigue filtrado por la empresa del SUPERADMIN — jamás encontraría
        // al dueño del tenant recién creado. Acá ya tenemos todo en memoria.
        var duenoDto = new UsuarioDto(
            dueno.Id, dueno.Nombre, dueno.UsuarioLogin, dueno.RolId, "Dueño",
            dueno.SucursalId, null, dueno.TelefonoWhatsapp, dueno.Activo, dueno.UltimoLogin, dueno.DebeCambiarPassword);

        return new AltaEmpresaResultDto(empresa.Id, sucursal.Id, duenoDto, passwordTemporal);
    }
}
