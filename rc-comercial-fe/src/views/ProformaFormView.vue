<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { watchDebounced } from '@vueuse/core'
import { toast } from 'vue-sonner'
import dayjs from 'dayjs'
import { Minus, Plus, Search, Trash2 } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { Permisos } from '@/utils/permisos'
import { buscarProductos } from '@/api/productos'
import { listarVehiculosPorCliente } from '@/api/clientes'
import { crearProforma, obtenerProformaPorId, rechazarProforma } from '@/api/proformas'
import SelectorCliente from '@/components/pos/SelectorCliente.vue'
import ModalConvertirProforma from '@/components/proformas/ModalConvertirProforma.vue'

const props = defineProps({
  id: { type: String, default: null },
})

const router = useRouter()
const auth = useAuthStore()

const esNuevo = computed(() => !props.id)
const cargando = ref(true)
const guardando = ref(false)
const errorGeneral = ref('')

// ── Solo lectura (proforma ya existente) ──
const proforma = ref(null)

// ── Edición (proforma nueva) ──
const clienteSeleccionado = ref(null) // { id, nombre } | null
const vehiculoId = ref('')
const vehiculos = ref([])
const validaHasta = ref('')
const lineas = ref([]) // { productoId, nombre, precioUnitario, cantidad, descuento, esServicio }

watch(clienteSeleccionado, async (c) => {
  vehiculoId.value = ''
  vehiculos.value = c ? await listarVehiculosPorCliente(c.id) : []
})

const textoBusqueda = ref('')
const resultadosBusqueda = ref([])
async function buscar() {
  const q = textoBusqueda.value.trim()
  if (!q) {
    resultadosBusqueda.value = []
    return
  }
  resultadosBusqueda.value = await buscarProductos(q)
}
watchDebounced(textoBusqueda, buscar, { debounce: 300 })

function agregarLinea(producto) {
  lineas.value.push({
    productoId: producto.id,
    nombre: producto.nombre,
    precioUnitario: producto.precioBase,
    cantidad: 1,
    descuento: 0,
    esServicio: !!producto.esServicio,
  })
  textoBusqueda.value = ''
  resultadosBusqueda.value = []
}

function quitarLinea(idx) {
  lineas.value.splice(idx, 1)
}

const subtotal = computed(() => lineas.value.reduce((acc, l) => acc + l.cantidad * l.precioUnitario, 0))
const descuentoTotal = computed(() => lineas.value.reduce((acc, l) => acc + Number(l.descuento || 0), 0))
const total = computed(() => subtotal.value - descuentoTotal.value)

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

async function cargarDatos() {
  cargando.value = true
  try {
    if (!esNuevo.value) {
      proforma.value = await obtenerProformaPorId(props.id)
    }
  } finally {
    cargando.value = false
  }
}
onMounted(cargarDatos)

async function guardar() {
  errorGeneral.value = ''
  if (lineas.value.length === 0) {
    errorGeneral.value = 'Agrega al menos una línea a la proforma.'
    return
  }
  guardando.value = true
  try {
    await crearProforma({
      clienteId: clienteSeleccionado.value?.id ?? null,
      vehiculoId: vehiculoId.value || null,
      validaHasta: validaHasta.value || null,
      detalles: lineas.value.map((l) => ({
        productoId: l.productoId,
        presentacionId: null,
        cantidad: l.cantidad,
        precioUnitario: l.precioUnitario,
        descuento: Number(l.descuento || 0),
      })),
    })
    toast.success('Proforma creada.')
    router.push({ name: 'proformas' })
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorGeneral.value = mensajes?.join(' ') || 'No se pudo guardar la proforma.'
  } finally {
    guardando.value = false
  }
}

const mostrarConvertir = ref(false)

async function alRechazar() {
  try {
    await rechazarProforma(props.id, null)
    toast.success('Proforma rechazada.')
    await cargarDatos()
  } catch {
    toast.error('No se pudo rechazar la proforma.')
  }
}

function alConvertida() {
  router.push({ name: 'proformas' })
}

const coloresEstado = {
  PENDIENTE: 'bg-aviso-tenue text-aviso',
  ACEPTADA: 'bg-aviso-tenue text-aviso',
  RECHAZADA: 'bg-peligro-tenue text-peligro',
  CONVERTIDA: 'bg-exito-tenue text-exito',
}
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto w-full max-w-[640px]">
      <h2 class="mb-6 font-display text-[24px] font-bold text-tinta">
        {{ esNuevo ? 'Nueva proforma' : `Proforma ${proforma?.numero ?? ''}` }}
      </h2>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">
        Cargando…
      </div>

      <!-- Solo lectura: proforma ya existente -->
      <div v-else-if="!esNuevo && proforma" class="rounded border border-linea bg-superficie p-6">
        <div class="mb-5 flex items-center justify-between">
          <span class="text-[13.6px] text-tinta-2">{{ dayjs(proforma.fecha).format('DD/MM/YYYY HH:mm') }}</span>
          <span
            class="rounded-chip px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide"
            :class="coloresEstado[proforma.estado] || 'bg-superficie-2 text-tinta-3'"
          >
            {{ proforma.estado.toLowerCase() }}
          </span>
        </div>

        <ul class="flex flex-col gap-2 border-b border-linea pb-4">
          <li v-for="d in proforma.detalles" :key="d.id" class="flex items-center justify-between text-[13.6px]">
            <span class="text-tinta-2">{{ d.cantidad }} × {{ fmtBs(d.precioUnitario) }}</span>
            <span class="tabular-nums font-medium text-tinta">{{ fmtBs(d.total) }}</span>
          </li>
        </ul>

        <div class="mt-4 flex items-center justify-between">
          <span class="font-display text-[17px] font-semibold text-tinta">TOTAL</span>
          <span class="font-display text-[24px] font-bold tabular-nums text-marca">{{ fmtBs(proforma.total) }}</span>
        </div>

        <div v-if="proforma.estado === 'PENDIENTE' || proforma.estado === 'ACEPTADA'" class="mt-6 flex gap-3">
          <button
            v-if="auth.tienePermiso(Permisos.ProformasAnular)"
            type="button"
            class="min-h-11 flex-1 rounded-s border border-linea text-[13.6px] font-semibold text-peligro hover:bg-peligro-tenue"
            @click="alRechazar"
          >
            Rechazar
          </button>
          <button
            v-if="auth.tienePermiso(Permisos.VentasCrear)"
            type="button"
            class="min-h-11 flex-1 rounded-s bg-marca font-bold text-sobre-marca hover:bg-marca-hover"
            @click="mostrarConvertir = true"
          >
            Convertir en venta
          </button>
        </div>
      </div>

      <!-- Edición: proforma nueva -->
      <form v-else class="rounded border border-linea bg-superficie p-6" novalidate @submit.prevent="guardar">
        <div v-if="errorGeneral" class="mb-5 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
          {{ errorGeneral }}
        </div>

        <div class="flex flex-col gap-4">
          <SelectorCliente v-model="clienteSeleccionado" />

          <label v-if="clienteSeleccionado" class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Vehículo (opcional)</span>
            <select
              v-model="vehiculoId"
              class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
            >
              <option value="">Ninguno</option>
              <option v-for="v in vehiculos" :key="v.id" :value="v.id">
                {{ v.placa }} — {{ [v.marca, v.modelo].filter(Boolean).join(' ') }}
              </option>
            </select>
          </label>

          <label class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Válida hasta (opcional)</span>
            <input
              v-model="validaHasta"
              type="date"
              class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
            />
          </label>

          <div class="relative">
            <span class="mb-1.5 block text-[0.8rem] font-semibold text-tinta-2">Agregar producto o servicio</span>
            <div class="relative">
              <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-tinta-3" />
              <input
                v-model="textoBusqueda"
                type="text"
                autocomplete="off"
                placeholder="Buscar…"
                class="min-h-11 w-full rounded-s border-[1.5px] border-linea bg-superficie-2 py-2 pl-10 pr-3 text-[13.6px] text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
            </div>
            <ul v-if="resultadosBusqueda.length" class="mt-1 max-h-48 overflow-y-auto rounded-s border border-linea bg-superficie">
              <li
                v-for="p in resultadosBusqueda"
                :key="p.id"
                class="flex cursor-pointer items-center justify-between border-b border-linea px-3 py-2.5 text-[13px] last:border-b-0 hover:bg-marca-tenue"
                @click="agregarLinea(p)"
              >
                <span class="text-tinta">{{ p.nombre }}</span>
                <span class="tabular-nums text-tinta-3">{{ fmtBs(p.precioBase) }}</span>
              </li>
            </ul>
          </div>

          <div v-if="lineas.length" class="flex flex-col gap-2">
            <div
              v-for="(l, idx) in lineas"
              :key="idx"
              class="flex items-center gap-3 rounded-s border border-linea px-3 py-2.5"
            >
              <div class="flex-1">
                <p class="text-[13.6px] font-medium text-tinta">{{ l.nombre }}</p>
                <span
                  v-if="l.esServicio"
                  class="mt-0.5 inline-block rounded-chip bg-marca-tenue px-1.5 py-0.5 text-[9px] font-bold uppercase tracking-wide text-marca"
                >
                  servicio
                </span>
              </div>
              <button
                type="button"
                class="flex h-6 w-6 items-center justify-center rounded-full bg-superficie-2 hover:bg-linea"
                @click="l.cantidad = Math.max(1, l.cantidad - 1)"
              >
                <Minus class="h-3.5 w-3.5" />
              </button>
              <span class="min-w-[16px] text-center tabular-nums">{{ l.cantidad }}</span>
              <button
                type="button"
                class="flex h-6 w-6 items-center justify-center rounded-full bg-superficie-2 hover:bg-linea"
                @click="l.cantidad += 1"
              >
                <Plus class="h-3.5 w-3.5" />
              </button>
              <span class="w-24 text-right tabular-nums text-tinta">{{ fmtBs(l.cantidad * l.precioUnitario) }}</span>
              <button
                type="button"
                class="flex h-8 w-8 items-center justify-center rounded-s text-tinta-3 hover:bg-peligro-tenue hover:text-peligro"
                aria-label="Quitar línea"
                @click="quitarLinea(idx)"
              >
                <Trash2 class="h-4 w-4" />
              </button>
            </div>
          </div>

          <div class="flex items-center justify-between border-t border-linea pt-4">
            <span class="font-display text-[17px] font-semibold text-tinta">TOTAL</span>
            <span class="font-display text-[24px] font-bold tabular-nums text-marca">{{ fmtBs(total) }}</span>
          </div>
        </div>

        <div class="mt-6 flex justify-end gap-3 border-t border-linea pt-5">
          <button
            type="button"
            class="min-h-11 rounded-s border border-linea px-5 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie-2"
            @click="router.push({ name: 'proformas' })"
          >
            Cancelar
          </button>
          <button
            type="submit"
            :disabled="guardando"
            class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
          >
            {{ guardando ? 'Guardando…' : 'Guardar proforma' }}
          </button>
        </div>
      </form>
    </div>
  </div>

  <ModalConvertirProforma
    v-if="proforma"
    v-model="mostrarConvertir"
    :proforma="proforma"
    @convertida="alConvertida"
  />
</template>
