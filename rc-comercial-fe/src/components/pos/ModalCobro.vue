<script setup>
import { computed, ref, watch } from 'vue'
import ModalBase from '@/components/ui/ModalBase.vue'
import { useVentaStore } from '@/stores/venta'
import { crearVenta } from '@/api/ventas'
import { encolarVentaPendiente, tomarSiguienteNumero } from '@/db/ventasDb'
import { descontarStockOptimista, obtenerProductoLocalPorId } from '@/db/catalogoDb'

const abierto = defineModel({ type: Boolean, default: false })
const emit = defineEmits(['venta-creada'])

const venta = useVentaStore()

const metodo = ref('EFECTIVO')
const monto = ref('')
const enviando = ref(false)
const errorGeneral = ref('')
const avisoStockInsuficiente = ref(null)
let confirmarPeseAStockInsuficiente = false

function fmtBs(n) {
  const [ent, dec] = Number(n).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

const faltante = computed(() => Math.max(0, Math.round(venta.diferenciaPago * 100) / 100))
const puedeConfirmar = computed(() => Math.abs(venta.diferenciaPago) <= 0.01 && venta.pagos.length > 0)

watch(abierto, (esta) => {
  if (esta) {
    metodo.value = 'EFECTIVO'
    monto.value = faltante.value > 0 ? faltante.value.toFixed(2) : ''
    errorGeneral.value = ''
    avisoStockInsuficiente.value = null
    confirmarPeseAStockInsuficiente = false
  }
})

function llenarMontoExacto() {
  monto.value = faltante.value.toFixed(2)
}

function agregarPago() {
  const valor = Number(monto.value)
  if (!valor || valor <= 0) return
  venta.agregarPago({ metodo: metodo.value, monto: valor, referenciaQr: null })
  monto.value = faltante.value > 0 ? faltante.value.toFixed(2) : ''
}

/**
 * Compara el carrito contra el stock del catálogo local (ya viene descontado
 * de forma optimista por cualquier venta offline previa todavía sin
 * sincronizar): así el POS avisa ANTES de dejar sobrevender, en vez de solo
 * anotar un stock local negativo en silencio.
 */
async function verificarStockLocal() {
  const insuficientes = []
  for (const item of venta.items) {
    const producto = await obtenerProductoLocalPorId(item.productoId)
    const necesita = item.cantidad * item.factor
    const disponible = producto?.stockTotal ?? 0
    if (disponible < necesita) insuficientes.push({ nombre: item.nombre, disponible, necesita })
  }
  return insuficientes
}

/** Guarda la venta en la cola local (IndexedDB) y descuenta stock local de forma optimista. */
async function guardarVentaOffline() {
  let numero = await tomarSiguienteNumero()
  if (!numero) {
    // Respaldo si el dispositivo nunca reservó rango estando en línea: se
    // deriva del propio Id (único por diseño), cabe en VARCHAR(20).
    numero = 'OFF' + venta.id.replace(/-/g, '').slice(0, 17).toUpperCase()
  }

  const comando = { ...venta.aComandoCrearVenta(), numero }
  await encolarVentaPendiente({
    id: venta.id,
    numero,
    creadoEn: new Date().toISOString(),
    total: venta.total,
    resumenItems: venta.items.map((i) => `${i.cantidad} × ${i.nombre}`).join(', '),
    comando,
  })

  for (const item of venta.items) {
    await descontarStockOptimista(item.productoId, item.cantidad * item.factor)
  }

  return numero
}

async function confirmarCobro() {
  errorGeneral.value = ''
  avisoStockInsuficiente.value = null
  enviando.value = true
  try {
    try {
      const ventaCreada = await crearVenta(venta.aComandoCrearVenta())
      emit('venta-creada', { ventaCreada, offline: false })
      abierto.value = false
      return
    } catch (error) {
      // Sin response = falla de red/servidor inalcanzable: no es un rechazo
      // real del backend, así que la venta se guarda offline en vez de
      // perderse. Un 422/403/etc SÍ es una respuesta real: se relanza abajo.
      if (error.response) throw error
    }

    if (!confirmarPeseAStockInsuficiente) {
      const insuficientes = await verificarStockLocal()
      if (insuficientes.length > 0) {
        avisoStockInsuficiente.value = insuficientes
        return
      }
    }

    const numero = await guardarVentaOffline()
    emit('venta-creada', { ventaCreada: { numero, total: venta.total, id: venta.id }, offline: true })
    abierto.value = false
  } catch (error) {
    const status = error.response?.status
    if (status === 422) {
      const mensajes = error.response.data?.errores?.map((e) => e.mensaje) ?? []
      errorGeneral.value = mensajes.join(' ') || 'La venta no pudo completarse.'
    } else if (status === 403) {
      errorGeneral.value = 'No tienes permiso para registrar ventas.'
    } else {
      errorGeneral.value = 'Ocurrió un error inesperado. Intenta de nuevo.'
    }
  } finally {
    enviando.value = false
  }
}

async function venderPeseAStockInsuficiente() {
  confirmarPeseAStockInsuficiente = true
  avisoStockInsuficiente.value = null
  await confirmarCobro()
}
</script>

<template>
  <ModalBase v-model="abierto" titulo="Cobrar venta">
    <div class="flex flex-col gap-5">
      <div class="rounded-s bg-marca-tenue px-4 py-3 text-center">
        <p class="text-[0.72rem] font-bold uppercase tracking-wide text-tinta-2">Total a cobrar</p>
        <p class="font-display text-[28px] font-bold tabular-nums text-marca">{{ fmtBs(venta.total) }}</p>
      </div>

      <div v-if="errorGeneral" class="rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
        {{ errorGeneral }}
      </div>

      <div v-if="avisoStockInsuficiente" class="rounded-s bg-aviso-tenue px-4 py-3 text-[13px] text-aviso">
        <p class="font-semibold">Sin conexión: el catálogo local no alcanza para</p>
        <ul class="mt-1 list-disc pl-4">
          <li v-for="i in avisoStockInsuficiente" :key="i.nombre">
            {{ i.nombre }} (disponible: {{ i.disponible }}, necesita: {{ i.necesita }})
          </li>
        </ul>
        <p class="mt-2">El servidor validará el stock real al sincronizar. ¿Vender de todas formas?</p>
        <div class="mt-3 grid grid-cols-2 gap-2">
          <button
            type="button"
            class="min-h-11 rounded-s border border-linea bg-superficie text-[13px] font-semibold text-tinta-2 hover:bg-superficie-2"
            @click="avisoStockInsuficiente = null"
          >
            Revisar carrito
          </button>
          <button
            type="button"
            class="min-h-11 rounded-s bg-aviso text-[13px] font-bold text-sobre-marca"
            @click="venderPeseAStockInsuficiente"
          >
            Vender de todas formas
          </button>
        </div>
      </div>

      <ul v-if="venta.pagos.length" class="flex flex-col gap-2">
        <li
          v-for="(pago, i) in venta.pagos"
          :key="i"
          class="flex items-center justify-between rounded-s border border-linea px-3 py-2 text-[13.6px]"
        >
          <span class="text-tinta-2">{{ pago.metodo === 'EFECTIVO' ? 'Efectivo' : 'QR' }}</span>
          <span class="tabular-nums font-medium text-tinta">{{ fmtBs(pago.monto) }}</span>
          <button
            type="button"
            class="text-[12px] font-semibold text-peligro hover:underline"
            @click="venta.quitarPago(i)"
          >
            Quitar
          </button>
        </li>
      </ul>

      <div class="flex items-end gap-2">
        <label class="flex flex-1 flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Método</span>
          <select
            v-model="metodo"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
          >
            <option value="EFECTIVO">Efectivo</option>
            <option value="QR">QR</option>
          </select>
        </label>
        <label class="flex flex-1 flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Monto</span>
          <input
            v-model="monto"
            type="number"
            min="0.01"
            step="0.01"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
          />
        </label>
        <button
          type="button"
          class="min-h-11 rounded-s border border-linea px-3 text-[12px] font-semibold text-tinta-2 hover:bg-superficie-2"
          @click="llenarMontoExacto"
        >
          Exacto
        </button>
        <button
          type="button"
          class="min-h-11 rounded-s bg-superficie-2 px-4 font-semibold text-tinta hover:bg-linea"
          @click="agregarPago"
        >
          Agregar
        </button>
      </div>

      <div class="flex justify-between text-[13.6px]">
        <span class="text-tinta-2">Pagado</span>
        <span class="tabular-nums font-medium text-tinta">{{ fmtBs(venta.totalPagado) }}</span>
      </div>
      <div class="-mt-3 flex justify-between text-[13.6px]">
        <span :class="faltante > 0 ? 'text-peligro' : 'text-exito'">
          {{ faltante > 0 ? 'Falta' : 'Vuelto' }}
        </span>
        <span class="tabular-nums font-semibold" :class="faltante > 0 ? 'text-peligro' : 'text-exito'">
          {{ fmtBs(faltante > 0 ? faltante : Math.abs(venta.diferenciaPago)) }}
        </span>
      </div>

      <button
        v-if="!avisoStockInsuficiente"
        type="button"
        :disabled="!puedeConfirmar || enviando"
        class="min-h-11 rounded-s bg-marca font-bold text-sobre-marca transition-colors hover:bg-marca-hover disabled:opacity-50"
        @click="confirmarCobro"
      >
        {{ enviando ? 'Cobrando…' : `Confirmar cobro · ${fmtBs(venta.total)}` }}
      </button>
    </div>
  </ModalBase>
</template>
