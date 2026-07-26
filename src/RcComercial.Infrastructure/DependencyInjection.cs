using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RcComercial.Application.Auth;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Infrastructure.Auth;
using RcComercial.Infrastructure.Persistence;
using RcComercial.Infrastructure.Persistence.Interceptors;

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

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
