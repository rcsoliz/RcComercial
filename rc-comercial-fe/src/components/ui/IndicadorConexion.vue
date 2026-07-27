<script setup>
import { WifiOff } from 'lucide-vue-next'
import { useConexion } from '@/composables/useConexion'

defineProps({
  compacto: { type: Boolean, default: false },
})

const { enLinea } = useConexion()
</script>

<template>
  <div
    v-if="!enLinea"
    class="flex items-center gap-2 rounded-chip bg-aviso-tenue px-3 py-1.5 text-[11px] font-bold uppercase tracking-wide text-aviso"
    role="status"
  >
    <WifiOff class="h-3.5 w-3.5 flex-shrink-0" aria-hidden="true" />
    <span v-if="compacto">Sin conexión</span>
    <span v-else>Sin conexión — ventas se guardarán localmente</span>
  </div>
</template>
