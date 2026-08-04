<script setup>
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue'
import { watchDebounced } from '@vueuse/core'
import { toast } from 'vue-sonner'
import { CircleX, Minus, Plus, Receipt, Search, Settings, User } from 'lucide-vue-next'
import { useVentaStore } from '@/stores/venta'
import { useAuthStore } from '@/stores/auth'
import { useConexion } from '@/composables/useConexion'
import { obtenerSesionAbierta, abrirCaja } from '@/api/caja'
import { buscarProductos, obtenerProductoPorId, obtenerProductoPorCodigoBarras } from '@/api/productos'
import { buscarEnCatalogoLocal, obtenerProductoLocalPorId, obtenerProductoLocalPorCodigoBarras } from '@/db/catalogoDb'
import { contarVentasRechazadas } from '@/db/ventasDb'
import ModalPresentacion from '@/components/pos/ModalPresentacion.vue'
import ModalReceta from '@/components/pos/ModalReceta.vue'
import ModalCobro from '@/components/pos/ModalCobro.vue'
import ModalCancelar from '@/components/pos/ModalCancelar.vue'

const venta = useVentaStore()
const auth = useAuthStore()
const { enLinea } = useConexion()

// ── Sesión de caja: se verifica al entrar; sin sesión abierta no se puede vender ──
const CLAVE_SESION_CAJA_CACHE = 'syscenters-caja-abierta-cache'
const cargandoCaja = ref(true)
const sesionCaja = ref(null)
const montoInicial = ref('')
const abriendoCaja = ref(false)
const errorCaja = ref('')

async function cargarSesionCaja() {
  cargandoCaja.value = true
  try {
    sesionCaja.value = await obtenerSesionAbierta()
    if (sesionCaja.value) localStorage.setItem(CLAVE_SESION_CAJA_CACHE, JSON.stringify(sesionCaja.value))
    else localStorage.removeItem(CLAVE_SESION_CAJA_CACHE)
  } catch {
    // Sin red no se puede confirmar el estado real de la caja: si el
    // cajero ya la había abierto en línea, seguimos con esa última sesión
    // conocida en vez de bloquear el POS entero por no tener conexión.
    const cache = localStorage.getItem(CLAVE_SESION_CAJA_CACHE)
    sesionCaja.value = cache ? JSON.parse(cache) : null
    if (sesionCaja.value) toast.info('Sin conexión: se usa la última sesión de caja conocida.')
  } finally {
    cargandoCaja.value = false
  }
}

async function abrirCajaAhora() {
  errorCaja.value = ''
  const monto = Number(montoInicial.value || 0)
  abriendoCaja.value = true
  try {
    sesionCaja.value = await abrirCaja(monto, null)
    localStorage.setItem(CLAVE_SESION_CAJA_CACHE, JSON.stringify(sesionCaja.value))
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorCaja.value = mensajes?.join(' ') || 'No se pudo abrir la caja.'
  } finally {
    abriendoCaja.value = false
  }
}

// ── Búsqueda de productos ──
const textoBusqueda = ref('')
const resultados = ref([])
const buscando = ref(false)
const inputBusqueda = ref(null)

async function ejecutarBusqueda() {
  buscando.value = true
  try {
    // IndexedDB primero: instantáneo y funciona sin red.
    resultados.value = await buscarEnCatalogoLocal(textoBusqueda.value)

    // El backend es solo complemento: si hay conexión y trae resultados
    // (mejor rankeados / más al día), los usamos; si falla o no hay red,
    // nos quedamos tranquilos con lo que ya mostramos desde IndexedDB.
    if (enLinea.value) {
      try {
        const remotos = await buscarProductos(textoBusqueda.value)
        if (remotos.length > 0 || resultados.value.length === 0) resultados.value = remotos
      } catch {
        /* seguimos con los resultados locales */
      }
    }
  } finally {
    buscando.value = false
  }
}

watchDebounced(textoBusqueda, ejecutarBusqueda, { debounce: 300 })

async function intentarCodigoBarras() {
  const texto = textoBusqueda.value.trim()
  if (!texto) return

  if (enLinea.value) {
    try {
      const resultado = await obtenerProductoPorCodigoBarras(texto)
      if (resultado?.esSugerencia) {
        toast.info(`"${resultado.sugerencia.nombre}" no está en tu catálogo todavía. Regístralo en Productos.`)
        return
      }
      if (resultado) {
        agregarProductoDesde(resultado.producto)
        textoBusqueda.value = ''
        await nextTick()
        inputBusqueda.value?.focus()
        return
      }
    } catch {
      /* sin red real pese a "en línea": probamos el catálogo local */
    }
  }

  const local = await obtenerProductoLocalPorCodigoBarras(texto)
  if (!local) return
  agregarProductoDesde(local)
  textoBusqueda.value = ''
  await nextTick()
  inputBusqueda.value?.focus()
}

// ── Carrito ──
const mostrarPresentacion = ref(false)
const productoSeleccionado = ref(null)
const ultimaVentaOffline = ref(null)

async function alTocarProducto(item) {
  // Ya viene completo del catálogo local (IndexedDB): no hace falta red.
  if (item.presentaciones) {
    agregarProductoDesde(item)
    return
  }
  try {
    const producto = await obtenerProductoPorId(item.id)
    agregarProductoDesde(producto)
  } catch {
    const local = await obtenerProductoLocalPorId(item.id)
    if (local) agregarProductoDesde(local)
    else toast.error('No se pudo cargar el producto. Intenta de nuevo.')
  }
}

function agregarProductoDesde(producto) {
  ultimaVentaOffline.value = null
  if (producto.presentaciones.length > 0) {
    productoSeleccionado.value = producto
    mostrarPresentacion.value = true
  } else {
    venta.agregarProducto(producto, null, 1)
  }
}

function alElegirPresentacion(presentacion) {
  venta.agregarProducto(productoSeleccionado.value, presentacion, 1)
}

function fmtBs(n) {
  const [ent, dec] = Number(n).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

// Nivel de stock por tarjeta de producto (badge del grid del POS): usa
// stockTotal/stockMinimo, ya presentes en buscarProductos() y en el
// catálogo local sincronizado — null si el producto todavía no trae esos
// campos (catálogo local sin resincronizar).
function nivelStock(p) {
  if (p.stockTotal == null) return null
  if (p.stockTotal <= 0) return 'agotado'
  if (p.stockMinimo != null && p.stockTotal <= p.stockMinimo) return 'bajo'
  return 'normal'
}

function fmtStock(n) {
  const num = Number(n)
  return Number.isInteger(num) ? num.toString() : num.toFixed(1)
}

const inicialesUsuario = computed(() => {
  const partes = auth.nombreUsuario.trim().split(/\s+/).filter(Boolean)
  if (partes.length === 0) return ''
  return (partes[0][0] + (partes[partes.length - 1][0] || '')).toUpperCase()
})

// ── Cobro / receta / cancelar ──
const mostrarReceta = ref(false)
const mostrarCobro = ref(false)
const mostrarCancelar = ref(false)

function iniciarCobro() {
  if (venta.vacia) return
  if (venta.requiereReceta && !venta.recetaCompleta) {
    mostrarReceta.value = true
    return
  }
  mostrarCobro.value = true
}

function alConfirmarReceta(datos) {
  venta.establecerReceta(datos)
  mostrarCobro.value = true
}

function alCrearVenta({ ventaCreada, offline }) {
  if (offline) {
    toast.warning(`Venta ${ventaCreada.numero} guardada sin conexión · ${fmtBs(ventaCreada.total)}`)
    ultimaVentaOffline.value = { numero: ventaCreada.numero, total: ventaCreada.total }
  } else {
    toast.success(`Venta ${ventaCreada.numero} registrada · ${fmtBs(ventaCreada.total)}`)
    ultimaVentaOffline.value = null
  }
  venta.iniciarNueva()
  nextTick(() => inputBusqueda.value?.focus())
}

// ── Atajos de teclado ──
function alPresionarTecla(e) {
  if (e.key === 'F2') {
    e.preventDefault()
    inputBusqueda.value?.focus()
  } else if (e.key === 'F12') {
    e.preventDefault()
    iniciarCobro()
  }
}

const rechazadasCount = ref(0)
let temporizadorRechazadas = null

async function actualizarRechazadasCount() {
  rechazadasCount.value = await contarVentasRechazadas()
}

onMounted(() => {
  cargarSesionCaja()
  ejecutarBusqueda()
  actualizarRechazadasCount()
  temporizadorRechazadas = setInterval(actualizarRechazadasCount, 30_000)
  window.addEventListener('keydown', alPresionarTecla)
})
onUnmounted(() => {
  window.removeEventListener('keydown', alPresionarTecla)
  clearInterval(temporizadorRechazadas)
})
</script>

<template>
  <!-- Verificación de sesión de caja al entrar -->
  <div v-if="cargandoCaja" class="flex h-full items-center justify-center p-6 text-tinta-2">Verificando caja…</div>

  <div v-else-if="!sesionCaja" class="flex h-full items-center justify-center px-4">
    <div class="w-full max-w-[360px] rounded border border-linea bg-superficie p-6 text-center shadow">
      <p class="font-display text-[19.2px] font-bold text-tinta">Abrir caja</p>
      <p class="mt-1 text-[13.6px] text-tinta-2">Necesitas una sesión de caja abierta para empezar a vender.</p>

      <div v-if="errorCaja" class="mt-4 rounded-s bg-peligro-tenue px-4 py-3 text-left text-[13px] text-peligro">
        {{ errorCaja }}
      </div>

      <label class="mt-4 flex flex-col gap-1.5 text-left">
        <span class="text-[0.8rem] font-semibold text-tinta-2">Monto inicial (Bs)</span>
        <input
          v-model="montoInicial"
          type="number"
          min="0"
          step="0.01"
          class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
        />
      </label>

      <button
        type="button"
        :disabled="abriendoCaja"
        class="mt-4 min-h-11 w-full rounded-s bg-marca font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
        @click="abrirCajaAhora"
      >
        {{ abriendoCaja ? 'Abriendo…' : 'Abrir caja' }}
      </button>
    </div>
  </div>

  <!-- Layout POS -->
  <div v-else class="fixed inset-x-0 top-0 bottom-28 flex flex-col md:bottom-0 md:left-64">
    <!-- Header: barra completa, al ras (sin margen/borde redondeado) -->
    <header class="flex h-16 flex-shrink-0 items-center justify-between border-b border-linea bg-superficie px-4 md:px-6">
      <div class="flex flex-wrap items-center gap-3">
        <div class="flex items-center gap-2 rounded-chip bg-superficie-2 px-3 py-1.5">
          <span class="h-2 w-2 animate-pulse rounded-full bg-exito" aria-hidden="true"></span>
          <span class="text-[11px] font-bold uppercase tracking-wide text-tinta-2">Caja activa</span>
        </div>
        <div class="hidden h-4 w-px bg-linea sm:block" aria-hidden="true"></div>
        <div class="hidden items-center gap-1.5 sm:flex">
          <User class="h-4 w-4 text-tinta-3" />
          <span class="text-[13px] text-tinta-2">
            Operador: <span class="font-bold text-tinta">{{ auth.nombreUsuario || '—' }}</span>
          </span>
        </div>
      </div>
      <div class="flex items-center gap-4">
        <RouterLink
          v-if="rechazadasCount > 0"
          :to="{ name: 'ventas-revisar' }"
          class="rounded-chip bg-peligro-tenue px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide text-peligro hover:underline"
        >
          {{ rechazadasCount }} por revisar
        </RouterLink>
        <RouterLink
          :to="{ name: 'ajustes' }"
          class="text-tinta-2 transition-colors hover:text-marca"
          aria-label="Ajustes"
        >
          <Settings class="h-5 w-5" />
        </RouterLink>
        <div
          class="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-marca-tenue text-[11px] font-bold text-marca"
          :title="auth.nombreUsuario"
        >
          {{ inicialesUsuario }}
        </div>
      </div>
    </header>

    <!-- Espacio de trabajo: grid de productos + ticket -->
    <div class="flex min-h-0 flex-1 flex-col md:flex-row">
      <!-- Columna izquierda: búsqueda + grid -->
      <section class="flex min-h-0 flex-1 flex-col p-4 md:p-6" aria-label="Catálogo de productos">
        <div class="relative mb-4">
          <Search class="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-tinta-3" />
          <input
            ref="inputBusqueda"
            v-model="textoBusqueda"
            type="text"
            autocomplete="off"
            placeholder="Buscar producto o código de barras…"
            class="min-h-11 w-full rounded border-[1.5px] border-transparent bg-superficie-2 py-4 pl-12 pr-14 text-tinta outline-none transition-colors placeholder:text-tinta-3 focus:border-marca focus:bg-superficie"
            @keydown.enter.prevent="intentarCodigoBarras"
          />
          <span
            class="absolute right-4 top-1/2 -translate-y-1/2 rounded border border-linea bg-superficie px-1.5 py-0.5 text-[10px] text-tinta-3"
          >
            F2
          </span>
        </div>

        <div class="min-h-0 flex-1 overflow-y-auto pr-1">
          <div v-if="!buscando && resultados.length === 0" class="p-6 text-center text-[13.6px] text-tinta-3">
            No se encontraron productos.
          </div>
          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            <button
              v-for="p in resultados"
              :key="p.id"
              type="button"
              class="flex min-h-[124px] flex-col rounded border-[1.5px] border-linea bg-superficie p-4 text-left transition-all hover:border-marca hover:shadow-sm active:scale-[.98]"
              @click="alTocarProducto(p)"
            >
              <span class="font-display text-[15px] leading-tight text-tinta">{{ p.nombre }}</span>
              <span v-if="p.codigo" class="mt-1 font-mono text-[11px] text-tinta-3">{{ p.codigo }}</span>
              <div class="mt-auto flex items-end justify-between gap-2 pt-3">
                <span class="font-mono text-[16px] tabular-nums text-marca">{{ fmtBs(p.precioBase) }}</span>
                <span
                  v-if="nivelStock(p)"
                  class="whitespace-nowrap rounded-chip px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide"
                  :class="{
                    'bg-exito-tenue text-exito': nivelStock(p) === 'normal',
                    'bg-aviso-tenue text-aviso': nivelStock(p) === 'bajo',
                    'bg-peligro-tenue text-peligro': nivelStock(p) === 'agotado',
                  }"
                >
                  {{ fmtStock(p.stockTotal) }} en stock
                </span>
              </div>
            </button>
          </div>
        </div>
      </section>

      <!-- Columna derecha: ticket (firma dentada) -->
      <section
        class="flex max-h-[46vh] flex-shrink-0 flex-col border-t border-linea bg-papel p-4 md:max-h-none md:w-[400px] md:border-l md:border-t-0 md:p-6"
        aria-label="Venta en curso"
      >
      <div class="flex min-h-0 flex-1 flex-col" style="filter: drop-shadow(0 4px 10px rgba(43, 48, 45, 0.08))">
        <div class="flex min-h-0 flex-1 flex-col rounded-t-lg bg-white">
          <div class="border-b border-dashed border-linea p-5 text-center">
            <h2 class="font-display text-[17px] font-semibold tracking-wide text-tinta">VENTA EN CURSO</h2>
          </div>

          <div v-if="ultimaVentaOffline" class="flex items-center justify-between gap-2 bg-aviso-tenue px-4 py-2.5">
            <span class="font-mono text-[11px] text-aviso">VENTA {{ ultimaVentaOffline.numero }}</span>
            <span class="text-[11px] font-bold uppercase tracking-wide text-aviso">Pendiente de sincronizar</span>
          </div>

          <div class="min-h-0 flex-1 overflow-y-auto px-4 py-2">
            <table v-if="venta.items.length" class="w-full border-collapse">
              <thead>
                <tr>
                  <th class="border-b border-linea py-2 text-left text-[10px] font-bold uppercase tracking-wide text-tinta-3">
                    Descripción
                  </th>
                  <th class="border-b border-linea py-2 text-center text-[10px] font-bold uppercase tracking-wide text-tinta-3">
                    Cant.
                  </th>
                  <th class="border-b border-linea py-2 text-right text-[10px] font-bold uppercase tracking-wide text-tinta-3">
                    Total
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in venta.items" :key="item.clave">
                  <td class="border-b border-linea/50 py-3 align-middle text-[13.6px]">
                    <p class="font-medium text-tinta">{{ item.nombre }}</p>
                    <p class="text-[11px] text-tinta-3">
                      {{ item.presentacionNombre || 'Unidad' }} · {{ fmtBs(item.precioUnitario) }}/u
                      <template v-if="item.factor !== 1">
                        · = {{ item.cantidad * item.factor }} unidades
                      </template>
                    </p>
                  </td>
                  <td class="border-b border-linea/50 py-3 align-middle">
                    <div class="flex items-center justify-center gap-2">
                      <button
                        type="button"
                        class="flex h-6 w-6 items-center justify-center rounded-full bg-superficie-2 hover:bg-linea"
                        :aria-label="`Quitar una unidad de ${item.nombre}`"
                        @click="venta.cambiarCantidad(item.clave, -1)"
                      >
                        <Minus class="h-3.5 w-3.5" />
                      </button>
                      <span class="min-w-[16px] text-center tabular-nums">{{ item.cantidad }}</span>
                      <button
                        type="button"
                        class="flex h-6 w-6 items-center justify-center rounded-full bg-superficie-2 hover:bg-linea"
                        :aria-label="`Añadir una unidad de ${item.nombre}`"
                        @click="venta.cambiarCantidad(item.clave, 1)"
                      >
                        <Plus class="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </td>
                  <td class="border-b border-linea/50 py-3 text-right tabular-nums">
                    {{ fmtBs(item.cantidad * item.precioUnitario - item.descuento) }}
                  </td>
                </tr>
              </tbody>
            </table>
            <div v-else class="p-8 text-center text-[13.6px] text-tinta-3">
              Busca o toca un producto para añadirlo a la venta.
            </div>
          </div>

          <div class="border-t border-dashed border-linea px-5 pb-5 pt-4">
            <div class="flex items-end justify-between">
              <span class="font-display text-[17px] font-semibold text-tinta">TOTAL</span>
              <span class="font-display text-[28px] font-bold tabular-nums text-marca">{{ fmtBs(venta.total) }}</span>
            </div>
            <p class="mt-1 text-right text-[11px] text-tinta-3">Impuestos incluidos (IVA 13 %)</p>
          </div>
        </div>

        <div
          class="h-2.5 flex-shrink-0"
          style="
            background: #fff;
            mask: linear-gradient(-45deg, #000 7px, transparent 0), linear-gradient(45deg, #000 7px, transparent 0);
            mask-size: 14px 14px;
            mask-position: left top;
            mask-repeat: repeat-x;
            -webkit-mask: linear-gradient(-45deg, #000 7px, transparent 0), linear-gradient(45deg, #000 7px, transparent 0);
            -webkit-mask-size: 14px 14px;
            -webkit-mask-position: left top;
            -webkit-mask-repeat: repeat-x;
          "
          aria-hidden="true"
        ></div>
      </div>

      <div class="mt-4 flex flex-shrink-0 flex-col gap-3">
        <button
          type="button"
          :disabled="venta.vacia"
          class="flex items-center justify-center gap-3 rounded bg-[#25514B] py-5 font-display font-bold uppercase tracking-wide text-sobre-marca transition-all hover:bg-[#25514B]/90 active:scale-[.98] disabled:opacity-50"
          @click="iniciarCobro"
        >
          <Receipt class="h-5 w-5" />
          Cobrar venta
          <span class="rounded border border-sobre-marca/30 bg-sobre-marca/10 px-1.5 py-0.5 text-[10px] normal-case">F12</span>
        </button>
        <button
          type="button"
          :disabled="venta.vacia"
          class="flex min-h-11 items-center justify-center gap-2 rounded border border-linea text-tinta-2 transition-colors hover:text-peligro disabled:opacity-50"
          @click="mostrarCancelar = true"
        >
          <CircleX class="h-4 w-4" />
          Cancelar Operación
        </button>
      </div>
      </section>
    </div>
  </div>

  <ModalPresentacion
    v-if="productoSeleccionado"
    v-model="mostrarPresentacion"
    :producto="productoSeleccionado"
    @seleccionar="alElegirPresentacion"
  />
  <ModalReceta v-model="mostrarReceta" @confirmar="alConfirmarReceta" />
  <ModalCobro v-model="mostrarCobro" @venta-creada="alCrearVenta" />
  <ModalCancelar v-model="mostrarCancelar" />
</template>
