<script setup>
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import dayjs from 'dayjs'
import ModalBase from '@/components/ui/ModalBase.vue'

const abierto = defineModel({ type: Boolean, default: false })
const emit = defineEmits(['confirmar'])

const esquema = toTypedSchema(
  z.object({
    medicoNombre: z.string().min(1, 'Ingresa el nombre del médico.'),
    medicoMatricula: z.string().min(1, 'Ingresa la matrícula del médico.'),
    pacienteNombre: z.string().min(1, 'Ingresa el nombre del paciente.'),
    pacienteCi: z.string().optional(),
    fechaReceta: z.string().min(1, 'Ingresa la fecha de la receta.'),
  }),
)

const { handleSubmit, defineField, errors, resetForm } = useForm({
  validationSchema: esquema,
  initialValues: { fechaReceta: dayjs().format('YYYY-MM-DD') },
})

const [medicoNombre, medicoNombreAttrs] = defineField('medicoNombre')
const [medicoMatricula, medicoMatriculaAttrs] = defineField('medicoMatricula')
const [pacienteNombre, pacienteNombreAttrs] = defineField('pacienteNombre')
const [pacienteCi, pacienteCiAttrs] = defineField('pacienteCi')
const [fechaReceta, fechaRecetaAttrs] = defineField('fechaReceta')

const onSubmit = handleSubmit((valores) => {
  emit('confirmar', {
    medicoNombre: valores.medicoNombre,
    medicoMatricula: valores.medicoMatricula,
    pacienteNombre: valores.pacienteNombre,
    pacienteCi: valores.pacienteCi || null,
    fechaReceta: valores.fechaReceta,
    imagenUrl: null,
  })
  resetForm({ values: { fechaReceta: dayjs().format('YYYY-MM-DD') } })
  abierto.value = false
})
</script>

<template>
  <ModalBase v-model="abierto" titulo="Receta médica requerida">
    <p class="mb-4 text-[13.6px] text-tinta-2">
      Esta venta incluye un producto controlado: registra la receta antes de cobrar.
    </p>

    <form class="flex flex-col gap-4" novalidate @submit.prevent="onSubmit">
      <label class="flex flex-col gap-1.5">
        <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre del médico</span>
        <input
          v-model="medicoNombre"
          v-bind="medicoNombreAttrs"
          type="text"
          class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
          :class="errors.medicoNombre ? 'border-peligro' : 'border-linea'"
        />
        <span v-if="errors.medicoNombre" class="text-[12px] text-peligro">{{ errors.medicoNombre }}</span>
      </label>

      <label class="flex flex-col gap-1.5">
        <span class="text-[0.8rem] font-semibold text-tinta-2">Matrícula profesional</span>
        <input
          v-model="medicoMatricula"
          v-bind="medicoMatriculaAttrs"
          type="text"
          class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
          :class="errors.medicoMatricula ? 'border-peligro' : 'border-linea'"
        />
        <span v-if="errors.medicoMatricula" class="text-[12px] text-peligro">{{ errors.medicoMatricula }}</span>
      </label>

      <div class="grid grid-cols-2 gap-4">
        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre del paciente</span>
          <input
            v-model="pacienteNombre"
            v-bind="pacienteNombreAttrs"
            type="text"
            class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
            :class="errors.pacienteNombre ? 'border-peligro' : 'border-linea'"
          />
          <span v-if="errors.pacienteNombre" class="text-[12px] text-peligro">{{ errors.pacienteNombre }}</span>
        </label>

        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">CI (opcional)</span>
          <input
            v-model="pacienteCi"
            v-bind="pacienteCiAttrs"
            type="text"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
          />
        </label>
      </div>

      <label class="flex flex-col gap-1.5">
        <span class="text-[0.8rem] font-semibold text-tinta-2">Fecha de la receta</span>
        <input
          v-model="fechaReceta"
          v-bind="fechaRecetaAttrs"
          type="date"
          class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
          :class="errors.fechaReceta ? 'border-peligro' : 'border-linea'"
        />
        <span v-if="errors.fechaReceta" class="text-[12px] text-peligro">{{ errors.fechaReceta }}</span>
      </label>

      <button
        type="submit"
        class="min-h-11 rounded-s bg-marca font-bold text-sobre-marca transition-colors hover:bg-marca-hover"
      >
        Guardar receta y continuar al cobro
      </button>
    </form>
  </ModalBase>
</template>
