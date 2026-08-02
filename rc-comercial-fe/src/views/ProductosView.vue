<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { watchDebounced } from '@vueuse/core'
import { toast } from 'vue-sonner'
import { ChevronLeft, ChevronRight, Package, Plus, Search } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { listarProductos, obtenerProductoPorCodigoBarras } from '@/api/productos'
import { Permisos } from '@/utils/permisos'

const router = useRouter()
const auth = useAuthStore()

const texto = ref('')
const estado = ref('activos') // 'activos' | 'inactivos' | 'todos'
const pagina = ref(1)
const resultados = ref([])
const total = ref(0)
const tamanoPagina = ref(10)
const cargando = ref(true)
const yaSeBusco = ref(false)

async function ejecutarBusqueda() {
  cargando.value = true
  try {
    const respuesta = await listarProductos(texto.value, pagina.value, estado.value)
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

async function alPresionarEnterBusqueda() {
  const codigo = texto.value.trim()
  if (!codigo) return
  const resultado = await obtenerProductoPorCodigoBarras(codigo)
  if (!resultado) return

  if (resultado.esSugerencia) {
    router.push({
      name: 'productos-nuevo',
      query: {
        codigoBarras: resultado.sugerencia.codigoBarras,
        nombre: resultado.sugerencia.nombre,
        marca: resultado.sugerencia.marca || undefined,
      },
    })
  } else {
    toast.info(`"${resultado.producto.nombre}" ya está en tu catálogo.`)
    router.push({ name: 'productos-editar', params: { id: resultado.producto.id } })
  }
}

function abrirProducto(producto) {
  router.push({ name: 'productos-editar', params: { id: producto.id } })
}

function fmtBs(n) {
  const [ent, dec] = Number(n).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

function estadoStock(p) {
  if (p.stockTotal <= 0) return 'agotado'
  if (p.stockTotal <= p.stockMinimo) return 'bajo'
  return 'normal'
}

function porcentajeStock(p) {
  const techo = Math.max(p.stockMinimo * 3, 1)
  return Math.max(0, Math.min(100, (p.stockTotal / techo) * 100))
}

const sinResultados = computed(() => yaSeBusco.value && !cargando.value && resultados.value.length === 0)
// "Catálogo vacío" (patrón C, con acción de crear) solo aplica a la vista por
// defecto — activos, sin texto: si el vacío es por buscar/filtrar, es un
// "sin resultados para este filtro", no "todavía no hay productos".
const catalogoVacio = computed(() => sinResultados.value && !texto.value.trim() && estado.value === 'activos')

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
            placeholder="Buscar producto o código de barras…"
            class="min-h-11 w-full rounded-s border-[1.5px] border-transparent bg-superficie-2 py-2 pl-10 pr-3 text-[13.6px] text-tinta outline-none transition-colors placeholder:text-tinta-3 focus:border-marca focus:bg-superficie"
            @keydown.enter.prevent="alPresionarEnterBusqueda"
          />
        </div>

        <div class="flex gap-1 rounded-s bg-superficie-2 p-1">
          <button
            v-for="opcion in [
              { valor: 'activos', label: 'Activos' },
              { valor: 'inactivos', label: 'Inactivos' },
              { valor: 'todos', label: 'Todos' },
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
        v-if="auth.tienePermiso(Permisos.ProductosCrearEditar)"
        type="button"
        class="flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca transition-colors hover:bg-marca-hover"
        @click="router.push({ name: 'productos-nuevo' })"
      >
        <Plus class="h-5 w-5" />
        Nuevo producto
      </button>
    </div>

    <!-- Estado vacío: catálogo sin productos (patrón C) -->
    <div v-if="catalogoVacio" class="flex flex-col items-center gap-3 rounded border border-linea bg-superficie px-6 py-16 text-center">
      <Package class="h-10 w-10 text-tinta-3" />
      <p class="font-display text-[19.2px] font-bold text-tinta">Todavía no hay productos</p>
      <p class="max-w-[360px] text-[13.6px] text-tinta-2">
        Crea el primero manualmente o escanea un código de barras en el buscador de arriba.
      </p>
      <button
        v-if="auth.tienePermiso(Permisos.ProductosCrearEditar)"
        type="button"
        class="mt-2 flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
        @click="router.push({ name: 'productos-nuevo' })"
      >
        <Plus class="h-5 w-5" />
        Crear el primer producto
      </button>
    </div>

    <div v-else class="overflow-hidden rounded border border-linea bg-superficie">
      <div class="overflow-x-auto">
        <table class="w-full border-collapse text-left">
          <thead>
            <tr class="border-b border-linea bg-superficie-2">
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Producto</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Categoría / Marca</th>
              <th class="px-6 py-3.5 text-right text-[10px] font-bold uppercase tracking-wide text-tinta-3">Precio</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Nivel de stock</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Estado</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="sinResultados">
              <td colspan="6" class="px-6 py-12 text-center text-[13.6px] text-tinta-3">
                {{ texto.trim() ? `No se encontraron productos para "${texto}".` : 'No hay productos con este filtro.' }}
              </td>
            </tr>
            <tr
              v-for="p in resultados"
              :key="p.id"
              class="cursor-pointer border-b border-linea transition-colors last:border-b-0 hover:bg-marca-tenue"
              tabindex="0"
              @click="abrirProducto(p)"
              @keydown.enter="abrirProducto(p)"
            >
              <td class="px-6 py-4 align-middle">
                <p class="font-display text-[15px] font-semibold leading-snug text-tinta">{{ p.nombre }}</p>
                <p v-if="p.codigo || p.codigoBarras" class="mt-0.5 font-mono text-[11px] uppercase text-tinta-3">
                  {{ p.codigo || p.codigoBarras }}
                </p>
              </td>
              <td class="px-6 py-4 align-middle text-[13.6px] text-tinta-2">
                <p v-if="p.categoriaNombre">{{ p.categoriaNombre }}</p>
                <p v-if="p.marcaNombre" class="text-[11px] text-tinta-3">{{ p.marcaNombre }}</p>
                <p v-if="!p.categoriaNombre && !p.marcaNombre" class="text-tinta-3">—</p>
              </td>
              <td class="px-6 py-4 text-right align-middle font-semibold tabular-nums text-tinta">
                {{ fmtBs(p.precioBase) }}
              </td>
              <td class="px-6 py-4 align-middle">
                <div class="flex min-w-[180px] items-center gap-3">
                  <div class="h-1.5 w-24 flex-shrink-0 overflow-hidden rounded-chip bg-superficie-2">
                    <div
                      class="h-full rounded-chip"
                      :class="{
                        'bg-marca': estadoStock(p) === 'normal',
                        'bg-aviso': estadoStock(p) === 'bajo',
                        'bg-peligro': estadoStock(p) === 'agotado',
                      }"
                      :style="{ width: porcentajeStock(p) + '%' }"
                    ></div>
                  </div>
                  <div>
                    <span
                      class="tabular-nums text-[13.6px] font-bold"
                      :class="{
                        'text-tinta': estadoStock(p) === 'normal',
                        'text-aviso': estadoStock(p) === 'bajo',
                        'text-peligro': estadoStock(p) === 'agotado',
                      }"
                    >
                      {{ p.stockTotal }}
                    </span>
                    <span
                      v-if="estadoStock(p) === 'bajo'"
                      class="block text-[10px] font-bold uppercase tracking-wide text-aviso"
                    >
                      Stock bajo
                    </span>
                    <span
                      v-else-if="estadoStock(p) === 'agotado'"
                      class="block text-[10px] font-bold uppercase tracking-wide text-peligro"
                    >
                      Agotado
                    </span>
                  </div>
                </div>
              </td>
              <td class="px-6 py-4 align-middle">
                <span
                  class="inline-block rounded-chip px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide"
                  :class="p.activo ? 'bg-exito-tenue text-exito' : 'bg-superficie-2 text-tinta-3'"
                >
                  {{ p.activo ? 'activo' : 'inactivo' }}
                </span>
              </td>
              <td class="px-6 py-4 align-middle">
                <button
                  type="button"
                  class="min-h-9 rounded-s border border-linea px-3 text-[12.6px] font-semibold text-tinta-2 transition-colors hover:bg-superficie-2"
                  @click.stop="abrirProducto(p)"
                >
                  Editar
                </button>
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
