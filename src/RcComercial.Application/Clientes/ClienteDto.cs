namespace RcComercial.Application.Clientes;

public record ClienteDto(
    Guid Id, string Nombre, string? NitCi, string TipoDocumento,
    string? TelefonoWhatsapp, string? Email, bool Activo);

public record ClienteListItemDto(
    Guid Id, string Nombre, string? NitCi, string TipoDocumento,
    string? TelefonoWhatsapp, bool Activo, decimal ComprasUltimoMes);
