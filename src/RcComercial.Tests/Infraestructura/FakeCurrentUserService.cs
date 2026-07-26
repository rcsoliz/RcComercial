using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Tests.Infraestructura;

/// <summary>ICurrentUserService falso y mutable: cada test arma el actor que necesita.</summary>
public class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UsuarioId { get; set; }
    public Guid? EmpresaId { get; set; }
    public Guid? SucursalId { get; set; }
    public HashSet<string> Permisos { get; set; } = [];

    public bool TienePermiso(string codigoPermiso) => Permisos.Contains(codigoPermiso);
}
