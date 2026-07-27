namespace RcComercial.Application.Proveedores;

public record ProveedorDto(
    Guid Id, string Nombre, string? Nit, string? TelefonoWhatsapp, int DiasCredito, int LeadTimeDias, bool Activo);
