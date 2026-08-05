<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import dayjs from 'dayjs'
import { ArrowLeft, Wrench } from 'lucide-vue-next'
import { obtenerHistorialVehiculo } from '@/api/vehiculos'

const props = defineProps({
  id: { type: String, required: true },
})

const router = useRouter()
const cargando = ref(true)
const items = ref([])

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

const coloresEstado = {
  COMPLETADA: 'bg-exito-tenue text-exito',
  ANULADA: 'bg-peligro-tenue text-peligro',
  PENDIENTE: 'bg-aviso-tenue text-aviso',
  ACEPTADA: 'bg-aviso-tenue text-aviso',
  RECHAZADA: 'bg-peligro-tenue text-peligro',
  CONVERTIDA: 'bg-exito-tenue text-exito',
}

onMounted(async () => {
  cargando.value = true
  try {
    items.value = await obtenerHistorialVehiculo(props.id)
  } finally {
    cargando.value = false
  }
})
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto w-full max-w-[720px]">
      <button
        type="button"
        class="mb-4 flex items-center gap-1.5 text-[13.6px] font-semibold text-tinta-2 hover:text-tinta"
        @click="router.back()"
      >
        <ArrowLeft class="h-4 w-4" />
        Volver
      </button>

      <h2 class="mb-6 font-display text-[24px] font-bold text-tinta">Historial del vehículo</h2>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">
        Cargando…
      </div>

      <div v-else-if="items.length === 0" class="flex flex-col items-center gap-3 rounded border border-linea bg-superficie px-6 py-16 text-center">
        <Wrench class="h-10 w-10 text-tinta-3" />
        <p class="text-[13.6px] text-tinta-2">Este vehículo todavía no tiene ventas ni proformas registradas.</p>
      </div>

      <ul v-else class="flex flex-col gap-3">
        <li v-for="item in items" :key="item.id" class="rounded border border-linea bg-superficie p-4">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <span class="rounded-chip bg-superficie-2 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">
                {{ item.tipoDocumento }}
              </span>
              <span class="font-mono text-[12px] text-tinta-3">{{ item.numero }}</span>
              <span class="text-[12.6px] text-tinta-2">{{ dayjs(item.fecha).format('DD/MM/YYYY HH:mm') }}</span>
              <span
                class="rounded-chip px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide"
                :class="coloresEstado[item.estado] || 'bg-superficie-2 text-tinta-3'"
              >
                {{ item.estado.toLowerCase() }}
              </span>
            </div>
            <span class="font-semibold tabular-nums text-tinta">{{ fmtBs(item.total) }}</span>
          </div>
          <ul class="mt-3 flex flex-col gap-1 border-t border-linea pt-3">
            <li v-for="(linea, idx) in item.lineas" :key="idx" class="flex justify-between text-[13px] text-tinta-2">
              <span>{{ linea.cantidad }} × {{ linea.productoNombre }}</span>
              <span class="tabular-nums">{{ fmtBs(linea.precioUnitario) }}</span>
            </li>
          </ul>
        </li>
      </ul>
    </div>
  </div>
</template>
