using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Compras.Dtos;
using RcComercial.Domain.Common;

namespace RcComercial.Application.Compras.Queries.ObtenerSugeridoCompra;

public class ObtenerSugeridoCompraQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ObtenerSugeridoCompraQuery, List<SugeridoCompraItemDto>>
{
    public async Task<List<SugeridoCompraItemDto>> Handle(ObtenerSugeridoCompraQuery request, CancellationToken ct)
    {
        var sucursalId = await SucursalResolver.ResolverAsync(db, currentUser, request.SucursalId, ct)
            ?? throw new ValidationException("No se pudo determinar la sucursal: especifique 'sucursalId'.");

        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Id == request.ProveedorId, ct)
            ?? throw new ValidationException("El proveedor no existe.");

        var hace30Dias = DateTimeOffset.UtcNow.AddDays(-30);

        var totalVendido30DiasPorProducto = await (
            from vd in db.VentaDetalles
            join v in db.Ventas on vd.VentaId equals v.Id
            where v.Estado == EstadosVenta.Completada && v.Fecha >= hace30Dias
            group vd by vd.ProductoId into g
            select new { ProductoId = g.Key, Total = g.Sum(x => x.CantidadBase) }
        ).ToDictionaryAsync(x => x.ProductoId, x => x.Total, ct);

        var stockActualPorProducto = await db.Stocks
            .Where(s => s.SucursalId == sucursalId)
            .GroupBy(s => s.ProductoId)
            .Select(g => new { ProductoId = g.Key, Total = g.Sum(x => x.Cantidad) })
            .ToDictionaryAsync(x => x.ProductoId, x => x.Total, ct);

        var productos = await db.Productos.Where(p => p.Activo).Select(p => new { p.Id, p.Nombre }).ToListAsync(ct);

        var diasCobertura = proveedor.LeadTimeDias + 7;
        var resultado = new List<SugeridoCompraItemDto>();

        foreach (var producto in productos)
        {
            var ventaDiaria = totalVendido30DiasPorProducto.GetValueOrDefault(producto.Id) / 30m;
            var stockActual = stockActualPorProducto.GetValueOrDefault(producto.Id);
            var cantidadSugerida = Math.Ceiling((ventaDiaria * diasCobertura) - stockActual);

            if (cantidadSugerida > 0)
                resultado.Add(new SugeridoCompraItemDto(producto.Id, producto.Nombre, ventaDiaria, stockActual, cantidadSugerida));
        }

        return resultado.OrderByDescending(x => x.CantidadSugerida).ToList();
    }
}
