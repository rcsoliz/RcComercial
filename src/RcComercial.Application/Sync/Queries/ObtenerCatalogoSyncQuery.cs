using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Sync.Queries;

public record ObtenerCatalogoSyncQuery : IRequest<SyncCatalogoDto>;

public class ObtenerCatalogoSyncQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ObtenerCatalogoSyncQuery, SyncCatalogoDto>
{
    public async Task<SyncCatalogoDto> Handle(ObtenerCatalogoSyncQuery request, CancellationToken ct)
    {
        var productos = await db.Productos
            .Where(p => p.Activo)
            .Include(p => p.Presentaciones.Where(pr => pr.Activo))
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

        var version = productos.Count == 0
            ? DateTimeOffset.UnixEpoch
            : productos.Max(p => p.ActualizadoEn ?? p.CreadoEn);

        var productosDto = productos
            .Select(p => new SyncProductoDto(
                p.Id, p.Codigo, p.CodigoBarras, p.Nombre, p.PrecioBase,
                p.ManejaLote, p.EsControlado, p.PermiteDecimales,
                p.Presentaciones.Select(pr => new SyncPresentacionDto(
                    pr.Id, pr.Nombre, pr.Factor, pr.CodigoBarras,
                    pr.Precio, pr.PrecioMayorista, pr.CantidadMinMayorista, pr.EsPredeterminada)).ToList()))
            .ToList();

        return new SyncCatalogoDto(version.ToString("O"), productosDto);
    }
}
