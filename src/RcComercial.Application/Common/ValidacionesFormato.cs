using System.Text.RegularExpressions;

namespace RcComercial.Application.Common;

/// <summary>Reglas de formato compartidas por Clientes y Proveedores (evita repetir el mismo regex en cada validator).</summary>
public static partial class ValidacionesFormato
{
    /// <summary>CI/NIT: solo dígitos. CEX/PAS (extranjero/pasaporte): alfanumérico — no llevan el mismo formato numérico.</summary>
    public static bool NitCiValido(string? valor, string tipoDocumento)
    {
        if (string.IsNullOrWhiteSpace(valor)) return true; // opcional: consumidor final "S/N"
        return tipoDocumento is "CI" or "NIT" ? RegexNumerico().IsMatch(valor) : RegexAlfanumerico().IsMatch(valor);
    }

    /// <summary>NIT de proveedor (siempre negocio, nunca CI/CEX/PAS): solo dígitos.</summary>
    public static bool NitValido(string? valor) => string.IsNullOrWhiteSpace(valor) || RegexNumerico().IsMatch(valor);

    /// <summary>Bolivia: +591 seguido de 8 dígitos; celular empieza con 6 o 7.</summary>
    public static bool TelefonoWhatsappValido(string? valor) =>
        string.IsNullOrWhiteSpace(valor) || RegexTelefono().IsMatch(valor);

    [GeneratedRegex(@"^\d{5,15}$")]
    private static partial Regex RegexNumerico();

    [GeneratedRegex(@"^[A-Za-z0-9]{5,20}$")]
    private static partial Regex RegexAlfanumerico();

    [GeneratedRegex(@"^\+591[67]\d{7}$")]
    private static partial Regex RegexTelefono();
}
