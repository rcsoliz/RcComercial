using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RcComercial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPermisosClientesProveedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permiso",
                columns: new[] { "id", "codigo", "es_sensible", "modulo", "nombre" },
                values: new object[,]
                {
                    { (short)80, "clientes.crear_editar", false, "Clientes", "Crear y editar clientes" },
                    { (short)81, "clientes.eliminar", true, "Clientes", "Desactivar clientes" },
                    { (short)90, "proveedores.crear_editar", false, "Proveedores", "Crear y editar proveedores" },
                    { (short)91, "proveedores.eliminar", true, "Proveedores", "Desactivar proveedores" }
                });

            migrationBuilder.InsertData(
                table: "rol_permiso",
                columns: new[] { "permiso_id", "rol_id" },
                values: new object[,]
                {
                    { (short)80, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)81, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)90, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)91, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)80, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)81, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)90, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)91, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)80, new Guid("a0000000-0000-0000-0000-000000000003") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)80, new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)81, new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)90, new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)91, new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)80, new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)81, new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)90, new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)91, new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "rol_permiso",
                keyColumns: new[] { "permiso_id", "rol_id" },
                keyValues: new object[] { (short)80, new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                table: "permiso",
                keyColumn: "id",
                keyValue: (short)80);

            migrationBuilder.DeleteData(
                table: "permiso",
                keyColumn: "id",
                keyValue: (short)81);

            migrationBuilder.DeleteData(
                table: "permiso",
                keyColumn: "id",
                keyValue: (short)90);

            migrationBuilder.DeleteData(
                table: "permiso",
                keyColumn: "id",
                keyValue: (short)91);
        }
    }
}
