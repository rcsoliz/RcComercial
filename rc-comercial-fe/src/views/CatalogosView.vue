<script setup>
import { computed, onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import { Plus, Tag, Trash2 } from 'lucide-vue-next'
import { actualizarCategoria, crearCategoria, desactivarCategoria, listarCategorias } from '@/api/categorias'
import { actualizarMarca, crearMarca, desactivarMarca, listarMarcas } from '@/api/marcas'

const tab = ref('categorias') // 'categorias' | 'marcas'

const categorias = ref([])
const marcas = ref([])
const cargando = ref(true)

const editandoId = ref(null) // null = nada; 'nuevo' = creando; Guid = editando esa fila
const nombre = ref('')
const padreId = ref('')
const guardando = ref(false)

const categoriasOrdenadas = computed(() => {
  // Hijos justo debajo de su padre, con sangría — más legible que una lista plana.
  const porPadre = new Map()
  for (const c of categorias.value) {
    const clave = c.padreId || 'raiz'
    if (!porPadre.has(clave)) porPadre.set(clave, [])
    porPadre.get(clave).push(c)
  }
  const resultado = []
  function agregar(padreClave, nivel) {
    for (const c of porPadre.get(padreClave) || []) {
      resultado.push({ ...c, nivel })
      agregar(c.id, nivel + 1)
    }
  }
  agregar('raiz', 0)
  return resultado
})

async function cargar() {
  cargando.value = true
  try {
    const [cats, mcas] = await Promise.all([listarCategorias(), listarMarcas()])
    categorias.value = cats
    marcas.value = mcas
  } finally {
    cargando.value = false
  }
}

function abrirNuevo() {
  editandoId.value = 'nuevo'
  nombre.value = ''
  padreId.value = ''
}

function abrirEditar(item) {
  editandoId.value = item.id
  nombre.value = item.nombre
  padreId.value = item.padreId || ''
}

function cerrar() {
  editandoId.value = null
}

async function guardar() {
  if (!nombre.value.trim()) {
    toast.error('Ponle un nombre.')
    return
  }
  guardando.value = true
  try {
    if (tab.value === 'categorias') {
      const comando = { nombre: nombre.value.trim(), padreId: padreId.value || null }
      if (editandoId.value === 'nuevo') await crearCategoria(comando)
      else await actualizarCategoria(editandoId.value, { id: editandoId.value, ...comando })
    } else {
      const comando = { nombre: nombre.value.trim() }
      if (editandoId.value === 'nuevo') await crearMarca(comando)
      else await actualizarMarca(editandoId.value, { id: editandoId.value, ...comando })
    }
    toast.success('Guardado')
    cerrar()
    await cargar()
  } catch {
    toast.error('No se pudo guardar.')
  } finally {
    guardando.value = false
  }
}

async function desactivar(item) {
  try {
    if (tab.value === 'categorias') await desactivarCategoria(item.id)
    else await desactivarMarca(item.id)
    toast.success('Desactivado')
    await cargar()
  } catch {
    toast.error('No se pudo desactivar.')
  }
}
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto flex w-full max-w-[640px] flex-col gap-6">
      <div class="flex items-center justify-between">
        <h2 class="font-display text-[24px] font-bold text-tinta">Categorías y marcas</h2>
        <button
          type="button"
          class="flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
          @click="abrirNuevo"
        >
          <Plus class="h-5 w-5" />
          {{ tab === 'categorias' ? 'Nueva categoría' : 'Nueva marca' }}
        </button>
      </div>

      <div class="flex gap-1 self-start rounded-s bg-superficie-2 p-1">
        <button
          type="button"
          class="min-h-9 rounded-chip px-4 text-[13px] font-semibold transition-colors"
          :class="tab === 'categorias' ? 'bg-superficie text-marca shadow-sm' : 'text-tinta-2 hover:text-tinta'"
          @click="((tab = 'categorias'), cerrar())"
        >
          Categorías
        </button>
        <button
          type="button"
          class="min-h-9 rounded-chip px-4 text-[13px] font-semibold transition-colors"
          :class="tab === 'marcas' ? 'bg-superficie text-marca shadow-sm' : 'text-tinta-2 hover:text-tinta'"
          @click="((tab = 'marcas'), cerrar())"
        >
          Marcas
        </button>
      </div>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">Cargando…</div>

      <template v-else>
        <div v-if="editandoId" class="rounded border border-linea bg-superficie p-5">
          <div class="flex flex-col gap-3 sm:flex-row sm:items-end">
            <label class="flex flex-1 flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre</span>
              <input
                v-model="nombre"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
            </label>
            <label v-if="tab === 'categorias'" class="flex flex-1 flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Categoría padre (opcional)</span>
              <select
                v-model="padreId"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              >
                <option value="">Ninguna (categoría raíz)</option>
                <option v-for="c in categorias.filter((c) => c.id !== editandoId)" :key="c.id" :value="c.id">
                  {{ c.nombre }}
                </option>
              </select>
            </label>
            <div class="flex gap-2">
              <button
                type="button"
                class="min-h-11 rounded-s border border-linea px-4 text-[13px] font-semibold text-tinta-2 hover:bg-superficie-2"
                @click="cerrar"
              >
                Cancelar
              </button>
              <button
                type="button"
                :disabled="guardando"
                class="min-h-11 rounded-s bg-marca px-4 text-[13px] font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
                @click="guardar"
              >
                {{ guardando ? 'Guardando…' : 'Guardar' }}
              </button>
            </div>
          </div>
        </div>

        <div class="overflow-hidden rounded border border-linea bg-superficie">
          <ul v-if="tab === 'categorias' ? categoriasOrdenadas.length : marcas.length">
            <li
              v-for="item in tab === 'categorias' ? categoriasOrdenadas : marcas"
              :key="item.id"
              class="flex items-center justify-between border-b border-linea px-5 py-3 last:border-b-0"
            >
              <span
                class="text-[13.6px] text-tinta"
                :style="tab === 'categorias' ? { paddingLeft: item.nivel * 20 + 'px' } : {}"
              >
                {{ item.nombre }}
              </span>
              <div class="flex items-center gap-1">
                <button type="button" class="text-[12px] font-semibold text-marca hover:underline" @click="abrirEditar(item)">
                  Editar
                </button>
                <button
                  type="button"
                  class="flex min-h-9 min-w-9 items-center justify-center rounded-s text-tinta-3 hover:bg-peligro-tenue hover:text-peligro"
                  aria-label="Desactivar"
                  @click="desactivar(item)"
                >
                  <Trash2 class="h-4 w-4" />
                </button>
              </div>
            </li>
          </ul>
          <div v-else class="flex flex-col items-center gap-3 px-6 py-16 text-center">
            <Tag class="h-10 w-10 text-tinta-3" />
            <p class="text-[13.6px] text-tinta-2">
              {{ tab === 'categorias' ? 'Todavía no hay categorías.' : 'Todavía no hay marcas.' }}
            </p>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>
