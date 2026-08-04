<script setup>
import { ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { useRouter, useRoute } from 'vue-router'
import { toast } from 'vue-sonner'
import dayjs from 'dayjs'
import { User, Lock, Eye, EyeOff, AlertTriangle, ArrowRight, ShieldCheck } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import TemaToggle from '@/components/ui/TemaToggle.vue'

const esquema = toTypedSchema(
  z.object({
    usuarioLogin: z.string().min(1, 'Ingresa tu usuario.'),
    password: z.string().min(1, 'Ingresa tu contraseña.'),
  }),
)

const { handleSubmit, defineField, errors, isSubmitting } = useForm({
  validationSchema: esquema,
})

const [usuarioLogin, usuarioLoginAttrs] = defineField('usuarioLogin')
const [password, passwordAttrs] = defineField('password')

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const mostrarPassword = ref(false)
const errorSubmit = ref(null)
const passwordInvalida = ref(false)

// El tenant se resuelve por subdominio (DESIGN.md §6); sin un endpoint aún
// que lo confirme contra la base, se deriva del propio host como referencia visual.
const nombreEmpresa = (() => {
  const partes = window.location.hostname.split('.')
  const esSubdominio = partes.length > 2 || (partes.length === 2 && partes[0] !== 'www')
  if (!esSubdominio) return 'SysCenterS'
  return partes[0].charAt(0).toUpperCase() + partes[0].slice(1)
})()

function alClicOlvidoPassword() {
  toast.info('Pide a tu administrador que te restablezca la contraseña desde Ajustes → Usuarios.')
}

const onSubmit = handleSubmit(async (valores) => {
  errorSubmit.value = null
  passwordInvalida.value = false
  try {
    await auth.iniciarSesion(valores.usuarioLogin, valores.password)
    router.push(route.query.redirect?.toString() || '/panel')
  } catch (error) {
    const status = error.response?.status
    const codigo = error.response?.data?.error
    if (status === 423) {
      const hasta = error.response.data?.bloqueadoHasta
      errorSubmit.value = hasta
        ? `Cuenta bloqueada temporalmente por demasiados intentos. Intenta de nuevo después de las ${dayjs(hasta).format('HH:mm')}.`
        : 'Cuenta bloqueada temporalmente por demasiados intentos.'
    } else if (status === 403 && codigo === 'empresa_suspendida') {
      errorSubmit.value = 'Esta empresa está suspendida. Contacta al administrador de la plataforma.'
    } else if (status === 401) {
      passwordInvalida.value = true
      errorSubmit.value = 'El usuario o la contraseña no son correctos.'
    } else {
      toast.error('No se pudo conectar con el servidor. Intenta de nuevo.')
    }
  }
})
</script>

<template>
  <div class="relative flex min-h-dvh items-center justify-center overflow-hidden bg-papel px-4">
    <div
      class="pointer-events-none absolute -left-16 -top-16 h-64 w-64 rounded-full bg-marca/5 blur-3xl"
      aria-hidden="true"
    />
    <div
      class="pointer-events-none absolute -bottom-16 -right-16 h-64 w-64 rounded-full bg-marca/5 blur-3xl"
      aria-hidden="true"
    />

    <TemaToggle class="absolute right-4 top-4" />

    <div class="relative w-full max-w-[420px]">
      <div class="relative overflow-visible rounded border-x border-t border-linea bg-superficie shadow">
        <div class="flex flex-col items-center gap-3 px-8 pb-6 pt-10 text-center">
          <div
            class="flex h-14 w-14 rotate-3 items-center justify-center rounded bg-marca font-display text-2xl font-bold text-sobre-marca shadow transition-transform duration-500 hover:rotate-0"
          >
            S
          </div>
          <div>
            <p class="font-display text-[19.2px] font-bold text-tinta">{{ nombreEmpresa }}</p>
            <p class="mt-1 text-[11px] font-bold uppercase tracking-widest text-tinta-3">
              Inicia sesión para continuar
            </p>
          </div>
        </div>

        <form class="flex flex-col gap-4 px-8 pb-8" novalidate @submit.prevent="onSubmit">
          <div
            v-if="errorSubmit"
            class="flex items-start gap-2 rounded-s bg-peligro-tenue px-3 py-2.5 text-[13px] leading-snug text-peligro"
          >
            <AlertTriangle class="mt-0.5 h-4 w-4 shrink-0" />
            <span>{{ errorSubmit }}</span>
          </div>

          <label class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Usuario</span>
            <div class="relative">
              <User class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-tinta-3" />
              <input
                v-model="usuarioLogin"
                v-bind="usuarioLoginAttrs"
                type="text"
                autocomplete="username"
                placeholder="Tu nombre de usuario"
                class="min-h-11 w-full rounded-s border-[1.5px] bg-superficie-2 pl-10 pr-3 text-tinta outline-none transition-colors focus:border-marca focus:bg-superficie"
                :class="errors.usuarioLogin ? 'border-peligro' : 'border-linea'"
              />
            </div>
            <span v-if="errors.usuarioLogin" class="text-[12px] text-peligro">{{ errors.usuarioLogin }}</span>
          </label>

          <label class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Contraseña</span>
            <div class="relative">
              <Lock class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-tinta-3" />
              <input
                v-model="password"
                v-bind="passwordAttrs"
                :type="mostrarPassword ? 'text' : 'password'"
                autocomplete="current-password"
                class="min-h-11 w-full rounded-s border-[1.5px] bg-superficie-2 pl-10 pr-10 text-tinta outline-none transition-colors focus:border-marca focus:bg-superficie"
                :class="errors.password || passwordInvalida ? 'border-peligro' : 'border-linea'"
              />
              <button
                type="button"
                tabindex="-1"
                class="absolute right-3 top-1/2 -translate-y-1/2 text-tinta-3 transition-colors hover:text-marca"
                :aria-label="mostrarPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                @click="mostrarPassword = !mostrarPassword"
              >
                <EyeOff v-if="mostrarPassword" class="h-4 w-4" />
                <Eye v-else class="h-4 w-4" />
              </button>
            </div>
            <span v-if="errors.password" class="text-[12px] text-peligro">{{ errors.password }}</span>
          </label>

          <button
            type="submit"
            :disabled="isSubmitting"
            class="group flex min-h-11 items-center justify-center gap-2 rounded-s bg-marca font-bold text-sobre-marca transition-colors hover:bg-marca-hover disabled:opacity-60"
          >
            Acceder al sistema
            <ArrowRight class="h-4 w-4 transition-transform group-hover:translate-x-1" />
          </button>

          <button
            type="button"
            class="text-center text-[13.6px] text-marca underline decoration-marca/30 underline-offset-4 transition-colors hover:text-marca-hover"
            @click="alClicOlvidoPassword"
          >
            ¿Olvidaste tu contraseña?
          </button>
        </form>
      </div>

      <div
        class="h-3 bg-superficie"
        style="
          mask: conic-gradient(from -45deg at bottom, #0000, #000 1deg 89deg, #0000 90deg) 50% / 14px 100%;
          -webkit-mask: conic-gradient(from -45deg at bottom, #0000, #000 1deg 89deg, #0000 90deg) 50% / 14px 100%;
          filter: drop-shadow(0 2px 2px rgba(43, 48, 45, 0.08));
        "
        aria-hidden="true"
      />

      <p class="mt-8 flex items-center justify-center gap-2 text-[13px] text-tinta-3">
        <ShieldCheck class="h-4 w-4" />
        Conexión segura y cifrada
      </p>
    </div>
  </div>
</template>
