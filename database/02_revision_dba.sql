-- ============================================================================
-- CORRECCIÓN 02 (REVISIÓN DBA): rendimiento, evolución y funcionalidad
-- Aplica sobre esquema base + corrección 01
-- ============================================================================

-- ============================================================================
-- 1. UUID v7 (ordenado por tiempo) en lugar de v4 aleatorio
--    v4 fragmenta los índices PK; v7 inserta secuencialmente como un IDENTITY
--    pero se puede seguir generando en el cliente (PWA offline).
--    En .NET: Guid.CreateVersion7(). En BD (PG < 18), función propia:
-- ============================================================================

CREATE OR REPLACE FUNCTION uuid_v7() RETURNS uuid AS $$
  SELECT encode(
    set_bit(set_bit(
      overlay(uuid_send(gen_random_uuid())
        PLACING substring(int8send((extract(epoch FROM clock_timestamp())*1000)::bigint) FROM 3)
        FROM 1 FOR 6), 52, 1), 53, 1), 'hex')::uuid;
$$ LANGUAGE sql VOLATILE;

-- Cambiar defaults (los IDs existentes no se tocan; conviven sin problema):
ALTER TABLE producto              ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE producto_presentacion ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE lote                  ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE stock                 ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE movimiento_inventario ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE compra                ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE compra_detalle        ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE pago                  ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE receta                ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE notificacion          ALTER COLUMN id SET DEFAULT uuid_v7();
ALTER TABLE refresh_token         ALTER COLUMN id SET DEFAULT uuid_v7();
-- venta / venta_detalle no llevan default: el ID lo genera el cliente (v7 en .NET/JS)

-- ============================================================================
-- 2. CAMPOS DE AUDITORÍA UNIFORMES
--    Toda tabla de negocio responde: ¿quién y cuándo la creó/modificó?
--    (En EF Core se llena automático con un SaveChanges interceptor)
-- ============================================================================

ALTER TABLE producto
  ADD COLUMN actualizado_en TIMESTAMPTZ,
  ADD COLUMN creado_por     UUID REFERENCES usuario(id),
  ADD COLUMN actualizado_por UUID REFERENCES usuario(id);

ALTER TABLE producto_presentacion
  ADD COLUMN creado_en      TIMESTAMPTZ NOT NULL DEFAULT now(),
  ADD COLUMN actualizado_en TIMESTAMPTZ,
  ADD COLUMN actualizado_por UUID REFERENCES usuario(id);

ALTER TABLE cliente
  ADD COLUMN creado_en      TIMESTAMPTZ NOT NULL DEFAULT now(),
  ADD COLUMN actualizado_en TIMESTAMPTZ;

ALTER TABLE proveedor
  ADD COLUMN creado_en      TIMESTAMPTZ NOT NULL DEFAULT now(),
  ADD COLUMN actualizado_en TIMESTAMPTZ;

-- ============================================================================
-- 3. HISTORIAL DE PRECIOS (nunca pisar un precio sin dejar rastro)
--    Sirve para: reportes de margen histórico, detección de manipulación,
--    y responder "¿a cuánto vendía esto antes?"
-- ============================================================================

CREATE TABLE precio_historial (
    id              UUID PRIMARY KEY DEFAULT uuid_v7(),
    producto_id     UUID NOT NULL REFERENCES producto(id),
    presentacion_id UUID REFERENCES producto_presentacion(id), -- NULL = precio_base
    precio_anterior NUMERIC(14,2) NOT NULL,
    precio_nuevo    NUMERIC(14,2) NOT NULL,
    usuario_id      UUID NOT NULL REFERENCES usuario(id),
    fecha           TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_precio_hist_producto ON precio_historial (producto_id, fecha);

-- ============================================================================
-- 4. SECUENCIAS DE DOCUMENTOS (adiós MAX(numero)+1 y sus duplicados)
--    Incremento atómico por sucursal y tipo de documento.
--    Para offline: cada dispositivo reserva un rango al sincronizar.
-- ============================================================================

CREATE TABLE secuencia (
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    sucursal_id     UUID NOT NULL REFERENCES sucursal(id),
    tipo_documento  VARCHAR(20) NOT NULL,   -- 'VENTA','COMPRA','DEVOLUCION','TRANSFERENCIA'
    prefijo         VARCHAR(10) NOT NULL DEFAULT '',
    siguiente       BIGINT NOT NULL DEFAULT 1,
    PRIMARY KEY (empresa_id, sucursal_id, tipo_documento)
);

-- Uso atómico (una sola sentencia, sin SELECT previo):
-- UPDATE secuencia SET siguiente = siguiente + 1
--  WHERE empresa_id=:e AND sucursal_id=:s AND tipo_documento='VENTA'
--  RETURNING prefijo || lpad((siguiente-1)::text, 8, '0');

-- ============================================================================
-- 5. DEVOLUCIONES (documento propio, ligado a la venta original
--    y preparado para nota de crédito SIAT)
-- ============================================================================

CREATE TABLE devolucion (
    id              UUID PRIMARY KEY,                  -- generado en cliente (offline)
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    sucursal_id     UUID NOT NULL REFERENCES sucursal(id),
    venta_id        UUID NOT NULL REFERENCES venta(id),
    numero          VARCHAR(20) NOT NULL,
    fecha           TIMESTAMPTZ NOT NULL DEFAULT now(),
    motivo          VARCHAR(200) NOT NULL,
    total           NUMERIC(14,2) NOT NULL,
    usuario_id      UUID NOT NULL REFERENCES usuario(id),
    cuf_nota_credito VARCHAR(100),                     -- nota de crédito-débito SIAT
    estado_siat     VARCHAR(15) NOT NULL DEFAULT 'SIN_NOTA',
    UNIQUE (sucursal_id, numero)
);

CREATE TABLE devolucion_detalle (
    id               UUID PRIMARY KEY,
    devolucion_id    UUID NOT NULL REFERENCES devolucion(id),
    venta_detalle_id UUID NOT NULL REFERENCES venta_detalle(id), -- amarra al ítem original
    cantidad_base    NUMERIC(14,3) NOT NULL CHECK (cantidad_base > 0),
    reingresa_stock  BOOLEAN NOT NULL DEFAULT TRUE,    -- FALSE si el producto vuelve dañado
    monto            NUMERIC(14,2) NOT NULL
);

CREATE INDEX ix_devolucion_venta ON devolucion (venta_id);

-- ============================================================================
-- 6. TRANSFERENCIAS ENTRE SUCURSALES (documento cabecera del kardex)
-- ============================================================================

CREATE TABLE transferencia (
    id                  UUID PRIMARY KEY DEFAULT uuid_v7(),
    empresa_id          UUID NOT NULL REFERENCES empresa(id),
    sucursal_origen_id  UUID NOT NULL REFERENCES sucursal(id),
    sucursal_destino_id UUID NOT NULL REFERENCES sucursal(id),
    numero              VARCHAR(20) NOT NULL,
    estado              VARCHAR(15) NOT NULL DEFAULT 'ENVIADA'
        CHECK (estado IN ('ENVIADA','RECIBIDA','ANULADA')),
    -- El stock sale del origen al ENVIAR y entra al destino al RECIBIR:
    -- así el inventario "en tránsito" queda visible y nadie lo vende dos veces.
    enviado_por         UUID NOT NULL REFERENCES usuario(id),
    recibido_por        UUID REFERENCES usuario(id),
    fecha_envio         TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_recepcion     TIMESTAMPTZ,
    CHECK (sucursal_origen_id <> sucursal_destino_id)
);

CREATE TABLE transferencia_detalle (
    id               UUID PRIMARY KEY DEFAULT uuid_v7(),
    transferencia_id UUID NOT NULL REFERENCES transferencia(id),
    producto_id      UUID NOT NULL REFERENCES producto(id),
    lote_id          UUID REFERENCES lote(id),
    cantidad_base    NUMERIC(14,3) NOT NULL CHECK (cantidad_base > 0)
);

-- ============================================================================
-- 7. CONFIGURACIÓN POR EMPRESA (clave-valor: evolucionar sin ALTER TABLE)
--    El 90% de los "yo quiero que..." de los clientes cae aquí.
-- ============================================================================

CREATE TABLE empresa_configuracion (
    empresa_id      UUID NOT NULL REFERENCES empresa(id),
    clave           VARCHAR(60) NOT NULL,   -- 'ticket.pie_pagina', 'alerta.monto_caja',
                                            -- 'whatsapp.hora_resumen', 'venta.permite_stock_negativo'
    valor           JSONB NOT NULL,         -- tipado flexible: texto, número, bool, objeto
    actualizado_en  TIMESTAMPTZ NOT NULL DEFAULT now(),
    actualizado_por UUID REFERENCES usuario(id),
    PRIMARY KEY (empresa_id, clave)
);

-- ============================================================================
-- 8. CATÁLOGO MAESTRO GLOBAL (tuyo, compartido entre todas las empresas)
--    Al escanear un código no registrado, se ofrece el producto precargado.
--    Elimina la barrera #1 de adopción: cargar el inventario inicial.
-- ============================================================================

CREATE TABLE producto_maestro (
    id              UUID PRIMARY KEY DEFAULT uuid_v7(),
    codigo_barras   VARCHAR(30) NOT NULL UNIQUE,
    nombre          VARCHAR(200) NOT NULL,
    marca           VARCHAR(100),
    contenido       VARCHAR(50),                       -- '2 L', '500 mg x 10'
    rubro_id        SMALLINT REFERENCES rubro(id),     -- sugerencia de rubro
    -- Para farmacia, sugerencias de la ficha:
    principio_activo VARCHAR(150),
    concentracion    VARCHAR(50),
    laboratorio      VARCHAR(100),
    verificado      BOOLEAN NOT NULL DEFAULT FALSE,    -- TRUE = curado por ti;
                                                       -- FALSE = aportado por alguna empresa
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Vincular el producto de cada empresa con el maestro (para estadísticas cruzadas)
ALTER TABLE producto ADD COLUMN producto_maestro_id UUID REFERENCES producto_maestro(id);

-- ============================================================================
-- 9. BÚSQUEDA COMO LA ESPERA EL CAJERO ("para 500" -> "Paracetamol 500mg")
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX ix_producto_nombre_trgm ON producto USING gin (nombre gin_trgm_ops);
CREATE INDEX ix_maestro_nombre_trgm  ON producto_maestro USING gin (nombre gin_trgm_ops);
-- Consulta del POS:
-- SELECT * FROM producto
--  WHERE empresa_id = :e AND activo AND nombre ILIKE '%' || :texto || '%'
--  ORDER BY similarity(nombre, :texto) DESC LIMIT 10;

-- ============================================================================
-- 10. ÍNDICES FALTANTES SOBRE FKs Y PARCIALES (solo filas vivas)
-- ============================================================================

CREATE INDEX ix_sucursal_empresa   ON sucursal (empresa_id);
CREATE INDEX ix_usuario_empresa    ON usuario (empresa_id) WHERE activo;
CREATE INDEX ix_categoria_empresa  ON categoria (empresa_id) WHERE activo;
CREATE INDEX ix_cliente_empresa    ON cliente (empresa_id) WHERE activo;
CREATE INDEX ix_cliente_whatsapp   ON cliente (empresa_id, telefono_whatsapp);
CREATE INDEX ix_proveedor_empresa  ON proveedor (empresa_id) WHERE activo;
CREATE INDEX ix_pago_venta         ON pago (venta_id);
CREATE INDEX ix_compra_proveedor   ON compra (proveedor_id, fecha);
CREATE INDEX ix_sesion_sucursal    ON sesion_caja (sucursal_id, apertura);
CREATE INDEX ix_receta_venta       ON receta (venta_id);

-- ============================================================================
-- 11. CONSTRAINTS DEFENSIVOS (que la BD sea la última línea de defensa)
-- ============================================================================

ALTER TABLE movimiento_inventario ADD CONSTRAINT ck_mov_cantidad_no_cero CHECK (cantidad <> 0);
ALTER TABLE venta          ADD CONSTRAINT ck_venta_total    CHECK (total >= 0 AND descuento >= 0);
ALTER TABLE venta_detalle  ADD CONSTRAINT ck_vdet_cantidad  CHECK (cantidad > 0 AND cantidad_base > 0);
ALTER TABLE compra_detalle ADD CONSTRAINT ck_cdet_cantidad  CHECK (cantidad > 0 AND cantidad_base > 0);
ALTER TABLE pago           ADD CONSTRAINT ck_pago_monto     CHECK (monto > 0);
ALTER TABLE producto_presentacion ADD CONSTRAINT ck_pres_precio CHECK (precio >= 0);
-- Stock negativo: se controla en la aplicación (configurable por empresa vía
-- 'venta.permite_stock_negativo'); la BD no lo prohíbe para no bloquear
-- la sincronización de ventas offline legítimas.

-- ============================================================================
-- 12. PARTICIONAMIENTO DE AUDITORÍA (la tabla de mayor crecimiento)
--     Crear particionada HOY cuesta una línea; particionarla poblada, una noche.
-- ============================================================================

-- Recrear auditoria como particionada por rango mensual:
DROP TABLE IF EXISTS auditoria;

CREATE TABLE auditoria (
    id              BIGINT GENERATED ALWAYS AS IDENTITY,
    empresa_id      UUID NOT NULL,
    usuario_id      UUID,
    accion          VARCHAR(50) NOT NULL,
    entidad         VARCHAR(50),
    entidad_id      UUID,
    detalle         JSONB,
    ip              VARCHAR(45),
    fecha           TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id, fecha)
) PARTITION BY RANGE (fecha);

-- Particiones iniciales (crear las siguientes con un job mensual o pg_partman):
CREATE TABLE auditoria_2026_07 PARTITION OF auditoria
    FOR VALUES FROM ('2026-07-01') TO ('2026-08-01');
CREATE TABLE auditoria_2026_08 PARTITION OF auditoria
    FOR VALUES FROM ('2026-08-01') TO ('2026-09-01');
CREATE TABLE auditoria_2026_09 PARTITION OF auditoria
    FOR VALUES FROM ('2026-09-01') TO ('2026-10-01');

CREATE INDEX ix_auditoria_empresa_fecha ON auditoria (empresa_id, fecha);
CREATE INDEX ix_auditoria_entidad       ON auditoria (entidad, entidad_id);

-- Para movimiento_inventario y notificacion: BRIN sobre fecha
-- (índice minúsculo, ideal para consultas por rango en tablas append-only)
CREATE INDEX ix_mov_fecha_brin   ON movimiento_inventario USING brin (fecha);
CREATE INDEX ix_notif_fecha_brin ON notificacion USING brin (creado_en);

-- ============================================================================
-- 13. VISTA DE STOCK CONSOLIDADO (la consulta más repetida del sistema)
-- ============================================================================

CREATE OR REPLACE VIEW v_stock_producto AS
SELECT s.sucursal_id,
       s.producto_id,
       p.empresa_id,
       p.nombre,
       p.stock_minimo,
       SUM(s.cantidad)                                    AS stock_total,
       MIN(l.fecha_vencimiento)                           AS proximo_vencimiento,
       SUM(s.cantidad) FILTER (WHERE l.fecha_vencimiento
           < CURRENT_DATE + INTERVAL '60 days')           AS cantidad_por_vencer,
       (SUM(s.cantidad) <= p.stock_minimo)                AS bajo_minimo
  FROM stock s
  JOIN producto p ON p.id = s.producto_id AND p.activo
  LEFT JOIN lote l ON l.id = s.lote_id
 GROUP BY s.sucursal_id, s.producto_id, p.empresa_id, p.nombre, p.stock_minimo;

-- ============================================================================
-- NOTA DE EVOLUCIÓN FUTURA (no requiere cambios hoy, el modelo ya lo soporta):
--  * Fiado / cuentas por cobrar: tabla cuenta_por_cobrar(venta_id, saldo)
--    + abono(cuenta_id, monto) — se acopla a venta sin tocar nada existente.
--  * Promociones/combos: tabla promocion + promocion_producto — se acopla
--    a venta_detalle con una FK opcional.
--  * Multi-moneda: empresa_configuracion['moneda'] + columna tipo_cambio
--    en venta si algún día se necesita.
--  * E-commerce/pedidos: documento pedido -> venta reutiliza todo el flujo.
-- ============================================================================
