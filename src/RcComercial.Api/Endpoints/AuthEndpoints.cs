using RcComercial.Application.Auth;
using RcComercial.Application.Common.Interfaces;

namespace RcComercial.Api.Endpoints;

public record LoginRequest(string UsuarioLogin, string Password);

public record RefreshRequest(string RefreshToken);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").RequireRateLimiting("auth");

        group.MapPost("/login", async (LoginRequest request, IAuthService authService, HttpContext http) =>
        {
            var ip = http.Connection.RemoteIpAddress?.ToString();
            var userAgent = http.Request.Headers.UserAgent.ToString();
            var result = await authService.LoginAsync(request.UsuarioLogin, request.Password, ip, userAgent);

            if (!result.Exitoso)
            {
                return result.Error == LoginError.CuentaBloqueada
                    ? Results.Json(
                        new { error = "cuenta_bloqueada", bloqueadoHasta = result.BloqueadoHasta },
                        statusCode: StatusCodes.Status423Locked)
                    : Results.Unauthorized();
            }

            return Results.Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiraEn = result.ExpiraEn,
            });
        });

        group.MapPost("/refresh", async (RefreshRequest request, IAuthService authService, HttpContext http) =>
        {
            var ip = http.Connection.RemoteIpAddress?.ToString();
            var userAgent = http.Request.Headers.UserAgent.ToString();
            var result = await authService.RefreshAsync(request.RefreshToken, ip, userAgent);

            if (!result.Exitoso) return Results.Unauthorized();

            return Results.Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiraEn = result.ExpiraEn,
            });
        });

        group.MapGet("/me", (ICurrentUserService currentUser, HttpContext http) => Results.Ok(new
        {
            usuarioId = currentUser.UsuarioId,
            empresaId = currentUser.EmpresaId,
            sucursalId = currentUser.SucursalId,
            permisos = http.User.FindAll("permiso").Select(c => c.Value),
        })).RequireAuthorization();
    }
}
