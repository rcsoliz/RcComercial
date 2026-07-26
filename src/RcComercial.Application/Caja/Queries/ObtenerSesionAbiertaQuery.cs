using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Caja.Queries;

public record ObtenerSesionAbiertaQuery : IRequest<SesionCajaDto?>;

public class ObtenerSesionAbiertaQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ObtenerSesionAbiertaQuery, SesionCajaDto?>
{
    public async Task<SesionCajaDto?> Handle(ObtenerSesionAbiertaQuery request, CancellationToken ct)
    {
        var sesion = await db.SesionesCaja
            .Where(s => s.UsuarioId == currentUser.UsuarioId && s.Estado == "ABIERTA")
            .FirstOrDefaultAsync(ct);

        return sesion is null
            ? null
            : new SesionCajaDto(sesion.Id, sesion.SucursalId, sesion.Apertura, sesion.Cierre,
                sesion.MontoInicial, sesion.MontoCierreDeclarado, sesion.MontoCierreCalculado, sesion.Estado);
    }
}
