<script setup>
import { onMounted, ref } from 'vue'
import dayjs from 'dayjs'
import { toast } from 'vue-sonner'
import { AlertTriangle, RefreshCw } from 'lucide-vue-next'
import { listarVentasRechazadas, reintentarVentaRechazada } from '@/db/ventasDb'
import { useSincronizacion } from '@/composables/useSincronizacion'

const rechazadas = ref([])
const cargando = ref(true)
const reintentandoId = ref(null)
const sincronizacion = useSincronizacion()

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

async function cargar() {
  cargando.value = true
  try {
    rechazadas.value = await listarVentasRechazadas()
  } finally {
    cargando.value = false
  }
}

async function reintentar(venta) {
  reintentandoId.value = venta.id
  try {
    await reintentarVentaRechazada(venta.id)
    await sincronizacion.intentarSincronizar()
    toast.success(`Venta ${venta.numero} reenviada.`)
  } catch {
    toast.error('No se pudo reintentar. Sigue en la lista.')
  } finally {
    reintentandoId.value = null
    await cargar()
  }
}

onMounted(cargar)
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto flex w-full max-w-[720px] flex-col gap-6">
      <div>
        <h2 class="font-display text-[24px] font-bold text-tinta">Ventas por revisar</h2>
        <p class="mt-1 text-[13.6px] text-tinta-2">
          Ventas hechas sin conexión que el servidor rechazó al sincronizar. No se pierden: corrige lo que haga
          falta y reintenta.
        </p>
      </div>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">
        Cargando…
      </div>

      <div
        v-else-if="rechazadas.length === 0"
        class="flex flex-col items-center gap-3 rounded border border-linea bg-superficie px-6 py-16 text-center"
      >
        <AlertTriangle class="h-10 w-10 text-tinta-3" />
        <p class="font-display text-[17px] font-semibold text-tinta">Nada por revisar</p>
        <p class="text-[13.6px] text-tinta-2">Todas las ventas offline se sincronizaron sin problemas.</p>
      </div>

      <div v-else class="flex flex-col gap-3">
        <div v-for="v in rechazadas" :key="v.id" class="rounded border border-peligro/30 bg-peligro-tenue p-5">
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <p class="font-mono text-[11px] text-tinta-3">VENTA {{ v.numero }}</p>
              <p class="mt-1 font-display text-[15px] font-semibold text-tinta">{{ v.resumenItems }}</p>
              <p class="mt-1 text-[12px] text-tinta-2">
                {{ dayjs(v.creadoEn).format('DD/MM/YYYY HH:mm') }} · rechazada
                {{ dayjs(v.rechazadaEn).format('DD/MM HH:mm') }}
              </p>
            </div>
            <span class="flex-shrink-0 font-display text-[17px] font-bold tabular-nums text-tinta">
              {{ fmtBs(v.total) }}
            </span>
          </div>

          <div class="mt-3 rounded-s bg-superficie px-3 py-2 text-[13px] text-peligro">
            {{ v.motivo }}
          </div>

          <button
            type="button"
            :disabled="reintentandoId === v.id"
            class="mt-3 flex min-h-11 items-center gap-2 rounded-s border border-linea bg-superficie px-4 text-[13px] font-semibold text-tinta-2 hover:bg-superficie-2 disabled:opacity-60"
            @click="reintentar(v)"
          >
            <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': reintentandoId === v.id }" />
            {{ reintentandoId === v.id ? 'Reintentando…' : 'Reintentar' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
