<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { watchDebounced } from '@vueuse/core'
import dayjs from 'dayjs'
import { ChevronLeft, ChevronRight, FileText, Plus, Search } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { listarProformas } from '@/api/proformas'
import { Permisos } from '@/utils/permisos'

const router = useRouter()
const auth = useAuthStore()

const texto = ref('')
const estado = ref('') // '' (todas) | 'PENDIENTE' | 'ACEPTADA' | 'RECHAZADA' | 'CONVERTIDA'
const pagina = ref(1)
const resultados = ref([])
const total = ref(0)
const tamanoPagina = ref(8)
const cargando = ref(true)
const yaSeBusco = ref(false)

async function ejecutarBusqueda() {
  cargando.value = true
  try {
    const respuesta = await listarProformas(texto.value, pagina.value, estado.value || null)
    resultados.value = respuesta.items
    total.value = respuesta.total
    tamanoPagina.value = respuesta.tamanoPagina
  } finally {
    cargando.value = false
    yaSeBusco.value = true
  }
}

watchDebounced(
  texto,
  () => {
    pagina.value = 1
    ejecutarBusqueda()
  },
  { debounce: 300 },
)

function elegirEstado(valor) {
  estado.value = valor
  pagina.value = 1
  ejecutarBusqueda()
}

function irAPagina(delta) {
  pagina.value += delta
  ejecutarBusqueda()
}

function abrirProforma(proforma) {
  router.push({ name: 'proformas-editar', params: { id: proforma.id } })
}

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

const coloresEstado = {
  PENDIENTE: 'bg-aviso-tenue text-aviso',
  ACEPTADA: 'bg-aviso-tenue text-aviso',
  RECHAZADA: 'bg-peligro-tenue text-peligro',
  CONVERTIDA: 'bg-exito-tenue text-exito',
}

const sinResultados = computed(() => yaSeBusco.value && !cargando.value && resultados.value.length === 0)
const listaVacia = computed(() => sinResultados.value && !texto.value.trim() && !estado.value)

const totalPaginas = computed(() => Math.max(1, Math.ceil(total.value / tamanoPagina.value)))
const desde = computed(() => (total.value === 0 ? 0 : (pagina.value - 1) * tamanoPagina.value + 1))
const hasta = computed(() => Math.min(pagina.value * tamanoPagina.value, total.value))

onMounted(ejecutarBusqueda)
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mb-6 flex flex-wrap items-center justify-between gap-4">
      <div class="flex flex-wrap items-center gap-3">
        <div class="relative w-full max-w-[340px]">
          <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-tinta-3" />
          <input
            v-model="texto"
            type="text"
            autocomplete="off"
            placeholder="Buscar por número…"
            class="min-h-11 w-full rounded-s border-[1.5px] border-transparent bg-superficie-2 py-2 pl-10 pr-3 text-[13.6px] text-tinta outline-none transition-colors placeholder:text-tinta-3 focus:border-marca focus:bg-superficie"
          />
        </div>

        <div class="flex flex-wrap gap-1 rounded-s bg-superficie-2 p-1">
          <button
            v-for="opcion in [
              { valor: '', label: 'Todas' },
              { valor: 'PENDIENTE', label: 'Pendientes' },
              { valor: 'ACEPTADA', label: 'Aceptadas' },
              { valor: 'RECHAZADA', label: 'Rechazadas' },
              { valor: 'CONVERTIDA', label: 'Convertidas' },
            ]"
            :key="opcion.valor"
            type="button"
            class="min-h-9 rounded-chip px-3 text-[12.6px] font-semibold transition-colors"
            :class="estado === opcion.valor ? 'bg-superficie text-marca shadow-sm' : 'text-tinta-2 hover:text-tinta'"
            @click="elegirEstado(opcion.valor)"
          >
            {{ opcion.label }}
          </button>
        </div>
      </div>

      <button
        v-if="auth.tienePermiso(Permisos.ProformasCrear)"
        type="button"
        class="flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca transition-colors hover:bg-marca-hover"
        @click="router.push({ name: 'proformas-nueva' })"
      >
        <Plus class="h-5 w-5" />
        Nueva proforma
      </button>
    </div>

    <!-- Estado vacío (patrón C) -->
    <div v-if="listaVacia" class="flex flex-col items-center gap-3 rounded border border-linea bg-superficie px-6 py-16 text-center">
      <FileText class="h-10 w-10 text-tinta-3" />
      <p class="font-display text-[19.2px] font-bold text-tinta">Todavía no hay proformas</p>
      <p class="max-w-[360px] text-[13.6px] text-tinta-2">Crea una cotización para un cliente y conviértela en venta cuando la acepte.</p>
      <button
        v-if="auth.tienePermiso(Permisos.ProformasCrear)"
        type="button"
        class="mt-2 flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
        @click="router.push({ name: 'proformas-nueva' })"
      >
        <Plus class="h-5 w-5" />
        Crear la primera proforma
      </button>
    </div>

    <div v-else class="overflow-hidden rounded border border-linea bg-superficie shadow">
      <div class="overflow-x-auto">
        <table class="w-full border-collapse text-left">
          <thead>
            <tr class="border-b border-linea bg-superficie-2">
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Número</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Fecha</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Cliente</th>
              <th class="px-6 py-3.5 text-right text-[10px] font-bold uppercase tracking-wide text-tinta-3">Total</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Estado</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="sinResultados">
              <td colspan="5" class="px-6 py-12 text-center text-[13.6px] text-tinta-3">
                No se encontraron proformas para "{{ texto }}".
              </td>
            </tr>
            <tr
              v-for="p in resultados"
              :key="p.id"
              class="cursor-pointer border-b border-linea transition-colors last:border-b-0 hover:bg-marca-tenue"
              tabindex="0"
              @click="abrirProforma(p)"
              @keydown.enter="abrirProforma(p)"
            >
              <td class="px-6 py-4 align-middle font-mono text-[13px] text-tinta">{{ p.numero }}</td>
              <td class="px-6 py-4 align-middle text-[13.6px] text-tinta-2">{{ dayjs(p.fecha).format('DD/MM/YYYY HH:mm') }}</td>
              <td class="px-6 py-4 align-middle text-[13.6px] text-tinta-2">{{ p.clienteNombre || 'Consumidor final' }}</td>
              <td class="px-6 py-4 text-right align-middle font-semibold tabular-nums text-tinta">{{ fmtBs(p.total) }}</td>
              <td class="px-6 py-4 align-middle">
                <span
                  class="inline-block rounded-chip px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide"
                  :class="coloresEstado[p.estado] || 'bg-superficie-2 text-tinta-3'"
                >
                  {{ p.estado.toLowerCase() }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="!sinResultados" class="flex flex-wrap items-center justify-between gap-4 border-t border-linea px-6 py-4">
        <p class="text-[13.6px] text-tinta-3">
          Mostrando <span class="tabular-nums text-tinta-2">{{ desde }}–{{ hasta }}</span> de
          <span class="tabular-nums text-tinta-2">{{ total }}</span> resultados
        </p>
        <div class="flex items-center gap-2">
          <span class="text-[12.6px] text-tinta-3">
            Página <span class="tabular-nums">{{ pagina }}</span> de <span class="tabular-nums">{{ totalPaginas }}</span>
          </span>
          <button
            type="button"
            :disabled="pagina === 1"
            class="flex min-h-11 min-w-11 items-center justify-center rounded-s border border-linea text-tinta-2 hover:bg-superficie-2 disabled:opacity-40"
            aria-label="Página anterior"
            @click="irAPagina(-1)"
          >
            <ChevronLeft class="h-4 w-4" />
          </button>
          <button
            type="button"
            :disabled="pagina >= totalPaginas"
            class="flex min-h-11 min-w-11 items-center justify-center rounded-s border border-linea text-tinta-2 hover:bg-superficie-2 disabled:opacity-40"
            aria-label="Página siguiente"
            @click="irAPagina(1)"
          >
            <ChevronRight class="h-4 w-4" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
