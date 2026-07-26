<script setup>
import ModalBase from '@/components/ui/ModalBase.vue'

const props = defineProps({
  producto: { type: Object, required: true },
})

const abierto = defineModel({ type: Boolean, default: false })
const emit = defineEmits(['seleccionar'])

function fmtBs(n) {
  const [ent, dec] = Number(n).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

function elegir(presentacion) {
  emit('seleccionar', presentacion)
  abierto.value = false
}
</script>

<template>
  <ModalBase v-model="abierto" :titulo="`¿En qué presentación? · ${producto.nombre}`">
    <div class="flex flex-col gap-2">
      <button
        type="button"
        class="flex min-h-11 items-center justify-between rounded-s border border-linea px-4 py-3 text-left hover:border-marca"
        @click="elegir(null)"
      >
        <span class="font-medium text-tinta">Unidad</span>
        <span class="font-semibold text-marca">{{ fmtBs(producto.precioBase) }}</span>
      </button>

      <button
        v-for="p in producto.presentaciones"
        :key="p.id"
        type="button"
        class="flex min-h-11 items-center justify-between rounded-s border border-linea px-4 py-3 text-left hover:border-marca"
        @click="elegir(p)"
      >
        <span>
          <span class="font-medium text-tinta">{{ p.nombre }}</span>
          <span class="ml-2 text-[12px] text-tinta-3">× {{ p.factor }}</span>
        </span>
        <span class="font-semibold text-marca">{{ fmtBs(p.precio) }}</span>
      </button>
    </div>
  </ModalBase>
</template>
