<script setup>
import { onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import { Building, Plus, Trash2 } from 'lucide-vue-next'
import { crearSucursal, desactivarSucursal, editarSucursal, listarSucursales } from '@/api/sucursales'

const sucursales = ref([])
const cargando = ref(true)
const guardando = ref(false)

const editandoId = ref(null) // null = nada; 'nuevo' = creando; Guid = editando esa fila
const nombre = ref('')
const direccion = ref('')

async function cargar() {
  cargando.value = true
  try {
    sucursales.value = await listarSucursales('activos')
  } finally {
    cargando.value = false
  }
}

function abrirNuevo() {
  editandoId.value = 'nuevo'
  nombre.value = ''
  direccion.value = ''
}

function abrirEditar(s) {
  editandoId.value = s.id
  nombre.value = s.nombre
  direccion.value = s.direccion || ''
}

function cerrar() {
  editandoId.value = null
}

async function guardar() {
  if (!nombre.value.trim()) {
    toast.error('Ponle un nombre a la sucursal.')
    return
  }
  guardando.value = true
  try {
    const comando = { nombre: nombre.value.trim(), direccion: direccion.value.trim() || null }
    if (editandoId.value === 'nuevo') await crearSucursal(comando)
    else await editarSucursal(editandoId.value, { id: editandoId.value, ...comando })
    toast.success('Guardado')
    cerrar()
    await cargar()
  } catch {
    toast.error('No se pudo guardar.')
  } finally {
    guardando.value = false
  }
}

async function desactivar(s) {
  try {
    await desactivarSucursal(s.id)
    toast.success('Sucursal desactivada')
    await cargar()
  } catch {
    toast.error('No se pudo desactivar. Verifica que no sea la única sucursal.')
  }
}

onMounted(cargar)
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto flex w-full max-w-[640px] flex-col gap-6">
      <div class="flex items-center justify-between">
        <h2 class="font-display text-[24px] font-bold text-tinta">Sucursales</h2>
        <button
          type="button"
          class="flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
          @click="abrirNuevo"
        >
          <Plus class="h-5 w-5" />
          Nueva sucursal
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
            <label class="flex flex-1 flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Dirección (opcional)</span>
              <input
                v-model="direccion"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
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
          <ul v-if="sucursales.length">
            <li
              v-for="s in sucursales"
              :key="s.id"
              class="flex items-center justify-between border-b border-linea px-5 py-3.5 last:border-b-0"
            >
              <div>
                <p class="text-[13.6px] font-semibold text-tinta">{{ s.nombre }}</p>
                <p v-if="s.direccion" class="text-[12px] text-tinta-3">{{ s.direccion }}</p>
              </div>
              <div class="flex items-center gap-1">
                <button type="button" class="text-[12px] font-semibold text-marca hover:underline" @click="abrirEditar(s)">
                  Editar
                </button>
                <button
                  type="button"
                  class="flex min-h-9 min-w-9 items-center justify-center rounded-s text-tinta-3 hover:bg-peligro-tenue hover:text-peligro"
                  aria-label="Desactivar"
                  @click="desactivar(s)"
                >
                  <Trash2 class="h-4 w-4" />
                </button>
              </div>
            </li>
          </ul>
          <div v-else class="flex flex-col items-center gap-3 px-6 py-16 text-center">
            <Building class="h-10 w-10 text-tinta-3" />
            <p class="text-[13.6px] text-tinta-2">Todavía no hay sucursales.</p>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>
