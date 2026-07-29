<script setup>
import { ref } from 'vue'
import { toast } from 'vue-sonner'
import { Copy } from 'lucide-vue-next'
import ModalBase from '@/components/ui/ModalBase.vue'

const props = defineProps({
  nombreUsuario: { type: String, default: '' },
  password: { type: String, default: '' },
})

const abierto = defineModel({ type: Boolean, default: false })
const copiado = ref(false)

async function copiar() {
  try {
    await navigator.clipboard.writeText(props.password)
    copiado.value = true
    toast.success('Contraseña copiada')
    setTimeout(() => (copiado.value = false), 2000)
  } catch {
    toast.error('No se pudo copiar. Selecciónala y cópiala a mano.')
  }
}
</script>

<template>
  <ModalBase v-model="abierto" titulo="Contraseña temporal" ancho="max-w-[420px]">
    <p class="text-[13.6px] text-tinta-2">
      Compártesela a <strong class="text-tinta">{{ nombreUsuario }}</strong> por un canal seguro. Solo se muestra
      esta vez: el sistema no la guarda en texto plano.
    </p>
    <div class="mt-4 flex items-center justify-between gap-3 rounded-s border border-linea bg-superficie-2 px-4 py-3">
      <span class="select-all font-mono text-[17px] font-bold tracking-wide text-tinta">{{ password }}</span>
      <button
        type="button"
        class="flex min-h-11 items-center gap-2 rounded-s border border-linea bg-superficie px-3 text-[12.6px] font-semibold text-tinta-2 hover:bg-superficie-2"
        @click="copiar"
      >
        <Copy class="h-4 w-4" />
        {{ copiado ? 'Copiada' : 'Copiar' }}
      </button>
    </div>
    <p class="mt-3 text-[12px] text-tinta-3">
      Deberá cambiarla apenas inicie sesión — el sistema se lo va a pedir automáticamente.
    </p>
    <button
      type="button"
      class="mt-5 min-h-11 w-full rounded-s bg-marca font-bold text-sobre-marca hover:bg-marca-hover"
      @click="abierto = false"
    >
      Listo
    </button>
  </ModalBase>
</template>
