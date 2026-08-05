using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RcComercial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPermisosTallerMecanico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permiso",
                columns: new[] { "id", "codigo", "es_sensible", "modulo", "nombre" },
                values: new object[,]
                {
                    { (short)100, "proformas.crear", false, "Proformas", "Crear y cotizar proformas" },
                    { (short)101, "proformas.anular", true, "Proformas", "Rechazar/anular proformas" },
                    { (short)110, "vehiculos.crear_editar", false, "Vehículos", "Crear y editar vehículos" },
                    { (short)111, "vehiculos.eliminar", true, "Vehículos", "Desactivar vehículos" }
                });

            migrationBuilder.InsertData(
                table: "rol_permiso",
                columns: new[] { "permiso_id", "rol_id" },
                values: new object[,]
                {
                    { (short)100, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)101, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)110, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)111, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)100, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)101, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)110, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)111, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)100, new Guid("a0000000-0000-0000-0000-000000000003") },
                    { (short)110, new Guid("a0000000-0000-0000-0000-000000000003") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)100, new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)101, new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)110, new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)111, new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)100, new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)101, new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)110, new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)111, new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)100, new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)110, new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                table: "permiso",
                keyColumn: "id",
                keyValue: (short)100);

            migrationBuilder.DeleteData(
                table: "permiso",
                keyColumn: "id",
                keyValue: (short)101);

            migrationBuilder.DeleteData(
                table: "permiso",
                keyColumn: "id",
                keyValue: (short)110);

            migrationBuilder.DeleteData(
                table: "permiso",
                keyColumn: "id",
                keyValue: (short)111);
        }
    }
}
