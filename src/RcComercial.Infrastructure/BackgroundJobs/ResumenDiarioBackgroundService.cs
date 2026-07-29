using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RcComercial.Application.Common;
using RcComercial.Application.Notificaciones;
using RcComercial.Application.Panel;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;
using RcComercial.Infrastructure.Persistence;

namespace RcComercial.Infrastructure.BackgroundJobs;

/// <summary>
/// Cada hora en punto, revisa qué empresas activas con WhatsApp configurado
/// tienen esa hora (empresa_configuracion: notificaciones.hora_resumen,
/// default 21:00 La Paz) y compone su resumen del día — RESUMEN_DIARIO, y
/// STOCK_MINIMO/VENCIMIENTOS si hay alertas — encolándolo en `notificacion`.
/// El envío real lo hace NotificacionDispatcherBackgroundService; aquí solo
/// se encola, una vez por empresa por día (se deduplica contra lo ya
/// encolado hoy, por si el proceso se reinicia dentro de la misma hora).
/// </summary>
public class ResumenDiarioBackgroundService(
    IServiceScopeFactory scopeFactory, ILogger<ResumenDiarioBackgroundService> logger) : BackgroundService
{
    private const int HoraPorDefecto = 21;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CalcularEsperaHastaProximaHoraEnPunto(), stoppingToken);
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

    internal static TimeSpan CalcularEsperaHastaProximaHoraEnPunto()
    {
        var ahoraLocal = HoraBolivia.AhoraLocal();
        var proximaHora = new DateTimeOffset(
            ahoraLocal.Year, ahoraLocal.Month, ahoraLocal.Day, ahoraLocal.Hour, 0, 0, HoraBolivia.Offset).AddHours(1);
        return proximaHora - ahoraLocal;
    }

    public async Task EjecutarResumenAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var horaActual = HoraBolivia.AhoraLocal().Hour;
        var (inicioHoy, _) = HoraBolivia.RangoDelDia(DateOnly.FromDateTime(HoraBolivia.AhoraLocal().Date));

        var empresas = await db.Empresas.IgnoreQueryFilters()
            .Where(e => e.Activo && e.TelefonoWhatsapp != null)
            .ToListAsync(ct);

        var horasConfiguradas = await db.EmpresaConfiguraciones.IgnoreQueryFilters()
            .Where(c => c.Clave == ClavesConfiguracion.NotificacionesHoraResumen)
            .ToDictionaryAsync(c => c.EmpresaId, c => c.Valor, ct);

        foreach (var empresa in empresas)
        {
            var horaConfigurada = horasConfiguradas.TryGetValue(empresa.Id, out var valor) && int.TryParse(valor, out var h)
                ? h : HoraPorDefecto;
            if (horaConfigurada != horaActual) continue;

            var yaEnviadoHoy = await db.Notificaciones.IgnoreQueryFilters().AnyAsync(
                n => n.EmpresaId == empresa.Id && n.Tipo == TiposNotificacion.ResumenDiario && n.CreadoEn >= inicioHoy, ct);
            if (yaEnviadoHoy) continue;

            // Llamada directa a los calculators (no vía IMediator/HTTP): son el
            // único lugar donde empresaId se pasa explícito en vez de salir del
            // JWT, y solo este servicio interno puede invocarlos así.
            var resumen = await PanelHoyCalculator.CalcularAsync(
                db, empresa.Id, sucursalId: null, incluirCostos: true, ct);
            db.Notificaciones.Add(new Notificacion
            {
                EmpresaId = empresa.Id,
                Tipo = TiposNotificacion.ResumenDiario,
                Destinatario = empresa.TelefonoWhatsapp!,
                Contenido = NotificacionTemplates.ResumenDiario(resumen),
            });

            var alertas = await PanelAlertasCalculator.CalcularAsync(db, empresa.Id, sucursalId: null, ct);

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
