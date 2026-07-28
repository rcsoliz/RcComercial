using FluentValidation;
using MediatR;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Sync.Commands;

public record RangoReservadoDto(long Inicio, long Fin);

/// <summary>DispositivoId es informativo (telemetría/depuración): la
/// no-colisión del rango la garantiza la secuencia compartida, no este campo.</summary>
public record ReservarRangoCommand(Guid? SucursalId, string DispositivoId, int Tamano = 500)
    : IRequest<RangoReservadoDto>;

public class ReservarRangoCommandValidator : AbstractValidator<ReservarRangoCommand>
{
    public ReservarRangoCommandValidator()
    {
        RuleFor(x => x.DispositivoId).NotEmpty();
        RuleFor(x => x.Tamano).InclusiveBetween(1, 2000);
    }
}

public class ReservarRangoCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ReservarRangoCommand, RangoReservadoDto>
{
    public async Task<RangoReservadoDto> Handle(ReservarRangoCommand request, CancellationToken ct)
    {
        var empresaId = currentUser.EmpresaId!.Value;
        var sucursalId = await SucursalResolver.ResolverAsync(db, currentUser, request.SucursalId, ct)
            ?? throw new ValidationException("No se pudo determinar la sucursal: especifique 'sucursalId'.");

        var inicio = await db.ReservarRangoNumeroAsync(empresaId, sucursalId, "VENTA", request.Tamano, ct);
        return new RangoReservadoDto(inicio, inicio + request.Tamano - 1);
    }
}
