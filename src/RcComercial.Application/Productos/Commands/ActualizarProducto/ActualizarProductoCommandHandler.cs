using MediatR;
using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Productos.Dtos;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Productos.Commands.ActualizarProducto;

public class ActualizarProductoCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ActualizarProductoCommand, ProductoDto?>
{
    public async Task<ProductoDto?> Handle(ActualizarProductoCommand request, CancellationToken ct)
    {
        var producto = await db.Productos
            .Include(p => p.Presentaciones)
            .Include(p => p.FichaFarmacia)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (producto is null) return null;

        producto.Codigo = request.Codigo;
        producto.CodigoBarras = request.CodigoBarras;
        producto.Nombre = request.Nombre;
        producto.CategoriaId = request.CategoriaId;
        producto.MarcaId = request.MarcaId;
        producto.UnidadBaseId = request.UnidadBaseId;
        producto.StockMinimo = request.StockMinimo;
        // Un servicio nunca maneja lote, sin importar lo que mande el cliente.
        producto.ManejaLote = request.EsServicio ? false : request.ManejaLote;
        producto.EsControlado = request.EsControlado;
        producto.PermiteDecimales = request.PermiteDecimales;
        producto.EsServicio = request.EsServicio;
        producto.CodigoProductoSin = request.CodigoProductoSin;
        producto.CodigoUnidadSin = request.CodigoUnidadSin;

        db.ProductoPresentaciones.RemoveRange(producto.Presentaciones);
        producto.Presentaciones.Clear();
        foreach (var p in request.Presentaciones)
        {
            producto.Presentaciones.Add(new ProductoPresentacion
            {
                ProductoId = producto.Id,
                Nombre = p.Nombre,
                Factor = p.Factor,
                CodigoBarras = p.CodigoBarras,
                Precio = p.Precio,
                PrecioMayorista = p.PrecioMayorista,
                CantidadMinMayorista = p.CantidadMinMayorista,
                EsPredeterminada = p.EsPredeterminada,
            });
        }

        await db.SaveChangesAsync(ct);
        return ProductoMapper.ToDto(producto);
    }
}
