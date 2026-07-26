using System.Security.Cryptography;

namespace RcComercial.Domain.Common;

/// <summary>
/// Generador de UUID v7 (ordenado por tiempo) para .NET 8.
/// Mantiene los índices PK sin fragmentación y permite generar IDs
/// en el cliente (modo offline) sin colisiones.
/// </summary>
public static class Uuid7
{
    public static Guid NewGuid()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[0] = (byte)(ms >> 40);
        bytes[1] = (byte)(ms >> 32);
        bytes[2] = (byte)(ms >> 24);
        bytes[3] = (byte)(ms >> 16);
        bytes[4] = (byte)(ms >> 8);
        bytes[5] = (byte)ms;

        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70); // versión 7
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // variante RFC 4122

        return new Guid(bytes, bigEndian: true);
    }
}
