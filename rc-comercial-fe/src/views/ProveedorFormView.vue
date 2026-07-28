<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { toast } from 'vue-sonner'
import dayjs from 'dayjs'
import { HelpCircle, Trash2 } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { Permisos } from '@/utils/permisos'
import {
  desactivarProveedor,
  editarProveedor,
  crearProveedor,
  obtenerProveedorPorId,
} from '@/api/proveedores'
import { listarCompras } from '@/api/compras'
import ModalDesactivar from '@/components/ui/ModalDesactivar.vue'

const props = defineProps({
  id: { type: String, default: null },
})

const router = useRouter()
const auth = useAuthStore()

const esNuevo = computed(() => !props.id)
const puedeGuardar = computed(() => auth.tienePermiso(Permisos.ProveedoresCrearEditar))
const puedeEliminar = computed(() => auth.tienePermiso(Permisos.ProveedoresEliminar))

const cargando = ref(true)
const guardando = ref(false)
const errorGeneral = ref('')
const proveedorOriginal = ref(null)
const historialCompras = ref([])
const cargandoHistorial = ref(false)

const mostrarDesactivar = ref(false)
const mensajeDesactivar = computed(
  () => `"${proveedorOriginal.value?.nombre}" ya no aparecerá para elegir en Compras. No se borra: se puede reactivar después desde la base de datos si hace falta.`,
)

const esquema = toTypedSchema(
  z.object({
    nombre: z.string().min(1, 'Ingresa el nombre del proveedor.').max(200, 'Máximo 200 caracteres.'),
    nit: z.string().optional(),
    telefonoWhatsapp: z.string().optional(),
    diasCredito: z.coerce.number().int().min(0, 'No puede ser negativo.'),
    leadTimeDias: z.coerce.number().int().min(0, 'No puede ser negativo.'),
  }),
)

const { handleSubmit, defineField, errors, setValues } = useForm({
  validationSchema: esquema,
  initialValues: { diasCredito: 0, leadTimeDias: 3 },
})

const [nombre, nombreAttrs] = defineField('nombre')
const [nit, nitAttrs] = defineField('nit')
const [telefonoWhatsapp, telefonoWhatsappAttrs] = defineField('telefonoWhatsapp')
const [diasCredito, diasCreditoAttrs] = defineField('diasCredito')
const [leadTimeDias, leadTimeDiasAttrs] = defineField('leadTimeDias')

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

async function cargarDatos() {
  cargando.value = true
  try {
    if (!esNuevo.value) {
      const proveedor = await obtenerProveedorPorId(props.id)
      proveedorOriginal.value = proveedor
      setValues({
        nombre: proveedor.nombre,
        nit: proveedor.nit || '',
        telefonoWhatsapp: proveedor.telefonoWhatsapp || '',
        diasCredito: proveedor.diasCredito,
        leadTimeDias: proveedor.leadTimeDias,
      })

      cargandoHistorial.value = true
      listarCompras(1, props.id)
        .then((c) => (historialCompras.value = c))
        .finally(() => (cargandoHistorial.value = false))
    }
  } finally {
    cargando.value = false
  }
}

function aComando(valores) {
  return {
    nombre: valores.nombre,
    nit: valores.nit || null,
    telefonoWhatsapp: valores.telefonoWhatsapp || null,
    diasCredito: valores.diasCredito,
    leadTimeDias: valores.leadTimeDias,
  }
}

const onSubmit = handleSubmit(async (valores) => {
  errorGeneral.value = ''
  guardando.value = true
  try {
    if (esNuevo.value) {
      await crearProveedor(aComando(valores))
    } else {
      await editarProveedor(props.id, { id: props.id, ...aComando(valores) })
    }
    toast.success('Proveedor guardado')
    router.push({ name: 'proveedores' })
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorGeneral.value = mensajes?.join(' ') || 'No se pudo guardar el proveedor.'
  } finally {
    guardando.value = false
  }
})

async function confirmarDesactivar() {
  try {
    await desactivarProveedor(props.id)
    toast.success('Proveedor desactivado')
    router.push({ name: 'proveedores' })
  } catch {
    toast.error('No se pudo desactivar el proveedor.')
  }
}

function irAlSugerido() {
  router.push({ name: 'compras', query: { proveedorId: props.id } })
}

onMounted(cargarDatos)
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto w-full max-w-[640px]">
      <h2 class="mb-6 font-display text-[24px] font-bold text-tinta">
        {{ esNuevo ? 'Nuevo proveedor' : 'Editar proveedor' }}
      </h2>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">
        Cargando…
      </div>

      <form v-else class="rounded border border-linea bg-superficie p-6" novalidate @submit.prevent="onSubmit">
        <div v-if="errorGeneral" class="mb-5 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
          {{ errorGeneral }}
        </div>

        <div v-if="!esNuevo" class="mb-5 flex items-center justify-between rounded-s border border-linea bg-superficie-2 px-4 py-3">
          <div>
            <p class="text-[0.72rem] font-bold uppercase tracking-wide text-tinta-3">Sugerido de compra</p>
            <p class="text-[13.6px] text-tinta-2">Calculado con el stock, las ventas y el lead time de este proveedor.</p>
          </div>
          <button
            type="button"
            class="min-h-11 rounded-s border border-linea px-4 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie"
            @click="irAlSugerido"
          >
            Ver sugerido
          </button>
        </div>

        <div class="flex flex-col gap-4">
          <label class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre del proveedor</span>
            <input
              v-model="nombre"
              v-bind="nombreAttrs"
              type="text"
              class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              :class="errors.nombre ? 'border-peligro' : 'border-linea'"
            />
            <span v-if="errors.nombre" class="text-[12px] text-peligro">{{ errors.nombre }}</span>
          </label>

          <div class="grid grid-cols-2 gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">NIT (opcional)</span>
              <input
                v-model="nit"
                v-bind="nitAttrs"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
                :class="errors.nit ? 'border-peligro' : 'border-linea'"
              />
              <span v-if="errors.nit" class="text-[12px] text-peligro">{{ errors.nit }}</span>
            </label>
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">WhatsApp (opcional)</span>
              <input
                v-model="telefonoWhatsapp"
                v-bind="telefonoWhatsappAttrs"
                type="text"
                placeholder="+59171234567"
                class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
                :class="errors.telefonoWhatsapp ? 'border-peligro' : 'border-linea'"
              />
              <span v-if="errors.telefonoWhatsapp" class="text-[12px] text-peligro">{{ errors.telefonoWhatsapp }}</span>
            </label>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Días de crédito</span>
              <input
                v-model="diasCredito"
                v-bind="diasCreditoAttrs"
                type="number"
                min="0"
                step="1"
                class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
                :class="errors.diasCredito ? 'border-peligro' : 'border-linea'"
              />
              <span v-if="errors.diasCredito" class="text-[12px] text-peligro">{{ errors.diasCredito }}</span>
              <span class="flex items-start gap-1 text-[11.6px] text-tinta-3">
                <HelpCircle class="mt-0.5 h-3.5 w-3.5 flex-shrink-0" />
                Días que da de plazo para pagarle. 0 = paga al contado.
              </span>
            </label>
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Lead time (días)</span>
              <input
                v-model="leadTimeDias"
                v-bind="leadTimeDiasAttrs"
                type="number"
                min="0"
                step="1"
                class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
                :class="errors.leadTimeDias ? 'border-peligro' : 'border-linea'"
              />
              <span v-if="errors.leadTimeDias" class="text-[12px] text-peligro">{{ errors.leadTimeDias }}</span>
              <span class="flex items-start gap-1 text-[11.6px] text-tinta-3">
                <HelpCircle class="mt-0.5 h-3.5 w-3.5 flex-shrink-0" />
                Días que tarda en entregar desde que le pides. Alimenta el sugerido de compra.
              </span>
            </label>
          </div>
        </div>

        <div class="mt-6 flex items-center justify-between border-t border-linea pt-5">
          <button
            v-if="!esNuevo && puedeEliminar"
            type="button"
            class="flex min-h-11 items-center gap-2 rounded-s px-3 text-[13.6px] font-semibold text-peligro hover:bg-peligro-tenue"
            @click="mostrarDesactivar = true"
          >
            <Trash2 class="h-4 w-4" />
            Desactivar proveedor
          </button>
          <span v-else></span>

          <div class="flex gap-3">
            <button
              type="button"
              class="min-h-11 rounded-s border border-linea px-5 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie-2"
              @click="router.push({ name: 'proveedores' })"
            >
              Cancelar
            </button>
            <button
              v-if="puedeGuardar"
              type="submit"
              :disabled="guardando"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
            >
              {{ guardando ? 'Guardando…' : 'Guardar proveedor' }}
            </button>
          </div>
        </div>
      </form>

      <div v-if="!esNuevo" class="mt-6 rounded border border-linea bg-superficie p-6">
        <h3 class="mb-4 font-display text-[15px] font-bold text-tinta">Historial de compras</h3>
        <p v-if="cargandoHistorial" class="text-[13.6px] text-tinta-2">Cargando…</p>
        <p v-else-if="historialCompras.length === 0" class="text-[13.6px] text-tinta-2">
          Todavía no hay compras registradas a este proveedor.
        </p>
        <ul v-else class="flex flex-col gap-2">
          <li
            v-for="c in historialCompras"
            :key="c.id"
            class="flex items-center justify-between rounded-s border border-linea px-3 py-2.5 text-[13.6px]"
          >
            <div>
              <span class="font-mono text-[11px] text-tinta-3">{{ c.numero }}</span>
              <span class="ml-2 text-tinta-2">{{ dayjs(c.fecha).format('DD/MM/YYYY HH:mm') }}</span>
              <span
                v-if="c.estado === 'ANULADA'"
                class="ml-2 rounded-chip bg-peligro-tenue px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-peligro"
              >
                anulada
              </span>
            </div>
            <span class="font-semibold tabular-nums text-tinta">{{ fmtBs(c.total) }}</span>
          </li>
        </ul>
      </div>
    </div>
  </div>

  <ModalDesactivar
    v-if="proveedorOriginal"
    v-model="mostrarDesactivar"
    titulo="Desactivar proveedor"
    :nombre="proveedorOriginal.nombre"
    :mensaje="mensajeDesactivar"
    @confirmar="confirmarDesactivar"
  />
</template>
