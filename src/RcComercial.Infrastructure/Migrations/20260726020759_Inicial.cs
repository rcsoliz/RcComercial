using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RcComercial.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    padre_id = table.Column<Guid>(type: "uuid", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cliente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nit_ci = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tipo_documento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefono_whatsapp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cliente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "compra",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proveedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nro_factura_prov = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_compra", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "devolucion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuf_nota_credito = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    estado_siat = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devolucion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "empresa_configuracion",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clave = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    valor = table.Column<string>(type: "jsonb", maxLength: 200, nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empresa_configuracion", x => new { x.empresa_id, x.clave });
                });

            migrationBuilder.CreateTable(
                name: "lote",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lote", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "marca",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_marca", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    referencia_tipo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    referencia_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimiento_inventario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notificacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    destinatario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contenido = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    estado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    referencia_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    enviado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notificacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permiso",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    es_sensible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permiso", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "precio_historial",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    presentacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    precio_anterior = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    precio_nuevo = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_precio_historial", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "producto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    codigo_barras = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    marca_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unidad_base_id = table.Column<short>(type: "smallint", nullable: false),
                    costo_promedio = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    precio_base = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    stock_minimo = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    maneja_lote = table.Column<bool>(type: "boolean", nullable: false),
                    es_controlado = table.Column<bool>(type: "boolean", nullable: false),
                    permite_decimales = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    producto_maestro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actualizado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_producto", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "producto_maestro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    marca = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contenido = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rubro_id = table.Column<short>(type: "smallint", nullable: true),
                    principio_activo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    concentracion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    laboratorio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    verificado = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_producto_maestro", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proveedor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nit = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    telefono_whatsapp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dias_credito = table.Column<int>(type: "integer", nullable: false),
                    lead_time_dias = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actualizado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proveedor", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "receta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    medico_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    medico_matricula = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    paciente_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    paciente_ci = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha_receta = table.Column<DateOnly>(type: "date", nullable: false),
                    imagen_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receta", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revocado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reemplazado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_creacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    es_sistema = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rubro",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    usa_lotes_por_defecto = table.Column<bool>(type: "boolean", nullable: false),
                    usa_controlados = table.Column<bool>(type: "boolean", nullable: false),
                    usa_ficha_farmacia = table.Column<bool>(type: "boolean", nullable: false),
                    usa_decimales_por_defecto = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rubro", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "secuencia",
                columns: table => new
                {
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prefijo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    siguiente = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_secuencia", x => new { x.empresa_id, x.sucursal_id, x.tipo_documento });
                });

            migrationBuilder.CreateTable(
                name: "sesion_caja",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    apertura = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    monto_inicial = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    monto_cierre_declarado = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    monto_cierre_calculado = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    estado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sesion_caja", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cantidad = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transferencia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_origen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_destino_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    enviado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    recibido_por = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_envio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_recepcion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transferencia", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unidad_medida",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    abreviatura = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unidad_medida", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "venta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_caja_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    motivo_anulacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    anulada_por = table.Column<Guid>(type: "uuid", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuf = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cufd = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    estado_siat = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    enviada_whatsapp = table.Column<bool>(type: "boolean", nullable: false),
                    creado_offline = table.Column<bool>(type: "boolean", nullable: false),
                    sincronizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venta", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "compra_detalle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    compra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    presentacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cantidad = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    numero_lote = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_compra_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_compra_detalle_compra_compra_id",
                        column: x => x.compra_id,
                        principalTable: "compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "devolucion_detalle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    devolucion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_detalle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    reingresa_stock = table.Column<bool>(type: "boolean", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devolucion_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_devolucion_detalle_devolucion_devolucion_id",
                        column: x => x.devolucion_id,
                        principalTable: "devolucion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "producto_farmacia",
                columns: table => new
                {
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    principio_activo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    concentracion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    forma_farmaceutica = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    laboratorio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    registro_sanitario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    clasificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requiere_cadena_frio = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_producto_farmacia", x => x.producto_id);
                    table.ForeignKey(
                        name: "fk_producto_farmacia_producto_producto_id",
                        column: x => x.producto_id,
                        principalTable: "producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "producto_presentacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    factor = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    precio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    precio_mayorista = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    cantidad_min_mayorista = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    es_predeterminada = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actualizado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_producto_presentacion", x => x.id);
                    table.ForeignKey(
                        name: "fk_producto_presentacion_producto_producto_id",
                        column: x => x.producto_id,
                        principalTable: "producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rol_permiso",
                columns: table => new
                {
                    rol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permiso_id = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol_permiso", x => new { x.rol_id, x.permiso_id });
                    table.ForeignKey(
                        name: "fk_rol_permiso_permiso_permiso_id",
                        column: x => x.permiso_id,
                        principalTable: "permiso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rol_permiso_rol_rol_id",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    usuario_login = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    telefono_whatsapp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    intentos_fallidos = table.Column<short>(type: "smallint", nullable: false),
                    bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    debe_cambiar_password = table.Column<bool>(type: "boolean", nullable: false),
                    permisos_version = table.Column<int>(type: "integer", nullable: false),
                    ultimo_login = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuario_rol_rol_id",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "empresa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    rubro_id = table.Column<short>(type: "smallint", nullable: false),
                    telefono_whatsapp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empresa", x => x.id);
                    table.ForeignKey(
                        name: "fk_empresa_rubros_rubro_id",
                        column: x => x.rubro_id,
                        principalTable: "rubro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transferencia_detalle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transferencia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cantidad_base = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transferencia_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_transferencia_detalle_transferencia_transferencia_id",
                        column: x => x.transferencia_id,
                        principalTable: "transferencia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pago",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    monto = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    referencia_qr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pago", x => x.id);
                    table.ForeignKey(
                        name: "fk_pago_ventas_venta_id",
                        column: x => x.venta_id,
                        principalTable: "venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "venta_detalle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    presentacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cantidad = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venta_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_venta_detalle_venta_venta_id",
                        column: x => x.venta_id,
                        principalTable: "venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sucursal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    codigo_sucursal_siat = table.Column<int>(type: "integer", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sucursal", x => x.id);
                    table.ForeignKey(
                        name: "fk_sucursal_empresa_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permiso",
                columns: new[] { "id", "codigo", "es_sensible", "modulo", "nombre" },
                values: new object[,]
                {
                    { (short)10, "ventas.crear", false, "Ventas", "Registrar ventas" },
                    { (short)11, "ventas.anular", true, "Ventas", "Anular ventas" },
                    { (short)12, "ventas.descuento", true, "Ventas", "Aplicar descuentos" },
                    { (short)13, "ventas.ver_historial", false, "Ventas", "Ver historial de ventas" },
                    { (short)20, "caja.abrir_cerrar", false, "Caja", "Abrir y cerrar caja" },
                    { (short)21, "caja.ver_todas", false, "Caja", "Ver cajas de otros usuarios" },
                    { (short)30, "inventario.ver", false, "Inventario", "Consultar stock" },
                    { (short)31, "inventario.ajustar", true, "Inventario", "Ajustes y mermas de inventario" },
                    { (short)32, "inventario.ver_costos", true, "Inventario", "Ver costos y utilidades" },
                    { (short)40, "compras.crear", false, "Compras", "Registrar compras" },
                    { (short)41, "compras.anular", true, "Compras", "Anular compras" },
                    { (short)50, "productos.crear_editar", false, "Productos", "Crear y editar productos" },
                    { (short)51, "productos.eliminar", true, "Productos", "Desactivar productos" },
                    { (short)52, "productos.cambiar_precios", true, "Productos", "Modificar precios" },
                    { (short)60, "reportes.ver", false, "Reportes", "Ver reportes y panel del negocio" },
                    { (short)70, "admin.usuarios", true, "Administración", "Crear, editar y desactivar usuarios" },
                    { (short)71, "admin.roles", true, "Administración", "Configurar roles y permisos" },
                    { (short)72, "admin.configuracion", true, "Administración", "Configuración del negocio y facturación" },
                    { (short)73, "admin.sucursales", true, "Administración", "Gestionar sucursales" }
                });

            migrationBuilder.InsertData(
                table: "rol",
                columns: new[] { "id", "activo", "empresa_id", "es_sistema", "nombre" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-0000-0000-000000000001"), true, null, true, "Dueño" },
                    { new Guid("a0000000-0000-0000-0000-000000000002"), true, null, true, "Encargado" },
                    { new Guid("a0000000-0000-0000-0000-000000000003"), true, null, true, "Vendedor" }
                });

            migrationBuilder.InsertData(
                table: "rubro",
                columns: new[] { "id", "activo", "codigo", "nombre", "usa_controlados", "usa_decimales_por_defecto", "usa_ficha_farmacia", "usa_lotes_por_defecto" },
                values: new object[,]
                {
                    { (short)1, true, "ALMACEN", "Almacén / Tienda de barrio", false, false, false, false },
                    { (short)2, true, "FARMACIA", "Farmacia", true, false, true, true },
                    { (short)3, true, "FERRETERIA", "Ferretería", false, true, false, false },
                    { (short)4, true, "LICORERIA", "Licorería", false, false, false, false },
                    { (short)5, true, "MINIMARKET", "Minimarket", false, false, false, false }
                });

            migrationBuilder.InsertData(
                table: "rol_permiso",
                columns: new[] { "permiso_id", "rol_id" },
                values: new object[,]
                {
                    { (short)10, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)11, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)12, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)13, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)20, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)21, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)30, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)31, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)32, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)40, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)41, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)50, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)51, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)52, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)60, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)70, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)71, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)72, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)73, new Guid("a0000000-0000-0000-0000-000000000001") },
                    { (short)10, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)11, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)12, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)13, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)20, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)21, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)30, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)31, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)32, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)40, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)41, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)50, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)51, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)52, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)60, new Guid("a0000000-0000-0000-0000-000000000002") },
                    { (short)10, new Guid("a0000000-0000-0000-0000-000000000003") },
                    { (short)13, new Guid("a0000000-0000-0000-0000-000000000003") },
                    { (short)20, new Guid("a0000000-0000-0000-0000-000000000003") },
                    { (short)30, new Guid("a0000000-0000-0000-0000-000000000003") }
                });

            migrationBuilder.CreateIndex(
                name: "ix_cliente_empresa_id_telefono_whatsapp",
                table: "cliente",
                columns: new[] { "empresa_id", "telefono_whatsapp" });

            migrationBuilder.CreateIndex(
                name: "ix_compra_proveedor_id_fecha",
                table: "compra",
                columns: new[] { "proveedor_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_compra_detalle_compra_id",
                table: "compra_detalle",
                column: "compra_id");

            migrationBuilder.CreateIndex(
                name: "ix_devolucion_sucursal_id_numero",
                table: "devolucion",
                columns: new[] { "sucursal_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devolucion_venta_id",
                table: "devolucion",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_devolucion_detalle_devolucion_id",
                table: "devolucion_detalle",
                column: "devolucion_id");

            migrationBuilder.CreateIndex(
                name: "ix_empresa_rubro_id",
                table: "empresa",
                column: "rubro_id");

            migrationBuilder.CreateIndex(
                name: "ix_lote_fecha_vencimiento",
                table: "lote",
                column: "fecha_vencimiento");

            migrationBuilder.CreateIndex(
                name: "ix_lote_producto_id_numero",
                table: "lote",
                columns: new[] { "producto_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_inventario_producto_id_fecha",
                table: "movimiento_inventario",
                columns: new[] { "producto_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_inventario_sucursal_id_fecha",
                table: "movimiento_inventario",
                columns: new[] { "sucursal_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_notificacion_estado_creado_en",
                table: "notificacion",
                columns: new[] { "estado", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_pago_venta_id",
                table: "pago",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_permiso_codigo",
                table: "permiso",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_precio_historial_producto_id_fecha",
                table: "precio_historial",
                columns: new[] { "producto_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_producto_empresa_id_activo_nombre",
                table: "producto",
                columns: new[] { "empresa_id", "activo", "nombre" });

            migrationBuilder.CreateIndex(
                name: "ix_producto_empresa_id_codigo",
                table: "producto",
                columns: new[] { "empresa_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_producto_empresa_id_codigo_barras",
                table: "producto",
                columns: new[] { "empresa_id", "codigo_barras" });

            migrationBuilder.CreateIndex(
                name: "ix_producto_farmacia_principio_activo",
                table: "producto_farmacia",
                column: "principio_activo");

            migrationBuilder.CreateIndex(
                name: "ix_producto_maestro_codigo_barras",
                table: "producto_maestro",
                column: "codigo_barras",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_producto_presentacion_codigo_barras",
                table: "producto_presentacion",
                column: "codigo_barras");

            migrationBuilder.CreateIndex(
                name: "ix_producto_presentacion_producto_id",
                table: "producto_presentacion",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_receta_venta_id",
                table: "receta",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_usuario_id_expira_en",
                table: "refresh_token",
                columns: new[] { "usuario_id", "expira_en" });

            migrationBuilder.CreateIndex(
                name: "ix_rol_empresa_id_nombre",
                table: "rol",
                columns: new[] { "empresa_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rol_permiso_permiso_id",
                table: "rol_permiso",
                column: "permiso_id");

            migrationBuilder.CreateIndex(
                name: "ix_rubro_codigo",
                table: "rubro",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sesion_caja_sucursal_id_apertura",
                table: "sesion_caja",
                columns: new[] { "sucursal_id", "apertura" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_producto_id_sucursal_id",
                table: "stock",
                columns: new[] { "producto_id", "sucursal_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_sucursal_id_producto_id_lote_id",
                table: "stock",
                columns: new[] { "sucursal_id", "producto_id", "lote_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sucursal_empresa_id",
                table: "sucursal",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_transferencia_detalle_transferencia_id",
                table: "transferencia_detalle",
                column: "transferencia_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_empresa_id_usuario_login",
                table: "usuario",
                columns: new[] { "empresa_id", "usuario_login" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_rol_id",
                table: "usuario",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "ix_venta_empresa_id_estado_fecha",
                table: "venta",
                columns: new[] { "empresa_id", "estado", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_venta_empresa_id_fecha",
                table: "venta",
                columns: new[] { "empresa_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_venta_sucursal_id_numero",
                table: "venta",
                columns: new[] { "sucursal_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venta_detalle_producto_id",
                table: "venta_detalle",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "ix_venta_detalle_venta_id",
                table: "venta_detalle",
                column: "venta_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "cliente");

            migrationBuilder.DropTable(
                name: "compra_detalle");

            migrationBuilder.DropTable(
                name: "devolucion_detalle");

            migrationBuilder.DropTable(
                name: "empresa_configuracion");

            migrationBuilder.DropTable(
                name: "lote");

            migrationBuilder.DropTable(
                name: "marca");

            migrationBuilder.DropTable(
                name: "movimiento_inventario");

            migrationBuilder.DropTable(
                name: "notificacion");

            migrationBuilder.DropTable(
                name: "pago");

            migrationBuilder.DropTable(
                name: "precio_historial");

            migrationBuilder.DropTable(
                name: "producto_farmacia");

            migrationBuilder.DropTable(
                name: "producto_maestro");

            migrationBuilder.DropTable(
                name: "producto_presentacion");

            migrationBuilder.DropTable(
                name: "proveedor");

            migrationBuilder.DropTable(
                name: "receta");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "rol_permiso");

            migrationBuilder.DropTable(
                name: "secuencia");

            migrationBuilder.DropTable(
                name: "sesion_caja");

            migrationBuilder.DropTable(
                name: "stock");

            migrationBuilder.DropTable(
                name: "sucursal");

            migrationBuilder.DropTable(
                name: "transferencia_detalle");

            migrationBuilder.DropTable(
                name: "unidad_medida");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "venta_detalle");

            migrationBuilder.DropTable(
                name: "compra");

            migrationBuilder.DropTable(
                name: "devolucion");

            migrationBuilder.DropTable(
                name: "producto");

            migrationBuilder.DropTable(
                name: "permiso");

            migrationBuilder.DropTable(
                name: "empresa");

            migrationBuilder.DropTable(
                name: "transferencia");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "venta");

            migrationBuilder.DropTable(
                name: "rubro");
        }
    }
}
