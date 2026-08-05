using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Vehiculos.Queries;

public record ListarVehiculosPorClienteQuery(Guid ClienteId) : IRequest<List<VehiculoDto>>;

public class ListarVehiculosPorClienteQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarVehiculosPorClienteQuery, List<VehiculoDto>>
{
    public async Task<List<VehiculoDto>> Handle(ListarVehiculosPorClienteQuery request, CancellationToken ct)
    {
        return await db.Vehiculos
            .Where(v => v.ClienteId == request.ClienteId && v.Activo)
            .OrderBy(v => v.Placa)
            .Select(v => new VehiculoDto(
                v.Id, v.ClienteId, v.Placa, v.Marca, v.Modelo, v.Anio, v.Color, v.NumeroChasis, v.Activo))
            .ToListAsync(ct);
    }
}
