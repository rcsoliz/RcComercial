using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Infrastructure.Persistence;
using RcComercial.Infrastructure.Persistence.Interceptors;

namespace RcComercial.Tests.Infraestructura;

public static class FabricaContexto
{
    /// <summary>
    /// Una instancia de AppDbContext por actor: el query filter multi-tenant
    /// lee el ICurrentUserService de ESTA instancia (ver AppDbContext.OnModelCreating),
    /// así que cada "quién pregunta" necesita su propio contexto.
    /// </summary>
    public static AppDbContext Crear(string connectionString, ICurrentUserService currentUser)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditableEntityInterceptor(currentUser))
            .Options;

        return new AppDbContext(options, currentUser);
    }
}
