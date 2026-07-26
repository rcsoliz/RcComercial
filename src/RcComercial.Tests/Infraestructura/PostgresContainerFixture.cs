using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using RcComercial.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RcComercial.Tests.Infraestructura;

/// <summary>
/// Un contenedor Postgres por corrida de tests: aplica migraciones EF una sola
/// vez y deja un Respawner listo para devolver la base a un estado limpio
/// antes de cada test (ver <see cref="PruebaBase"/>).
/// </summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer _contenedor = null!;
    private NpgsqlConnection _conexionRespawn = null!;
    private Respawner _respawner = null!;

    public string ConnectionString => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        _contenedor = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("rc_comercial_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _contenedor.StartAsync();

        await using (var db = FabricaContexto.Crear(ConnectionString, new FakeCurrentUserService()))
        {
            await db.Database.MigrateAsync();
        }

        // auditoria está ExcludeFromMigrations (tabla particionada, ver
        // AuditoriaConfig): en producción se crea con database/02_revision_dba.sql.
        // Para tests se replica esa DDL con una partición DEFAULT en vez de
        // las mensuales fijas del script original (ver hallazgo reportado
        // sobre ese script: solo cubre 2026-07..09 y no tiene DEFAULT).
        await CrearTablaAuditoriaAsync();

        _conexionRespawn = new NpgsqlConnection(ConnectionString);
        await _conexionRespawn.OpenAsync();

        _respawner = await Respawner.CreateAsync(_conexionRespawn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore =
            [
                "__ef_migrations_history",
                "rubro",
                "permiso",
                "rol",
                "rol_permiso",
                "unidad_medida",
            ],
        });
    }

    private async Task CrearTablaAuditoriaAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            DROP TABLE IF EXISTS auditoria;

            CREATE TABLE auditoria (
                id              BIGINT GENERATED ALWAYS AS IDENTITY,
                empresa_id      UUID NOT NULL,
                usuario_id      UUID,
                accion          VARCHAR(50) NOT NULL,
                entidad         VARCHAR(50),
                entidad_id      UUID,
                detalle         JSONB,
                ip              VARCHAR(45),
                fecha           TIMESTAMPTZ NOT NULL DEFAULT now(),
                PRIMARY KEY (id, fecha)
            ) PARTITION BY RANGE (fecha);

            CREATE TABLE auditoria_default PARTITION OF auditoria DEFAULT;

            CREATE INDEX ix_auditoria_empresa_fecha ON auditoria (empresa_id, fecha);
            CREATE INDEX ix_auditoria_entidad ON auditoria (entidad, entidad_id);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public Task ResetearAsync() => _respawner.ResetAsync(_conexionRespawn);

    public async Task DisposeAsync()
    {
        await _conexionRespawn.DisposeAsync();
        await _contenedor.DisposeAsync();
    }
}
