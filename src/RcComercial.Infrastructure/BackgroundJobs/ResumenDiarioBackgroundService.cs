using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RcComercial.Application.Common;
using RcComercial.Application.Notificaciones;
using RcComercial.Application.Panel.Queries.ObtenerPanelAlertas;
using RcComercial.Application.Panel.Queries.ObtenerPanelHoy;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;
using RcComercial.Infrastructure.Persistence;

namespace RcComercial.Infrastructure.BackgroundJobs;

/// <summary>
/// A las 21:00 hora La Paz, compone el resumen del día de cada empresa
/// activa con WhatsApp configurado y lo encola en `notificacion`
/// (RESUMEN_DIARIO, y STOCK_MINIMO/VENCIMIENTOS si hay alertas). El envío
/// real lo hace NotificacionDispatcherBackgroundService; aquí solo se encola.
/// </summary>
public class ResumenDiarioBackgroundService(
    IServiceScopeFactory scopeFactory, ILogger<ResumenDiarioBackgroundService> logger) : BackgroundService
{
    private const int HoraEjecucionLocal = 21;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CalcularEsperaHastaProximaEjecucion(), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await EjecutarResumenAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generando el resumen diario.");
            }
        }
    }

    internal static TimeSpan CalcularEsperaHastaProximaEjecucion()
    {
        var ahoraLocal = HoraBolivia.AhoraLocal();
        var proximaEjecucion = new DateTimeOffset(
            ahoraLocal.Year, ahoraLocal.Month, ahoraLocal.Day, HoraEjecucionLocal, 0, 0, HoraBolivia.Offset);
        if (proximaEjecucion <= ahoraLocal) proximaEjecucion = proximaEjecucion.AddDays(1);
        return proximaEjecucion - ahoraLocal;
    }

    public async Task EjecutarResumenAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var empresas = await db.Empresas.IgnoreQueryFilters()
            .Where(e => e.Activo && e.TelefonoWhatsapp != null)
            .ToListAsync(ct);

        foreach (var empresa in empresas)
        {
            var resumen = await mediator.Send(
                new ObtenerPanelHoyQuery(EmpresaId: empresa.Id, IncluirCostos: true), ct);
            db.Notificaciones.Add(new Notificacion
            {
                EmpresaId = empresa.Id,
                Tipo = TiposNotificacion.ResumenDiario,
                Destinatario = empresa.TelefonoWhatsapp!,
                Contenido = NotificacionTemplates.ResumenDiario(resumen),
            });

            var alertas = await mediator.Send(new ObtenerPanelAlertasQuery(EmpresaId: empresa.Id), ct);

            if (alertas.ProductosBajoMinimo.Count > 0)
            {
                db.Notificaciones.Add(new Notificacion
                {
                    EmpresaId = empresa.Id,
                    Tipo = TiposNotificacion.StockMinimo,
                    Destinatario = empresa.TelefonoWhatsapp!,
                    Contenido = NotificacionTemplates.StockMinimo(alertas.ProductosBajoMinimo),
                });
            }

            if (alertas.LotesPorVencer30.Count > 0)
            {
                db.Notificaciones.Add(new Notificacion
                {
                    EmpresaId = empresa.Id,
                    Tipo = TiposNotificacion.Vencimientos,
                    Destinatario = empresa.TelefonoWhatsapp!,
                    Contenido = NotificacionTemplates.Vencimientos(alertas.LotesPorVencer30),
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
