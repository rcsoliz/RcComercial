<script setup>
import { watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { toast } from 'vue-sonner'
import ModalBase from '@/components/ui/ModalBase.vue'
import { crearVehiculo, editarVehiculo } from '@/api/vehiculos'

const props = defineProps({
  clienteId: { type: String, required: true },
  vehiculo: { type: Object, default: null }, // null = crear, objeto = editar
})
const abierto = defineModel({ type: Boolean, default: false })
const emit = defineEmits(['guardado'])

const esquema = toTypedSchema(
  z.object({
    placa: z.string().min(1, 'Ingresa la placa.').max(20, 'Máximo 20 caracteres.'),
    marca: z.string().optional(),
    modelo: z.string().optional(),
    anio: z.coerce.number().optional(),
    color: z.string().optional(),
  }),
)

const { handleSubmit, defineField, errors, resetForm, isSubmitting } = useForm({ validationSchema: esquema })

const [placa, placaAttrs] = defineField('placa')
const [marca] = defineField('marca')
const [modelo] = defineField('modelo')
const [anio] = defineField('anio')
const [color] = defineField('color')

watch(
  () => [props.vehiculo, abierto.value],
  () => {
    if (!abierto.value) return
    resetForm({
      values: {
        placa: props.vehiculo?.placa ?? '',
        marca: props.vehiculo?.marca ?? '',
        modelo: props.vehiculo?.modelo ?? '',
        anio: props.vehiculo?.anio ?? undefined,
        color: props.vehiculo?.color ?? '',
      },
    })
  },
  { immediate: true },
)

const onSubmit = handleSubmit(async (valores) => {
  const comando = {
    clienteId: props.clienteId,
    placa: valores.placa.trim(),
    marca: valores.marca || null,
    modelo: valores.modelo || null,
    anio: valores.anio || null,
    color: valores.color || null,
    numeroChasis: null,
  }
  try {
    if (props.vehiculo) await editarVehiculo(props.vehiculo.id, { id: props.vehiculo.id, ...comando })
    else await crearVehiculo(comando)
    toast.success('Vehículo guardado.')
    abierto.value = false
    emit('guardado')
  } catch {
    toast.error('No se pudo guardar el vehículo.')
  }
})
</script>

<template>
  <ModalBase v-model="abierto" :titulo="vehiculo ? 'Editar vehículo' : 'Nuevo vehículo'">
    <form class="flex flex-col gap-4" novalidate @submit.prevent="onSubmit">
      <label class="flex flex-col gap-1.5">
        <span class="text-[0.8rem] font-semibold text-tinta-2">Placa</span>
        <input
          v-model="placa"
          v-bind="placaAttrs"
          type="text"
          class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
          :class="errors.placa ? 'border-peligro' : 'border-linea'"
        />
        <span v-if="errors.placa" class="text-[12px] text-peligro">{{ errors.placa }}</span>
      </label>

      <div class="grid grid-cols-2 gap-4">
        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Marca</span>
          <input
            v-model="marca"
            type="text"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
          />
        </label>
        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Modelo</span>
          <input
            v-model="modelo"
            type="text"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
          />
        </label>
      </div>

      <div class="grid grid-cols-2 gap-4">
        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Año</span>
          <input
            v-model="anio"
            type="number"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
          />
        </label>
        <label class="flex flex-col gap-1.5">
          <span class="text-[0.8rem] font-semibold text-tinta-2">Color</span>
          <input
            v-model="color"
            type="text"
            class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
          />
        </label>
      </div>

      <button
        type="submit"
        :disabled="isSubmitting"
        class="min-h-11 rounded-s bg-marca font-bold text-sobre-marca transition-colors hover:bg-marca-hover disabled:opacity-60"
      >
        Guardar vehículo
      </button>
    </form>
  </ModalBase>
</template>
