using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Infrastructure.Persistence;
using RcComercial.Infrastructure.Whatsapp;

namespace RcComercial.Infrastructure.BackgroundJobs;

/// <summary>
/// Procesa `notificacion` PENDIENTE en un ciclo corto (no espera a un
/// horario, a diferencia de ResumenDiarioBackgroundService). 3 intentos con
/// backoff (1 min, 5 min); al agotarlos queda FALLIDA — nunca se pierde ni
/// se borra la fila.
/// </summary>
public class NotificacionDispatcherBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificacionDispatcherSettings> settings,
    ILogger<NotificacionDispatcherBackgroundService> logger) : BackgroundService
{
    private const short MaxIntentos = 3;
    private static readonly TimeSpan[] Backoff = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(1, settings.Value.IntervaloDespachoSegundos));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarPendientesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error despachando notificaciones.");
            }

            try
            {
                await Task.Delay(intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task ProcesarPendientesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IWhatsappSender>();

        var ahora = DateTimeOffset.UtcNow;
        var pendientes = await db.Notificaciones.IgnoreQueryFilters()
            .Where(n => n.Estado == EstadosNotificacion.Pendiente
                && (n.ProximoIntentoEn == null || n.ProximoIntentoEn <= ahora))
            .ToListAsync(ct);

        foreach (var n in pendientes)
        {
            var resultado = await sender.EnviarAsync(n.Destinatario, n.Contenido, ct);

            if (resultado.Exitoso)
            {
                n.Estado = EstadosNotificacion.Enviada;
                n.EnviadoEn = DateTimeOffset.UtcNow;
                n.EnlaceGenerado = resultado.EnlaceGenerado;
            }
            else
            {
                n.Intentos++;
                if (n.Intentos >= MaxIntentos)
                {
                    n.Estado = EstadosNotificacion.Fallida;
                }
                else
                {
                    n.ProximoIntentoEn = DateTimeOffset.UtcNow.Add(Backoff[n.Intentos - 1]);
                }
                logger.LogWarning(
                    "Fallo al enviar notificación {Id} (intento {Intentos}): {Error}", n.Id, n.Intentos, resultado.Error);
            }
        }

        if (pendientes.Count > 0) await db.SaveChangesAsync(ct);
    }
}
