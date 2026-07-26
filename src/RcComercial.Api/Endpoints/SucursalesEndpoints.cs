using Microsoft.EntityFrameworkCore;
using RcComercial.Domain.Common;
using RcComercial.Infrastructure.Persistence;

namespace RcComercial.Api.Endpoints;

/// <summary>
/// Endpoint mínimo para listar sucursales de la empresa actual. Sirve además
/// como verificación de la Fase 1: prueba en runtime que el query filter
/// multi-tenant y las policies por permiso funcionan de punta a punta,
/// sin adelantar alcance del módulo de productos (Fase 2).
/// </summary>
public static class SucursalesEndpoints
{
    public static void MapSucursalesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sucursales", async (AppDbContext db) =>
        {
            var sucursales = await db.Sucursales
                .Where(s => s.Activo)
                .Select(s => new { s.Id, s.Nombre, s.Direccion })
                .ToListAsync();
            return Results.Ok(sucursales);
        }).RequireAuthorization(Permisos.AdminSucursales);
    }
}
