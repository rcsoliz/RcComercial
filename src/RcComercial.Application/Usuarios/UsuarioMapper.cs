using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Application.Usuarios;

internal static class UsuarioMapper
{
    public static Task<UsuarioDto?> ObtenerDtoAsync(IApplicationDbContext db, Guid id, CancellationToken ct) =>
        (
            from u in db.Usuarios
            join r in db.Roles on u.RolId equals r.Id
            join s in db.Sucursales on u.SucursalId equals s.Id into sucursales
            from s in sucursales.DefaultIfEmpty()
            where u.Id == id
            select new UsuarioDto(
                u.Id, u.Nombre, u.UsuarioLogin, u.RolId, r.Nombre,
                u.SucursalId, s != null ? s.Nombre : null, u.TelefonoWhatsapp, u.Activo,
                u.UltimoLogin, u.DebeCambiarPassword)
        ).FirstOrDefaultAsync(ct);
}
