using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Entities;
using RcComercial.Infrastructure.Persistence;

namespace RcComercial.Api.Authorization;

/// <summary>
/// Audita automáticamente cualquier endpoint protegido por un permiso marcado
/// es_sensible=true (ej. ventas.anular, admin.usuarios): si la respuesta es
/// 2xx, registra la acción en auditoria. Se aplica una sola vez al grupo
/// "/api" en Program.cs; los endpoints nuevos no necesitan hacer nada extra
/// más que usar .RequireAuthorization(Permisos.XXX).
/// </summary>
public class AuditoriaPermisosSensiblesFilter(IMemoryCache cache) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        var http = context.HttpContext;

        var policiesDelEndpoint = http.GetEndpoint()?.Metadata
            .OfType<IAuthorizeData>()
            .Select(a => a.Policy)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
        if (policiesDelEndpoint is not { Count: > 0 }) return result;

        var statusCode = result is IStatusCodeHttpResult withCode
            ? withCode.StatusCode ?? StatusCodes.Status200OK
            : StatusCodes.Status200OK;
        if (statusCode is < 200 or >= 300) return result;

        var db = http.RequestServices.GetRequiredService<AppDbContext>();
        var permisosSensibles = await cache.GetOrCreateAsync("permisos_sensibles", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return (await db.Permisos.Where(p => p.EsSensible).Select(p => p.Codigo).ToListAsync())
                .ToHashSet();
        });

        var permisoSensible = policiesDelEndpoint.FirstOrDefault(p => permisosSensibles!.Contains(p!));
        if (permisoSensible is null) return result;

        var currentUser = http.RequestServices.GetRequiredService<ICurrentUserService>();
        if (currentUser.EmpresaId is not { } empresaId) return result;

        db.Auditorias.Add(new Auditoria
        {
            EmpresaId = empresaId,
            UsuarioId = currentUser.UsuarioId,
            Accion = permisoSensible!,
            Ip = http.Connection.RemoteIpAddress?.ToString(),
        });
        await db.SaveChangesAsync();

        return result;
    }
}
