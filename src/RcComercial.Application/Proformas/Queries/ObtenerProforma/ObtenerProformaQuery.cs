using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Proformas.Dtos;

namespace RcComercial.Application.Proformas.Queries.ObtenerProforma;

public record ObtenerProformaQuery(Guid Id) : IRequest<ProformaDto?>;

public class ObtenerProformaQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ObtenerProformaQuery, ProformaDto?>
{
    public async Task<ProformaDto?> Handle(ObtenerProformaQuery request, CancellationToken ct)
    {
        var proforma = await db.Proformas
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        return proforma is null ? null : ProformaMapper.ToDto(proforma);
    }
}
