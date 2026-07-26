using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Infrastructure.Whatsapp;

/// <summary>
/// Envío real vía Meta WhatsApp Cloud API. Implementado según el contrato
/// público (POST /{phone-number-id}/messages, Bearer token), pero NO
/// probado contra la infraestructura real de Meta: el proyecto no tiene
/// credenciales de WhatsApp Business todavía. Se activa vía
/// Whatsapp:Proveedor=CloudApi + Whatsapp:CloudApi:PhoneNumberId/AccessToken.
/// </summary>
public class WhatsappCloudApiSender(HttpClient httpClient, IOptions<WhatsappCloudApiSettings> options)
    : IWhatsappSender
{
    private readonly WhatsappCloudApiSettings _settings = options.Value;

    public async Task<WhatsappEnvioResultado> EnviarAsync(
        string destinatario, string mensaje, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.PhoneNumberId) || string.IsNullOrWhiteSpace(_settings.AccessToken))
        {
            return new WhatsappEnvioResultado(
                false, null, "Whatsapp:CloudApi no está configurado (PhoneNumberId/AccessToken).");
        }

        var telefono = new string(destinatario.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(telefono))
            return new WhatsappEnvioResultado(false, null, "Destinatario inválido o vacío.");

        var request = new HttpRequestMessage(
            HttpMethod.Post, $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken) },
            Content = JsonContent.Create(new
            {
                messaging_product = "whatsapp",
                to = telefono,
                type = "text",
                text = new { body = mensaje },
            }),
        };

        try
        {
            var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return new WhatsappEnvioResultado(true, null, null);

            var detalle = await response.Content.ReadAsStringAsync(ct);
            return new WhatsappEnvioResultado(false, null, $"Meta Cloud API respondió {(int)response.StatusCode}: {detalle}");
        }
        catch (Exception ex)
        {
            return new WhatsappEnvioResultado(false, null, ex.Message);
        }
    }
}
