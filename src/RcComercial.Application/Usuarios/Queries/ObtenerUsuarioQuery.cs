using MediatR;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Usuarios.Queries;

public record ObtenerUsuarioQuery(Guid Id) : IRequest<UsuarioDto?>;

public class ObtenerUsuarioQueryHandler(IApplicationDbContext db) : IRequestHandler<ObtenerUsuarioQuery, UsuarioDto?>
{
    public Task<UsuarioDto?> Handle(ObtenerUsuarioQuery request, CancellationToken ct) =>
        UsuarioMapper.ObtenerDtoAsync(db, request.Id, ct);
}
