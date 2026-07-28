using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Ventas.Queries;

public record VentaResumenDto(Guid Id, string Numero, DateTimeOffset Fecha, string Estado, decimal Total);

/// <summary>Historial de ventas de un cliente puntual (detalle de ClientesView). Gateado por ventas.ver_historial.</summary>
public record ListarVentasPorClienteQuery(Guid ClienteId, int Pagina = 1) : IRequest<List<VentaResumenDto>>;

public class ListarVentasPorClienteQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarVentasPorClienteQuery, List<VentaResumenDto>>
{
    private const int TamanoPagina = 20;

    public async Task<List<VentaResumenDto>> Handle(ListarVentasPorClienteQuery request, CancellationToken ct)
    {
        var pagina = Math.Max(1, request.Pagina);
        return await db.Ventas
            .Where(v => v.ClienteId == request.ClienteId)
            .OrderByDescending(v => v.Fecha)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(v => new VentaResumenDto(v.Id, v.Numero, v.Fecha, v.Estado, v.Total))
            .ToListAsync(ct);
    }
}
