<script setup>
import { computed, onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import dayjs from 'dayjs'
import { Wallet } from 'lucide-vue-next'
import { abrirCaja, cerrarCaja, listarHistorialCaja, obtenerSesionAbierta } from '@/api/caja'

const cargando = ref(true)
const sesionActual = ref(null)
const resultadoCierre = ref(null)

const montoInicial = ref('')
const montoDeclarado = ref('')
const enviando = ref(false)
const error = ref('')

const historial = ref([])
const pagina = ref(1)
const cargandoHistorial = ref(true)

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

async function cargarSesion() {
  cargando.value = true
  try {
    sesionActual.value = await obtenerSesionAbierta()
  } finally {
    cargando.value = false
  }
}

async function cargarHistorial() {
  cargandoHistorial.value = true
  try {
    historial.value = await listarHistorialCaja(pagina.value)
  } finally {
    cargandoHistorial.value = false
  }
}

async function irAPagina(delta) {
  pagina.value += delta
  await cargarHistorial()
}

async function alAbrirCaja() {
  error.value = ''
  const monto = Number(montoInicial.value || 0)
  enviando.value = true
  try {
    sesionActual.value = await abrirCaja(monto, null)
    toast.success('Caja abierta')
    montoInicial.value = ''
  } catch (e) {
    const mensajes = e.response?.data?.errores?.map((x) => x.mensaje)
    error.value = mensajes?.join(' ') || 'No se pudo abrir la caja.'
  } finally {
    enviando.value = false
  }
}

async function alCerrarCaja() {
  error.value = ''
  const monto = Number(montoDeclarado.value)
  if (montoDeclarado.value === '' || Number.isNaN(monto) || monto < 0) {
    error.value = 'Ingresa el monto que contaste en caja.'
    return
  }
  enviando.value = true
  try {
    resultadoCierre.value = await cerrarCaja(sesionActual.value.id, monto)
    toast.success('Caja cerrada')
    await cargarHistorial()
  } catch (e) {
    const mensajes = e.response?.data?.errores?.map((x) => x.mensaje)
    error.value = mensajes?.join(' ') || 'No se pudo cerrar la caja.'
  } finally {
    enviando.value = false
  }
}

async function alEntenderResultado() {
  resultadoCierre.value = null
  montoDeclarado.value = ''
  await cargarSesion()
}

const diferenciaCierre = computed(() => {
  if (!resultadoCierre.value) return 0
  return resultadoCierre.value.montoCierreDeclarado - resultadoCierre.value.montoCierreCalculado
})
const cierreCuadra = computed(() => Math.abs(diferenciaCierre.value) <= 0.01)

function diferenciaHistorial(s) {
  if (s.montoCierreDeclarado === null || s.montoCierreCalculado === null) return null
  return s.montoCierreDeclarado - s.montoCierreCalculado
}

onMounted(() => {
  cargarSesion()
  cargarHistorial()
})
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto flex w-full max-w-[640px] flex-col gap-6">
      <h2 class="font-display text-[24px] font-bold text-tinta">Caja</h2>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">Cargando…</div>

      <!-- Resultado del cierre: recién ahora se ve el monto calculado -->
      <div v-else-if="resultadoCierre" class="rounded border border-linea bg-superficie p-6">
        <div class="mb-5 flex items-center gap-3">
          <Wallet class="h-5 w-5 text-marca" />
          <p class="font-display text-[17px] font-semibold text-tinta">Caja cerrada</p>
        </div>

        <div class="flex flex-col gap-3">
          <div class="flex justify-between text-[13.6px]">
            <span class="text-tinta-2">Monto que declaraste</span>
            <span class="font-semibold tabular-nums text-tinta">{{ fmtBs(resultadoCierre.montoCierreDeclarado) }}</span>
          </div>
          <div class="flex justify-between text-[13.6px]">
            <span class="text-tinta-2">Monto calculado por el sistema</span>
            <span class="font-semibold tabular-nums text-tinta">{{ fmtBs(resultadoCierre.montoCierreCalculado) }}</span>
          </div>
          <div class="flex justify-between border-t border-dashed border-linea pt-3 text-[15px]">
            <span class="font-semibold" :class="cierreCuadra ? 'text-exito' : 'text-peligro'">
              {{ cierreCuadra ? 'Cuadra' : 'Diferencia' }}
            </span>
            <span class="font-bold tabular-nums" :class="cierreCuadra ? 'text-exito' : 'text-peligro'">
              {{ fmtBs(Math.abs(diferenciaCierre)) }}
            </span>
          </div>
        </div>

        <button
          type="button"
          class="mt-6 min-h-11 w-full rounded-s bg-marca font-bold text-sobre-marca hover:bg-marca-hover"
          @click="alEntenderResultado"
        >
          Entendido
        </button>
      </div>

      <!-- Sin sesión abierta: abrir caja -->
      <div v-else-if="!sesionActual" class="rounded border border-linea bg-superficie p-6">
        <p class="font-display text-[17px] font-semibold text-tinta">Abrir caja</p>
        <p class="mt-1 text-[13.6px] text-tinta-2">Registra con cuánto efectivo empiezas el turno.</p>

        <div v-if="error" class="mt-4 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">{{ error }}</div>

        <label class="mt-4 flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Monto inicial (Bs)</span>
          <input
            v-model="montoInicial"
            type="number"
            min="0"
            step="0.01"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
          />
        </label>

        <button
          type="button"
          :disabled="enviando"
          class="mt-4 min-h-11 w-full rounded-s bg-marca font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
          @click="alAbrirCaja"
        >
          {{ enviando ? 'Abriendo…' : 'Abrir caja' }}
        </button>
      </div>

      <!-- Sesión abierta: estado + cerrar caja -->
      <div v-else class="flex flex-col gap-6">
        <div class="rounded border border-linea bg-superficie p-6">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <Wallet class="h-5 w-5 text-marca" />
              <p class="font-display text-[17px] font-semibold text-tinta">Caja activa</p>
            </div>
            <span class="rounded-chip bg-exito-tenue px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-exito">
              Abierta
            </span>
          </div>
          <div class="mt-4 flex justify-between text-[13.6px]">
            <span class="text-tinta-2">Apertura</span>
            <span class="tabular-nums text-tinta">{{ dayjs(sesionActual.apertura).format('DD/MM/YYYY HH:mm') }}</span>
          </div>
          <div class="mt-2 flex justify-between text-[13.6px]">
            <span class="text-tinta-2">Monto inicial</span>
            <span class="font-semibold tabular-nums text-tinta">{{ fmtBs(sesionActual.montoInicial) }}</span>
          </div>
        </div>

        <div class="rounded border border-linea bg-superficie p-6">
          <p class="font-display text-[17px] font-semibold text-tinta">Cerrar caja</p>
          <p class="mt-1 text-[13.6px] text-tinta-2">
            Cuenta el efectivo físico y declara el monto antes de ver el cálculo del sistema.
          </p>

          <div v-if="error" class="mt-4 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">{{ error }}</div>

          <label class="mt-4 flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Monto contado (Bs)</span>
            <input
              v-model="montoDeclarado"
              type="number"
              min="0"
              step="0.01"
              class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
            />
          </label>

          <button
            type="button"
            :disabled="enviando"
            class="mt-4 min-h-11 w-full rounded-s bg-marca font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
            @click="alCerrarCaja"
          >
            {{ enviando ? 'Cerrando…' : 'Cerrar caja' }}
          </button>
        </div>
      </div>

      <!-- Historial de sesiones (patrón LISTADO) -->
      <section>
        <h3 class="mb-3 font-display text-[17px] font-semibold text-tinta">Historial de sesiones</h3>

        <div class="overflow-hidden rounded border border-linea bg-superficie">
          <div class="overflow-x-auto">
            <table class="w-full border-collapse text-left">
              <thead>
                <tr class="border-b border-linea bg-superficie-2">
                  <th class="px-4 py-3 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Apertura</th>
                  <th class="px-4 py-3 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Cajero</th>
                  <th class="px-4 py-3 text-right text-[10px] font-bold uppercase tracking-wide text-tinta-3">Diferencia</th>
                  <th class="px-4 py-3 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Estado</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="!cargandoHistorial && historial.length === 0">
                  <td colspan="4" class="px-4 py-10 text-center text-[13.6px] text-tinta-3">
                    Todavía no hay sesiones cerradas.
                  </td>
                </tr>
                <tr
                  v-for="s in historial"
                  :key="s.id"
                  class="border-b border-linea transition-colors last:border-b-0 hover:bg-marca-tenue"
                >
                  <td class="px-4 py-3 align-middle text-[13px] tabular-nums text-tinta">
                    {{ dayjs(s.apertura).format('DD/MM/YY HH:mm') }}
                  </td>
                  <td class="px-4 py-3 align-middle text-[13px] text-tinta-2">{{ s.usuarioNombre }}</td>
                  <td class="px-4 py-3 text-right align-middle">
                    <span
                      v-if="diferenciaHistorial(s) !== null"
                      class="font-semibold tabular-nums"
                      :class="Math.abs(diferenciaHistorial(s)) <= 0.01 ? 'text-exito' : 'text-peligro'"
                    >
                      {{ fmtBs(Math.abs(diferenciaHistorial(s))) }}
                    </span>
                    <span v-else class="text-tinta-3">—</span>
                  </td>
                  <td class="px-4 py-3 align-middle">
                    <span
                      class="inline-block rounded-chip px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide"
                      :class="s.estado === 'ABIERTA' ? 'bg-exito-tenue text-exito' : 'bg-superficie-2 text-tinta-3'"
                    >
                      {{ s.estado === 'ABIERTA' ? 'abierta' : 'cerrada' }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="flex items-center justify-between border-t border-linea px-4 py-3">
            <p class="text-[12.8px] text-tinta-3">Página <span class="tabular-nums">{{ pagina }}</span></p>
            <div class="flex gap-2">
              <button
                type="button"
                :disabled="pagina === 1"
                class="flex min-h-11 min-w-11 items-center justify-center rounded-s border border-linea text-tinta-2 hover:bg-superficie-2 disabled:opacity-40"
                @click="irAPagina(-1)"
              >
                ‹
              </button>
              <button
                type="button"
                :disabled="historial.length < 20"
                class="flex min-h-11 min-w-11 items-center justify-center rounded-s border border-linea text-tinta-2 hover:bg-superficie-2 disabled:opacity-40"
                @click="irAPagina(1)"
              >
                ›
              </button>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>
