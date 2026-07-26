using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RcComercial.Application.Auth;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Infrastructure.Auth;
using RcComercial.Infrastructure.BackgroundJobs;
using RcComercial.Infrastructure.Persistence;
using RcComercial.Infrastructure.Persistence.Interceptors;
using RcComercial.Infrastructure.Whatsapp;

namespace RcComercial.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .UseSnakeCaseNamingConvention() // C# PascalCase -> BD snake_case automático
                   .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<WhatsappCloudApiSettings>(configuration.GetSection("Whatsapp:CloudApi"));
        services.Configure<NotificacionDispatcherSettings>(configuration.GetSection("Whatsapp"));
        if (string.Equals(configuration["Whatsapp:Proveedor"], "CloudApi", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IWhatsappSender, WhatsappCloudApiSender>();
        }
        else
        {
            services.AddScoped<IWhatsappSender, WaLinkSender>();
        }

        services.AddHostedService<ResumenDiarioBackgroundService>();
        services.AddHostedService<NotificacionDispatcherBackgroundService>();

        return services;
    }
}
