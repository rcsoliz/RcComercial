<script setup>
import ModalBase from '@/components/ui/ModalBase.vue'

const props = defineProps({
  nombre: { type: String, required: true },
  titulo: { type: String, default: 'Desactivar' },
  /** Si no se pasa, usa el texto genérico de "no se borra, se puede reactivar". */
  mensaje: { type: String, default: null },
})

const abierto = defineModel({ type: Boolean, default: false })
const emit = defineEmits(['confirmar'])

function confirmar() {
  emit('confirmar')
  abierto.value = false
}
</script>

<template>
  <ModalBase v-model="abierto" :titulo="titulo" ancho="max-w-[380px]">
    <p class="text-[13.6px] text-tinta-2">
      {{ mensaje ?? `"${nombre}" quedará inactivo. No se borra: se puede reactivar después desde la base de datos si hace falta.` }}
    </p>
    <div class="mt-5 grid grid-cols-2 gap-3">
      <button
        type="button"
        class="min-h-11 rounded-s border border-linea text-tinta-2 hover:bg-superficie-2"
        @click="abierto = false"
      >
        Volver
      </button>
      <button type="button" class="min-h-11 rounded-s bg-peligro font-bold text-sobre-marca" @click="confirmar">
        Desactivar
      </button>
    </div>
  </ModalBase>
</template>
