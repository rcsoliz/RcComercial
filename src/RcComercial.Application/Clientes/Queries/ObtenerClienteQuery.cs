using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Clientes.Queries;

public record ObtenerClienteQuery(Guid Id) : IRequest<ClienteDto?>;

public class ObtenerClienteQueryHandler(IApplicationDbContext db) : IRequestHandler<ObtenerClienteQuery, ClienteDto?>
{
    public async Task<ClienteDto?> Handle(ObtenerClienteQuery request, CancellationToken ct) =>
        await db.Clientes
            .Where(c => c.Id == request.Id)
            .Select(c => new ClienteDto(c.Id, c.Nombre, c.NitCi, c.TipoDocumento, c.TelefonoWhatsapp, c.Email, c.Activo))
            .FirstOrDefaultAsync(ct);
}
