using MediatR;

namespace RcComercial.Application.Productos.Commands.ImportarProductos;

public record ImportarProductosFila(string CodigoBarras, string Nombre, decimal Precio, decimal StockInicial);

public record ImportarProductosResult(int Creados, List<string> Errores);

/// <summary>
/// El parseo del archivo (CSV) ocurre en Api; aquí solo se procesan filas ya
/// validadas en forma. ErroresParseo son filas que ni siquiera se pudieron
/// leer y se reportan igual en el resultado final.
/// </summary>
public record ImportarProductosCommand(
    Guid SucursalId,
    List<ImportarProductosFila> FilasValidas,
    List<string> ErroresParseo) : IRequest<ImportarProductosResult>;
