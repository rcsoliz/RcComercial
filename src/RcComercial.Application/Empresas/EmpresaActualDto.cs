namespace RcComercial.Application.Empresas;

public record EmpresaActualDto(
    Guid Id,
    string Nombre,
    string? Nit,
    string? TelefonoWhatsapp,
    short RubroId,
    string RubroNombre,
    bool UsaLotesPorDefecto,
    bool UsaControlados,
    bool UsaFichaFarmacia,
    bool UsaDecimalesPorDefecto);
