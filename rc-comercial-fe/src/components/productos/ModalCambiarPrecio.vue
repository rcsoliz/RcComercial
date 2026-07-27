<script setup>
import { computed, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import ModalBase from '@/components/ui/ModalBase.vue'
import { cambiarPrecio } from '@/api/productos'

const props = defineProps({
  producto: { type: Object, required: true },
})

const abierto = defineModel({ type: Boolean, default: false })
const emit = defineEmits(['cambiado'])

const objetivo = ref('base')
const nuevoPrecio = ref('')
const enviando = ref(false)
const error = ref('')

const opciones = computed(() => [
  { valor: 'base', etiqueta: 'Precio base (unidad)', precioActual: props.producto.precioBase },
  ...props.producto.presentaciones.map((p) => ({ valor: p.id, etiqueta: p.nombre, precioActual: p.precio })),
])

watch(
  [abierto, objetivo],
  ([esta]) => {
    if (esta) {
      error.value = ''
      const opcion = opciones.value.find((o) => o.valor === objetivo.value)
      nuevoPrecio.value = opcion ? opcion.precioActual.toFixed(2) : ''
    }
  },
  { immediate: true },
)

async function confirmar() {
  error.value = ''
  const valor = Number(nuevoPrecio.value)
  if (!valor || valor < 0) {
    error.value = 'Ingresa un precio válido.'
    return
  }

  enviando.value = true
  try {
    const presentacionId = objetivo.value === 'base' ? null : objetivo.value
    await cambiarPrecio(props.producto.id, valor, presentacionId)
    toast.success('Precio actualizado')
    emit('cambiado')
    abierto.value = false
  } catch (e) {
    const mensajes = e.response?.data?.errores?.map((x) => x.mensaje)
    error.value = mensajes?.join(' ') || 'No se pudo cambiar el precio.'
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <ModalBase v-model="abierto" titulo="Cambiar precio" ancho="max-w-[380px]">
    <div class="flex flex-col gap-4">
      <div v-if="error" class="rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">{{ error }}</div>

      <label class="flex flex-col gap-1.5">
        <span class="text-[0.8rem] font-semibold text-tinta-2">Qué precio cambiar</span>
        <select
          v-model="objetivo"
          class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
        >
          <option v-for="o in opciones" :key="o.valor" :value="o.valor">{{ o.etiqueta }}</option>
        </select>
      </label>

      <label class="flex flex-col gap-1.5">
        <span class="text-[0.8rem] font-semibold text-tinta-2">Nuevo precio (Bs)</span>
        <input
          v-model="nuevoPrecio"
          type="number"
          min="0"
          step="0.01"
          class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
        />
      </label>

      <button
        type="button"
        :disabled="enviando"
        class="min-h-11 rounded-s bg-marca font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
        @click="confirmar"
      >
        {{ enviando ? 'Guardando…' : 'Cambiar precio' }}
      </button>
    </div>
  </ModalBase>
</template>
