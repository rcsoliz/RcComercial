namespace RcComercial.Application.Common.Interfaces;

public record WhatsappEnvioResultado(bool Exitoso, string? EnlaceGenerado, string? Error);

public interface IWhatsappSender
{
    Task<WhatsappEnvioResultado> EnviarAsync(string destinatario, string mensaje, CancellationToken ct = default);
}
