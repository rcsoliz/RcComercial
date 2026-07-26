using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RcComercial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposFacturacionSin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "actividad_economica",
                table: "sucursal",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "codigo_producto_sin",
                table: "producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "codigo_unidad_sin",
                table: "producto",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "actividad_economica",
                table: "sucursal");

            migrationBuilder.DropColumn(
                name: "codigo_producto_sin",
                table: "producto");

            migrationBuilder.DropColumn(
                name: "codigo_unidad_sin",
                table: "producto");
        }
    }
}
