namespace RcComercial.Application.Configuracion;

/// <summary>
/// Forma tipada de empresa_configuracion (clave-valor en BD) para el
/// formulario de ConfiguracionView. Agregar un campo nuevo acá = una clave
/// más, no un ALTER TABLE.
/// </summary>
public record ConfiguracionDto(bool PermiteStockNegativo, int HoraResumenWhatsapp);
