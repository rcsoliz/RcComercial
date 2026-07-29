using System.Security.Claims;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RcComercial.Api;
using RcComercial.Api.Authorization;
using RcComercial.Api.Endpoints;
using RcComercial.Api.Services;
using RcComercial.Application;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Infrastructure;
using RcComercial.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddMemoryCache();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Secret.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSecret)),
        };
        options.Events = new JwtBearerEvents
        {
            // permisos_version del claim vs BD (cache 5 min): si el dueño
            // cambió los permisos del rol, el JWT viejo deja de servir sin
            // esperar a que expire.
            OnTokenValidated = async context =>
            {
                var usuarioIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var permisosVersionClaim = context.Principal?.FindFirstValue("permisos_version");
                if (!Guid.TryParse(usuarioIdClaim, out var usuarioId) ||
                    !int.TryParse(permisosVersionClaim, out var permisosVersionDelToken))
                {
                    context.Fail("Token inválido.");
                    return;
                }

                var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                var permisosVersionActual = await cache.GetOrCreateAsync(
                    $"usuario_permisos_version:{usuarioId}",
                    async entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        return await db.Usuarios.IgnoreQueryFilters()
                            .Where(u => u.Id == usuarioId && u.Activo)
                            .Select(u => (int?)u.PermisosVersion)
                            .FirstOrDefaultAsync();
                    });

                if (permisosVersionActual is null || permisosVersionActual != permisosVersionDelToken)
                {
                    context.Fail("Los permisos del usuario cambiaron; vuelva a iniciar sesión.");
                    return;
                }

                // Con contraseña temporal, el token solo sirve para cambiarla:
                // el bloqueo real vive acá (el redirect del frontend es solo
                // comodidad, no seguridad — un cliente hostil podría ignorarlo).
                var debeCambiarPassword = context.Principal?.FindFirstValue("debe_cambiar_password") == "true";
                var esRutaDeCambioDePassword = context.HttpContext.Request.Path
                    .Equals("/api/auth/cambiar-password-obligatorio", StringComparison.OrdinalIgnoreCase);
                if (debeCambiarPassword && !esRutaDeCambioDePassword)
                    context.Fail("Debe cambiar su contraseña antes de continuar.");
            },
        };
    });

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermisoPolicyProvider>();
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            []
        },
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (error is ValidationException validationEx)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            // ValidationException(string) — el patrón "throw new ValidationException(mensaje)"
            // usado en varios handlers (CrearVenta, CrearCompra, etc.) — deja
            // Errors VACÍO; solo Message trae el texto real. Sin este fallback
            // el cliente recibía { errores: [] } y perdía el motivo.
            var errores = validationEx.Errors.Any()
                ? validationEx.Errors.Select(e => new { campo = e.PropertyName, mensaje = e.ErrorMessage })
                : new[] { new { campo = "", mensaje = validationEx.Message } }.AsEnumerable();
            await context.Response.WriteAsJsonAsync(new { errores });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Ocurrió un error inesperado." });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await DevSeed.SeedAsync(app.Services);
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { estado = "ok", fecha = DateTimeOffset.UtcNow }));

var api = app.MapGroup("/api").AddEndpointFilter<AuditoriaPermisosSensiblesFilter>();
api.MapAuthEndpoints();
api.MapSucursalesEndpoints();
api.MapEmpresasEndpoints();
api.MapUsuariosEndpoints();
api.MapRolesEndpoints();
api.MapConfiguracionEndpoints();
api.MapUnidadesMedidaEndpoints();
api.MapProductosEndpoints();
api.MapCategoriasEndpoints();
api.MapMarcasEndpoints();
api.MapVentasEndpoints();
api.MapClientesEndpoints();
api.MapCajaEndpoints();
api.MapDevolucionesEndpoints();
api.MapPanelEndpoints();
api.MapComprasEndpoints();
api.MapProveedoresEndpoints();
api.MapSyncEndpoints();
api.MapPlataformaEndpoints();

app.Run();

// Marcador requerido por WebApplicationFactory<Program> (tests HTTP reales
// de endpoints, ver RcComercial.Tests/Infraestructura/ApiWebFactory.cs):
// un Program de top-level statements no es referenciable desde afuera sin esto.
public partial class Program { }
