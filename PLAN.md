# PLAN.md — Plan de desarrollo RcComercial

> Instrucciones para Claude Code: trabajar UNA fase por sesión, en orden.
> Cada fase termina con: compilar sin errores, probar criterios de aceptación,
> commit + push para revisión en GitHub. No avanzar a la siguiente fase sin
> confirmación del desarrollador. Respetar siempre las reglas de CLAUDE.md.

---

## FASE 1 — Autenticación y seguridad (JWT + RBAC)  ← SIGUIENTE

**Objetivo:** login funcional que emite JWT con claims multi-tenant y permisos.

Tareas:
1. Agregar paquetes: `Microsoft.AspNetCore.Authentication.JwtBearer`,
   `BCrypt.Net-Next` (hash de passwords).
2. Endpoint `POST /api/auth/login` (usuario + password):
   - Validar contra `usuario` (BCrypt.Verify).
   - Lockout: incrementar `intentos_fallidos`; con 5 fallos, `bloqueado_hasta = now() + 15 min`.
   - Emitir access token (15 min) con claims: `sub` (usuario_id), `empresa_id`,
     `sucursal_id`, `permisos_version`, y un claim `permiso` por cada permiso del rol.
   - Emitir refresh token opaco (64 bytes aleatorios), guardar SOLO su hash SHA-256
     en `refresh_token` con expiración de 7 días.
3. Endpoint `POST /api/auth/refresh`: rotación — revocar el usado, emitir par nuevo,
   registrar `reemplazado_por`. Si llega un token YA revocado: revocar toda la cadena
   del usuario (indicio de robo) y registrar en auditoría.
4. Autorización por permisos: policy provider dinámico que mapea
   `[Authorize(Policy = Permisos.XXX)]` contra los claims `permiso`.
   Verificar además `permisos_version` del claim vs BD (cache 5 min).
5. Middleware/filtro de auditoría: registrar en `auditoria` login exitoso,
   login fallido y toda acción con permiso `es_sensible = true`.
6. Rate limiting en `/api/auth/*` (builtin .NET 8: `AddRateLimiter`, 10 req/min por IP).
7. Seed de desarrollo: crear empresa demo + sucursal + usuario dueño
   (solo si la BD está vacía, en `Program.cs` bajo `IsDevelopment`).

Criterios de aceptación:
- [ ] Login correcto devuelve access + refresh token; el JWT decodificado
      contiene empresa_id y lista de permisos.
- [ ] 5 logins fallidos bloquean la cuenta 15 minutos.
- [ ] Un endpoint protegido con permiso `admin.usuarios` devuelve 403 a un vendedor.
- [ ] Refresh reutilizado (replay) revoca la cadena completa.
- [ ] Con JWT válido, una consulta a productos SOLO devuelve los de esa empresa.

---

## FASE 2 — Módulo productos y catálogo

**Objetivo:** CRUD completo de productos con presentaciones, listo para el POS.

Tareas:
1. CQRS ligero (MediatR + FluentValidation, como en rc-clean-arc):
   commands/queries en Application, endpoints en Api.
2. `GET /api/productos?buscar=` — búsqueda con `ILIKE` + `similarity()` (pg_trgm),
   paginada, ordenada por relevancia. Máximo 20 resultados.
3. `GET /api/productos/por-codigo/{codigoBarras}` — busca primero en producto y
   producto_presentacion de la empresa; si no existe, busca en `producto_maestro`
   y devuelve sugerencia `{ esSugerencia: true, datos }` para alta rápida.
4. CRUD producto (permiso `productos.crear_editar`): crear con presentaciones
   anidadas; si `empresa.rubro.usa_ficha_farmacia`, aceptar ficha farmacia.
4b. MIGRACIÓN — campos para facturación (Facturacion API los exige por ítem):
   - `producto.codigo_producto_sin` (int, nullable): código de la paramétrica
     de productos/servicios del SIN.
   - `producto.codigo_unidad_sin` (int, nullable): unidad de medida en código SIN.
   - `sucursal.actividad_economica` (varchar, nullable): código CAEB registrado.
   Nullable a propósito: la tienda que no factura no está obligada a llenarlos;
   el flujo de emisión (Fase 9) valida que existan solo al facturar.
5. Cambio de precio (permiso `productos.cambiar_precios`): registrar SIEMPRE
   en `precio_historial` en la misma transacción.
6. Desactivar producto (permiso `productos.eliminar`): soft delete.
7. CRUD simple de categorías y marcas.
8. Import inicial: `POST /api/productos/importar` que recibe un Excel/CSV
   (columnas: codigo_barras, nombre, precio, stock_inicial) y crea productos
   + movimiento INVENTARIO_INICIAL + stock. Reportar filas con error sin abortar.

Criterios de aceptación:
- [ ] Buscar "para 500" encuentra "Paracetamol 500mg".
- [ ] Escanear un código no registrado devuelve la sugerencia del maestro.
- [ ] Cambiar un precio deja rastro en precio_historial.
- [ ] Importar un CSV de 50 productos crea stock y kardex consistentes.

---

## FASE 3 — POS: ventas, caja y pagos

**Objetivo:** el flujo de venta completo, el corazón del sistema.

Tareas:
1. Sesión de caja: `POST /api/caja/abrir` (monto inicial) y `POST /api/caja/cerrar`
   (calcular monto esperado desde pagos EFECTIVO de la sesión; guardar declarado
   y calculado; si difieren, crear notificación DIFERENCIA_CAJA).
2. `POST /api/ventas` — el endpoint más crítico. En UNA transacción:
   - Validar sesión de caja abierta del usuario.
   - Número de venta vía `secuencia` (UPDATE ... RETURNING, atómico).
   - Por cada detalle: convertir a cantidad_base por el factor de la presentación;
     si el producto maneja_lote, asignar lote por FEFO (menor fecha_vencimiento
     con stock > 0); validar stock suficiente (salvo config
     `venta.permite_stock_negativo`).
   - Insertar venta + detalles + pagos; descontar `stock`; insertar
     `movimiento_inventario` tipo VENTA por cada detalle.
   - Si algún producto `es_controlado`: exigir objeto receta en el request
     y guardarlo; si falta, rechazar con 422.
   - Encolar notificación FACTURA_CLIENTE si el cliente tiene WhatsApp.
3. `POST /api/ventas/{id}/anular` (permiso `ventas.anular`): motivo obligatorio,
   reversar stock + kardex tipo DEVOLUCION, registrar en auditoría y encolar
   notificación ANULACION al dueño.
4. Aceptar `Id` de venta generado por el cliente (UUID v7) — validar unicidad,
   ignorar duplicados idempotentemente (para el futuro modo offline).
5. Devoluciones parciales: `POST /api/devoluciones` contra venta_detalle,
   con flag reingresa_stock.

Criterios de aceptación:
- [ ] Vender 2 blísteres descuenta 20 tabletas del lote que vence primero.
- [ ] Dos ventas simultáneas jamás repiten número.
- [ ] Venta de psicotrópico sin receta → 422.
- [ ] Reenviar la misma venta (mismo Id) no duplica: responde la existente.
- [ ] Anulación reversa stock exacto y queda en auditoría.

---

## FASE 4 — Panel del dueño (diferenciador #2)

**Objetivo:** el dueño ve su negocio desde el celular en tiempo real.

Tareas:
1. `GET /api/panel/hoy`: total vendido, nro de ventas, ticket promedio,
   top 5 productos, ventas por usuario, anulaciones y descuentos del día,
   estado de cajas abiertas. (Permiso `reportes.ver`.)
2. `GET /api/panel/alertas`: productos bajo stock mínimo (vista v_stock_producto),
   lotes que vencen en 30/60/90 días, diferencias de caja de la semana.
3. `GET /api/panel/historico?desde&hasta`: ventas por día para gráfica.
4. Job nocturno (BackgroundService, 21:00 hora La Paz): componer el resumen
   del día y encolarlo en `notificacion` tipo RESUMEN_DIARIO al WhatsApp
   de la empresa.

Criterios de aceptación:
- [ ] El resumen coincide al centavo con la suma de ventas COMPLETADAS del día.
- [ ] Un usuario sin `reportes.ver` recibe 403.
- [ ] Un usuario sin `inventario.ver_costos` NO recibe campos de costo/utilidad.

---

## FASE 5 — Notificaciones WhatsApp (diferenciador #1)

**Objetivo:** despachar la cola `notificacion` por WhatsApp.

Tareas:
1. Abstracción `IWhatsappSender` con dos implementaciones:
   - `WaLinkSender` (fase inicial, gratis): genera enlaces wa.me con texto
     prellenado que el cajero toca para enviar.
   - `WhatsappCloudApiSender` (Meta Cloud API) detrás de config.
2. BackgroundService que procesa `notificacion` PENDIENTE (reintentos: 3,
   backoff exponencial, marcar FALLIDA).
3. Plantillas de mensaje por tipo (recibo de venta, resumen diario, stock
   mínimo, vencimientos, anulación, diferencia de caja).

Criterios de aceptación:
- [ ] Una venta con cliente con WhatsApp genera notificación y se despacha.
- [ ] Caída del sender no pierde notificaciones: quedan PENDIENTE y se reintentan.

---

## FASE 6 — Sugerido de compra (diferenciador #4)

1. `GET /api/compras/sugerido?proveedorId=`: query de rotación 30 días vs stock
   (base en database/00, sección 9), agrupado por proveedor.
2. `POST /api/compras` con recepción: capturar lote y vencimiento por línea,
   crear/actualizar lote, stock y kardex COMPRA; recalcular costo_promedio
   ponderado.
3. Botón "enviar pedido al proveedor" → notificación PEDIDO_PROVEEDOR (WhatsApp).

Criterios: [ ] el sugerido excluye productos con stock suficiente;
[ ] recibir compra con lote nuevo lo crea y el FEFO lo considera.

---

## FASE 7 — Frontend Vue 3 (PWA)

1. Proyecto `rc-comercial-fe`: Vue 3 + Vite + Tailwind + Pinia + Router
   (misma base que rc-clean-arc-fe).
2. Pantallas en orden: Login → POS de venta (buscador + carrito + cobro) →
   Productos → Panel del dueño (mobile-first) → Caja → Compras.
3. Ocultar UI según permisos del JWT (solo estética; el backend ya valida).
4. PWA: manifest + service worker. El modo offline REAL (IndexedDB + cola
   de sincronización) es la fase 8, no intentarlo aquí.

## FASE 8 — Offline-first (diferenciador #6)

1. Cachear catálogo (productos/presentaciones/precios, SIN costos) en IndexedDB,
   filtrado según permisos.
2. Venta offline: generar UUID v7 en el cliente, numeración temporal local,
   cola de sincronización con reintentos; el backend revalida todo al recibir.
3. Rango de numeración reservado por dispositivo (extender tabla secuencia).

## FASE 9 — Facturación SIAT (integración con Facturacion API)

> RcComercial NO implementa SIAT. Consume el servicio independiente
> `facturador-by-rc` (repo aparte, producto vendible por sí mismo) vía REST.
> RcComercial nunca conoce CUFD/CUIS/XML/SOAP: emite → 202 → webhook.
> Para desarrollo se usa el SiatFakeAdapter del facturador (sin credenciales).

Tareas (lado RcComercial):
1. `IFacturacionApiClient` (HttpClient tipado + Polly): emitir, consultar,
   anular, obtener PDF. Config: base URL + X-Api-Key del tenant (cifrada
   en `empresa_configuracion`).
2. Al completar una venta de empresa con facturación activa
   (`empresa_configuracion['siat.activo'] = true`):
   - Validar que todos los productos tengan `codigo_producto_sin` y
     `codigo_unidad_sin`; si falta alguno, la venta se guarda igual y la
     factura queda en error visible (no bloquear la venta).
   - `POST /api/v1/facturas` con `referenciaExterna = venta.Id` (UUID v7:
     idempotencia gratis, la venta offline re-sincronizada jamás factura doble).
   - `venta.estado_siat = 'PENDIENTE'`.
3. Endpoint receptor de webhooks `POST /api/webhooks/facturacion`:
   - Verificar firma HMAC-SHA256 (timestamp + cuerpo) con el secreto del tenant;
     rechazar timestamps viejos (> 5 min).
   - Actualizar venta: EMITIDA (guardar cuf) o RECHAZADA (guardar motivo).
   - Idempotente: el mismo webhook recibido dos veces no rompe nada.
4. Polling de respaldo (job cada 10 min) para facturas PENDIENTE > 15 min.
5. Anulación de venta facturada → `POST /facturas/{id}/anulacion` con código
   de motivo SIN; la venta no queda ANULADA localmente hasta confirmar webhook.
6. WhatsApp: adjuntar el PDF (`GET /facturas/{id}/pdf`) en la notificación
   FACTURA_CLIENTE en lugar del recibo interno.
7. Pantalla de configuración (permiso `admin.configuracion`): API key del
   facturador, actividad económica, y asistente para mapear productos a
   códigos SIN (masivo, por categoría).

Criterios de aceptación:
- [ ] Venta con siat.activo emite y el webhook la marca EMITIDA con CUF.
- [ ] Webhook con firma inválida → 401 y no toca la venta.
- [ ] Reenviar la misma venta al facturador no duplica factura (idempotencia).
- [ ] Producto sin codigo_producto_sin: la venta se completa, la factura
      queda en error visible en el panel.
- [ ] Todo el flujo corre contra el SiatFakeAdapter, sin credenciales reales.

Trabajo restante en el repo facturador-by-rc (paralelo, con su propio CLAUDE.md):
- Clientes SOAP desde los WSDL públicos del SIN + pruebas contra WireMock.Net.
- Completar SiatComputarizadaAdapter (hoy NotImplementedException).
- Limpiar TODOs obsoletos (CufCalculator/XmlFacturaBuilder ya existen y
  están testeados, pero los comentarios del adaptador dicen "pendiente").
- Cuando lleguen las credenciales del ambiente piloto: smoke tests reales
  y homologación. Nada de esto bloquea las fases 1–8 de RcComercial.

---

FASE 9, sesión 1 — integración con Facturacion API (facturador-by-rc).
Lee PLAN.md Fase 9 completa y CLAUDE.md. RcComercial NUNCA conoce
CUFD/CUIS/XML: emite → 202 → webhook. Desarrollo contra el facturador
corriendo local con SiatFakeAdapter (docker-compose del otro repo).

1. IFacturacionApiClient (HttpClient tipado + Polly: retry con backoff
   y circuit breaker): emitir factura, consultar estado, solicitar
   anulación, obtener PDF. Config por empresa en empresa_configuracion:
   siat.activo (bool), siat.api_key (cifrada con Data Protection),
   siat.webhook_secret (cifrado).
2. Al completar una venta de empresa con siat.activo=true:
   - Si algún producto no tiene codigo_producto_sin o codigo_unidad_sin:
     la venta se guarda igual, estado_siat='RECHAZADA' con motivo claro
     visible en panel — la venta JAMÁS se bloquea por facturación.
   - Si están completos: POST al facturador con referenciaExterna =
     venta.Id, estado_siat='PENDIENTE'. La llamada va DESPUÉS del commit
     de la venta (nunca dentro de la transacción) y en un job Hangfire/
     BackgroundService con reintentos — la venta responde rápido al POS.
3. Tests (Testcontainers + mock del facturador con WireMock.Net):
   venta con siat.activo emite; sin códigos SIN queda rechazada sin
   bloquear; caída del facturador → reintenta sin perder facturas.

FASE 9, sesión 2 — receptor de webhooks y UI. Requiere 9.1 aprobada.

1. POST /api/webhooks/facturacion (SIN [Authorize] — autentica por
   firma): verificar HMAC-SHA256 de timestamp+cuerpo con el secreto del
   tenant, rechazar timestamps > 5 min (replay), responder 401 sin
   detalle si falla. Actualizar venta: EMITIDA (guardar cuf) o RECHAZADA
   (motivo). Idempotente: el mismo webhook dos veces no cambia nada.
2. Polling de respaldo: job cada 10 min consulta al facturador las
   ventas PENDIENTE > 15 min.
3. Anulación: anular una venta facturada exige código de motivo SIN y
   dispara la anulación en el facturador; la venta queda ANULADA local
   solo al confirmar el webhook de la nota.
4. WhatsApp: si la venta tiene factura EMITIDA, la notificación
   FACTURA_CLIENTE adjunta el PDF del facturador en vez del recibo.
5. Frontend: badge de estado SIAT en el ticket y en el historial
   (SIN_FACTURA gris, PENDIENTE --aviso, EMITIDA --exito, RECHAZADA
   --peligro con motivo); pantalla de configuración (admin.configuracion)
   para API key, actividad económica y el asistente de mapeo masivo de
   códigos SIN por categoría.
6. Tests: firma inválida → 401 sin tocar la venta; webhook duplicado
   idempotente; el criterio final del PLAN: todo el flujo corre contra
   el SiatFakeAdapter sin credenciales reales.
7. Al terminar: reporte de cierre de la Fase 9 y del PLAN completo.

## Backlog futuro (fuera de las fases numeradas)

**Reset de contraseña self-service ("¿Olvidaste tu contraseña?")**
Hoy el link del login solo indica "contacta a tu administrador" — reusa el
flujo ya existente de `UsuariosView` (admin.usuarios → restablecer
contraseña → temporal + `DebeCambiarPassword`), sin código nuevo. Un reset
self-service de verdad requiere:
- Un canal capaz de entregar el mensaje sin intervención humana. Hoy
  `WaLinkSender` (Fase 5) genera un enlace wa.me que el cajero debe tocar
  manualmente — no sirve para esto. Depende de que se construya
  `WhatsappCloudApiSender` (Meta Cloud API, ya previsto en Fase 5 pero no
  implementado: requiere cuenta de negocio y plantillas pre-aprobadas por
  Meta) o de agregar email (campo `Email` en `Usuario` + proveedor SMTP).
- Tabla de tokens de reset (mismo patrón que `RefreshToken`: hash,
  expiración corta, un solo uso).
- `POST /auth/solicitar-reset` y `POST /auth/confirmar-reset`, con
  respuesta idéntica exista o no el usuario (no filtrar información) y
  rate limiting (grupo `auth` ya existente).
- Invalidar refresh tokens vigentes del usuario al confirmar el reset.

## Recordatorios permanentes
- Una fase por sesión. Compilar y probar criterios ANTES de commit.
- Parches mínimos; no tocar infraestructura compartida sin avisar.
- Toda entidad nueva de negocio: ITenantEntity + soft delete.
- Stock y kardex: misma transacción, siempre.
