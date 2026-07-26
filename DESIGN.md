# DESIGN.md — Sistema de diseño SysCenterS

> Instrucciones para Claude Code: este documento es la ÚNICA fuente de verdad
> visual del frontend (rc-comercial-fe). Toda pantalla nueva se construye con
> estos tokens y patrones. No inventar colores, fuentes ni componentes nuevos;
> si una pantalla parece necesitar algo que no está aquí, preguntar primero.
> Referencia visual aprobada: design/syscenters-mockup.html (4 vistas).

## 1. Identidad

- Nombre del producto: **SysCenterS**
- Personalidad: herramienta de trabajo seria y descansada. Un cajero la mira
  8 horas; un dueño la revisa desde el celular. Nada grita, todo se lee.
- Elemento firma: **el ticket** — el carrito del POS es un ticket de venta con
  borde inferior dentado; el mismo borde remata la tarjeta de login. No usar
  el borde dentado en ningún otro lugar (la firma se gasta si se repite).

## 2. Paleta ("bajo consumo visual")

Regla madre: **nunca blanco #FFF ni negro #000**. Croma bajo en todo.
Un solo color de marca; si todo es petróleo, nada es importante.

### Tema Día (default)
| Token | Hex | Uso |
|---|---|---|
| `--papel` | `#EDECE6` | fondo de la app |
| `--superficie` | `#F6F5F0` | tarjetas, paneles, tabla |
| `--superficie-2` | `#E4E3DC` | inputs, zonas hundidas, botones suaves |
| `--linea` | `#D4D3CA` | bordes y divisores |
| `--tinta` | `#2B302D` | texto principal |
| `--tinta-2` | `#5F6763` | texto secundario |
| `--tinta-3` | `#8B928D` | placeholders, pistas, metadatos |
| `--marca` | `#3E6963` | SOLO acciones, foco, selección, enlaces |
| `--marca-hover` | `#345A55` | hover de acciones |
| `--marca-tenue` | `#DCE5E1` | fondo de fila/opción seleccionada |
| `--sobre-marca` | `#F2F5F3` | texto sobre `--marca` |
| `--ocre` | `#9C7A3F` | acento de montos/datos destacados (con moderación) |
| `--exito` / `--exito-tenue` | `#4E7A5D` / `#DEE8E0` | confirmaciones, "abierta" |
| `--peligro` / `--peligro-tenue` | `#A05A4C` / `#ECDFDB` | errores, diferencias de caja |
| `--aviso` / `--aviso-tenue` | `#A5833F` / `#ECE4D2` | vencimientos, stock bajo |

### Tema Noche (conmutables por el usuario; persistir preferencia)
`--papel #1D2220 · --superficie #262C29 · --superficie-2 #1A1F1D · --linea #363D3A ·
--tinta #D9DBD5 · --tinta-2 #9AA29D · --tinta-3 #6C736F · --marca #6E9A92 ·
--marca-hover #7FABA3 · --marca-tenue #2E3B38 · --sobre-marca #16201E ·
--ocre #C2A26A · --exito #7BA98B/#2A3630 · --peligro #C08574/#3A2C28 · --aviso #BFA066/#383021`

Implementación: CSS custom properties en `:root` y `[data-tema="noche"]`;
en Tailwind v4 mapear con `@theme` a estos tokens. Los componentes usan
SIEMPRE los tokens, jamás un hex directo.

## 3. Tipografía

| Rol | Fuente | Uso |
|---|---|---|
| Display | **Bricolage Grotesque** (500–700) | logo, títulos de pantalla, montos hero del panel |
| UI / cuerpo | **Instrument Sans** (400–700) | todo lo demás |
| Datos | **Spline Sans Mono** (400–500) | códigos, números de documento, lotes, kbd |

- **`font-variant-numeric: tabular-nums` global**: los montos siempre alinean.
- Fuentes **self-hosted** (woff2 en `/public/fonts` + `@font-face`): la PWA
  debe abrir sin internet; nada de Google Fonts CDN en producción.
- Escala: 12.8 / 13.6 / 15 (base) / 17 / 19.2 / 24 / 32 px. Peso antes que
  tamaño para jerarquizar dentro de un mismo bloque.
- Etiquetas de sección/tabla: 0.72rem, peso 700, uppercase, letter-spacing .06em, `--tinta-3`.

## 4. Espaciado, forma y elevación

- Espaciado en escala de 4px (Tailwind estándar). Densidad: cómoda en
  formularios, compacta en tablas y en el ticket.
- Radios: `--radio: 10px` (tarjetas), `--radio-s: 7px` (botones, inputs), `999px` (chips/badges).
- Sombra única y sutil (`--sombra`); la elevación se comunica con borde + sombra,
  nunca con sombras dramáticas.
- Targets táctiles mínimos **44×44px** en POS y panel (se usan con el dedo).

## 5. Componentes (anatomía fija)

- **Botón primario**: fondo `--marca`, texto `--sobre-marca`, peso 700. Uno por
  vista como máximo. Dice lo que hace: "Guardar producto", "Cobrar · Bs 41,50" — nunca "Aceptar/Enviar".
- **Botón secundario**: borde `--linea`, texto `--tinta-2`, fondo transparente.
- **Inputs**: fondo `--superficie-2`, borde 1.5px `--linea`; foco = borde `--marca`
  y fondo `--superficie`. Etiqueta arriba (0.8rem, 600, `--tinta-2`); ayuda/error
  debajo del campo (error en `--peligro`, borde del campo en `--peligro`).
  Los errores viven junto al campo, JAMÁS en toast/popup.
- **Badges de estado**: fondo tenue + texto del color pleno, minúsculas
  ("activo", "stock bajo", "vence 09/26").
- **Tablas**: encabezado uppercase `--tinta-3`; fila completa clicable con hover
  `--marca-tenue` (sin iconos de lápiz/basurero por celda); montos `.num`
  alineados a la derecha; acciones destructivas dentro del detalle, nunca en la fila.
- **Toasts** (vue-sonner): solo confirmaciones de acciones ("Producto guardado")
  y errores de red. Espejo del botón: "Guardar producto" → "Producto guardado".
- **Modales**: solo para confirmar acciones destructivas y para el flujo de
  cobro del POS. Todo lo demás navega a su pantalla.

## 6. Patrones de pantalla (cubren todo el sistema)

- **PATRÓN SHELL** (adoptado de la iteración v2): en escritorio, sidebar
  izquierda fija con logo arriba, navegación con icono+texto (item activo:
  fondo `--marca-tenue`, texto `--marca`) y "Cerrar sesión" abajo; el contenido
  vive en `--papel`. En móvil, la navegación es una barra inferior de 4-5
  pestañas (el POS siempre presente). Iconos: lucide (o Material Symbols
  outlined); un solo set en toda la app, nunca mezclados.
- **Barras de stock** (listado de inventario): barra fina de progreso junto a
  la cantidad — `--marca` nivel normal, `--aviso` bajo mínimo, `--peligro`
  agotado. Es la excepción autorizada de color en tablas.
- **PATRÓN POS** (mockup vista 1): 2 columnas ≥900px (búsqueda+grid / ticket);
  en móvil, el ticket es hoja inferior deslizable. Atajos: F2 buscar, F12 cobrar,
  +/− cantidades. El total es el texto más grande de la pantalla. En el ticket,
  el precio siempre muestra "IVA 13 % incluido" como línea informativa; jamás
  desglosar impuestos como suma al total (precio final = precio al público).
- **PATRÓN PANEL** (vista 2): móvil-first; tarjeta hero `--marca` con el monto
  del día en Bricolage; tarjeta "Necesitan tu atención" con alertas accionables.
- **PATRÓN LOGIN** (vista 3): tarjeta centrada ≤380px, símbolo de marca,
  nombre de la empresa (el tenant se resuelve por subdominio/config, no se
  escribe), borde de ticket al pie. Errores específicos y humanos:
  "La contraseña no es correcta. Te quedan 3 intentos."
- **PATRÓN FORMULARIO** (vista 4-A): tarjeta única ≤640px, campos apilados,
  máximo 2 por fila, checkboxes como tarjetas `.check`, acciones abajo-derecha
  (Cancelar + primario). Aplica a productos, clientes, proveedores, usuarios, config.
- **PATRÓN LISTADO** (vista 4-B): cabecera = buscador izquierda + "+ Nuevo"
  derecha; tabla según §5; paginación o scroll infinito según el volumen.
- **PATRÓN VACÍO** (vista 4-C): qué no hay + cómo crear lo primero. Nunca
  una tabla vacía muda ni un spinner eterno.

## 7. Redacción en la interfaz

- Español boliviano, voz activa, sentence case (nunca Title Case), tuteo.
- Moneda: `Bs 1.234,50` (punto de miles, coma decimal). Fechas: `26/07/2026`,
  horas 24h. Nombres del mundo del usuario: "fiado", "caja", "vencimientos" —
  no "cuentas por cobrar", "sesión de terminal", "fechas de expiración".
- Los errores dicen qué pasó y qué hacer; no se disculpan ni son vagos.

## 8. Accesibilidad y calidad (piso no negociable)

- Contraste AA mínimo en ambos temas (los tokens ya lo cumplen; verificar
  cualquier combinación nueva).
- Foco visible SIEMPRE: `outline 2px var(--marca), offset 2px`. El POS se
  opera completo por teclado.
- `prefers-reduced-motion`: desactivar transiciones. Las animaciones existentes
  son micro (≤150ms, ease) y solo en hover/estado; nada decorativo.
- Responsive real: POS y listados usables desde 360px de ancho.

## 9. Stack de implementación (Fase 7)

Vue 3 + Vite + Pinia + Router + Axios (interceptores JWT/refresh) + Tailwind v4
(tokens vía `@theme`) + **Reka UI** (headless: diálogos, select, combobox, tabs)
+ VeeValidate/Zod + ApexCharts (panel) + lucide-vue-next + vue-sonner + dayjs
+ VueUse + vite-plugin-pwa. Prohibido: librerías de componentes con tema propio
(Vuetify, PrimeVue temado, Element) — rompen la identidad.

## 10. Contra qué revisar cada pantalla nueva (checklist)

- [ ] ¿Solo tokens? (cero hex sueltos, cero #FFF/#000, cero alias tipo `--marca-2`)
- [ ] ¿Un solo botón primario? ¿Dice lo que hace?
- [ ] ¿Montos tabulares alineados a la derecha? ¿Mono solo en códigos/lotes?
- [ ] ¿Todo en español boliviano, moneda `Bs 1.234,50`, IVA 13 % incluido?
- [ ] ¿Funciona en tema noche sin ajustes manuales?
- [ ] ¿Targets ≥44px, foco visible, teclado completo?
- [ ] ¿Textos en el idioma del usuario (§7)?
- [ ] ¿Sigue el patrón de su familia (§6) sin inventar anatomía nueva?
- [ ] ¿La firma dentada aparece SOLO en el ticket del POS y el pie del login?

## 11. Gobierno del sistema

Este archivo es el ÚNICO DESIGN.md del proyecto. Si una herramienta de diseño
(Claude Design u otra) exporta tokens o documentos por pantalla, esos archivos
son artefactos descartables generados DESDE este maestro — nunca se commitean
ni se editan por separado. Cualquier aporte bueno de una iteración (un patrón
nuevo, un componente) se incorpora AQUÍ primero y recién entonces existe.
