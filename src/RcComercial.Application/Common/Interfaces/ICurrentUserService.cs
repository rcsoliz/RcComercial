namespace RcComercial.Application.Common.Interfaces;

/// <summary>
/// Contexto del usuario autenticado, leído SIEMPRE de los claims del JWT.
/// REGLA DE SEGURIDAD: EmpresaId jamás viene del request (body/URL);
/// el query filter multi-tenant del DbContext depende de este servicio.
/// </summary>
public interface ICurrentUserService
{
    Guid? UsuarioId { get; }
    Guid? EmpresaId { get; }
    Guid? SucursalId { get; }
    bool TienePermiso(string codigoPermiso);
}
