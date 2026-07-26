using System.Globalization;
using System.Text;
using RcComercial.Application.Productos.Commands.ImportarProductos;

namespace RcComercial.Api.Productos;

/// <summary>Parser CSV mínimo (columnas: codigo_barras,nombre,precio,stock_inicial).</summary>
public static class CsvProductoParser
{
    public record Resultado(List<ImportarProductosFila> Filas, List<string> Errores);

    public static async Task<Resultado> ParsearAsync(Stream csv)
    {
        var filas = new List<ImportarProductosFila>();
        var errores = new List<string>();

        using var reader = new StreamReader(csv);
        var numeroLinea = 0;
        string? linea;
        while ((linea = await reader.ReadLineAsync()) is not null)
        {
            numeroLinea++;
            if (numeroLinea == 1) continue; // encabezado
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var campos = ParsearLinea(linea);
            if (campos.Count < 4)
            {
                errores.Add($"Línea {numeroLinea}: se esperaban 4 columnas " +
                    "(codigo_barras,nombre,precio,stock_inicial).");
                continue;
            }

            var codigoBarras = campos[0].Trim();
            var nombre = campos[1].Trim();

            if (string.IsNullOrWhiteSpace(codigoBarras) || string.IsNullOrWhiteSpace(nombre))
            {
                errores.Add($"Línea {numeroLinea}: código de barras y nombre son obligatorios.");
                continue;
            }
            if (!decimal.TryParse(campos[2].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var precio)
                || precio < 0)
            {
                errores.Add($"Línea {numeroLinea}: precio inválido ('{campos[2]}').");
                continue;
            }
            if (!decimal.TryParse(
                    campos[3].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var stockInicial)
                || stockInicial < 0)
            {
                errores.Add($"Línea {numeroLinea}: stock_inicial inválido ('{campos[3]}').");
                continue;
            }

            filas.Add(new ImportarProductosFila(codigoBarras, nombre, precio, stockInicial));
        }

        return new Resultado(filas, errores);
    }

    private static List<string> ParsearLinea(string linea)
    {
        var campos = new List<string>();
        var actual = new StringBuilder();
        var dentroComillas = false;

        foreach (var c in linea)
        {
            if (c == '"')
                dentroComillas = !dentroComillas;
            else if (c == ',' && !dentroComillas)
            {
                campos.Add(actual.ToString());
                actual.Clear();
            }
            else
                actual.Append(c);
        }
        campos.Add(actual.ToString());
        return campos;
    }
}
