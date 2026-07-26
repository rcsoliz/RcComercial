using System.Security.Claims;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Api.Services;

/// <summary>
/// Lee la identidad desde los claims del JWT. Mientras no exista
/// autenticación (fase 1 del roadmap), devuelve null y el query filter
/// multi-tenant no retorna datos: seguro por defecto.
/// </summary>
public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UsuarioId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public Guid? EmpresaId =>
        Guid.TryParse(User?.FindFirstValue("empresa_id"), out var id) ? id : null;

    public Guid? SucursalId =>
        Guid.TryParse(User?.FindFirstValue("sucursal_id"), out var id) ? id : null;

    public bool TienePermiso(string codigoPermiso) =>
        User?.HasClaim("permiso", codigoPermiso) ?? false;
}
