<script setup>
import { reactive } from 'vue'
import { z } from 'zod'
import { Plus, Trash2 } from 'lucide-vue-next'

const filas = defineModel({ type: Array, default: () => [] })

const esquemaFila = z.object({
  nombre: z.string().min(1, 'Ponle un nombre a la presentación.'),
  factor: z.coerce.number().positive('El factor debe ser mayor a 0.'),
  precio: z.coerce.number().min(0, 'El precio no puede ser negativo.'),
  codigoBarras: z.string().optional(),
  precioMayorista: z.union([z.coerce.number().min(0), z.literal('')]).optional(),
  cantidadMinMayorista: z.union([z.coerce.number().min(0), z.literal('')]).optional(),
})

const errores = reactive({})

function agregar() {
  filas.value = [
    ...filas.value,
    { nombre: '', factor: 1, precio: 0, codigoBarras: '', precioMayorista: '', cantidadMinMayorista: '', esPredeterminada: filas.value.length === 0 },
  ]
}

function quitar(indice) {
  filas.value = filas.value.filter((_, i) => i !== indice)
  delete errores[indice]
}

function validarFila(indice) {
  const resultado = esquemaFila.safeParse(filas.value[indice])
  errores[indice] = resultado.success ? {} : resultado.error.flatten().fieldErrors
}

function validarTodo() {
  let ok = true
  filas.value.forEach((_, i) => {
    validarFila(i)
    if (errores[i] && Object.keys(errores[i]).length > 0) ok = false
  })
  return ok
}

defineExpose({ validarTodo })
</script>

<template>
  <div class="flex flex-col gap-3">
    <div v-if="filas.length === 0" class="rounded-s border border-dashed border-linea px-4 py-6 text-center text-[13px] text-tinta-3">
      Sin presentaciones adicionales: se vende solo por unidad.
    </div>

    <div
      v-for="(fila, i) in filas"
      :key="i"
      class="rounded-s border border-linea bg-superficie-2 p-4"
    >
      <div class="mb-3 flex items-center justify-between">
        <span class="text-[0.8rem] font-semibold text-tinta-2">Presentación {{ i + 1 }}</span>
        <button type="button" class="text-tinta-3 hover:text-peligro" aria-label="Quitar presentación" @click="quitar(i)">
          <Trash2 class="h-4 w-4" />
        </button>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <label class="col-span-2 flex flex-col gap-1 sm:col-span-1">
          <span class="text-[0.72rem] font-semibold uppercase tracking-wide text-tinta-3">Nombre</span>
          <input
            v-model="fila.nombre"
            type="text"
            placeholder="Caja x10"
            class="min-h-10 rounded-s border-[1.5px] bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca"
            :class="errores[i]?.nombre ? 'border-peligro' : 'border-linea'"
            @blur="validarFila(i)"
          />
          <span v-if="errores[i]?.nombre" class="text-[11px] text-peligro">{{ errores[i].nombre[0] }}</span>
        </label>

        <label class="flex flex-col gap-1">
          <span class="text-[0.72rem] font-semibold uppercase tracking-wide text-tinta-3">Factor (× unidad base)</span>
          <input
            v-model="fila.factor"
            type="number"
            step="0.0001"
            class="min-h-10 rounded-s border-[1.5px] bg-superficie px-3 tabular-nums text-[13.6px] text-tinta outline-none focus:border-marca"
            :class="errores[i]?.factor ? 'border-peligro' : 'border-linea'"
            @blur="validarFila(i)"
          />
          <span v-if="errores[i]?.factor" class="text-[11px] text-peligro">{{ errores[i].factor[0] }}</span>
        </label>

        <label class="flex flex-col gap-1">
          <span class="text-[0.72rem] font-semibold uppercase tracking-wide text-tinta-3">Precio (Bs)</span>
          <input
            v-model="fila.precio"
            type="number"
            step="0.01"
            class="min-h-10 rounded-s border-[1.5px] bg-superficie px-3 tabular-nums text-[13.6px] text-tinta outline-none focus:border-marca"
            :class="errores[i]?.precio ? 'border-peligro' : 'border-linea'"
            @blur="validarFila(i)"
          />
          <span v-if="errores[i]?.precio" class="text-[11px] text-peligro">{{ errores[i].precio[0] }}</span>
        </label>

        <label class="flex flex-col gap-1">
          <span class="text-[0.72rem] font-semibold uppercase tracking-wide text-tinta-3">Código de barras</span>
          <input
            v-model="fila.codigoBarras"
            type="text"
            class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca"
          />
        </label>

        <label class="flex flex-col gap-1">
          <span class="text-[0.72rem] font-semibold uppercase tracking-wide text-tinta-3">Precio mayorista (opcional)</span>
          <input
            v-model="fila.precioMayorista"
            type="number"
            step="0.01"
            class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 tabular-nums text-[13.6px] text-tinta outline-none focus:border-marca"
          />
        </label>

        <label class="flex flex-col gap-1">
          <span class="text-[0.72rem] font-semibold uppercase tracking-wide text-tinta-3">Cant. mín. mayorista</span>
          <input
            v-model="fila.cantidadMinMayorista"
            type="number"
            step="0.01"
            class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 tabular-nums text-[13.6px] text-tinta outline-none focus:border-marca"
          />
        </label>

        <label class="col-span-2 flex items-center gap-2 pt-1">
          <input v-model="fila.esPredeterminada" type="checkbox" class="h-4 w-4 accent-marca" />
          <span class="text-[13px] text-tinta-2">Presentación predeterminada en el POS</span>
        </label>
      </div>
    </div>

    <button
      type="button"
      class="flex min-h-10 items-center justify-center gap-2 rounded-s border border-dashed border-linea text-[13.6px] font-semibold text-tinta-2 hover:border-marca hover:text-marca"
      @click="agregar"
    >
      <Plus class="h-4 w-4" />
      Agregar presentación
    </button>
  </div>
</template>
