using RcComercial.Application.Productos.Dtos;
using RcComercial.Domain.Entities;

namespace RcComercial.Application.Productos;

internal static class ProductoMapper
{
    public static ProductoDto ToDto(Producto p) => new(
        p.Id, p.Codigo, p.CodigoBarras, p.Nombre, p.CategoriaId, p.MarcaId, p.UnidadBaseId,
        p.PrecioBase, p.StockMinimo, p.ManejaLote, p.EsControlado, p.PermiteDecimales, p.Activo,
        p.Presentaciones.Select(pr => new PresentacionDto(
            pr.Id, pr.Nombre, pr.Factor, pr.CodigoBarras, pr.Precio,
            pr.PrecioMayorista, pr.CantidadMinMayorista, pr.EsPredeterminada)).ToList(),
        p.FichaFarmacia is null
            ? null
            : new FichaFarmaciaDto(
                p.FichaFarmacia.PrincipioActivo, p.FichaFarmacia.Concentracion,
                p.FichaFarmacia.FormaFarmaceutica, p.FichaFarmacia.Laboratorio,
                p.FichaFarmacia.RegistroSanitario, p.FichaFarmacia.Clasificacion,
                p.FichaFarmacia.RequiereCadenaFrio));
}
