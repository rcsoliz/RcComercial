<script setup>
import { ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import ModalBase from '@/components/ui/ModalBase.vue'
import { convertirProformaEnVenta } from '@/api/proformas'
import { uuid7 } from '@/utils/uuid7'

const props = defineProps({
  proforma: { type: Object, required: true },
})
const abierto = defineModel({ type: Boolean, default: false })
const emit = defineEmits(['convertida'])

const metodo = ref('EFECTIVO')
const enviando = ref(false)
const errorGeneral = ref('')

watch(abierto, (esta) => {
  if (esta) {
    metodo.value = 'EFECTIVO'
    errorGeneral.value = ''
  }
})

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

async function confirmar() {
  errorGeneral.value = ''
  enviando.value = true
  try {
    const venta = await convertirProformaEnVenta(props.proforma.id, {
      ventaId: uuid7(),
      pagos: [{ metodo: metodo.value, monto: props.proforma.total, referenciaQr: null }],
      descuentoAdicional: 0,
    })
    toast.success(`Venta ${venta.numero} registrada.`)
    abierto.value = false
    emit('convertida', venta)
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorGeneral.value = mensajes?.join(' ') || 'No se pudo convertir la proforma en venta.'
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <ModalBase v-model="abierto" titulo="Convertir en venta">
    <div class="flex flex-col gap-5">
      <div class="rounded-s bg-marca-tenue px-4 py-3 text-center">
        <p class="text-[0.72rem] font-bold uppercase tracking-wide text-tinta-2">Total a cobrar</p>
        <p class="font-display text-[28px] font-bold tabular-nums text-marca">{{ fmtBs(proforma.total) }}</p>
      </div>

      <div v-if="errorGeneral" class="rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
        {{ errorGeneral }}
      </div>

      <label class="flex flex-col gap-1.5">
        <span class="text-[0.8rem] font-semibold text-tinta-2">Método de pago</span>
        <select
          v-model="metodo"
          class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
        >
          <option value="EFECTIVO">Efectivo</option>
          <option value="QR">QR</option>
        </select>
      </label>

      <button
        type="button"
        :disabled="enviando"
        class="min-h-11 rounded-s bg-marca font-bold text-sobre-marca transition-colors hover:bg-marca-hover disabled:opacity-50"
        @click="confirmar"
      >
        {{ enviando ? 'Cobrando…' : `Confirmar y cobrar · ${fmtBs(proforma.total)}` }}
      </button>
    </div>
  </ModalBase>
</template>
