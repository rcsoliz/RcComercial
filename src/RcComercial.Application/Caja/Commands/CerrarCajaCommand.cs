using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Notificaciones;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Caja.Commands;

public record CerrarCajaCommand(Guid SesionId, decimal MontoDeclarado) : IRequest<SesionCajaDto?>;

public class CerrarCajaCommandValidator : AbstractValidator<CerrarCajaCommand>
{
    public CerrarCajaCommandValidator() => RuleFor(x => x.MontoDeclarado).GreaterThanOrEqualTo(0);
}

public class CerrarCajaCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CerrarCajaCommand, SesionCajaDto?>
{
    private const decimal ToleranciaDiferencia = 0.01m;

    public async Task<SesionCajaDto?> Handle(CerrarCajaCommand request, CancellationToken ct)
    {
        var sesion = await db.SesionesCaja.FirstOrDefaultAsync(
            s => s.Id == request.SesionId && s.UsuarioId == currentUser.UsuarioId, ct);
        if (sesion is null || sesion.Estado != "ABIERTA") return null;

        var efectivoVendido = await (
            from p in db.Pagos
            join v in db.Ventas on p.VentaId equals v.Id
            where v.SesionCajaId == sesion.Id
                && v.Estado == EstadosVenta.Completada
                && p.Metodo == MetodosPago.Efectivo
            select p.Monto).SumAsync(ct);

        var montoCalculado = sesion.MontoInicial + efectivoVendido;

        sesion.MontoCierreDeclarado = request.MontoDeclarado;
        sesion.MontoCierreCalculado = montoCalculado;
        sesion.Estado = "CERRADA";
        sesion.Cierre = DateTimeOffset.UtcNow;

        if (Math.Abs(request.MontoDeclarado - montoCalculado) > ToleranciaDiferencia)
        {
            var telefono = await db.Empresas
                .Where(e => e.Id == currentUser.EmpresaId)
                .Select(e => e.TelefonoWhatsapp)
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrWhiteSpace(telefono))
            {
                db.Notificaciones.Add(new Notificacion
                {
                    EmpresaId = currentUser.EmpresaId!.Value,
                    Tipo = TiposNotificacion.DiferenciaCaja,
                    Destinatario = telefono,
                    Contenido = NotificacionTemplates.DiferenciaCaja(request.MontoDeclarado, montoCalculado),
                    ReferenciaId = sesion.Id,
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return new SesionCajaDto(sesion.Id, sesion.SucursalId, sesion.Apertura, sesion.Cierre,
            sesion.MontoInicial, sesion.MontoCierreDeclarado, sesion.MontoCierreCalculado, sesion.Estado);
    }
}
