using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Proveedores.Queries;

public record ObtenerProveedorQuery(Guid Id) : IRequest<ProveedorDto?>;

public class ObtenerProveedorQueryHandler(IApplicationDbContext db) : IRequestHandler<ObtenerProveedorQuery, ProveedorDto?>
{
    public async Task<ProveedorDto?> Handle(ObtenerProveedorQuery request, CancellationToken ct) =>
        await db.Proveedores
            .Where(p => p.Id == request.Id)
            .Select(p => new ProveedorDto(p.Id, p.Nombre, p.Nit, p.TelefonoWhatsapp, p.DiasCredito, p.LeadTimeDias, p.Activo))
            .FirstOrDefaultAsync(ct);
}
