using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Productos.Dtos;

namespace RcComercial.Application.Productos.Queries.ObtenerProducto;

public class ObtenerProductoQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ObtenerProductoQuery, ProductoDto?>
{
    public async Task<ProductoDto?> Handle(ObtenerProductoQuery request, CancellationToken ct)
    {
        var producto = await db.Productos
            .Include(p => p.Presentaciones)
            .Include(p => p.FichaFarmacia)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        return producto is null ? null : ProductoMapper.ToDto(producto);
    }
}
