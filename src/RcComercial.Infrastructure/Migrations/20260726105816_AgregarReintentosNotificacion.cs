using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RcComercial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReintentosNotificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "enlace_generado",
                table: "notificacion",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "intentos",
                table: "notificacion",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "proximo_intento_en",
                table: "notificacion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_notificacion_estado_proximo_intento_en",
                table: "notificacion",
                columns: new[] { "estado", "proximo_intento_en" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notificacion_estado_proximo_intento_en",
                table: "notificacion");

            migrationBuilder.DropColumn(
                name: "enlace_generado",
                table: "notificacion");

            migrationBuilder.DropColumn(
                name: "intentos",
                table: "notificacion");

            migrationBuilder.DropColumn(
                name: "proximo_intento_en",
                table: "notificacion");
        }
    }
}
