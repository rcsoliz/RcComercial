using RcComercial.Domain.Common;

namespace RcComercial.Domain.Entities;

/// <summary>Base del panel del dueño y del control de empleados.</summary>
public class SesionCaja : BaseEntity
{
    public Guid SucursalId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTimeOffset Apertura { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? Cierre { get; set; }
    public decimal MontoInicial { get; set; }
    public decimal? MontoCierreDeclarado { get; set; } // lo que el cajero cuenta
    public decimal? MontoCierreCalculado { get; set; } // lo que el sistema dice
    public string Estado { get; set; } = "ABIERTA";
}
