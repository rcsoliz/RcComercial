using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;

namespace RcComercial.Application.Proformas.Commands.RechazarProforma;

public record RechazarProformaCommand(Guid Id, string? MotivoRechazo) : IRequest<bool>;

public class RechazarProformaCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RechazarProformaCommand, bool>
{
    public async Task<bool> Handle(RechazarProformaCommand request, CancellationToken ct)
    {
        var proforma = await db.Proformas.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (proforma is null) return false;

        if (proforma.Estado == EstadosProforma.Convertida)
            throw new ValidationException("No se puede rechazar una proforma ya convertida en venta.");

        proforma.Estado = EstadosProforma.Rechazada;
        proforma.MotivoRechazo = request.MotivoRechazo;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
