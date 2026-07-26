using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RcComercial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCatalogoProductos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "marca",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "unidad_medida",
                columns: new[] { "id", "abreviatura", "nombre" },
                values: new object[,]
                {
                    { (short)1, "UND", "Unidad" },
                    { (short)2, "KG", "Kilogramo" },
                    { (short)3, "GR", "Gramo" },
                    { (short)4, "LT", "Litro" },
                    { (short)5, "ML", "Mililitro" },
                    { (short)6, "M", "Metro" },
                    { (short)7, "CM", "Centímetro" },
                    { (short)8, "CJA", "Caja" },
                    { (short)9, "TAB", "Tableta" },
                    { (short)10, "PAR", "Par" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_producto_nombre",
                table: "producto",
                column: "nombre")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_producto_nombre",
                table: "producto");

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)7);

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)8);

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)9);

            migrationBuilder.DeleteData(
                table: "unidad_medida",
                keyColumn: "id",
                keyValue: (short)10);

            migrationBuilder.DropColumn(
                name: "activo",
                table: "marca");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
