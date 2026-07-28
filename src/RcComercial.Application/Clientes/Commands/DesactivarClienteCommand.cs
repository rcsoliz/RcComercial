using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Clientes.Commands;

public record DesactivarClienteCommand(Guid Id) : IRequest<bool>;

public class DesactivarClienteCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DesactivarClienteCommand, bool>
{
    public async Task<bool> Handle(DesactivarClienteCommand request, CancellationToken ct)
    {
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (cliente is null) return false;

        cliente.Activo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
