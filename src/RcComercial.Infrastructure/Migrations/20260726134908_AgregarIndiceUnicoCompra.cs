using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RcComercial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIndiceUnicoCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_compra_sucursal_id_numero",
                table: "compra",
                columns: new[] { "sucursal_id", "numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_compra_sucursal_id_numero",
                table: "compra");
        }
    }
}
