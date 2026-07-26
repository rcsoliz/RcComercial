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

## FASE 9 — Facturación SIAT

Investigar y aplicar la normativa vigente de Impuestos Nacionales (facturación
en línea / computarizada): registro CUIS/CUFD, generación de CUF, firma,
XML, paquetes de contingencia. Campos ya previstos en venta y devolucion.
(Requiere credenciales de pruebas del SIAT — gestionarlas antes de esta fase.)

---

## Recordatorios permanentes
- Una fase por sesión. Compilar y probar criterios ANTES de commit.
- Parches mínimos; no tocar infraestructura compartida sin avisar.
- Toda entidad nueva de negocio: ITenantEntity + soft delete.
- Stock y kardex: misma transacción, siempre.
