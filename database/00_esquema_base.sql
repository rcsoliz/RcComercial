-- ============================================================================
-- ESQUEMA DE BASE DE DATOS: Sistema comercial multi-rubro (Almacén / Farmacia / Ferretería)
-- Motor: PostgreSQL 15+  (portable a SQL Server: UUID -> UNIQUEIDENTIFIER,
--        TIMESTAMPTZ -> DATETIMEOFFSET, gen_random_uuid() -> NEWID())
-- Diseño: multi-tenant (una BD, todas las empresas), offline-first (UUIDs cliente)
-- ============================================================================

-- ============================================================================
-- 1. NÚCLEO MULTI-TENANT
-- ============================================================================

CREATE TABLE empresa (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre          VARCHAR(150) NOT NULL,
    nit             VARCHAR(20),
    rubro           VARCHAR(20) NOT NULL CHECK (rubro IN ('ALMACEN','FARMACIA','FERRETERIA')),
    telefono_whatsapp VARCHAR(20),          -- destino del resumen nocturno al dueño
    activo          BOOLEAN NOT NULL DEFAULT TRUE,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE sucursal (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    nombre          VARCHAR(100) NOT NULL,
    direccion       VARCHAR(200),
    codigo_sucursal_siat INT,               -- código de sucursal registrado en el SIAT
    activo          BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE usuario (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    sucursal_id     UUID REFERENCES sucursal(id),      -- NULL = acceso a todas
    nombre          VARCHAR(100) NOT NULL,
    usuario_login   VARCHAR(50) NOT NULL,
    password_hash   VARCHAR(200) NOT NULL,
    rol             VARCHAR(20) NOT NULL CHECK (rol IN ('DUENO','ADMIN','CAJERO','ALMACENERO')),
    telefono_whatsapp VARCHAR(20),
    activo          BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE (empresa_id, usuario_login)
);

-- ============================================================================
-- 2. CATÁLOGO DE PRODUCTOS
-- ============================================================================

CREATE TABLE categoria (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    nombre          VARCHAR(100) NOT NULL,
    padre_id        UUID REFERENCES categoria(id),     -- jerarquía: Analgésicos > Antiinflamatorios
    activo          BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE marca (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    nombre          VARCHAR(100) NOT NULL
);

-- Catálogo global (no por empresa): UND, KG, GR, M, CM, LT, ML, CJA, TAB...
CREATE TABLE unidad_medida (
    id              SMALLINT PRIMARY KEY,
    nombre          VARCHAR(50) NOT NULL,
    abreviatura     VARCHAR(10) NOT NULL
);

CREATE TABLE producto (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id        UUID NOT NULL REFERENCES empresa(id),
    codigo            VARCHAR(30),                     -- código interno
    codigo_barras     VARCHAR(30),                     -- EAN del producto suelto / unidad base
    nombre            VARCHAR(200) NOT NULL,
    categoria_id      UUID REFERENCES categoria(id),
    marca_id          UUID REFERENCES marca(id),
    unidad_base_id    SMALLINT NOT NULL REFERENCES unidad_medida(id),
    -- REGLA DE ORO: todo stock, movimiento y cantidad_base se expresa en unidad_base.
    -- Farmacia: la tableta. Ferretería: el metro / kilo / unidad.
    costo_promedio    NUMERIC(14,4) NOT NULL DEFAULT 0,  -- costo promedio ponderado por unidad base
    precio_base       NUMERIC(14,2) NOT NULL DEFAULT 0,  -- precio de venta por unidad base
    stock_minimo      NUMERIC(14,3) NOT NULL DEFAULT 0,  -- dispara alerta WhatsApp + sugerido de compra
    -- Flags que activan comportamiento por producto (no por sistema):
    maneja_lote       BOOLEAN NOT NULL DEFAULT FALSE,  -- TRUE: stock detallado por lote + FEFO
    es_controlado     BOOLEAN NOT NULL DEFAULT FALSE,  -- TRUE: la venta exige registro de receta
    permite_decimales BOOLEAN NOT NULL DEFAULT FALSE,  -- TRUE: vender 2.5 kg, 3.75 m
    activo            BOOLEAN NOT NULL DEFAULT TRUE,
    creado_en         TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (empresa_id, codigo)
);

CREATE INDEX ix_producto_busqueda ON producto (empresa_id, activo, nombre);
CREATE INDEX ix_producto_barras   ON producto (empresa_id, codigo_barras);

-- Presentaciones: cómo se compra/vende el producto. Resuelve a la vez el
-- fraccionamiento de farmacia (caja/blíster/tableta) y el multi-unidad de
-- ferretería (rollo/metro, quintal/kilo) y el precio mayorista por cantidad.
CREATE TABLE producto_presentacion (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    producto_id     UUID NOT NULL REFERENCES producto(id),
    nombre          VARCHAR(100) NOT NULL,             -- 'Caja x 100', 'Blíster x 10', 'Rollo 50 m'
    factor          NUMERIC(14,4) NOT NULL CHECK (factor > 0), -- unidades base que contiene
    codigo_barras   VARCHAR(30),                       -- la caja suele tener su propio EAN
    precio          NUMERIC(14,2) NOT NULL,            -- precio de esta presentación
    precio_mayorista NUMERIC(14,2),                    -- precio a partir de cantidad_min_mayorista
    cantidad_min_mayorista NUMERIC(14,3),
    es_predeterminada BOOLEAN NOT NULL DEFAULT FALSE,  -- la que sale por defecto en el POS
    activo          BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX ix_presentacion_producto ON producto_presentacion (producto_id);
CREATE INDEX ix_presentacion_barras   ON producto_presentacion (codigo_barras);

-- ============================================================================
-- 3. EXTENSIÓN FARMACIA (tabla 1:1, solo existe fila si el producto es farmacéutico)
-- ============================================================================

CREATE TABLE producto_farmacia (
    producto_id        UUID PRIMARY KEY REFERENCES producto(id),
    principio_activo   VARCHAR(150),                   -- 'Ibuprofeno' -> buscar equivalencias entre marcas
    concentracion      VARCHAR(50),                    -- '400 mg'
    forma_farmaceutica VARCHAR(50),                    -- 'Comprimido', 'Jarabe', 'Ampolla'
    laboratorio        VARCHAR(100),
    registro_sanitario VARCHAR(50),                    -- registro AGEMED
    clasificacion      VARCHAR(20) NOT NULL DEFAULT 'LIBRE'
        CHECK (clasificacion IN ('LIBRE','RECETA','PSICOTROPICO','ESTUPEFACIENTE')),
    requiere_cadena_frio BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX ix_farmacia_principio ON producto_farmacia (principio_activo);

-- ============================================================================
-- 4. INVENTARIO
-- ============================================================================

CREATE TABLE lote (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    producto_id       UUID NOT NULL REFERENCES producto(id),
    numero            VARCHAR(50) NOT NULL,
    fecha_vencimiento DATE,                            -- NULL permitido (ferretería con lote sin vencimiento)
    UNIQUE (producto_id, numero)
);

-- Alerta de vencimientos (resumen WhatsApp: "5 productos vencen en 30 días")
CREATE INDEX ix_lote_vencimiento ON lote (fecha_vencimiento) WHERE fecha_vencimiento IS NOT NULL;

-- Saldo materializado de existencias. La verdad histórica vive en movimiento_inventario.
CREATE TABLE stock (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sucursal_id     UUID NOT NULL REFERENCES sucursal(id),
    producto_id     UUID NOT NULL REFERENCES producto(id),
    lote_id         UUID REFERENCES lote(id),          -- NULL si producto.maneja_lote = FALSE
    cantidad        NUMERIC(14,3) NOT NULL DEFAULT 0,  -- SIEMPRE en unidad base
    actualizado_en  TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (sucursal_id, producto_id, lote_id)
);

CREATE INDEX ix_stock_producto ON stock (producto_id, sucursal_id);

-- Kardex: única fuente de verdad. Todo entra/sale por aquí.
CREATE TABLE movimiento_inventario (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    sucursal_id     UUID NOT NULL REFERENCES sucursal(id),
    producto_id     UUID NOT NULL REFERENCES producto(id),
    lote_id         UUID REFERENCES lote(id),
    tipo            VARCHAR(20) NOT NULL CHECK (tipo IN
        ('COMPRA','VENTA','AJUSTE','MERMA','VENCIMIENTO','TRANSFERENCIA','DEVOLUCION','INVENTARIO_INICIAL')),
    cantidad        NUMERIC(14,3) NOT NULL,            -- positiva entra, negativa sale (unidad base)
    costo_unitario  NUMERIC(14,4),                     -- costo al momento del movimiento
    referencia_tipo VARCHAR(20),                       -- 'VENTA' | 'COMPRA' | ...
    referencia_id   UUID,                              -- id del documento origen
    usuario_id      UUID NOT NULL REFERENCES usuario(id),
    observacion     VARCHAR(300),
    fecha           TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_mov_producto_fecha ON movimiento_inventario (producto_id, fecha);
CREATE INDEX ix_mov_sucursal_fecha ON movimiento_inventario (sucursal_id, fecha);

-- ============================================================================
-- 5. COMPRAS Y PROVEEDORES
-- ============================================================================

CREATE TABLE proveedor (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    nombre          VARCHAR(150) NOT NULL,
    nit             VARCHAR(20),
    telefono_whatsapp VARCHAR(20),                     -- enviar el pedido sugerido directo al proveedor
    dias_credito    INT NOT NULL DEFAULT 0,
    lead_time_dias  INT NOT NULL DEFAULT 3,            -- insumo del sugerido de compra (feature 4)
    activo          BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE compra (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    sucursal_id     UUID NOT NULL REFERENCES sucursal(id),
    proveedor_id    UUID NOT NULL REFERENCES proveedor(id),
    numero          VARCHAR(20) NOT NULL,              -- correlativo interno
    nro_factura_prov VARCHAR(30),                      -- factura del proveedor (crédito fiscal)
    fecha           TIMESTAMPTZ NOT NULL DEFAULT now(),
    estado          VARCHAR(15) NOT NULL DEFAULT 'RECIBIDA'
        CHECK (estado IN ('PEDIDA','RECIBIDA','ANULADA')),
    subtotal        NUMERIC(14,2) NOT NULL DEFAULT 0,
    total           NUMERIC(14,2) NOT NULL DEFAULT 0,
    usuario_id      UUID NOT NULL REFERENCES usuario(id)
);

CREATE TABLE compra_detalle (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    compra_id       UUID NOT NULL REFERENCES compra(id),
    producto_id     UUID NOT NULL REFERENCES producto(id),
    presentacion_id UUID REFERENCES producto_presentacion(id),
    cantidad        NUMERIC(14,3) NOT NULL,            -- en la presentación comprada (ej: 5 cajas)
    cantidad_base   NUMERIC(14,3) NOT NULL,            -- convertida (ej: 500 tabletas)
    costo_unitario  NUMERIC(14,4) NOT NULL,            -- por unidad base
    -- Capturados al recibir; el backend crea/actualiza el lote y el stock:
    numero_lote       VARCHAR(50),
    fecha_vencimiento DATE
);

CREATE INDEX ix_compra_det_compra ON compra_detalle (compra_id);

-- ============================================================================
-- 6. VENTAS, CAJA Y PAGOS
-- ============================================================================

CREATE TABLE cliente (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    nombre          VARCHAR(150) NOT NULL,
    nit_ci          VARCHAR(20),                       -- para la factura SIAT
    tipo_documento  VARCHAR(10) DEFAULT 'CI',          -- CI | NIT | CEX | PAS
    telefono_whatsapp VARCHAR(20),                     -- envío de factura + recordatorios
    email           VARCHAR(150),
    activo          BOOLEAN NOT NULL DEFAULT TRUE
);

-- Sesión de caja: base del panel del dueño (feature 2) y control de empleados
CREATE TABLE sesion_caja (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sucursal_id           UUID NOT NULL REFERENCES sucursal(id),
    usuario_id            UUID NOT NULL REFERENCES usuario(id),
    apertura              TIMESTAMPTZ NOT NULL DEFAULT now(),
    cierre                TIMESTAMPTZ,
    monto_inicial         NUMERIC(14,2) NOT NULL DEFAULT 0,
    monto_cierre_declarado NUMERIC(14,2),              -- lo que el cajero cuenta
    monto_cierre_calculado NUMERIC(14,2),              -- lo que el sistema dice
    -- diferencia entre ambos = alerta al dueño por WhatsApp
    estado                VARCHAR(10) NOT NULL DEFAULT 'ABIERTA'
        CHECK (estado IN ('ABIERTA','CERRADA'))
);

CREATE TABLE venta (
    id              UUID PRIMARY KEY,                  -- SIN default: lo genera el CLIENTE (offline-first)
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    sucursal_id     UUID NOT NULL REFERENCES sucursal(id),
    sesion_caja_id  UUID REFERENCES sesion_caja(id),
    cliente_id      UUID REFERENCES cliente(id),       -- NULL = consumidor final "S/N"
    numero          VARCHAR(20) NOT NULL,              -- correlativo por sucursal
    fecha           TIMESTAMPTZ NOT NULL DEFAULT now(),
    estado          VARCHAR(15) NOT NULL DEFAULT 'COMPLETADA'
        CHECK (estado IN ('COMPLETADA','ANULADA')),
    motivo_anulacion VARCHAR(200),                     -- insumo de alertas de anulaciones sospechosas
    anulada_por     UUID REFERENCES usuario(id),
    subtotal        NUMERIC(14,2) NOT NULL DEFAULT 0,
    descuento       NUMERIC(14,2) NOT NULL DEFAULT 0,
    total           NUMERIC(14,2) NOT NULL DEFAULT 0,
    usuario_id      UUID NOT NULL REFERENCES usuario(id),
    -- Facturación SIAT:
    cuf             VARCHAR(100),                      -- código único de factura
    cufd            VARCHAR(100),                      -- código único de facturación diaria
    estado_siat     VARCHAR(15) NOT NULL DEFAULT 'SIN_FACTURA'
        CHECK (estado_siat IN ('SIN_FACTURA','PENDIENTE','EMITIDA','RECHAZADA','CONTINGENCIA')),
    -- Integraciones / offline:
    enviada_whatsapp BOOLEAN NOT NULL DEFAULT FALSE,
    creado_offline   BOOLEAN NOT NULL DEFAULT FALSE,
    sincronizado_en  TIMESTAMPTZ,
    UNIQUE (sucursal_id, numero)
);

CREATE INDEX ix_venta_fecha    ON venta (empresa_id, fecha);
CREATE INDEX ix_venta_sesion   ON venta (sesion_caja_id);
CREATE INDEX ix_venta_estado   ON venta (empresa_id, estado, fecha); -- anulaciones del día

CREATE TABLE venta_detalle (
    id              UUID PRIMARY KEY,                  -- también generado en cliente
    venta_id        UUID NOT NULL REFERENCES venta(id),
    producto_id     UUID NOT NULL REFERENCES producto(id),
    presentacion_id UUID REFERENCES producto_presentacion(id), -- NULL = unidad base
    lote_id         UUID REFERENCES lote(id),          -- asignado por FEFO si maneja_lote
    cantidad        NUMERIC(14,3) NOT NULL,            -- en la presentación vendida
    cantidad_base   NUMERIC(14,3) NOT NULL,            -- convertida a unidad base
    precio_unitario NUMERIC(14,2) NOT NULL,
    descuento       NUMERIC(14,2) NOT NULL DEFAULT 0,
    total           NUMERIC(14,2) NOT NULL
);

CREATE INDEX ix_venta_det_venta    ON venta_detalle (venta_id);
CREATE INDEX ix_venta_det_producto ON venta_detalle (producto_id); -- top productos, sugerido de compra

CREATE TABLE pago (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    venta_id        UUID NOT NULL REFERENCES venta(id),
    metodo          VARCHAR(15) NOT NULL CHECK (metodo IN ('EFECTIVO','QR','TARJETA','TRANSFERENCIA')),
    monto           NUMERIC(14,2) NOT NULL,
    referencia_qr   VARCHAR(100),                      -- id de transacción del QR bancario
    fecha           TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ============================================================================
-- 7. CONTROLADOS (SOLO FARMACIA): registro de receta obligatorio
-- ============================================================================

CREATE TABLE receta (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    venta_id         UUID NOT NULL REFERENCES venta(id),
    medico_nombre    VARCHAR(150) NOT NULL,
    medico_matricula VARCHAR(50)  NOT NULL,
    paciente_nombre  VARCHAR(150) NOT NULL,
    paciente_ci      VARCHAR(20),
    fecha_receta     DATE NOT NULL,
    imagen_url       VARCHAR(300)                      -- foto de la receta (respaldo ante inspección)
);

-- ============================================================================
-- 8. NOTIFICACIONES WHATSAPP (feature 1): log de todo lo enviado
-- ============================================================================

CREATE TABLE notificacion (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    tipo            VARCHAR(30) NOT NULL CHECK (tipo IN
        ('FACTURA_CLIENTE','RESUMEN_DIARIO','STOCK_MINIMO','VENCIMIENTOS',
         'DIFERENCIA_CAJA','ANULACION','PEDIDO_PROVEEDOR')),
    destinatario    VARCHAR(20) NOT NULL,              -- número WhatsApp
    contenido       TEXT NOT NULL,
    estado          VARCHAR(15) NOT NULL DEFAULT 'PENDIENTE'
        CHECK (estado IN ('PENDIENTE','ENVIADA','FALLIDA')),
    referencia_id   UUID,                              -- venta/producto que la originó
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now(),
    enviado_en      TIMESTAMPTZ
);

CREATE INDEX ix_notif_pendientes ON notificacion (estado, creado_en) WHERE estado = 'PENDIENTE';

-- ============================================================================
-- 9. CONSULTAS CLAVE (referencia de implementación)
-- ============================================================================

-- FEFO: elegir el lote a descargar en una venta de producto con maneja_lote = TRUE
-- SELECT s.lote_id, s.cantidad
--   FROM stock s JOIN lote l ON l.id = s.lote_id
--  WHERE s.sucursal_id = :sucursal AND s.producto_id = :producto AND s.cantidad > 0
--  ORDER BY l.fecha_vencimiento ASC NULLS LAST
--  LIMIT 1;

-- Sugerido de compra (feature 4): venta promedio diaria de 30 días vs stock actual
-- SELECT p.id, p.nombre,
--        COALESCE(v.venta_diaria, 0)                                   AS venta_diaria,
--        st.stock_actual,
--        CEIL(COALESCE(v.venta_diaria,0) * (pr.lead_time_dias + 7)
--             - st.stock_actual)                                       AS cantidad_sugerida
--   FROM producto p
--   JOIN (SELECT producto_id, SUM(cantidad) AS stock_actual
--           FROM stock WHERE sucursal_id = :sucursal GROUP BY producto_id) st
--     ON st.producto_id = p.id
--   LEFT JOIN (SELECT vd.producto_id, SUM(vd.cantidad_base)/30.0 AS venta_diaria
--                FROM venta_detalle vd
--                JOIN venta v ON v.id = vd.venta_id AND v.estado = 'COMPLETADA'
--               WHERE v.fecha >= now() - INTERVAL '30 days'
--               GROUP BY vd.producto_id) v ON v.producto_id = p.id
--   LEFT JOIN proveedor pr ON pr.id = :proveedor
--  WHERE p.empresa_id = :empresa AND p.activo
--    AND st.stock_actual < COALESCE(v.venta_diaria,0) * (pr.lead_time_dias + 7);

-- Resumen nocturno del dueño (feature 1 + 2):
-- SELECT COUNT(*) FILTER (WHERE estado='COMPLETADA')  AS ventas,
--        COALESCE(SUM(total) FILTER (WHERE estado='COMPLETADA'),0) AS total_bs,
--        COUNT(*) FILTER (WHERE estado='ANULADA')     AS anuladas
--   FROM venta
--  WHERE empresa_id = :empresa AND fecha::date = CURRENT_DATE;
