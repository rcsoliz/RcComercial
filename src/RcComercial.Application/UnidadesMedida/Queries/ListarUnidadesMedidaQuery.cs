using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.UnidadesMedida.Queries;

public record ListarUnidadesMedidaQuery : IRequest<List<UnidadMedidaDto>>;

public class ListarUnidadesMedidaQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarUnidadesMedidaQuery, List<UnidadMedidaDto>>
{
    public async Task<List<UnidadMedidaDto>> Handle(ListarUnidadesMedidaQuery request, CancellationToken ct) =>
        await db.UnidadesMedida
            .OrderBy(u => u.Nombre)
            .Select(u => new UnidadMedidaDto(u.Id, u.Nombre, u.Abreviatura))
            .ToListAsync(ct);
}
