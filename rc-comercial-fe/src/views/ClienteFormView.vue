<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { toast } from 'vue-sonner'
import dayjs from 'dayjs'
import { Trash2 } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { Permisos } from '@/utils/permisos'
import { crearCliente, desactivarCliente, editarCliente, listarVentasPorCliente, obtenerClientePorId } from '@/api/clientes'
import ModalDesactivar from '@/components/ui/ModalDesactivar.vue'

const props = defineProps({
  id: { type: String, default: null },
})

const router = useRouter()
const auth = useAuthStore()

const esNuevo = computed(() => !props.id)
const puedeGuardar = computed(() => auth.tienePermiso(Permisos.ClientesCrearEditar))
const puedeEliminar = computed(() => auth.tienePermiso(Permisos.ClientesEliminar))
const puedeVerHistorial = computed(() => auth.tienePermiso(Permisos.VentasVerHistorial))

const cargando = ref(true)
const guardando = ref(false)
const errorGeneral = ref('')
const clienteOriginal = ref(null)
const historialVentas = ref([])
const cargandoHistorial = ref(false)

const mostrarDesactivar = ref(false)
const mensajeDesactivar = computed(
  () => `"${clienteOriginal.value?.nombre}" ya no aparecerá para elegir en el cobro. No se borra: se puede reactivar después desde la base de datos si hace falta.`,
)

const esquema = toTypedSchema(
  z.object({
    nombre: z.string().min(1, 'Ingresa el nombre del cliente.').max(150, 'Máximo 150 caracteres.'),
    tipoDocumento: z.string().min(1),
    nitCi: z.string().optional(),
    telefonoWhatsapp: z.string().optional(),
    email: z.string().email('El correo no es válido.').optional().or(z.literal('')),
  }),
)

const { handleSubmit, defineField, errors, setValues } = useForm({ validationSchema: esquema })

const [nombre, nombreAttrs] = defineField('nombre')
const [tipoDocumento, tipoDocumentoAttrs] = defineField('tipoDocumento')
const [nitCi, nitCiAttrs] = defineField('nitCi')
const [telefonoWhatsapp, telefonoWhatsappAttrs] = defineField('telefonoWhatsapp')
const [email, emailAttrs] = defineField('email')

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

async function cargarDatos() {
  cargando.value = true
  try {
    setValues({ tipoDocumento: 'CI' })

    if (!esNuevo.value) {
      const cliente = await obtenerClientePorId(props.id)
      clienteOriginal.value = cliente
      setValues({
        nombre: cliente.nombre,
        tipoDocumento: cliente.tipoDocumento,
        nitCi: cliente.nitCi || '',
        telefonoWhatsapp: cliente.telefonoWhatsapp || '',
        email: cliente.email || '',
      })

      if (puedeVerHistorial.value) {
        cargandoHistorial.value = true
        listarVentasPorCliente(props.id)
          .then((v) => (historialVentas.value = v))
          .finally(() => (cargandoHistorial.value = false))
      }
    }
  } finally {
    cargando.value = false
  }
}

function aComando(valores) {
  return {
    nombre: valores.nombre,
    tipoDocumento: valores.tipoDocumento,
    nitCi: valores.nitCi || null,
    telefonoWhatsapp: valores.telefonoWhatsapp || null,
    email: valores.email || null,
  }
}

const onSubmit = handleSubmit(async (valores) => {
  errorGeneral.value = ''
  guardando.value = true
  try {
    if (esNuevo.value) {
      await crearCliente(aComando(valores))
    } else {
      await editarCliente(props.id, { id: props.id, ...aComando(valores) })
    }
    toast.success('Cliente guardado')
    router.push({ name: 'clientes' })
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorGeneral.value = mensajes?.join(' ') || 'No se pudo guardar el cliente.'
  } finally {
    guardando.value = false
  }
})

async function confirmarDesactivar() {
  try {
    await desactivarCliente(props.id)
    toast.success('Cliente desactivado')
    router.push({ name: 'clientes' })
  } catch {
    toast.error('No se pudo desactivar el cliente.')
  }
}

onMounted(cargarDatos)
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto w-full max-w-[640px]">
      <h2 class="mb-6 font-display text-[24px] font-bold text-tinta">
        {{ esNuevo ? 'Nuevo cliente' : 'Editar cliente' }}
      </h2>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">
        Cargando…
      </div>

      <form v-else class="rounded border border-linea bg-superficie p-6" novalidate @submit.prevent="onSubmit">
        <div v-if="errorGeneral" class="mb-5 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
          {{ errorGeneral }}
        </div>

        <div class="flex flex-col gap-4">
          <label class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre completo o razón social</span>
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
              <span class="text-[0.8rem] font-semibold text-tinta-2">Tipo de documento</span>
              <select
                v-model="tipoDocumento"
                v-bind="tipoDocumentoAttrs"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              >
                <option value="CI">Cédula de identidad</option>
                <option value="NIT">NIT</option>
                <option value="CEX">Carnet de extranjero</option>
                <option value="PAS">Pasaporte</option>
              </select>
            </label>
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Número de documento (opcional)</span>
              <input
                v-model="nitCi"
                v-bind="nitCiAttrs"
                type="text"
                placeholder="Para la factura"
                class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
                :class="errors.nitCi ? 'border-peligro' : 'border-linea'"
              />
              <span v-if="errors.nitCi" class="text-[12px] text-peligro">{{ errors.nitCi }}</span>
            </label>
          </div>

          <div class="grid grid-cols-2 gap-4">
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
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Correo (opcional)</span>
              <input
                v-model="email"
                v-bind="emailAttrs"
                type="email"
                class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
                :class="errors.email ? 'border-peligro' : 'border-linea'"
              />
              <span v-if="errors.email" class="text-[12px] text-peligro">{{ errors.email }}</span>
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
            Desactivar cliente
          </button>
          <span v-else></span>

          <div class="flex gap-3">
            <button
              type="button"
              class="min-h-11 rounded-s border border-linea px-5 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie-2"
              @click="router.push({ name: 'clientes' })"
            >
              Cancelar
            </button>
            <button
              v-if="puedeGuardar"
              type="submit"
              :disabled="guardando"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
            >
              {{ guardando ? 'Guardando…' : 'Guardar cliente' }}
            </button>
          </div>
        </div>
      </form>

      <!-- Historial de ventas: reusa el permiso ventas.ver_historial -->
      <div v-if="!esNuevo && puedeVerHistorial" class="mt-6 rounded border border-linea bg-superficie p-6">
        <h3 class="mb-4 font-display text-[15px] font-bold text-tinta">Historial de ventas</h3>
        <p v-if="cargandoHistorial" class="text-[13.6px] text-tinta-2">Cargando…</p>
        <p v-else-if="historialVentas.length === 0" class="text-[13.6px] text-tinta-2">
          Este cliente todavía no tiene ventas registradas.
        </p>
        <ul v-else class="flex flex-col gap-2">
          <li
            v-for="v in historialVentas"
            :key="v.id"
            class="flex items-center justify-between rounded-s border border-linea px-3 py-2.5 text-[13.6px]"
          >
            <div>
              <span class="font-mono text-[11px] text-tinta-3">{{ v.numero }}</span>
              <span class="ml-2 text-tinta-2">{{ dayjs(v.fecha).format('DD/MM/YYYY HH:mm') }}</span>
              <span
                v-if="v.estado === 'ANULADA'"
                class="ml-2 rounded-chip bg-peligro-tenue px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-peligro"
              >
                anulada
              </span>
            </div>
            <span class="font-semibold tabular-nums text-tinta">{{ fmtBs(v.total) }}</span>
          </li>
        </ul>
      </div>
    </div>
  </div>

  <ModalDesactivar
    v-if="clienteOriginal"
    v-model="mostrarDesactivar"
    titulo="Desactivar cliente"
    :nombre="clienteOriginal.nombre"
    :mensaje="mensajeDesactivar"
    @confirmar="confirmarDesactivar"
  />
</template>
