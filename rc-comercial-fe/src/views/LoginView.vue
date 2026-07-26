<script setup>
import { ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { useRouter, useRoute } from 'vue-router'
import { toast } from 'vue-sonner'
import dayjs from 'dayjs'
import { useAuthStore } from '@/stores/auth'
import TemaToggle from '@/components/ui/TemaToggle.vue'

const esquema = toTypedSchema(
  z.object({
    usuarioLogin: z.string().min(1, 'Ingresa tu usuario.'),
    password: z.string().min(1, 'Ingresa tu contraseña.'),
  }),
)

const { handleSubmit, defineField, errors, setFieldError, isSubmitting } = useForm({
  validationSchema: esquema,
})

const [usuarioLogin, usuarioLoginAttrs] = defineField('usuarioLogin')
const [password, passwordAttrs] = defineField('password')

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const bloqueadoHasta = ref(null)

// El tenant se resuelve por subdominio (DESIGN.md §6); sin un endpoint aún
// que lo confirme contra la base, se deriva del propio host como referencia visual.
const nombreEmpresa = (() => {
  const partes = window.location.hostname.split('.')
  const esSubdominio = partes.length > 2 || (partes.length === 2 && partes[0] !== 'www')
  if (!esSubdominio) return 'SysCenterS'
  return partes[0].charAt(0).toUpperCase() + partes[0].slice(1)
})()

const onSubmit = handleSubmit(async (valores) => {
  bloqueadoHasta.value = null
  try {
    await auth.iniciarSesion(valores.usuarioLogin, valores.password)
    router.push(route.query.redirect?.toString() || '/panel')
  } catch (error) {
    const status = error.response?.status
    if (status === 423) {
      bloqueadoHasta.value = error.response.data?.bloqueadoHasta ?? null
    } else if (status === 401) {
      setFieldError('password', 'El usuario o la contraseña no son correctos.')
    } else {
      toast.error('No se pudo conectar con el servidor. Intenta de nuevo.')
    }
  }
})
</script>

<template>
  <div class="relative flex min-h-dvh items-center justify-center bg-papel px-4">
    <TemaToggle class="absolute right-4 top-4" />

    <div class="w-full max-w-[380px] overflow-hidden rounded border border-linea shadow">
      <div class="flex flex-col items-center gap-3 bg-superficie px-8 pb-6 pt-10 text-center">
        <div
          class="flex h-12 w-12 items-center justify-center rounded-s bg-marca font-display text-lg font-bold text-sobre-marca"
        >
          S
        </div>
        <div>
          <p class="font-display text-[19.2px] font-bold text-tinta">{{ nombreEmpresa }}</p>
          <p class="mt-1 text-[13.6px] text-tinta-2">Inicia sesión para continuar</p>
        </div>
      </div>

      <form class="flex flex-col gap-4 bg-superficie px-8 pb-8" novalidate @submit.prevent="onSubmit">
        <div v-if="bloqueadoHasta" class="rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
          Cuenta bloqueada temporalmente por demasiados intentos. Intenta de nuevo después de las
          {{ dayjs(bloqueadoHasta).format('HH:mm') }}.
        </div>

        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Usuario</span>
          <input
            v-model="usuarioLogin"
            v-bind="usuarioLoginAttrs"
            type="text"
            autocomplete="username"
            class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none transition-colors focus:border-marca focus:bg-superficie"
            :class="errors.usuarioLogin ? 'border-peligro' : 'border-linea'"
          />
          <span v-if="errors.usuarioLogin" class="text-[12px] text-peligro">{{ errors.usuarioLogin }}</span>
        </label>

        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Contraseña</span>
          <input
            v-model="password"
            v-bind="passwordAttrs"
            type="password"
            autocomplete="current-password"
            class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none transition-colors focus:border-marca focus:bg-superficie"
            :class="errors.password ? 'border-peligro' : 'border-linea'"
          />
          <span v-if="errors.password" class="text-[12px] text-peligro">{{ errors.password }}</span>
        </label>

        <button
          type="submit"
          :disabled="isSubmitting || !!bloqueadoHasta"
          class="min-h-11 rounded-s bg-marca font-bold text-sobre-marca transition-colors hover:bg-marca-hover disabled:opacity-60"
        >
          Iniciar sesión
        </button>
      </form>

      <div
        class="h-3 bg-superficie"
        style="
          mask: conic-gradient(from -45deg at bottom, #0000, #000 1deg 89deg, #0000 90deg) 50% / 14px 100%;
          -webkit-mask: conic-gradient(from -45deg at bottom, #0000, #000 1deg 89deg, #0000 90deg) 50% / 14px 100%;
          filter: drop-shadow(0 2px 2px rgba(43, 48, 45, 0.08));
        "
        aria-hidden="true"
      />
    </div>
  </div>
</template>
