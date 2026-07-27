<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { toast } from 'vue-sonner'
import { Trash2 } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { Permisos } from '@/utils/permisos'
import { actualizarProducto, crearProducto, desactivarProducto, obtenerProductoPorId } from '@/api/productos'
import { listarCategorias } from '@/api/categorias'
import { listarMarcas } from '@/api/marcas'
import { listarUnidadesMedida } from '@/api/unidadesMedida'
import { obtenerEmpresaActual } from '@/api/empresa'
import PresentacionesEditor from '@/components/productos/PresentacionesEditor.vue'
import ModalCambiarPrecio from '@/components/productos/ModalCambiarPrecio.vue'
import ModalDesactivar from '@/components/productos/ModalDesactivar.vue'

const props = defineProps({
  id: { type: String, default: null },
})

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const esNuevo = computed(() => !props.id)
const puedeGuardar = computed(() => auth.tienePermiso(Permisos.ProductosCrearEditar))
const puedeCambiarPrecio = computed(() => auth.tienePermiso(Permisos.ProductosCambiarPrecios))
const puedeEliminar = computed(() => auth.tienePermiso(Permisos.ProductosEliminar))

const cargando = ref(true)
const guardando = ref(false)
const errorGeneral = ref('')

const categorias = ref([])
const marcas = ref([])
const unidades = ref([])
const empresaActual = ref(null)
const productoOriginal = ref(null)

const presentaciones = ref([])
const tieneFichaFarmacia = ref(false)
const fichaFarmacia = ref({
  principioActivo: '',
  concentracion: '',
  formaFarmaceutica: '',
  laboratorio: '',
  registroSanitario: '',
  clasificacion: 'LIBRE',
  requiereCadenaFrio: false,
})

const mostrarCambiarPrecio = ref(false)
const mostrarDesactivar = ref(false)
const editorPresentaciones = ref(null)

const esquema = toTypedSchema(
  z.object({
    codigo: z.string().optional(),
    codigoBarras: z.string().optional(),
    nombre: z.string().min(1, 'Ingresa el nombre del producto.').max(200, 'Máximo 200 caracteres.'),
    categoriaId: z.string().optional(),
    marcaId: z.string().optional(),
    unidadBaseId: z.coerce.number().positive('Selecciona una unidad de medida.'),
    precioBase: esNuevo.value ? z.coerce.number().min(0, 'No puede ser negativo.') : z.coerce.number().optional(),
    stockMinimo: z.coerce.number().min(0, 'No puede ser negativo.'),
    manejaLote: z.boolean().default(false),
    esControlado: z.boolean().default(false),
    permiteDecimales: z.boolean().default(false),
  }),
)

const { handleSubmit, defineField, errors, setValues } = useForm({ validationSchema: esquema })

const [codigo, codigoAttrs] = defineField('codigo')
const [codigoBarras, codigoBarrasAttrs] = defineField('codigoBarras')
const [nombre, nombreAttrs] = defineField('nombre')
const [categoriaId, categoriaIdAttrs] = defineField('categoriaId')
const [marcaId, marcaIdAttrs] = defineField('marcaId')
const [unidadBaseId, unidadBaseIdAttrs] = defineField('unidadBaseId')
const [precioBase, precioBaseAttrs] = defineField('precioBase')
const [stockMinimo, stockMinimoAttrs] = defineField('stockMinimo')
const [manejaLote] = defineField('manejaLote')
const [esControlado] = defineField('esControlado')
const [permiteDecimales] = defineField('permiteDecimales')

function limpiarNumero(n) {
  return n === null || n === undefined || n === '' ? '' : n
}

async function cargarDatos() {
  cargando.value = true
  try {
    const [cats, mcas, unids, empresa] = await Promise.all([
      listarCategorias(),
      listarMarcas(),
      listarUnidadesMedida(),
      obtenerEmpresaActual(),
    ])
    categorias.value = cats
    marcas.value = mcas
    unidades.value = unids
    empresaActual.value = empresa

    if (!esNuevo.value) {
      const producto = await obtenerProductoPorId(props.id)
      productoOriginal.value = producto
      setValues({
        codigo: producto.codigo || '',
        codigoBarras: producto.codigoBarras || '',
        nombre: producto.nombre,
        categoriaId: producto.categoriaId || '',
        marcaId: producto.marcaId || '',
        unidadBaseId: producto.unidadBaseId,
        stockMinimo: producto.stockMinimo,
        manejaLote: producto.manejaLote,
        esControlado: producto.esControlado,
        permiteDecimales: producto.permiteDecimales,
      })
      presentaciones.value = producto.presentaciones.map((p) => ({
        nombre: p.nombre,
        factor: p.factor,
        precio: p.precio,
        codigoBarras: p.codigoBarras || '',
        precioMayorista: limpiarNumero(p.precioMayorista),
        cantidadMinMayorista: limpiarNumero(p.cantidadMinMayorista),
        esPredeterminada: p.esPredeterminada,
      }))
      if (producto.fichaFarmacia) {
        tieneFichaFarmacia.value = true
        fichaFarmacia.value = { ...producto.fichaFarmacia }
      }
    } else {
      // Alta rápida por código de barras: precarga desde producto_maestro (route query).
      const marcaSugerida = route.query.marca
        ? mcas.find((m) => m.nombre.toLowerCase() === String(route.query.marca).toLowerCase())
        : null
      setValues({
        codigoBarras: route.query.codigoBarras || '',
        nombre: route.query.nombre || '',
        marcaId: marcaSugerida?.id || '',
      })
    }
  } finally {
    cargando.value = false
  }
}

onMounted(cargarDatos)

function aComando(valores) {
  return {
    codigo: valores.codigo || null,
    codigoBarras: valores.codigoBarras || null,
    nombre: valores.nombre,
    categoriaId: valores.categoriaId || null,
    marcaId: valores.marcaId || null,
    unidadBaseId: valores.unidadBaseId,
    stockMinimo: valores.stockMinimo,
    manejaLote: valores.manejaLote,
    esControlado: valores.esControlado,
    permiteDecimales: valores.permiteDecimales,
    codigoProductoSin: null,
    codigoUnidadSin: null,
    presentaciones: presentaciones.value.map((p) => ({
      nombre: p.nombre,
      factor: Number(p.factor),
      codigoBarras: p.codigoBarras || null,
      precio: Number(p.precio),
      precioMayorista: p.precioMayorista === '' ? null : Number(p.precioMayorista),
      cantidadMinMayorista: p.cantidadMinMayorista === '' ? null : Number(p.cantidadMinMayorista),
      esPredeterminada: !!p.esPredeterminada,
    })),
    fichaFarmacia: tieneFichaFarmacia.value
      ? {
          principioActivo: fichaFarmacia.value.principioActivo || null,
          concentracion: fichaFarmacia.value.concentracion || null,
          formaFarmaceutica: fichaFarmacia.value.formaFarmaceutica || null,
          laboratorio: fichaFarmacia.value.laboratorio || null,
          registroSanitario: fichaFarmacia.value.registroSanitario || null,
          clasificacion: fichaFarmacia.value.clasificacion,
          requiereCadenaFrio: !!fichaFarmacia.value.requiereCadenaFrio,
        }
      : null,
  }
}

const onSubmit = handleSubmit(async (valores) => {
  errorGeneral.value = ''

  if (!editorPresentaciones.value?.validarTodo()) {
    errorGeneral.value = 'Revisa los datos de las presentaciones: hay campos inválidos.'
    return
  }

  guardando.value = true
  try {
    if (esNuevo.value) {
      const comando = { ...aComando(valores), precioBase: valores.precioBase }
      await crearProducto(comando)
      toast.success('Producto guardado')
    } else {
      await actualizarProducto(props.id, { id: props.id, ...aComando(valores) })
      toast.success('Producto guardado')
    }
    router.push({ name: 'productos' })
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorGeneral.value = mensajes?.join(' ') || 'No se pudo guardar el producto.'
  } finally {
    guardando.value = false
  }
})

async function confirmarDesactivar() {
  try {
    await desactivarProducto(props.id)
    toast.success('Producto desactivado')
    router.push({ name: 'productos' })
  } catch {
    toast.error('No se pudo desactivar el producto.')
  }
}

function fmtBs(n) {
  const [ent, dec] = Number(n).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto w-full max-w-[640px]">
      <h2 class="mb-6 font-display text-[24px] font-bold text-tinta">
        {{ esNuevo ? 'Nuevo producto' : 'Editar producto' }}
      </h2>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">
        Cargando…
      </div>

      <form v-else class="rounded border border-linea bg-superficie p-6" novalidate @submit.prevent="onSubmit">
        <div v-if="errorGeneral" class="mb-5 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
          {{ errorGeneral }}
        </div>

        <!-- Precio (solo lectura en edición: cambia por flujo separado con permiso propio) -->
        <div v-if="!esNuevo" class="mb-5 flex items-center justify-between rounded-s border border-linea bg-superficie-2 px-4 py-3">
          <div>
            <p class="text-[0.72rem] font-bold uppercase tracking-wide text-tinta-3">Precio base actual</p>
            <p class="font-display text-[19.2px] font-bold tabular-nums text-marca">{{ fmtBs(productoOriginal.precioBase) }}</p>
          </div>
          <button
            v-if="puedeCambiarPrecio"
            type="button"
            class="min-h-11 rounded-s border border-linea px-4 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie"
            @click="mostrarCambiarPrecio = true"
          >
            Cambiar precio
          </button>
        </div>

        <div class="flex flex-col gap-4">
          <label class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre del producto</span>
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
              <span class="text-[0.8rem] font-semibold text-tinta-2">Código interno (opcional)</span>
              <input
                v-model="codigo"
                v-bind="codigoAttrs"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
            </label>
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Código de barras</span>
              <input
                v-model="codigoBarras"
                v-bind="codigoBarrasAttrs"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
            </label>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Categoría (opcional)</span>
              <select
                v-model="categoriaId"
                v-bind="categoriaIdAttrs"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              >
                <option value="">Sin categoría</option>
                <option v-for="c in categorias" :key="c.id" :value="c.id">{{ c.nombre }}</option>
              </select>
            </label>
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Marca (opcional)</span>
              <select
                v-model="marcaId"
                v-bind="marcaIdAttrs"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              >
                <option value="">Sin marca</option>
                <option v-for="m in marcas" :key="m.id" :value="m.id">{{ m.nombre }}</option>
              </select>
            </label>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Unidad de medida base</span>
              <select
                v-model="unidadBaseId"
                v-bind="unidadBaseIdAttrs"
                class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
                :class="errors.unidadBaseId ? 'border-peligro' : 'border-linea'"
              >
                <option value="">Selecciona…</option>
                <option v-for="u in unidades" :key="u.id" :value="u.id">{{ u.nombre }} ({{ u.abreviatura }})</option>
              </select>
              <span v-if="errors.unidadBaseId" class="text-[12px] text-peligro">{{ errors.unidadBaseId }}</span>
            </label>

            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Stock mínimo</span>
              <input
                v-model="stockMinimo"
                v-bind="stockMinimoAttrs"
                type="number"
                step="0.001"
                class="min-h-11 rounded-s border-[1.5px] bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
                :class="errors.stockMinimo ? 'border-peligro' : 'border-linea'"
              />
              <span v-if="errors.stockMinimo" class="text-[12px] text-peligro">{{ errors.stockMinimo }}</span>
            </label>
          </div>

          <label v-if="esNuevo" class="flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Precio base (Bs)</span>
            <input
              v-model="precioBase"
              v-bind="precioBaseAttrs"
              type="number"
              step="0.01"
              class="min-h-11 w-full rounded-s border-[1.5px] bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie sm:w-[calc(50%-8px)]"
              :class="errors.precioBase ? 'border-peligro' : 'border-linea'"
            />
            <span v-if="errors.precioBase" class="text-[12px] text-peligro">{{ errors.precioBase }}</span>
          </label>

          <!-- Checkboxes-tarjeta -->
          <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <label
              class="flex cursor-pointer items-start gap-3 rounded-s border-[1.5px] p-4 transition-colors"
              :class="manejaLote ? 'border-marca bg-marca-tenue' : 'border-linea bg-superficie-2'"
            >
              <input v-model="manejaLote" type="checkbox" class="mt-0.5 h-4 w-4 accent-marca" />
              <span>
                <span class="block text-[13.6px] font-semibold text-tinta">Maneja lote</span>
                <span class="block text-[12px] text-tinta-2">Vencimientos y FEFO al vender.</span>
              </span>
            </label>

            <label
              class="flex cursor-pointer items-start gap-3 rounded-s border-[1.5px] p-4 transition-colors"
              :class="esControlado ? 'border-marca bg-marca-tenue' : 'border-linea bg-superficie-2'"
            >
              <input v-model="esControlado" type="checkbox" class="mt-0.5 h-4 w-4 accent-marca" />
              <span>
                <span class="block text-[13.6px] font-semibold text-tinta">Es controlado</span>
                <span class="block text-[12px] text-tinta-2">Exige receta antes de cobrar.</span>
              </span>
            </label>

            <label
              class="flex cursor-pointer items-start gap-3 rounded-s border-[1.5px] p-4 transition-colors"
              :class="permiteDecimales ? 'border-marca bg-marca-tenue' : 'border-linea bg-superficie-2'"
            >
              <input v-model="permiteDecimales" type="checkbox" class="mt-0.5 h-4 w-4 accent-marca" />
              <span>
                <span class="block text-[13.6px] font-semibold text-tinta">Permite decimales</span>
                <span class="block text-[12px] text-tinta-2">Se vende a granel (kg, litros).</span>
              </span>
            </label>

            <label
              v-if="empresaActual?.usaFichaFarmacia"
              class="flex cursor-pointer items-start gap-3 rounded-s border-[1.5px] p-4 transition-colors"
              :class="tieneFichaFarmacia ? 'border-marca bg-marca-tenue' : 'border-linea bg-superficie-2'"
            >
              <input v-model="tieneFichaFarmacia" type="checkbox" class="mt-0.5 h-4 w-4 accent-marca" />
              <span>
                <span class="block text-[13.6px] font-semibold text-tinta">Ficha farmacéutica</span>
                <span class="block text-[12px] text-tinta-2">Principio activo, laboratorio, clasificación.</span>
              </span>
            </label>
          </div>

          <!-- Ficha farmacéutica (solo si el rubro la usa y está activada) -->
          <div v-if="empresaActual?.usaFichaFarmacia && tieneFichaFarmacia" class="rounded-s border border-linea bg-superficie-2 p-4">
            <p class="mb-3 text-[0.72rem] font-bold uppercase tracking-wide text-tinta-3">Ficha farmacéutica</p>
            <div class="grid grid-cols-2 gap-3">
              <label class="flex flex-col gap-1">
                <span class="text-[0.72rem] font-semibold text-tinta-2">Principio activo</span>
                <input v-model="fichaFarmacia.principioActivo" type="text" class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca" />
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.72rem] font-semibold text-tinta-2">Concentración</span>
                <input v-model="fichaFarmacia.concentracion" type="text" class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca" />
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.72rem] font-semibold text-tinta-2">Forma farmacéutica</span>
                <input v-model="fichaFarmacia.formaFarmaceutica" type="text" class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca" />
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.72rem] font-semibold text-tinta-2">Laboratorio</span>
                <input v-model="fichaFarmacia.laboratorio" type="text" class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca" />
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.72rem] font-semibold text-tinta-2">Registro sanitario</span>
                <input v-model="fichaFarmacia.registroSanitario" type="text" class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca" />
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.72rem] font-semibold text-tinta-2">Clasificación</span>
                <select v-model="fichaFarmacia.clasificacion" class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] text-tinta outline-none focus:border-marca">
                  <option value="LIBRE">Libre</option>
                  <option value="RECETA">Con receta</option>
                  <option value="PSICOTROPICO">Psicotrópico</option>
                  <option value="ESTUPEFACIENTE">Estupefaciente</option>
                </select>
              </label>
              <label class="col-span-2 flex items-center gap-2 pt-1">
                <input v-model="fichaFarmacia.requiereCadenaFrio" type="checkbox" class="h-4 w-4 accent-marca" />
                <span class="text-[13px] text-tinta-2">Requiere cadena de frío</span>
              </label>
            </div>
          </div>

          <!-- Presentaciones -->
          <div>
            <p class="mb-3 text-[0.72rem] font-bold uppercase tracking-wide text-tinta-3">Presentaciones</p>
            <PresentacionesEditor ref="editorPresentaciones" v-model="presentaciones" />
          </div>
        </div>

        <div class="mt-6 flex items-center justify-between border-t border-linea pt-5">
          <button
            v-if="!esNuevo && puedeEliminar"
            type="button"
            class="flex items-center gap-2 text-[13.6px] font-semibold text-peligro hover:underline"
            @click="mostrarDesactivar = true"
          >
            <Trash2 class="h-4 w-4" />
            Desactivar producto
          </button>
          <span v-else></span>

          <div class="flex gap-3">
            <button
              type="button"
              class="min-h-11 rounded-s border border-linea px-5 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie-2"
              @click="router.push({ name: 'productos' })"
            >
              Cancelar
            </button>
            <button
              v-if="puedeGuardar"
              type="submit"
              :disabled="guardando"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
            >
              {{ guardando ? 'Guardando…' : 'Guardar producto' }}
            </button>
          </div>
        </div>
      </form>
    </div>
  </div>

  <ModalCambiarPrecio
    v-if="productoOriginal"
    v-model="mostrarCambiarPrecio"
    :producto="productoOriginal"
    @cambiado="cargarDatos"
  />
  <ModalDesactivar
    v-if="productoOriginal"
    v-model="mostrarDesactivar"
    :nombre="productoOriginal.nombre"
    @confirmar="confirmarDesactivar"
  />
</template>
