<script setup>
import { DialogClose, DialogContent, DialogOverlay, DialogPortal, DialogRoot, DialogTitle } from 'reka-ui'
import { X } from 'lucide-vue-next'

defineProps({
  titulo: { type: String, required: true },
  ancho: { type: String, default: 'max-w-[420px]' },
})

const abierto = defineModel({ type: Boolean, default: false })
</script>

<template>
  <DialogRoot v-model:open="abierto">
    <DialogPortal>
      <DialogOverlay class="fixed inset-0 z-50 bg-overlay" />
      <DialogContent
        class="fixed left-1/2 top-1/2 z-50 w-[calc(100%-2rem)] -translate-x-1/2 -translate-y-1/2 rounded bg-superficie shadow focus:outline-none"
        :class="ancho"
      >
        <div class="flex items-center justify-between border-b border-linea px-5 py-4">
          <DialogTitle class="font-display text-[17px] font-bold text-tinta">{{ titulo }}</DialogTitle>
          <DialogClose
            class="flex h-8 w-8 items-center justify-center rounded-s text-tinta-2 hover:bg-superficie-2"
            aria-label="Cerrar"
          >
            <X class="h-4 w-4" />
          </DialogClose>
        </div>
        <div class="max-h-[70vh] overflow-y-auto px-5 py-4">
          <slot />
        </div>
      </DialogContent>
    </DialogPortal>
  </DialogRoot>
</template>
