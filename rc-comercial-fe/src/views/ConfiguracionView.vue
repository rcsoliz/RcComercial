<script setup>
import { onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import { HelpCircle } from 'lucide-vue-next'
import { obtenerEmpresaActual } from '@/api/empresa'
import { actualizarConfiguracion, editarEmpresa, obtenerConfiguracion } from '@/api/configuracion'

const cargando = ref(true)
const guardandoEmpresa = ref(false)
const guardandoConfig = ref(false)
const errorEmpresa = ref('')
const errorConfig = ref('')

const nombreEmpresa = ref('')
const nit = ref('')
const telefonoWhatsapp = ref('')
const rubroNombre = ref('')

const permiteStockNegativo = ref(false)
const horaResumenWhatsapp = ref(21)

async function cargar() {
  cargando.value = true
  try {
    const [empresa, config] = await Promise.all([obtenerEmpresaActual(), obtenerConfiguracion()])
    nombreEmpresa.value = empresa.nombre
    nit.value = empresa.nit || ''
    telefonoWhatsapp.value = empresa.telefonoWhatsapp || ''
    rubroNombre.value = empresa.rubroNombre
    permiteStockNegativo.value = config.permiteStockNegativo
    horaResumenWhatsapp.value = config.horaResumenWhatsapp
  } finally {
    cargando.value = false
  }
}

async function guardarEmpresa() {
  errorEmpresa.value = ''
  guardandoEmpresa.value = true
  try {
    await editarEmpresa({
      nombre: nombreEmpresa.value,
      nit: nit.value.trim() || null,
      telefonoWhatsapp: telefonoWhatsapp.value.trim() || null,
    })
    toast.success('Datos de la empresa guardados')
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorEmpresa.value = mensajes?.join(' ') || 'No se pudo guardar.'
  } finally {
    guardandoEmpresa.value = false
  }
}

async function guardarConfig() {
  errorConfig.value = ''
  guardandoConfig.value = true
  try {
    await actualizarConfiguracion({
      permiteStockNegativo: permiteStockNegativo.value,
      horaResumenWhatsapp: Number(horaResumenWhatsapp.value),
    })
    toast.success('Configuración guardada')
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorConfig.value = mensajes?.join(' ') || 'No se pudo guardar.'
  } finally {
    guardandoConfig.value = false
  }
}

onMounted(cargar)
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto flex w-full max-w-[640px] flex-col gap-6">
      <h2 class="font-display text-[24px] font-bold text-tinta">Configuración</h2>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">Cargando…</div>

      <template v-else>
        <form class="rounded border border-linea bg-superficie p-6" novalidate @submit.prevent="guardarEmpresa">
          <h3 class="mb-4 font-display text-[15px] font-bold text-tinta">Datos de la empresa</h3>
          <div v-if="errorEmpresa" class="mb-4 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
            {{ errorEmpresa }}
          </div>
          <div class="flex flex-col gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre / razón social</span>
              <input
                v-model="nombreEmpresa"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
            </label>
            <div class="grid grid-cols-2 gap-4">
              <label class="flex flex-col gap-1.5">
                <span class="text-[0.8rem] font-semibold text-tinta-2">NIT (opcional)</span>
                <input
                  v-model="nit"
                  type="text"
                  class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
                />
              </label>
              <label class="flex flex-col gap-1.5">
                <span class="text-[0.8rem] font-semibold text-tinta-2">WhatsApp (opcional)</span>
                <input
                  v-model="telefonoWhatsapp"
                  type="text"
                  placeholder="+59171234567"
                  class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
                />
              </label>
            </div>
            <p class="text-[12px] text-tinta-3">Rubro: {{ rubroNombre }} (se define al crear la empresa)</p>
          </div>
          <div class="mt-5 flex justify-end border-t border-linea pt-5">
            <button
              type="submit"
              :disabled="guardandoEmpresa"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
            >
              {{ guardandoEmpresa ? 'Guardando…' : 'Guardar datos de la empresa' }}
            </button>
          </div>
        </form>

        <form class="rounded border border-linea bg-superficie p-6" novalidate @submit.prevent="guardarConfig">
          <h3 class="mb-4 font-display text-[15px] font-bold text-tinta">Ventas y notificaciones</h3>
          <div v-if="errorConfig" class="mb-4 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
            {{ errorConfig }}
          </div>
          <div class="flex flex-col gap-4">
            <label class="check flex cursor-pointer items-start gap-3 rounded-s border border-linea px-4 py-3.5">
              <input v-model="permiteStockNegativo" type="checkbox" class="mt-0.5 h-4 w-4" />
              <span>
                <span class="block text-[13.6px] font-semibold text-tinta">Permitir vender sin stock</span>
                <span class="block text-[12px] text-tinta-2">
                  Si lo activas, el POS deja completar una venta aunque el stock quede en negativo (para
                  regularizar después con un ajuste).
                </span>
              </span>
            </label>

            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Hora del resumen diario por WhatsApp</span>
              <select
                v-model="horaResumenWhatsapp"
                class="min-h-11 max-w-[200px] rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
              >
                <option v-for="h in 24" :key="h - 1" :value="h - 1">{{ String(h - 1).padStart(2, '0') }}:00</option>
              </select>
              <span class="flex items-start gap-1 text-[11.6px] text-tinta-3">
                <HelpCircle class="mt-0.5 h-3.5 w-3.5 flex-shrink-0" />
                A esta hora (hora Bolivia) se manda el resumen del día y las alertas de stock/vencimientos, si hay.
              </span>
            </label>
          </div>
          <div class="mt-5 flex justify-end border-t border-linea pt-5">
            <button
              type="submit"
              :disabled="guardandoConfig"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
            >
              {{ guardandoConfig ? 'Guardando…' : 'Guardar configuración' }}
            </button>
          </div>
        </form>
      </template>
    </div>
  </div>
</template>
