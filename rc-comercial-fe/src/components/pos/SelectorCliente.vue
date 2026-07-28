<script setup>
import { ref } from 'vue'
import { watchDebounced } from '@vueuse/core'
import { toast } from 'vue-sonner'
import { Search, UserPlus, X } from 'lucide-vue-next'
import { buscarClientes, crearCliente } from '@/api/clientes'

const clienteElegido = defineModel({ type: Object, default: null }) // { id, nombre } | null

const texto = ref('')
const resultados = ref([])
const buscando = ref(false)
const mostrarResultados = ref(false)
const mostrarCreacionRapida = ref(false)

const nuevoNombre = ref('')
const nuevoNit = ref('')
const nuevoWhatsapp = ref('')
const creando = ref(false)
const errorCreacion = ref('')

async function buscar() {
  const q = texto.value.trim()
  if (!q) {
    resultados.value = []
    return
  }
  buscando.value = true
  try {
    resultados.value = await buscarClientes(q, 'activos', 1)
  } catch {
    // Sin conexión: la selección de cliente es opcional, no bloquea la venta.
    resultados.value = []
  } finally {
    buscando.value = false
  }
}

watchDebounced(texto, buscar, { debounce: 300 })

function elegir(cliente) {
  clienteElegido.value = { id: cliente.id, nombre: cliente.nombre }
  mostrarResultados.value = false
  texto.value = ''
  resultados.value = []
}

function quitar() {
  clienteElegido.value = null
}

function abrirCreacionRapida() {
  nuevoNombre.value = texto.value.trim() && Number.isNaN(Number(texto.value.trim())) ? texto.value.trim() : ''
  nuevoNit.value = texto.value.trim() && !Number.isNaN(Number(texto.value.trim())) ? texto.value.trim() : ''
  nuevoWhatsapp.value = ''
  errorCreacion.value = ''
  mostrarCreacionRapida.value = true
  mostrarResultados.value = false
}

async function guardarClienteRapido() {
  errorCreacion.value = ''
  if (!nuevoNombre.value.trim()) {
    errorCreacion.value = 'Ponle un nombre al cliente.'
    return
  }
  creando.value = true
  try {
    const creado = await crearCliente({
      nombre: nuevoNombre.value.trim(),
      tipoDocumento: 'CI',
      nitCi: nuevoNit.value.trim() || null,
      telefonoWhatsapp: nuevoWhatsapp.value.trim() || null,
      email: null,
    })
    clienteElegido.value = { id: creado.id, nombre: creado.nombre }
    mostrarCreacionRapida.value = false
    texto.value = ''
    toast.success('Cliente creado')
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorCreacion.value = mensajes?.join(' ') || 'No se pudo crear el cliente.'
  } finally {
    creando.value = false
  }
}
</script>

<template>
  <div class="flex flex-col gap-2">
    <span class="text-[0.8rem] font-semibold text-tinta-2">Cliente (opcional)</span>

    <div v-if="clienteElegido" class="flex items-center justify-between rounded-s border border-linea bg-superficie-2 px-3 py-2.5">
      <span class="text-[13.6px] font-semibold text-tinta">{{ clienteElegido.nombre }}</span>
      <button type="button" class="text-[12px] font-semibold text-peligro hover:underline" @click="quitar">
        Quitar
      </button>
    </div>

    <template v-else>
      <div class="relative">
        <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-tinta-3" />
        <input
          v-model="texto"
          type="text"
          autocomplete="off"
          placeholder="Buscar por NIT/CI o nombre… (o deja vacío = consumidor final)"
          class="min-h-11 w-full rounded-s border-[1.5px] border-linea bg-superficie-2 py-2 pl-10 pr-3 text-[13.6px] text-tinta outline-none focus:border-marca focus:bg-superficie"
          @focus="mostrarResultados = true"
        />
      </div>

      <div v-if="mostrarResultados && texto.trim()" class="rounded-s border border-linea bg-superficie">
        <p v-if="buscando" class="px-3 py-2.5 text-[13px] text-tinta-3">Buscando…</p>
        <ul v-else-if="resultados.length > 0" class="max-h-40 overflow-y-auto">
          <li
            v-for="c in resultados"
            :key="c.id"
            class="cursor-pointer border-b border-linea px-3 py-2.5 text-[13px] last:border-b-0 hover:bg-marca-tenue"
            @click="elegir(c)"
          >
            <span class="font-semibold text-tinta">{{ c.nombre }}</span>
            <span v-if="c.nitCi" class="ml-2 font-mono text-[11px] text-tinta-3">{{ c.nitCi }}</span>
          </li>
        </ul>
        <button
          v-else
          type="button"
          class="flex w-full items-center gap-2 px-3 py-2.5 text-[13px] font-semibold text-marca hover:bg-marca-tenue"
          @click="abrirCreacionRapida"
        >
          <UserPlus class="h-4 w-4" />
          No hay resultados: crear cliente "{{ texto.trim() }}"
        </button>
      </div>
    </template>

    <div v-if="mostrarCreacionRapida" class="rounded-s border border-linea bg-superficie-2 p-3">
      <div class="mb-2 flex items-center justify-between">
        <span class="text-[12px] font-bold uppercase tracking-wide text-tinta-3">Cliente rápido</span>
        <button type="button" class="text-tinta-3 hover:text-tinta" @click="mostrarCreacionRapida = false">
          <X class="h-4 w-4" />
        </button>
      </div>
      <div v-if="errorCreacion" class="mb-2 rounded-s bg-peligro-tenue px-3 py-2 text-[12px] text-peligro">
        {{ errorCreacion }}
      </div>
      <div class="flex flex-col gap-2">
        <input
          v-model="nuevoNombre"
          type="text"
          placeholder="Nombre"
          class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca"
        />
        <div class="grid grid-cols-2 gap-2">
          <input
            v-model="nuevoNit"
            type="text"
            placeholder="NIT/CI (opcional)"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca"
          />
          <input
            v-model="nuevoWhatsapp"
            type="text"
            placeholder="+59171234567"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca"
          />
        </div>
        <button
          type="button"
          :disabled="creando"
          class="min-h-11 rounded-s bg-marca font-semibold text-sobre-marca disabled:opacity-60"
          @click="guardarClienteRapido"
        >
          {{ creando ? 'Creando…' : 'Crear y usar este cliente' }}
        </button>
      </div>
    </div>
  </div>
</template>
