using System.Security.Cryptography;

namespace RcComercial.Application.Common;

/// <summary>Contraseñas temporales para usuarios nuevos / restablecidos: se muestran una sola vez al admin.</summary>
public static class GeneradorPassword
{
    // Sin 0/O ni 1/l/I: por teléfono o a mano se confunden fácil.
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

    public static string Temporal(int longitud = 10)
    {
        var bytes = RandomNumberGenerator.GetBytes(longitud);
        var chars = new char[longitud];
        for (var i = 0; i < longitud; i++)
            chars[i] = Alfabeto[bytes[i] % Alfabeto.Length];
        return new string(chars);
    }
}
