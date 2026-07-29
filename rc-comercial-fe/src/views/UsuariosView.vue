<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { watchDebounced } from '@vueuse/core'
import dayjs from 'dayjs'
import { ChevronLeft, ChevronRight, Plus, Search, UserCog } from 'lucide-vue-next'
import { buscarUsuarios } from '@/api/usuarios'

const router = useRouter()

const texto = ref('')
const estado = ref('activos') // 'activos' | 'inactivos' | 'todos'
const pagina = ref(1)
const resultados = ref([])
const cargando = ref(true)
const yaSeBusco = ref(false)

async function ejecutarBusqueda() {
  cargando.value = true
  try {
    resultados.value = await buscarUsuarios(texto.value, estado.value, pagina.value)
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

function abrirUsuario(usuario) {
  router.push({ name: 'usuarios-editar', params: { id: usuario.id } })
}

function fmtUltimoAcceso(fecha) {
  return fecha ? dayjs(fecha).format('DD/MM/YYYY HH:mm') : 'Nunca'
}

const sinResultados = computed(() => yaSeBusco.value && !cargando.value && resultados.value.length === 0)
const listaVacia = computed(() => sinResultados.value && !texto.value.trim() && estado.value === 'activos')

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
            placeholder="Buscar por nombre o usuario…"
            class="min-h-11 w-full rounded-s border-[1.5px] border-transparent bg-superficie-2 py-2 pl-10 pr-3 text-[13.6px] text-tinta outline-none transition-colors placeholder:text-tinta-3 focus:border-marca focus:bg-superficie"
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
        type="button"
        class="flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca transition-colors hover:bg-marca-hover"
        @click="router.push({ name: 'usuarios-nuevo' })"
      >
        <Plus class="h-5 w-5" />
        Nuevo usuario
      </button>
    </div>

    <div v-if="listaVacia" class="flex flex-col items-center gap-3 rounded border border-linea bg-superficie px-6 py-16 text-center">
      <UserCog class="h-10 w-10 text-tinta-3" />
      <p class="font-display text-[19.2px] font-bold text-tinta">Todavía no hay usuarios</p>
      <p class="max-w-[360px] text-[13.6px] text-tinta-2">Crea el primero para que tu equipo pueda entrar al sistema.</p>
      <button
        type="button"
        class="mt-2 flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
        @click="router.push({ name: 'usuarios-nuevo' })"
      >
        <Plus class="h-5 w-5" />
        Crear el primer usuario
      </button>
    </div>

    <div v-else class="overflow-hidden rounded border border-linea bg-superficie">
      <div class="overflow-x-auto">
        <table class="w-full border-collapse text-left">
          <thead>
            <tr class="border-b border-linea bg-superficie-2">
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Usuario</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Rol</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Sucursal</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Último acceso</th>
              <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Estado</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="sinResultados">
              <td colspan="5" class="px-6 py-12 text-center text-[13.6px] text-tinta-3">
                No se encontraron usuarios para "{{ texto }}".
              </td>
            </tr>
            <tr
              v-for="u in resultados"
              :key="u.id"
              class="cursor-pointer border-b border-linea transition-colors last:border-b-0 hover:bg-marca-tenue"
              tabindex="0"
              @click="abrirUsuario(u)"
              @keydown.enter="abrirUsuario(u)"
            >
              <td class="px-6 py-4 align-middle">
                <p class="font-display text-[15px] font-semibold text-tinta">{{ u.nombre }}</p>
                <p class="mt-0.5 font-mono text-[11px] text-tinta-3">{{ u.usuarioLogin }}</p>
              </td>
              <td class="px-6 py-4 align-middle text-[13.6px] text-tinta-2">{{ u.rolNombre }}</td>
              <td class="px-6 py-4 align-middle text-[13.6px] text-tinta-2">{{ u.sucursalNombre || 'Todas' }}</td>
              <td class="px-6 py-4 align-middle text-[13px] text-tinta-2">{{ fmtUltimoAcceso(u.ultimoLogin) }}</td>
              <td class="px-6 py-4 align-middle">
                <span
                  class="inline-block rounded-chip px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide"
                  :class="u.activo ? 'bg-exito-tenue text-exito' : 'bg-superficie-2 text-tinta-3'"
                >
                  {{ u.activo ? 'activo' : 'inactivo' }}
                </span>
                <span
                  v-if="u.debeCambiarPassword"
                  class="ml-1.5 inline-block rounded-chip bg-aviso-tenue px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide text-aviso"
                >
                  pendiente
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="!sinResultados" class="flex flex-wrap items-center justify-between gap-4 border-t border-linea px-6 py-4">
        <p class="text-[13.6px] text-tinta-3">Página <span class="tabular-nums">{{ pagina }}</span></p>
        <div class="flex gap-2">
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
            :disabled="resultados.length < 50"
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
