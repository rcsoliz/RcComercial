using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Proformas.Dtos;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Proformas.Queries.ListarProformas;

public class ListarProformasQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarProformasQuery, ListarProformasResultDto>
{
    private const int TamanoPagina = 8;

    public async Task<ListarProformasResultDto> Handle(ListarProformasQuery request, CancellationToken ct)
    {
        var texto = request.Buscar?.Trim();
        var pagina = Math.Max(1, request.Pagina);

        IQueryable<Proforma> query = db.Proformas;

        if (!string.IsNullOrWhiteSpace(request.Estado))
            query = query.Where(p => p.Estado == request.Estado.Trim().ToUpperInvariant());
        if (request.ClienteId is { } clienteId)
            query = query.Where(p => p.ClienteId == clienteId);
        if (!string.IsNullOrWhiteSpace(texto))
            query = query.Where(p => EF.Functions.ILike(p.Numero, $"%{texto}%"));

        var total = await query.CountAsync(ct);

        var proformas = await query
            .OrderByDescending(p => p.Fecha)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(p => new { p.Id, p.Numero, p.Fecha, p.ClienteId, p.Estado, p.Total })
            .ToListAsync(ct);

        var clienteIds = proformas.Where(p => p.ClienteId is not null).Select(p => p.ClienteId!.Value).ToList();
        var nombresCliente = await db.Clientes
            .Where(c => clienteIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);

        var items = proformas
            .Select(p => new ProformaListItemDto(
                p.Id, p.Numero, p.Fecha, p.ClienteId,
                p.ClienteId is { } cId ? nombresCliente.GetValueOrDefault(cId) : null,
                p.Estado, p.Total))
            .ToList();

        return new ListarProformasResultDto(items, total, pagina, TamanoPagina);
    }
}
