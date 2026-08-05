using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Clientes.Queries.ListarClientes;

/// <summary>
/// Handler nuevo y paralelo a BuscarClientesQueryHandler (que sigue igual,
/// lo usa SelectorCliente.vue en el POS): misma búsqueda/filtro, pero
/// devolviendo el total para paginar de verdad en ClientesView.
/// </summary>
public class ListarClientesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarClientesQuery, ListarClientesResultDto>
{
    private const int TamanoPagina = 8;

    public async Task<ListarClientesResultDto> Handle(ListarClientesQuery request, CancellationToken ct)
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

        var total = await query.CountAsync(ct);

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

        var items = clientes
            .Select(c => new ClienteListItemDto(
                c.Id, c.Nombre, c.NitCi, c.TipoDocumento, c.TelefonoWhatsapp, c.Activo,
                comprasPorCliente.GetValueOrDefault(c.Id)))
            .ToList();

        return new ListarClientesResultDto(items, total, pagina, TamanoPagina);
    }
}
