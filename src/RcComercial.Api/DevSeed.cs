using Microsoft.EntityFrameworkCore;
using RcComercial.Application.Common;
using RcComercial.Application.Common.Interfaces;
using RcComercial.Domain.Common;
using RcComercial.Domain.Entities;
using RcComercial.Infrastructure.Persistence;

namespace RcComercial.Api;

/// <summary>Empresa + sucursal + usuarios demo para probar login/RBAC en desarrollo.</summary>
public static class DevSeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await db.Empresas.AnyAsync()) return;

        var empresa = new Empresa { Nombre = "Almacén Demo", RubroId = 1 };
        var sucursal = new Sucursal { EmpresaId = empresa.Id, Nombre = "Sucursal Central" };

        db.Empresas.Add(empresa);
        db.Sucursales.Add(sucursal);
        db.Usuarios.AddRange(
            new Usuario
            {
                EmpresaId = empresa.Id,
                SucursalId = sucursal.Id,
                Nombre = "Dueño Demo",
                UsuarioLogin = "admin",
                PasswordHash = hasher.Hash("Admin123!"),
                RolId = RolesSistema.Dueno,
                DebeCambiarPassword = false,
            },
            new Usuario
            {
                EmpresaId = empresa.Id,
                SucursalId = sucursal.Id,
                Nombre = "Vendedor Demo",
                UsuarioLogin = "vendedor",
                PasswordHash = hasher.Hash("Vendedor123!"),
                RolId = RolesSistema.Vendedor,
                DebeCambiarPassword = false,
            });

        // Empresa "de sistema" que hospeda al superadmin: Activo = true a
        // propósito (si no, el bloqueo de "empresa suspendida no loguea"
        // dejaría afuera al propio superadmin) — "inactiva para comercio"
        // significa que nunca se le crean productos/ventas, no que activo=false.
        var empresaPlataforma = new Empresa { Nombre = "SysCenterS Plataforma", RubroId = 1, Activo = true };
        var passwordSuperadmin = GeneradorPassword.Temporal();
        db.Empresas.Add(empresaPlataforma);
        db.Usuarios.Add(new Usuario
        {
            EmpresaId = empresaPlataforma.Id,
            SucursalId = null,
            Nombre = "Superadmin",
            UsuarioLogin = "superadmin",
            PasswordHash = hasher.Hash(passwordSuperadmin),
            RolId = RolesSistema.Dueno,
            DebeCambiarPassword = true,
            EsSuperadmin = true,
        });

        await db.SaveChangesAsync();

        Console.WriteLine("============================================================");
        Console.WriteLine(" Superadmin de plataforma sembrado (solo entorno Development)");
        Console.WriteLine($"   usuario:  superadmin");
        Console.WriteLine($"   password: {passwordSuperadmin}  (temporal — pedirá cambiarla al entrar)");
        Console.WriteLine("============================================================");
    }
}
