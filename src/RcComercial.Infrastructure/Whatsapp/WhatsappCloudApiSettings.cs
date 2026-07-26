namespace RcComercial.Infrastructure.Whatsapp;

public class WhatsappCloudApiSettings
{
    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string ApiVersion { get; set; } = "v20.0";
}
