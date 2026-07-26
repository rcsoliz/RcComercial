using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Productos.Dtos;

namespace RcComercial.Application.Productos.Queries.ObtenerProductoPorCodigoBarras;

public class ObtenerProductoPorCodigoBarrasQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ObtenerProductoPorCodigoBarrasQuery, ProductoPorCodigoResult?>
{
    public async Task<ProductoPorCodigoResult?> Handle(
        ObtenerProductoPorCodigoBarrasQuery request, CancellationToken ct)
    {
        var producto = await db.Productos
            .Include(p => p.Presentaciones)
            .Include(p => p.FichaFarmacia)
            .FirstOrDefaultAsync(p => p.CodigoBarras == request.CodigoBarras
                || p.Presentaciones.Any(pr => pr.CodigoBarras == request.CodigoBarras), ct);

        if (producto is not null)
            return new ProductoPorCodigoResult(false, ProductoMapper.ToDto(producto), null);

        var maestro = await db.ProductosMaestro
            .FirstOrDefaultAsync(m => m.CodigoBarras == request.CodigoBarras, ct);

        if (maestro is null) return null;

        return new ProductoPorCodigoResult(true, null, new ProductoMaestroDto(
            maestro.Id, maestro.CodigoBarras, maestro.Nombre, maestro.Marca, maestro.Contenido, maestro.RubroId));
    }
}
