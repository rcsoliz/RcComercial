<script setup>
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useDocumentVisibility, useIntervalFn } from '@vueuse/core'
import VueApexCharts from 'vue3-apexcharts'
import dayjs from 'dayjs'
import { toast } from 'vue-sonner'
import { AlertTriangle, Banknote, PackageSearch, RefreshCw, TrendingUp } from 'lucide-vue-next'
import { useTema } from '@/composables/useTema'
import { leerColorToken } from '@/utils/colorTokens'
import { obtenerPanelAlertas, obtenerPanelHistorico, obtenerPanelHoy } from '@/api/panel'

const router = useRouter()
const { tema } = useTema()

// Valores por defecto en cero: el panel nunca debe crashear ni mostrar
// huecos, ni siquiera si la primera carga falla (queda en 0, no en null).
const cargando = ref(true)
const panelHoy = ref({
  totalVendido: 0,
  numeroVentas: 0,
  ticketPromedio: 0,
  ventasPorUsuario: [],
  numeroAnulaciones: 0,
  montoAnulaciones: 0,
  montoDescuentos: 0,
  topProductos: [],
  cajasAbiertas: [],
  montosPorMetodoPago: [],
})
const panelAlertas = ref({
  productosBajoMinimo: [],
  lotesPorVencer30: [],
  lotesPorVencer60: [],
  lotesPorVencer90: [],
  diferenciasCaja: [],
})
const historico = ref([])

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

async function cargarTodo({ silencioso = false } = {}) {
  const hoy = dayjs()
  const desde = hoy.subtract(6, 'day').format('YYYY-MM-DD')
  const hasta = hoy.format('YYYY-MM-DD')
  try {
    const [hoyDto, alertasDto, historicoDto] = await Promise.all([
      obtenerPanelHoy(),
      obtenerPanelAlertas(),
      obtenerPanelHistorico(desde, hasta),
    ])
    panelHoy.value = hoyDto
    panelAlertas.value = alertasDto
    historico.value = historicoDto
  } catch {
    if (!silencioso) toast.error('No se pudo actualizar el panel. Verifica tu conexión.')
  } finally {
    cargando.value = false
  }
}

// ── Auto-refresco cada 60s, en pausa si la pestaña no está visible ──
const visibilidad = useDocumentVisibility()
const { pause, resume } = useIntervalFn(() => cargarTodo({ silencioso: true }), 60_000)

watch(visibilidad, (v) => {
  if (v === 'hidden') {
    pause()
  } else {
    resume()
    cargarTodo({ silencioso: true })
  }
})

// ── Pull-to-refresh (móvil) ──
const distanciaPull = ref(0)
const refrescandoPull = ref(false)
let arrastrando = false
let inicioY = 0

function alTocarInicio(e) {
  if (window.scrollY > 0) return
  inicioY = e.touches[0].clientY
  arrastrando = true
}
function alMover(e) {
  if (!arrastrando) return
  const delta = e.touches[0].clientY - inicioY
  if (delta > 0 && window.scrollY === 0) {
    distanciaPull.value = Math.min(delta * 0.5, 80)
  } else {
    arrastrando = false
    distanciaPull.value = 0
  }
}
async function alSoltar() {
  if (!arrastrando) return
  arrastrando = false
  if (distanciaPull.value > 56) {
    refrescandoPull.value = true
    await cargarTodo()
    refrescandoPull.value = false
  }
  distanciaPull.value = 0
}

onMounted(() => cargarTodo())
onUnmounted(() => pause())

// ── Histórico de 7 días completos (rellena días sin ventas con 0) ──
const serieDias = computed(() => {
  const dias = []
  for (let i = 6; i >= 0; i--) dias.push(dayjs().subtract(i, 'day').format('YYYY-MM-DD'))
  return dias.map((dia) => historico.value.find((h) => h.dia === dia)?.total ?? 0)
})

const seriesSparkline = computed(() => [{ name: 'Ventas', data: serieDias.value }])
const opcionesSparkline = computed(() => {
  void tema.value // fuerza recomputar colores al cambiar de tema
  const sobreMarca = leerColorToken('--sobre-marca')
  const tinta = leerColorToken('--tinta')
  return {
    chart: { type: 'area', sparkline: { enabled: true }, animations: { enabled: false } },
    stroke: { curve: 'smooth', width: 2, colors: [sobreMarca] },
    fill: { type: 'solid', colors: [sobreMarca], opacity: 0.22 },
    tooltip: {
      // custom (no theme:'dark'): así el tooltip también sale 100% de tokens,
      // no de la paleta gris genérica que trae ApexCharts por defecto.
      custom: ({ series, seriesIndex, dataPointIndex }) => {
        const dias = []
        for (let i = 6; i >= 0; i--) dias.push(dayjs().subtract(i, 'day'))
        const etiqueta = dias[dataPointIndex]?.format('dddd DD/MM') ?? ''
        const valor = fmtBs(series[seriesIndex][dataPointIndex])
        return `<div style="background:${tinta};color:${sobreMarca};padding:6px 10px;border-radius:6px;font-size:12px;font-family:'Instrument Sans',sans-serif">
          <div style="opacity:.7;text-transform:capitalize">${etiqueta}</div>
          <div style="font-weight:700">${valor}</div>
        </div>`
      },
    },
  }
})

// ── Métodos de pago ──
const coloresMetodo = ['bg-marca', 'bg-ocre', 'bg-aviso', 'bg-tinta-3']
const etiquetasMetodo = { EFECTIVO: 'Efectivo', QR: 'QR / Digital', TARJETA: 'Tarjeta', TRANSFERENCIA: 'Transferencia' }

const metodosPago = computed(() => {
  const lista = panelHoy.value?.montosPorMetodoPago ?? []
  const total = lista.reduce((acc, m) => acc + m.monto, 0)
  if (total <= 0) return []
  return lista.map((m, i) => ({
    etiqueta: etiquetasMetodo[m.metodo] ?? m.metodo,
    monto: m.monto,
    porcentaje: Math.round((m.monto / total) * 100),
    color: coloresMetodo[i % coloresMetodo.length],
  }))
})

// ── Alertas: producto+vencimientos navegan a Productos; diferencias de caja son informativas ──
const alertas = computed(() => {
  const a = panelAlertas.value
  if (!a) return []
  const lista = []

  for (const d of a.diferenciasCaja) {
    lista.push({
      tipo: 'peligro',
      titulo: 'Diferencia de caja',
      detalle: `${d.usuarioNombre} · ${fmtBs(Math.abs(d.diferencia))} de diferencia el ${dayjs(d.cierre).format('DD/MM')}.`,
      accion: null,
    })
  }

  if (a.productosBajoMinimo.length > 0) {
    const nombres = a.productosBajoMinimo.slice(0, 3).map((p) => p.nombre).join(', ')
    lista.push({
      tipo: 'aviso',
      titulo: 'Stock bajo',
      detalle: `${a.productosBajoMinimo.length} producto(s) por debajo del mínimo: ${nombres}${a.productosBajoMinimo.length > 3 ? '…' : ''}.`,
      accion: { etiqueta: 'Ver productos', ir: () => router.push({ name: 'productos' }) },
    })
  }

  if (a.lotesPorVencer30.length > 0) {
    lista.push({
      tipo: 'aviso',
      titulo: 'Productos por vencer',
      detalle: `${a.lotesPorVencer30.length} lote(s) vencen en los próximos 30 días.`,
      accion: { etiqueta: 'Ver productos', ir: () => router.push({ name: 'productos' }) },
    })
  }

  return lista
})

const estadoCaja = computed(() => {
  const cajas = panelHoy.value?.cajasAbiertas ?? []
  return {
    abierta: cajas.length > 0,
    texto:
      cajas.length === 0
        ? 'Ninguna caja abierta'
        : cajas.length === 1
          ? cajas[0].usuarioNombre
          : `${cajas.length} cajas: ${cajas.map((c) => c.usuarioNombre).join(', ')}`,
  }
})
</script>

<template>
  <div
    class="relative p-4 md:p-6"
    @touchstart.passive="alTocarInicio"
    @touchmove.passive="alMover"
    @touchend="alSoltar"
  >
    <div
      class="pointer-events-none absolute left-1/2 top-2 z-10 flex -translate-x-1/2 items-center gap-2 text-tinta-2 transition-opacity md:hidden"
      :style="{ opacity: distanciaPull > 0 || refrescandoPull ? 1 : 0, transform: `translate(-50%, ${Math.min(distanciaPull, 56)}px)` }"
    >
      <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': refrescandoPull }" />
      <span class="text-[12px]">{{ refrescandoPull ? 'Actualizando…' : 'Suelta para actualizar' }}</span>
    </div>

    <div v-if="cargando" class="flex justify-center py-16 text-tinta-2">Cargando panel…</div>

    <div v-else class="mx-auto grid max-w-[1280px] grid-cols-1 gap-6 md:grid-cols-3">
      <!-- Hero: ventas de hoy -->
      <section
        class="relative overflow-hidden rounded bg-marca p-6 shadow md:col-span-2 md:p-8"
        aria-label="Ventas de hoy"
      >
        <TrendingUp class="pointer-events-none absolute -right-2 -top-2 h-24 w-24 text-sobre-marca opacity-10" />

        <p class="text-[11px] font-bold uppercase tracking-[.1em] text-sobre-marca/70">Ventas de hoy</p>
        <p class="mt-1 font-display text-[38px] font-bold leading-none tracking-tight text-sobre-marca tabular-nums md:text-[48px]">
          {{ fmtBs(panelHoy.totalVendido) }}
        </p>

        <div class="mt-6 flex flex-wrap items-end justify-between gap-6">
          <div class="flex gap-8">
            <div>
              <p class="text-[10px] font-bold uppercase tracking-wide text-sobre-marca/60">Ticket promedio</p>
              <p class="mt-1 text-[17px] font-semibold tabular-nums text-sobre-marca">{{ fmtBs(panelHoy.ticketPromedio) }}</p>
            </div>
            <div>
              <p class="text-[10px] font-bold uppercase tracking-wide text-sobre-marca/60">Transacciones</p>
              <p class="mt-1 text-[17px] font-semibold tabular-nums text-sobre-marca">{{ panelHoy.numeroVentas }}</p>
            </div>
            <div>
              <p class="text-[10px] font-bold uppercase tracking-wide text-sobre-marca/60">Anuladas</p>
              <p class="mt-1 text-[17px] font-semibold tabular-nums text-sobre-marca">{{ panelHoy.numeroAnulaciones }}</p>
            </div>
          </div>

          <div class="h-12 w-full max-w-[160px]">
            <VueApexCharts type="area" height="48" :options="opcionesSparkline" :series="seriesSparkline" />
          </div>
        </div>
      </section>

      <!-- Columna estado: caja + métodos de pago -->
      <div class="grid grid-cols-2 gap-4 max-[479px]:grid-cols-1 md:contents">
        <div class="flex flex-col justify-between gap-4 rounded border border-linea bg-superficie p-5 shadow">
          <div class="flex items-center justify-between">
            <Banknote class="h-5 w-5 text-marca" />
            <span
              class="rounded-chip px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wide"
              :class="estadoCaja.abierta ? 'bg-exito-tenue text-exito' : 'bg-superficie-2 text-tinta-3'"
            >
              {{ estadoCaja.abierta ? 'Abierta' : 'Cerrada' }}
            </span>
          </div>
          <div>
            <p class="text-[10px] font-bold uppercase tracking-wide text-tinta-3">Estado de caja</p>
            <p class="mt-1 font-display text-[15px] font-semibold text-tinta">{{ estadoCaja.texto }}</p>
          </div>
        </div>

        <div class="flex flex-col gap-3 rounded border border-linea bg-superficie p-5 shadow">
          <p class="text-[10px] font-bold uppercase tracking-wide text-tinta-3">Métodos de pago</p>
          <div v-if="metodosPago.length === 0" class="text-[12.8px] text-tinta-3">Sin ventas registradas hoy.</div>
          <div v-for="m in metodosPago" :key="m.etiqueta">
            <div class="flex items-baseline justify-between text-[12px]">
              <span class="text-tinta-2">{{ m.etiqueta }}</span>
              <span class="font-semibold tabular-nums">{{ m.porcentaje }} %</span>
            </div>
            <div class="my-1.5 h-1 w-full overflow-hidden rounded-chip bg-superficie-2">
              <div class="h-full rounded-chip" :class="m.color" :style="{ width: m.porcentaje + '%' }"></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Alertas -->
      <section class="md:col-span-2" aria-label="Alertas pendientes">
        <div class="mb-4 flex items-center justify-between">
          <h2 class="flex items-center gap-2 font-display text-[17px] font-semibold text-tinta">
            Necesitan tu atención
            <span v-if="alertas.length" class="h-2 w-2 rounded-full bg-peligro" aria-hidden="true"></span>
          </h2>
        </div>

        <div v-if="alertas.length === 0" class="rounded border border-linea bg-superficie px-5 py-8 text-center text-[13.6px] text-tinta-2">
          Todo en orden por ahora.
        </div>

        <div v-else class="flex flex-col gap-3">
          <div
            v-for="(a, i) in alertas"
            :key="i"
            class="flex gap-4 rounded p-4"
            :class="a.tipo === 'peligro' ? 'bg-peligro-tenue' : 'bg-aviso-tenue'"
          >
            <div class="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-s bg-superficie">
              <AlertTriangle class="h-5 w-5" :class="a.tipo === 'peligro' ? 'text-peligro' : 'text-aviso'" />
            </div>
            <div class="min-w-0">
              <p class="font-display text-[14px] font-semibold" :class="a.tipo === 'peligro' ? 'text-peligro' : 'text-aviso'">
                {{ a.titulo }}
              </p>
              <p class="mt-0.5 text-[12px] text-tinta-2">{{ a.detalle }}</p>
              <button
                v-if="a.accion"
                type="button"
                class="mt-3 inline-flex min-h-11 items-center rounded-s px-4 text-[11px] font-bold text-sobre-marca"
                :class="a.tipo === 'peligro' ? 'bg-peligro' : 'bg-aviso'"
                @click="a.accion.ir"
              >
                {{ a.accion.etiqueta }}
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- Más vendidos -->
      <section aria-label="Productos más vendidos">
        <div class="mb-4 flex items-center justify-between">
          <h2 class="font-display text-[17px] font-semibold text-tinta">Más vendidos</h2>
          <span class="text-[12px] text-tinta-3">Hoy</span>
        </div>

        <div v-if="panelHoy.topProductos.length === 0" class="rounded border border-linea bg-superficie px-5 py-8 text-center text-[13.6px] text-tinta-2">
          Todavía no hay ventas hoy.
        </div>

        <div v-else class="flex flex-col gap-2">
          <div
            v-for="p in panelHoy.topProductos"
            :key="p.productoId"
            class="flex items-center gap-4 rounded border border-linea bg-superficie p-3"
          >
            <div class="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-s border border-linea bg-papel text-tinta-3">
              <PackageSearch class="h-5 w-5" />
            </div>
            <div class="min-w-0 flex-1">
              <p class="truncate font-display text-[14px] font-semibold text-tinta">{{ p.nombre }}</p>
            </div>
            <div class="flex-shrink-0 text-right">
              <p class="font-semibold tabular-nums text-marca">{{ p.cantidadVendida }} uds</p>
              <p v-if="p.utilidad !== null && p.utilidad !== undefined" class="text-[10px] font-bold tabular-nums text-exito">
                +{{ fmtBs(p.utilidad) }} util.
              </p>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>
