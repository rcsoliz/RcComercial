using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Ventas.Dtos;

namespace RcComercial.Application.Ventas.Queries;

public record ObtenerVentaQuery(Guid Id) : IRequest<VentaDto?>;

public class ObtenerVentaQueryHandler(IApplicationDbContext db) : IRequestHandler<ObtenerVentaQuery, VentaDto?>
{
    public async Task<VentaDto?> Handle(ObtenerVentaQuery request, CancellationToken ct)
    {
        var venta = await db.Ventas
            .Include(v => v.Detalles)
            .Include(v => v.Pagos)
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        return venta is null ? null : VentaMapper.ToDto(venta);
    }
}
