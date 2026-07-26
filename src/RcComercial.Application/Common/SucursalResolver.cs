using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Common;

/// <summary>
/// Resuelve a qué sucursal aplica una operación cuando el usuario no tiene
/// una fija (SucursalId null = acceso a todas): usa la solicitada
/// explícitamente, o la del usuario, o la única sucursal activa de la
/// empresa. Null = ambiguo (hay más de una y no se especificó cuál) o la
/// sucursal solicitada no existe / no es de la empresa actual.
/// </summary>
public static class SucursalResolver
{
    public static async Task<Guid?> ResolverAsync(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        Guid? sucursalIdSolicitada,
        CancellationToken ct)
    {
        if (sucursalIdSolicitada is { } explicita)
        {
            // Sucursal tiene ITenantEntity: si no es de la empresa actual, el
            // filtro global la vuelve invisible aquí y AnyAsync da false.
            var perteneceALaEmpresa = await db.Sucursales.AnyAsync(s => s.Id == explicita, ct);
            return perteneceALaEmpresa ? explicita : null;
        }
        if (currentUser.SucursalId is { } delUsuario) return delUsuario;

        var sucursales = await db.Sucursales.Where(s => s.Activo).Select(s => s.Id).ToListAsync(ct);
        return sucursales.Count == 1 ? sucursales[0] : null;
    }
}
