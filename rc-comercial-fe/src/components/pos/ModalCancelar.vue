<script setup>
import ModalBase from '@/components/ui/ModalBase.vue'
import { useVentaStore } from '@/stores/venta'

const abierto = defineModel({ type: Boolean, default: false })
const venta = useVentaStore()

function confirmar() {
  venta.iniciarNueva()
  abierto.value = false
}
</script>

<template>
  <ModalBase v-model="abierto" titulo="Cancelar operación" ancho="max-w-[360px]">
    <p class="text-[13.6px] text-tinta-2">
      Se va a vaciar el carrito completo de esta venta. Esta acción no se puede deshacer.
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
        Sí, cancelar
      </button>
    </div>
  </ModalBase>
</template>
