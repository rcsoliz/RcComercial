using MediatR;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Application.Productos.Dtos;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Productos.Commands.CrearProducto;

public class CrearProductoCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CrearProductoCommand, ProductoDto>
{
    public async Task<ProductoDto> Handle(CrearProductoCommand request, CancellationToken ct)
    {
        var producto = new Producto
        {
            EmpresaId = currentUser.EmpresaId!.Value,
            Codigo = request.Codigo,
            CodigoBarras = request.CodigoBarras,
            Nombre = request.Nombre,
            CategoriaId = request.CategoriaId,
            MarcaId = request.MarcaId,
            UnidadBaseId = request.UnidadBaseId,
            PrecioBase = request.PrecioBase,
            StockMinimo = request.StockMinimo,
            ManejaLote = request.ManejaLote,
            EsControlado = request.EsControlado,
            PermiteDecimales = request.PermiteDecimales,
        };

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

        if (request.FichaFarmacia is { } ficha)
        {
            producto.FichaFarmacia = new ProductoFarmacia
            {
                ProductoId = producto.Id,
                PrincipioActivo = ficha.PrincipioActivo,
                Concentracion = ficha.Concentracion,
                FormaFarmaceutica = ficha.FormaFarmaceutica,
                Laboratorio = ficha.Laboratorio,
                RegistroSanitario = ficha.RegistroSanitario,
                Clasificacion = ficha.Clasificacion,
                RequiereCadenaFrio = ficha.RequiereCadenaFrio,
            };
        }

        db.Productos.Add(producto);
        await db.SaveChangesAsync(ct);

        return ProductoMapper.ToDto(producto);
    }
}
