namespace RcComercial.Application.Common;

/// <summary>
/// Bolivia usa UTC-4 todo el año (sin horario de verano): un offset fijo es
/// suficiente y no depende de que el contenedor tenga tzdata instalado.
/// </summary>
public static class HoraBolivia
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-4);

    public static DateTimeOffset AhoraLocal() => DateTimeOffset.UtcNow.ToOffset(Offset);

    /// <summary>
    /// Rango [inicio, fin) del día calendario boliviano indicado, normalizado
    /// a offset UTC=0: Npgsql solo acepta "timestamp with time zone" en UTC.
    /// </summary>
    public static (DateTimeOffset Inicio, DateTimeOffset Fin) RangoDelDia(DateOnly dia)
    {
        var inicio = new DateTimeOffset(dia.Year, dia.Month, dia.Day, 0, 0, 0, Offset).ToUniversalTime();
        return (inicio, inicio.AddDays(1));
    }
}
