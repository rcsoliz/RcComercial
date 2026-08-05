using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RcComercial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVehiculos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "vehiculo_id",
                table: "venta",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "es_servicio",
                table: "producto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "proforma",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehiculo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    valida_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo_rechazo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proforma", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehiculo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    marca = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    modelo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    anio = table.Column<short>(type: "smallint", nullable: true),
                    color = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    numero_chasis = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehiculo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proforma_detalle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proforma_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    presentacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cantidad = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proforma_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_proforma_detalle_proforma_proforma_id",
                        column: x => x.proforma_id,
                        principalTable: "proforma",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_venta_vehiculo_id",
                table: "venta",
                column: "vehiculo_id");

            migrationBuilder.CreateIndex(
                name: "ix_proforma_cliente_id",
                table: "proforma",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_proforma_sucursal_id_numero",
                table: "proforma",
                columns: new[] { "sucursal_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proforma_vehiculo_id",
                table: "proforma",
                column: "vehiculo_id");

            migrationBuilder.CreateIndex(
                name: "ix_proforma_detalle_proforma_id",
                table: "proforma_detalle",
                column: "proforma_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehiculo_cliente_id",
                table: "vehiculo",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehiculo_empresa_id_placa",
                table: "vehiculo",
                columns: new[] { "empresa_id", "placa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "proforma_detalle");

            migrationBuilder.DropTable(
                name: "vehiculo");

            migrationBuilder.DropTable(
                name: "proforma");

            migrationBuilder.DropIndex(
                name: "ix_venta_vehiculo_id",
                table: "venta");

            migrationBuilder.DropColumn(
                name: "vehiculo_id",
                table: "venta");

            migrationBuilder.DropColumn(
                name: "es_servicio",
                table: "producto");
        }
    }
}
