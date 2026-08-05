using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RcComercial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRubroTallerMecanico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "es_tipo_servicio",
                table: "rubro",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "rubro",
                keyColumn: "id",
                keyValue: (short)1,
                column: "es_tipo_servicio",
                value: false);

            migrationBuilder.UpdateData(
                table: "rubro",
                keyColumn: "id",
                keyValue: (short)2,
                column: "es_tipo_servicio",
                value: false);

            migrationBuilder.UpdateData(
                table: "rubro",
                keyColumn: "id",
                keyValue: (short)3,
                column: "es_tipo_servicio",
                value: false);

            migrationBuilder.UpdateData(
                table: "rubro",
                keyColumn: "id",
                keyValue: (short)4,
                column: "es_tipo_servicio",
                value: false);

            migrationBuilder.UpdateData(
                table: "rubro",
                keyColumn: "id",
                keyValue: (short)5,
                column: "es_tipo_servicio",
                value: false);

            migrationBuilder.InsertData(
                table: "rubro",
                columns: new[] { "id", "activo", "codigo", "es_tipo_servicio", "nombre", "usa_controlados", "usa_decimales_por_defecto", "usa_ficha_farmacia", "usa_lotes_por_defecto" },
                values: new object[] { (short)6, true, "TALLER", true, "Taller mecánico", false, false, false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rubro",
                keyColumn: "id",
                keyValue: (short)6);

            migrationBuilder.DropColumn(
                name: "es_tipo_servicio",
                table: "rubro");
        }
    }
}
