<script setup>
import { ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import { useAuthStore } from '@/stores/auth'
import TemaToggle from '@/components/ui/TemaToggle.vue'

const esquema = toTypedSchema(
  z
    .object({
      passwordActual: z.string().min(1, 'Ingresa tu contraseña actual.'),
      passwordNueva: z.string().min(8, 'Al menos 8 caracteres.'),
      confirmacion: z.string().min(1, 'Repite la contraseña nueva.'),
    })
    .refine((v) => v.passwordNueva === v.confirmacion, {
      message: 'Las contraseñas no coinciden.',
      path: ['confirmacion'],
    }),
)

const { handleSubmit, defineField, errors, setFieldError, isSubmitting } = useForm({ validationSchema: esquema })

const [passwordActual, passwordActualAttrs] = defineField('passwordActual')
const [passwordNueva, passwordNuevaAttrs] = defineField('passwordNueva')
const [confirmacion, confirmacionAttrs] = defineField('confirmacion')

const auth = useAuthStore()
const router = useRouter()
const errorGeneral = ref('')

const onSubmit = handleSubmit(async (valores) => {
  errorGeneral.value = ''
  try {
    await auth.cambiarPasswordObligatorio(valores.passwordActual, valores.passwordNueva)
    toast.success('Contraseña actualizada')
    router.push('/pos')
  } catch (error) {
    const status = error.response?.status
    if (status === 401) {
      setFieldError('passwordActual', 'La contraseña actual no es correcta.')
    } else if (status === 400) {
      errorGeneral.value = error.response.data?.error || 'No se pudo cambiar la contraseña.'
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
          <p class="font-display text-[19.2px] font-bold text-tinta">Cambia tu contraseña</p>
          <p class="mt-1 max-w-[280px] text-[13.6px] text-tinta-2">
            Es tu primer ingreso (o te la restablecieron): antes de continuar, elige una contraseña propia.
          </p>
        </div>
      </div>

      <form class="flex flex-col gap-4 bg-superficie px-8 pb-8" novalidate @submit.prevent="onSubmit">
        <div v-if="errorGeneral" class="rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
          {{ errorGeneral }}
        </div>

        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Contraseña actual (temporal)</span>
          <input
            v-model="passwordActual"
            v-bind="passwordActualAttrs"
            type="password"
            autocomplete="current-password"
            class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
            :class="errors.passwordActual ? 'border-peligro' : 'border-linea'"
          />
          <span v-if="errors.passwordActual" class="text-[12px] text-peligro">{{ errors.passwordActual }}</span>
        </label>

        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Contraseña nueva</span>
          <input
            v-model="passwordNueva"
            v-bind="passwordNuevaAttrs"
            type="password"
            autocomplete="new-password"
            class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
            :class="errors.passwordNueva ? 'border-peligro' : 'border-linea'"
          />
          <span v-if="errors.passwordNueva" class="text-[12px] text-peligro">{{ errors.passwordNueva }}</span>
        </label>

        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Repite la contraseña nueva</span>
          <input
            v-model="confirmacion"
            v-bind="confirmacionAttrs"
            type="password"
            autocomplete="new-password"
            class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
            :class="errors.confirmacion ? 'border-peligro' : 'border-linea'"
          />
          <span v-if="errors.confirmacion" class="text-[12px] text-peligro">{{ errors.confirmacion }}</span>
        </label>

        <button
          type="submit"
          :disabled="isSubmitting"
          class="min-h-11 rounded-s bg-marca font-bold text-sobre-marca transition-colors hover:bg-marca-hover disabled:opacity-60"
        >
          Cambiar contraseña
        </button>
      </form>
    </div>
  </div>
</template>
