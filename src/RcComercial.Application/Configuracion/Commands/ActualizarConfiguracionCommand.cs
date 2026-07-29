using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Configuracion.Commands;

public record ActualizarConfiguracionCommand(bool PermiteStockNegativo, int HoraResumenWhatsapp) : IRequest;

public class ActualizarConfiguracionCommandValidator : AbstractValidator<ActualizarConfiguracionCommand>
{
    public ActualizarConfiguracionCommandValidator() =>
        RuleFor(x => x.HoraResumenWhatsapp).InclusiveBetween(0, 23)
            .WithMessage("La hora debe estar entre 0 y 23.");
}

public class ActualizarConfiguracionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ActualizarConfiguracionCommand>
{
    public async Task Handle(ActualizarConfiguracionCommand request, CancellationToken ct)
    {
        await EstablecerAsync(ClavesConfiguracion.VentaPermiteStockNegativo, request.PermiteStockNegativo.ToString(), ct);
        await EstablecerAsync(ClavesConfiguracion.NotificacionesHoraResumen, request.HoraResumenWhatsapp.ToString(), ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task EstablecerAsync(string clave, string valor, CancellationToken ct)
    {
        var empresaId = currentUser.EmpresaId!.Value;
        var existente = await db.EmpresaConfiguraciones
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Clave == clave, ct);

        if (existente is null)
        {
            db.EmpresaConfiguraciones.Add(new EmpresaConfiguracion
            {
                EmpresaId = empresaId, Clave = clave, Valor = valor, ActualizadoPor = currentUser.UsuarioId,
            });
        }
        else
        {
            existente.Valor = valor;
            existente.ActualizadoEn = DateTimeOffset.UtcNow;
            existente.ActualizadoPor = currentUser.UsuarioId;
        }
    }
}
