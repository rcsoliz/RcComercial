using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RcComercial.Tests.Infraestructura;

/// <summary>
/// Levanta el Program.cs REAL (endpoints, autenticación JWT, manejador de
/// excepciones) contra el MISMO Postgres de Testcontainers que usa el resto
/// de la suite. Necesario solo para probar comportamiento que vive en la
/// capa HTTP (p. ej. que un endpoint descarta un campo del body antes de
/// llegar al handler) — eso no se puede demostrar enviando el comando
/// directo por MediatR con EnviarComoAsync.
/// </summary>
public class ApiWebFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" (no "Development"): evita DevSeed y Swagger, sin tocar
        // el manejador de excepciones ni la autenticación, que no dependen
        // del entorno.
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
            });
        });
    }
}
