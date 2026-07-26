using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Infrastructure.Whatsapp;

/// <summary>
/// Genera un enlace wa.me con el texto prellenado; no envía nada por red.
/// El "envío" se considera completo al generar el enlace — el clic real del
/// cajero (Fase 7, frontend) queda fuera del alcance del backend.
/// </summary>
public class WaLinkSender : IWhatsappSender
{
    public Task<WhatsappEnvioResultado> EnviarAsync(
        string destinatario, string mensaje, CancellationToken ct = default)
    {
        var telefono = new string(destinatario.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(telefono))
            return Task.FromResult(new WhatsappEnvioResultado(false, null, "Destinatario inválido o vacío."));

        var enlace = $"https://wa.me/{telefono}?text={Uri.EscapeDataString(mensaje)}";
        return Task.FromResult(new WhatsappEnvioResultado(true, enlace, null));
    }
}
