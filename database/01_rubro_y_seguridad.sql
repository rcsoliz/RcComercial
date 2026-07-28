-- ============================================================================
-- CORRECCIÓN 01: RUBRO COMO CATÁLOGO + SEGURIDAD RBAC + AUDITORÍA
-- Aplica sobre esquema_almacen_farmacia.sql
-- ============================================================================

-- ============================================================================
-- 1. RUBRO: de CHECK constraint a tabla catálogo
--    Agregar un rubro nuevo = un INSERT, no una migración.
-- ============================================================================

CREATE TABLE rubro (
    id              SMALLINT PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE,       -- 'FARMACIA', 'ALMACEN', 'FERRETERIA'...
    nombre          VARCHAR(100) NOT NULL,
    -- Flags de comportamiento: qué módulos/validaciones activa este rubro.
    -- Así el código pregunta por capacidades, no por "si es farmacia":
    usa_lotes_por_defecto   BOOLEAN NOT NULL DEFAULT FALSE,
    usa_controlados         BOOLEAN NOT NULL DEFAULT FALSE, -- habilita módulo receta
    usa_ficha_farmacia      BOOLEAN NOT NULL DEFAULT FALSE, -- habilita producto_farmacia
    usa_decimales_por_defecto BOOLEAN NOT NULL DEFAULT FALSE,
    activo          BOOLEAN NOT NULL DEFAULT TRUE
);

INSERT INTO rubro (id, codigo, nombre, usa_lotes_por_defecto, usa_controlados, usa_ficha_farmacia, usa_decimales_por_defecto) VALUES
 (1, 'ALMACEN',    'Almacén / Tienda de barrio', FALSE, FALSE, FALSE, FALSE),
 (2, 'FARMACIA',   'Farmacia',                   TRUE,  TRUE,  TRUE,  FALSE),
 (3, 'FERRETERIA', 'Ferretería',                 FALSE, FALSE, FALSE, TRUE),
 (4, 'LICORERIA',  'Licorería',                  FALSE, FALSE, FALSE, FALSE),
 (5, 'MINIMARKET', 'Minimarket',                 FALSE, FALSE, FALSE, FALSE);

-- Migrar la columna de empresa:
ALTER TABLE empresa ADD COLUMN rubro_id SMALLINT REFERENCES rubro(id);
UPDATE empresa SET rubro_id = CASE rubro
    WHEN 'ALMACEN' THEN 1 WHEN 'FARMACIA' THEN 2 WHEN 'FERRETERIA' THEN 3 END;
ALTER TABLE empresa ALTER COLUMN rubro_id SET NOT NULL;
ALTER TABLE empresa DROP COLUMN rubro;

-- ============================================================================
-- 2. RBAC: ROLES Y PERMISOS GRANULARES
--    Regla de oro: el frontend OCULTA opciones según permisos (usabilidad),
--    pero el backend VALIDA cada permiso en cada endpoint (seguridad).
--    Nunca confíes en lo que manda el cliente web/PWA.
-- ============================================================================

-- Catálogo GLOBAL de permisos (lo define el sistema, no las empresas).
-- Convención: modulo.accion — fácil de mapear a policies en .NET.
CREATE TABLE permiso (
    id              SMALLINT PRIMARY KEY,
    codigo          VARCHAR(50) NOT NULL UNIQUE,       -- 'ventas.anular'
    modulo          VARCHAR(30) NOT NULL,              -- agrupador para la pantalla de configuración
    nombre          VARCHAR(100) NOT NULL,             -- texto legible para el dueño
    es_sensible     BOOLEAN NOT NULL DEFAULT FALSE     -- TRUE = siempre se audita y alerta al dueño
);

INSERT INTO permiso (id, codigo, modulo, nombre, es_sensible) VALUES
 -- Ventas
 (10, 'ventas.crear',            'Ventas',        'Registrar ventas',                       FALSE),
 (11, 'ventas.anular',           'Ventas',        'Anular ventas',                          TRUE),
 (12, 'ventas.descuento',        'Ventas',        'Aplicar descuentos',                     TRUE),
 (13, 'ventas.ver_historial',    'Ventas',        'Ver historial de ventas',                FALSE),
 -- Caja
 (20, 'caja.abrir_cerrar',       'Caja',          'Abrir y cerrar caja',                    FALSE),
 (21, 'caja.ver_todas',          'Caja',          'Ver cajas de otros usuarios',            FALSE),
 -- Inventario
 (30, 'inventario.ver',          'Inventario',    'Consultar stock',                        FALSE),
 (31, 'inventario.ajustar',      'Inventario',    'Ajustes y mermas de inventario',         TRUE),
 (32, 'inventario.ver_costos',   'Inventario',    'Ver costos y utilidades',                TRUE),
 -- Compras
 (40, 'compras.crear',           'Compras',       'Registrar compras',                      FALSE),
 (41, 'compras.anular',          'Compras',       'Anular compras',                         TRUE),
 -- Productos
 (50, 'productos.crear_editar',  'Productos',     'Crear y editar productos',               FALSE),
 (51, 'productos.eliminar',      'Productos',     'Desactivar productos',                   TRUE),
 (52, 'productos.cambiar_precios','Productos',    'Modificar precios',                      TRUE),
 -- Clientes
 (80, 'clientes.crear_editar',   'Clientes',      'Crear y editar clientes',                FALSE),
 (81, 'clientes.eliminar',       'Clientes',      'Desactivar clientes',                    TRUE),
 -- Proveedores
 (90, 'proveedores.crear_editar','Proveedores',   'Crear y editar proveedores',             FALSE),
 (91, 'proveedores.eliminar',    'Proveedores',   'Desactivar proveedores',                 TRUE),
 -- Reportes
 (60, 'reportes.ver',            'Reportes',      'Ver reportes y panel del negocio',       FALSE),
 -- Administración (lo que mencionas: NO todos pueden tocar usuarios/config)
 (70, 'admin.usuarios',          'Administración','Crear, editar y desactivar usuarios',    TRUE),
 (71, 'admin.roles',             'Administración','Configurar roles y permisos',            TRUE),
 (72, 'admin.configuracion',     'Administración','Configuración del negocio y facturación',TRUE),
 (73, 'admin.sucursales',        'Administración','Gestionar sucursales',                   TRUE);

-- Roles: los de sistema (empresa_id NULL, es_sistema TRUE) vienen precargados
-- y NO son editables; cada empresa puede además crear los suyos.
CREATE TABLE rol (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID REFERENCES empresa(id),       -- NULL = rol de sistema (plantilla global)
    nombre          VARCHAR(50) NOT NULL,
    es_sistema      BOOLEAN NOT NULL DEFAULT FALSE,
    activo          BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE (empresa_id, nombre)
);

CREATE TABLE rol_permiso (
    rol_id          UUID NOT NULL REFERENCES rol(id) ON DELETE CASCADE,
    permiso_id      SMALLINT NOT NULL REFERENCES permiso(id),
    PRIMARY KEY (rol_id, permiso_id)
);

-- Roles de sistema precargados:
INSERT INTO rol (id, empresa_id, nombre, es_sistema) VALUES
 ('a0000000-0000-0000-0000-000000000001', NULL, 'Dueño',      TRUE),
 ('a0000000-0000-0000-0000-000000000002', NULL, 'Encargado',  TRUE),
 ('a0000000-0000-0000-0000-000000000003', NULL, 'Vendedor',   TRUE);

-- Dueño: todos los permisos
INSERT INTO rol_permiso SELECT 'a0000000-0000-0000-0000-000000000001', id FROM permiso;
-- Encargado: opera todo, pero NO administra usuarios/roles/configuración
INSERT INTO rol_permiso SELECT 'a0000000-0000-0000-0000-000000000002', id
  FROM permiso WHERE codigo NOT LIKE 'admin.%';
-- Vendedor: solo vender y consultar (clientes.crear_editar: alta rápida de
-- cliente al cobrar, sin poder desactivar clientes ni tocar proveedores)
INSERT INTO rol_permiso SELECT 'a0000000-0000-0000-0000-000000000003', id
  FROM permiso WHERE codigo IN
  ('ventas.crear','ventas.ver_historial','caja.abrir_cerrar','inventario.ver','clientes.crear_editar');

-- Migrar usuario: de rol texto a rol_id
ALTER TABLE usuario ADD COLUMN rol_id UUID REFERENCES rol(id);
UPDATE usuario SET rol_id = CASE rol
    WHEN 'DUENO'      THEN 'a0000000-0000-0000-0000-000000000001'::uuid
    WHEN 'ADMIN'      THEN 'a0000000-0000-0000-0000-000000000002'::uuid
    ELSE                   'a0000000-0000-0000-0000-000000000003'::uuid END;
ALTER TABLE usuario ALTER COLUMN rol_id SET NOT NULL;
ALTER TABLE usuario DROP COLUMN rol;

-- Endurecer la tabla usuario para un sistema web:
ALTER TABLE usuario
  ADD COLUMN intentos_fallidos    SMALLINT NOT NULL DEFAULT 0,
  ADD COLUMN bloqueado_hasta      TIMESTAMPTZ,               -- lockout tras N intentos
  ADD COLUMN debe_cambiar_password BOOLEAN NOT NULL DEFAULT TRUE,
  ADD COLUMN permisos_version     INT NOT NULL DEFAULT 1,    -- invalida JWT viejos al cambiar permisos
  ADD COLUMN ultimo_login         TIMESTAMPTZ;

-- Refresh tokens con rotación (patrón que ya usaste en DevConnect):
CREATE TABLE refresh_token (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id      UUID NOT NULL REFERENCES usuario(id),
    token_hash      VARCHAR(200) NOT NULL,             -- guardar el HASH, nunca el token en claro
    expira_en       TIMESTAMPTZ NOT NULL,
    revocado_en     TIMESTAMPTZ,
    reemplazado_por UUID,                              -- rotación: detecta reuso = robo de token
    ip_creacion     VARCHAR(45),
    user_agent      VARCHAR(300),
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_refresh_usuario ON refresh_token (usuario_id, expira_en);

-- ============================================================================
-- 3. AUDITORÍA: toda acción sensible queda registrada
--    (además alimenta las alertas WhatsApp al dueño: anulaciones, cambios de
--     permisos, ajustes de inventario, diferencias de caja)
-- ============================================================================

CREATE TABLE auditoria (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    empresa_id      UUID NOT NULL,
    usuario_id      UUID,                              -- NULL en intentos de login fallidos
    accion          VARCHAR(50) NOT NULL,              -- 'ventas.anular', 'admin.usuarios', 'login.fallido'
    entidad         VARCHAR(50),                       -- 'venta', 'usuario', 'producto'
    entidad_id      UUID,
    detalle         JSONB,                             -- valores antes/después del cambio
    ip              VARCHAR(45),
    fecha           TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_auditoria_empresa_fecha ON auditoria (empresa_id, fecha);
CREATE INDEX ix_auditoria_entidad       ON auditoria (entidad, entidad_id);

-- ============================================================================
-- 4. REGLA DE DISEÑO: NADA SE ELIMINA FÍSICAMENTE
--    Usuarios, productos, roles: siempre activo = FALSE (soft delete).
--    El DELETE físico rompe integridad referencial con ventas históricas
--    y borra evidencia. La única excepción son los refresh_token expirados.
-- ============================================================================
