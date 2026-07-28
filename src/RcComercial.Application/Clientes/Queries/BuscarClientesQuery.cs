using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Clientes.Queries;

/// <summary>Estado: "activos" (default) | "inactivos" | "todos".</summary>
public record BuscarClientesQuery(string? Buscar, string? Estado, int Pagina = 1) : IRequest<List<ClienteListItemDto>>;

public class BuscarClientesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<BuscarClientesQuery, List<ClienteListItemDto>>
{
    private const int TamanoPagina = 20;

    public async Task<List<ClienteListItemDto>> Handle(BuscarClientesQuery request, CancellationToken ct)
    {
        var texto = request.Buscar?.Trim();
        var pagina = Math.Max(1, request.Pagina);

        IQueryable<Cliente> query = request.Estado?.Trim().ToUpperInvariant() switch
        {
            "INACTIVOS" => db.Clientes.Where(c => !c.Activo),
            "TODOS" => db.Clientes,
            _ => db.Clientes.Where(c => c.Activo),
        };

        if (!string.IsNullOrWhiteSpace(texto))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Nombre, $"%{texto}%") ||
                (c.NitCi != null && EF.Functions.ILike(c.NitCi, $"%{texto}%")) ||
                (c.TelefonoWhatsapp != null && EF.Functions.ILike(c.TelefonoWhatsapp, $"%{texto}%")));
        }

        var clientes = await query
            .OrderBy(c => c.Nombre)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(c => new { c.Id, c.Nombre, c.NitCi, c.TipoDocumento, c.TelefonoWhatsapp, c.Activo })
            .ToListAsync(ct);

        var clienteIds = clientes.Select(c => c.Id).ToList();
        var desde = DateTimeOffset.UtcNow.AddDays(-30);
        var comprasPorCliente = await db.Ventas
            .Where(v => v.ClienteId != null && clienteIds.Contains(v.ClienteId.Value)
                && v.Fecha >= desde && v.Estado == EstadosVenta.Completada)
            .GroupBy(v => v.ClienteId!.Value)
            .Select(g => new { ClienteId = g.Key, Total = g.Sum(v => v.Total) })
            .ToDictionaryAsync(x => x.ClienteId, x => x.Total, ct);

        return clientes
            .Select(c => new ClienteListItemDto(
                c.Id, c.Nombre, c.NitCi, c.TipoDocumento, c.TelefonoWhatsapp, c.Activo,
                comprasPorCliente.GetValueOrDefault(c.Id)))
            .ToList();
    }
}
