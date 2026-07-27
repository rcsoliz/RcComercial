using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Proveedores.Queries;

public record ListarProveedoresQuery : IRequest<List<ProveedorDto>>;

public class ListarProveedoresQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListarProveedoresQuery, List<ProveedorDto>>
{
    public async Task<List<ProveedorDto>> Handle(ListarProveedoresQuery request, CancellationToken ct) =>
        await db.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new ProveedorDto(p.Id, p.Nombre, p.Nit, p.TelefonoWhatsapp, p.DiasCredito, p.LeadTimeDias, p.Activo))
            .ToListAsync(ct);
}
