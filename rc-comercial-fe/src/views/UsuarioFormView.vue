<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { toast } from 'vue-sonner'
import { KeyRound, Trash2 } from 'lucide-vue-next'
import {
  crearUsuario,
  desactivarUsuario,
  editarUsuario,
  obtenerUsuarioPorId,
  restablecerPassword,
} from '@/api/usuarios'
import { listarRoles } from '@/api/roles'
import { listarSucursales } from '@/api/sucursales'
import ModalDesactivar from '@/components/ui/ModalDesactivar.vue'
import ModalPasswordTemporal from '@/components/usuarios/ModalPasswordTemporal.vue'

const props = defineProps({
  id: { type: String, default: null },
})

const router = useRouter()

const esNuevo = computed(() => !props.id)

const cargando = ref(true)
const guardando = ref(false)
const errorGeneral = ref('')
const usuarioOriginal = ref(null)
const roles = ref([])
const sucursales = ref([])

const mostrarDesactivar = ref(false)
const mostrarPasswordTemporal = ref(false)
const passwordTemporal = ref('')
const mensajeDesactivar = computed(
  () => `"${usuarioOriginal.value?.nombre}" ya no podrá iniciar sesión. No se borra: se puede reactivar después desde la base de datos si hace falta.`,
)

const esquema = toTypedSchema(
  z.object({
    nombre: z.string().min(1, 'Ingresa el nombre.').max(100, 'Máximo 100 caracteres.'),
    usuarioLogin: z.string().min(1, 'Ingresa el usuario de acceso.').max(50, 'Máximo 50 caracteres.'),
    rolId: z.string().min(1, 'Elige un rol.'),
    sucursalId: z.string().optional(),
    telefonoWhatsapp: z.string().optional(),
  }),
)

const { handleSubmit, defineField, errors, setValues } = useForm({ validationSchema: esquema })

const [nombre, nombreAttrs] = defineField('nombre')
const [usuarioLogin, usuarioLoginAttrs] = defineField('usuarioLogin')
const [rolId, rolIdAttrs] = defineField('rolId')
const [sucursalId, sucursalIdAttrs] = defineField('sucursalId')
const [telefonoWhatsapp, telefonoWhatsappAttrs] = defineField('telefonoWhatsapp')

// El rol determina los permisos del JWT (permisos_version): si se cambia,
// las sesiones activas de este usuario dejan de servir en minutos.
const cambioDeRolPendiente = computed(
  () => !esNuevo.value && usuarioOriginal.value && rolId.value && rolId.value !== usuarioOriginal.value.rolId,
)

async function cargarDatos() {
  cargando.value = true
  try {
    const [rls, sucs] = await Promise.all([listarRoles(), listarSucursales('activos')])
    roles.value = rls
    sucursales.value = sucs

    if (!esNuevo.value) {
      const usuario = await obtenerUsuarioPorId(props.id)
      usuarioOriginal.value = usuario
      setValues({
        nombre: usuario.nombre,
        usuarioLogin: usuario.usuarioLogin,
        rolId: usuario.rolId,
        sucursalId: usuario.sucursalId || '',
        telefonoWhatsapp: usuario.telefonoWhatsapp || '',
      })
    }
  } finally {
    cargando.value = false
  }
}

function aComando(valores) {
  return {
    nombre: valores.nombre,
    usuarioLogin: valores.usuarioLogin,
    rolId: valores.rolId,
    sucursalId: valores.sucursalId || null,
    telefonoWhatsapp: valores.telefonoWhatsapp || null,
  }
}

const onSubmit = handleSubmit(async (valores) => {
  errorGeneral.value = ''
  guardando.value = true
  try {
    if (esNuevo.value) {
      const creado = await crearUsuario(aComando(valores))
      passwordTemporal.value = creado.passwordTemporal
      usuarioOriginal.value = creado.usuario
      mostrarPasswordTemporal.value = true
      toast.success('Usuario creado')
    } else {
      const huboRotacion = cambioDeRolPendiente.value
      await editarUsuario(props.id, { id: props.id, ...aComando(valores) })
      toast.success(
        huboRotacion
          ? 'Usuario guardado. Sus sesiones activas se cerrarán en los próximos minutos por el cambio de rol.'
          : 'Usuario guardado',
      )
      router.push({ name: 'usuarios' })
    }
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorGeneral.value = mensajes?.join(' ') || 'No se pudo guardar el usuario.'
  } finally {
    guardando.value = false
  }
})

watch(mostrarPasswordTemporal, (esta) => {
  if (!esta && esNuevo.value) router.push({ name: 'usuarios' })
})

async function restablecer() {
  try {
    const resultado = await restablecerPassword(props.id)
    passwordTemporal.value = resultado.passwordTemporal
    mostrarPasswordTemporal.value = true
    toast.success('Contraseña restablecida')
  } catch {
    toast.error('No se pudo restablecer la contraseña.')
  }
}

async function confirmarDesactivar() {
  try {
    await desactivarUsuario(props.id)
    toast.success('Usuario desactivado')
    router.push({ name: 'usuarios' })
  } catch {
    toast.error('No se pudo desactivar el usuario.')
  }
}

onMounted(cargarDatos)
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto w-full max-w-[640px]">
      <h2 class="mb-6 font-display text-[24px] font-bold text-tinta">
        {{ esNuevo ? 'Nuevo usuario' : 'Editar usuario' }}
      </h2>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">
        Cargando…
      </div>

      <form v-else class="rounded border border-linea bg-superficie p-6" novalidate @submit.prevent="onSubmit">
        <div v-if="errorGeneral" class="mb-5 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
          {{ errorGeneral }}
        </div>

        <div v-if="cambioDeRolPendiente" class="mb-5 rounded-s bg-aviso-tenue px-4 py-3 text-[13px] text-aviso">
          Al guardar, las sesiones activas de este usuario dejarán de servir en los próximos minutos (el sistema
          revisa los permisos contra el rol nuevo) — deberá volver a iniciar sesión.
        </div>

        <div class="flex flex-col gap-4">
          <label class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre completo</span>
            <input
              v-model="nombre"
              v-bind="nombreAttrs"
              type="text"
              class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              :class="errors.nombre ? 'border-peligro' : 'border-linea'"
            />
            <span v-if="errors.nombre" class="text-[12px] text-peligro">{{ errors.nombre }}</span>
          </label>

          <label class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Usuario de acceso</span>
            <input
              v-model="usuarioLogin"
              v-bind="usuarioLoginAttrs"
              type="text"
              autocomplete="off"
              class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              :class="errors.usuarioLogin ? 'border-peligro' : 'border-linea'"
            />
            <span v-if="errors.usuarioLogin" class="text-[12px] text-peligro">{{ errors.usuarioLogin }}</span>
          </label>

          <div class="grid grid-cols-2 gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Rol</span>
              <select
                v-model="rolId"
                v-bind="rolIdAttrs"
                class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
                :class="errors.rolId ? 'border-peligro' : 'border-linea'"
              >
                <option value="">Selecciona…</option>
                <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.nombre }}</option>
              </select>
              <span v-if="errors.rolId" class="text-[12px] text-peligro">{{ errors.rolId }}</span>
            </label>
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Sucursal</span>
              <select
                v-model="sucursalId"
                v-bind="sucursalIdAttrs"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              >
                <option value="">Todas</option>
                <option v-for="s in sucursales" :key="s.id" :value="s.id">{{ s.nombre }}</option>
              </select>
            </label>
          </div>

          <label class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">WhatsApp (opcional)</span>
            <input
              v-model="telefonoWhatsapp"
              v-bind="telefonoWhatsappAttrs"
              type="text"
              placeholder="+59171234567"
              class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
            />
          </label>
        </div>

        <div class="mt-6 flex items-center justify-between border-t border-linea pt-5">
          <div v-if="!esNuevo" class="flex gap-2">
            <button
              type="button"
              class="flex min-h-11 items-center gap-2 rounded-s px-3 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie-2"
              @click="restablecer"
            >
              <KeyRound class="h-4 w-4" />
              Restablecer contraseña
            </button>
            <button
              type="button"
              class="flex min-h-11 items-center gap-2 rounded-s px-3 text-[13.6px] font-semibold text-peligro hover:bg-peligro-tenue"
              @click="mostrarDesactivar = true"
            >
              <Trash2 class="h-4 w-4" />
              Desactivar
            </button>
          </div>
          <span v-else></span>

          <div class="flex gap-3">
            <button
              type="button"
              class="min-h-11 rounded-s border border-linea px-5 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie-2"
              @click="router.push({ name: 'usuarios' })"
            >
              Cancelar
            </button>
            <button
              type="submit"
              :disabled="guardando"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
            >
              {{ guardando ? 'Guardando…' : 'Guardar usuario' }}
            </button>
          </div>
        </div>
      </form>
    </div>
  </div>

  <ModalDesactivar
    v-if="usuarioOriginal"
    v-model="mostrarDesactivar"
    titulo="Desactivar usuario"
    :nombre="usuarioOriginal.nombre"
    :mensaje="mensajeDesactivar"
    @confirmar="confirmarDesactivar"
  />
  <ModalPasswordTemporal
    v-model="mostrarPasswordTemporal"
    :nombre-usuario="usuarioOriginal?.nombre || nombre"
    :password="passwordTemporal"
  />
</template>
