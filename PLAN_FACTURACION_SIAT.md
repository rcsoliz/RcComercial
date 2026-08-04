# Plan: Integración de facturación electrónica SIAT (Bolivia)

> Estado: **propuesta para revisión**. Nada de esto se implementa hasta que Roberto
> dé el OK. v2 de este documento: revisado tras analizar el repositorio real
> [rcsoliz/facturador-by-rc](https://github.com/rcsoliz/facturador-by-rc), que
> resuelve varias de las preguntas abiertas de la v1.

## 1. Qué es `facturador-by-rc` (analizado del repo)

No es una librería suelta ni un prototipo de un solo tenant: es un
**servicio standalone, multi-tenant, pensado como producto vendible**
("los clientes pagan por factura emitida o suscripción por NIT" — cita
textual del CLAUDE.md del repo). Esto ya resuelve la pregunta más grande
de la v1 de este plan.

**Arquitectura** (Clean Architecture, igual filosofía que RcComercial):

```
src/
├── Facturacion.Domain          # Entidades, máquina de estados, puertos. Cero dependencias.
├── Facturacion.Application     # Casos de uso (emitir, anular, consultar, procesar)
├── Facturacion.Infrastructure  # Adaptadores: SIAT (SOAP/XML), persistencia, colas, webhooks
├── Facturacion.Api             # REST público + Swagger
└── Facturacion.Workers         # Jobs: procesamiento, CUFD, catálogos, contingencia
```

**Contrato REST (v1):**

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/v1/facturas` | Emitir — 202, asíncrono, **idempotente por `referenciaExterna`** |
| GET | `/api/v1/facturas/{id}` | Consultar estado |
| POST | `/api/v1/facturas/{id}/anulacion` | Anular — 202, asíncrono |
| GET | `/api/v1/facturas/{id}/pdf` | Representación gráfica |

Resultado final de emisión/anulación llega por **webhook** al sistema
cliente (con polling como respaldo). Autenticación por `X-Api-Key`, que
resuelve el tenant.

**Puerto central:** `Domain/Ports/IProveedorFiscal` — abstrae al ente
fiscal (hoy SIAT Bolivia, diseñado para poder agregar SUNAT/AFIP a
futuro). Domain y Application de `facturador-by-rc` nunca tocan SOAP, XML
ni CUFD — eso vive en los adaptadores de Infrastructure:

- **v1 — `SiatComputarizadaAdapter`**: hash SHA256, sin firma XMLDSig
  (modalidad Computarizada en Línea).
- **v1.5 — `SiatElectronicaAdapter`**: firma XMLDSig con certificado por
  tenant (modalidad Electrónica en Línea).

La `ModalidadFacturacion` del tenant decide qué adaptador se inyecta.

**El Domain ya está construido de forma sólida** (no es solo un
esqueleto): entidades `Factura` (agregado raíz con máquina de estados
explícita — `Pendiente → Generada → Enviada → Validada/Rechazada/
EnContingencia → Anulada`, nunca se setea el estado a mano), `DetalleFactura`,
`Tenant`, `Sucursal`, `PuntoVenta`, `CredencialSiat`, `ItemCatalogo`; value
objects `Cuf` y `Nit`; y una batería de puertos ya definidos
(`IFacturaRepository`, `ITenantRepository`, `ICredencialSiatRepository`,
`ICatalogoRepository`, `IGestorCredencialesSiat`, `INotificadorWebhook`,
`IProteccionDatos`, `IGeneradorRepresentacionGrafica`).

**Lo que todavía NO está construido** (según el checklist del propio
CLAUDE.md del repo, no verificado línea por línea por mí): persistencia
EF Core/Npgsql real, middleware de auth por API key (hoy usa un
`TenantDev` de relleno), clientes SOAP del SIN, `CufCalculator`,
`XmlFacturaBuilder`, colas Hangfire, webhooks firmados, PDF con QR,
endpoint de onboarding de tenants. Es decir: **el diseño está resuelto,
pero el servicio todavía no es usable end-to-end.**

## 2. La restricción es la misma, y ya la tenías resuelta ahí

El CLAUDE.md de `facturador-by-rc` tiene, casi palabra por palabra, la
misma estrategia que yo había propuesto de forma independiente en la v1
de este documento:

> "Roberto aún NO tiene NIT ni credenciales SIAT (...). NO intentar
> conectarse a ningún endpoint real del SIN. TODO el desarrollo de la
> integración SIAT se hace contra MOCKS (...). Crear un
> `SiatFakeAdapter : IProveedorFiscal` para desarrollo local end-to-end
> (...). Cuando llegue el NIT, el cambio debe ser SOLO de configuración,
> cero cambios de código."

Esto es una buena noticia para el plan: **el "modo simulado" no hay que
inventarlo del lado de RcComercial** — ya está previsto que
`facturador-by-rc` lo resuelva internamente con `SiatFakeAdapter`. Lo que
le toca a RcComercial es integrarse contra el **contrato REST real** de
`facturador-by-rc` (que ya existe, aunque el servicio detrás todavía
esté incompleto), no simular su propio mock aparte.

## 3. Decisión de arquitectura — ya no es una decisión, es un hecho

En la v1 de este plan planteaba "librería embebida vs. servicio
separado" como algo a decidir. Ya no aplica: `facturador-by-rc` **ya está
construido como servicio HTTP independiente**, multi-tenant, con su
propio ciclo de vida y su propio repositorio. RcComercial lo consume
como cualquier API externa:

- `RcComercial.Infrastructure` gana un adaptador nuevo (mismo patrón que
  `WhatsappCloudApiSender` hoy) que implementa una interfaz de Application,
  p. ej. `IFacturadorFiscal`, y por dentro llama por HTTP a
  `POST /api/v1/facturas` de `facturador-by-rc` con `X-Api-Key` de la
  empresa.
- RcComercial recibe el resultado por webhook (necesita un endpoint nuevo,
  p. ej. `POST /api/webhooks/facturacion`) y actualiza `Venta.EstadoSiat`,
  `Venta.Cuf`, `Venta.Cufd`.
- Nunca hay dependencia de ensamblados .NET entre los dos proyectos — cada
  uno se despliega y versiona por separado, tal como ya lo diseñó
  `facturador-by-rc` para poder venderse a otros clientes además de
  RcComercial.

## 4. Mapeo de datos: `Venta` (RcComercial) → `Factura` (facturador-by-rc)

Con los campos reales de `Factura.cs` ya puedo bosquejar el mapeo (esto
todavía no es contrato final — falta ver el DTO REST exacto de
`Facturacion.Api`, que no pude leer del repo en esta pasada):

| Campo en `Factura` (facturador-by-rc) | Origen en RcComercial |
|---|---|
| `ReferenciaExterna` (idempotencia) | `Venta.Id` (ya es UUIDv7 generado por el cliente — encaja perfecto) |
| `SucursalId`, `PuntoVentaId` | `Venta.SucursalId` (falta noción de "punto de venta" en RcComercial — ver §6) |
| `CodigoDocumentoSector` | Fijo (1 = compra-venta) para el rubro de estas pymes |
| `RazonSocialComprador`, `CodigoTipoDocumentoIdentidad`, `NumeroDocumentoComprador` | `Cliente` (o "consumidor final S/N" si `Venta.ClienteId` es null — ya contemplado en el dominio de RcComercial) |
| `CodigoMoneda`, `TipoCambio` | Fijo (bolivianos, 1:1) — no hay multi-moneda hoy |
| `CodigoMetodoPago` | `Pago.Metodo` (`EFECTIVO/QR/TARJETA/TRANSFERENCIA`) mapeado a la paramétrica SIAT (1..308) |
| `Detalles` | `VentaDetalle` + `Producto.CodigoProductoSin` / `Producto.CodigoUnidadSin` (**ya existen en el modelo**, ver plan v1 §2) |

Esta tabla confirma que el modelo de datos de RcComercial ya tiene casi
todo lo que `facturador-by-rc` va a pedir — el trabajo real es el
adaptador y el mapeo, no rediseñar entidades.

## 5. Patrón de integración del lado RcComercial (sin cambios respecto a v1)

Sigue aplicando lo de la v1: la venta en `CrearVentaCommandHandler` nunca
espera a `facturador-by-rc` — queda `PENDIENTE` y un
`FacturacionDispatcherBackgroundService` (mismo esqueleto que
`NotificacionDispatcherBackgroundService`: reintentos con backoff, nunca
borra filas) hace el POST real. El webhook de vuelta actualiza el estado.
`EstadosSiat.CONTINGENCIA` ya existe para cuando `facturador-by-rc` (o el
SIN detrás) no responde.

## 6. Lo que sigue abierto (más acotado que en la v1)

1. **Coordinación de timing entre los dos proyectos.** `facturador-by-rc`
   todavía no tiene persistencia, auth real ni cola de jobs funcionando
   (según su propio roadmap). ¿En qué orden conviene avanzar? Dos
   opciones: (a) RcComercial espera a que `facturador-by-rc` tenga al
   menos el flujo completo funcionando contra su propio `SiatFakeAdapter`
   antes de integrar; o (b) ambos avanzan en paralelo y se integran
   temprano contra un `facturador-by-rc` todavía incompleto, iterando
   juntos. Te recomendaría (a): integrar contra una API que cambia de
   forma todos los días es más retrabajo que esperar a que el contrato
   REST esté realmente estable.
2. **Concepto de "punto de venta".** `Factura` pide `PuntoVentaId` y
   RcComercial hoy modela `Sucursal` pero no "punto de venta" dentro de
   ella (una sucursal puede tener varias cajas/puntos SIAT). Hay que
   decidir si esto se agrega al modelo de RcComercial o si
   `Sucursal = PuntoVenta` por ahora (simplificación razonable para una
   pyme con una caja por sucursal).
3. **Quién da de alta la empresa como Tenant en `facturador-by-rc` y
   gestiona su API key.** ¿Un paso manual tuyo por ahora, o una pantalla
   en "Configuración" de RcComercial que llama al endpoint de onboarding
   de `facturador-by-rc` (todavía no existe, está en su roadmap)?
4. **DTO REST exacto.** No pude leer los controllers/DTOs de
   `Facturacion.Api` en esta sesión (llegué al límite de calls a la API
   de GitHub sin autenticar). Antes de escribir el adaptador HTTP en
   RcComercial hay que confirmar el shape exacto del `POST /api/v1/facturas`.
5. **Preguntas de la v1 que siguen abiertas:** estimación de cuándo
   tendrías el NIT de prueba (sigue "en trámite" según el propio CLAUDE.md
   del repo).

**Ya resueltas (no hace falta preguntarlas):** multi-tenant → sí, nativo.
Modalidad → Computarizada en Línea primero, Electrónica en Línea después.
Almacenamiento de credenciales → ya diseñado en `facturador-by-rc`
(`CredencialSiat`, `IProteccionDatos`, cifrado en reposo con clave
maestra por variable de entorno) — RcComercial no necesita construir
nada de esto, solo consumir la API.

## 7. Para el OK/NO-GO

- [ ] ¿De acuerdo con integrar por REST/webhook contra `facturador-by-rc`
      (§3), sin evaluar ya la opción de embeberlo?
- [ ] ¿Opción (a) o (b) del punto 1 de §6 — esperar a que
      `facturador-by-rc` esté más maduro, o avanzar en paralelo?
- [ ] ¿"Sucursal = punto de venta" por ahora está bien, o ya sabes que
      vas a necesitar varios puntos de venta por sucursal?
- [ ] Si das el OK a avanzar: arranco revisando `Facturacion.Api` a fondo
      (necesito acceso — GitHub sin autenticar se quedó sin cupo de
      requests) para confirmar el DTO real antes de tocar código en
      RcComercial.
