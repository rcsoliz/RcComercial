using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Caja.Commands;

public record AbrirCajaCommand(decimal MontoInicial, Guid? SucursalId) : IRequest<SesionCajaDto>;

public class AbrirCajaCommandValidator : AbstractValidator<AbrirCajaCommand>
{
    public AbrirCajaCommandValidator(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        RuleFor(x => x.MontoInicial).GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
                await SucursalResolver.ResolverAsync(db, currentUser, cmd.SucursalId, ct) is not null)
            .WithMessage("No se pudo determinar la sucursal: especifique 'sucursalId'.");

        RuleFor(x => x)
            .MustAsync(async (_, ct) =>
            {
                var usuarioId = currentUser.UsuarioId!.Value;
                var hayAbierta = await db.SesionesCaja
                    .AnyAsync(s => s.UsuarioId == usuarioId && s.Estado == "ABIERTA", ct);
                return !hayAbierta;
            })
            .WithMessage("Ya existe una sesión de caja abierta para este usuario.");
    }
}

public class AbrirCajaCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<AbrirCajaCommand, SesionCajaDto>
{
    public async Task<SesionCajaDto> Handle(AbrirCajaCommand request, CancellationToken ct)
    {
        var sucursalId = (await SucursalResolver.ResolverAsync(db, currentUser, request.SucursalId, ct))!.Value;

        var sesion = new SesionCaja
        {
            SucursalId = sucursalId,
            UsuarioId = currentUser.UsuarioId!.Value,
            MontoInicial = request.MontoInicial,
            Estado = "ABIERTA",
        };
        db.SesionesCaja.Add(sesion);
        await db.SaveChangesAsync(ct);

        return new SesionCajaDto(sesion.Id, sesion.SucursalId, sesion.Apertura, sesion.Cierre,
            sesion.MontoInicial, sesion.MontoCierreDeclarado, sesion.MontoCierreCalculado, sesion.Estado);
    }
}
