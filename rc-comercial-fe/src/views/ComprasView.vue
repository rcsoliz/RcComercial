<script setup>
import { computed, onMounted, ref } from 'vue'
import { watchDebounced } from '@vueuse/core'
import { toast } from 'vue-sonner'
import dayjs from 'dayjs'
import { Plus, Send, Trash2 } from 'lucide-vue-next'
import { crearCompra, enviarPedidoProveedor, listarCompras, obtenerSugeridoCompra } from '@/api/compras'
import { crearProveedor, listarProveedores } from '@/api/proveedores'
import { buscarProductos, obtenerProductoPorId } from '@/api/productos'

function fmtBs(n) {
  const [ent, dec] = Number(n || 0).toFixed(2).split('.')
  return 'Bs ' + ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.') + ',' + dec
}

// ── Listado ──
const vista = ref('listado') // 'listado' | 'nueva'
const compras = ref([])
const pagina = ref(1)
const cargandoListado = ref(true)

async function cargarListado() {
  cargandoListado.value = true
  try {
    compras.value = await listarCompras(pagina.value)
  } finally {
    cargandoListado.value = false
  }
}

async function irAPagina(delta) {
  pagina.value += delta
  await cargarListado()
}

// ── Proveedores ──
const proveedores = ref([])
const proveedorId = ref('')
const nroFacturaProv = ref('')

const mostrarNuevoProveedor = ref(false)
const nuevoProveedor = ref({ nombre: '', telefonoWhatsapp: '', leadTimeDias: 3 })
const guardandoProveedor = ref(false)

async function cargarProveedores() {
  proveedores.value = await listarProveedores()
}

async function guardarProveedorRapido() {
  if (!nuevoProveedor.value.nombre.trim()) {
    toast.error('Ponle un nombre al proveedor.')
    return
  }
  guardandoProveedor.value = true
  try {
    const creado = await crearProveedor({
      nombre: nuevoProveedor.value.nombre,
      nit: null,
      telefonoWhatsapp: nuevoProveedor.value.telefonoWhatsapp || null,
      diasCredito: 0,
      leadTimeDias: Number(nuevoProveedor.value.leadTimeDias) || 0,
    })
    proveedores.value.push(creado)
    proveedorId.value = creado.id
    mostrarNuevoProveedor.value = false
    nuevoProveedor.value = { nombre: '', telefonoWhatsapp: '', leadTimeDias: 3 }
    toast.success('Proveedor guardado')
  } catch {
    toast.error('No se pudo guardar el proveedor.')
  } finally {
    guardandoProveedor.value = false
  }
}

// ── Líneas de la compra en curso ──
const lineas = ref([])
const textoBuscarProducto = ref('')
const resultadosBusqueda = ref([])

watchDebounced(
  textoBuscarProducto,
  async () => {
    resultadosBusqueda.value = textoBuscarProducto.value.trim()
      ? await buscarProductos(textoBuscarProducto.value)
      : []
  },
  { debounce: 300 },
)

async function agregarLinea(item) {
  const producto = await obtenerProductoPorId(item.id)
  lineas.value.push({
    productoId: producto.id,
    nombre: producto.nombre,
    presentacionId: null,
    presentaciones: producto.presentaciones,
    factor: 1,
    cantidad: 1,
    costoUnitario: producto.precioBase ? Number((producto.precioBase * 0.7).toFixed(2)) : 0,
    manejaLote: producto.manejaLote,
    numeroLote: '',
    fechaVencimiento: '',
    stockActual: item.stockTotal,
  })
  textoBuscarProducto.value = ''
  resultadosBusqueda.value = []
}

function alCambiarPresentacion(linea) {
  const p = linea.presentaciones.find((x) => x.id === linea.presentacionId)
  linea.factor = p ? p.factor : 1
}

function quitarLinea(index) {
  lineas.value.splice(index, 1)
}

const totalCompra = computed(() =>
  lineas.value.reduce((acc, l) => acc + (Number(l.cantidad) || 0) * (Number(l.costoUnitario) || 0), 0),
)

// ── Sugerido de compra ──
const sugerido = ref([])
const cargandoSugerido = ref(false)

async function cargarSugerido() {
  if (!proveedorId.value) {
    toast.error('Elige un proveedor primero.')
    return
  }
  cargandoSugerido.value = true
  try {
    sugerido.value = await obtenerSugeridoCompra(proveedorId.value)
    if (sugerido.value.length === 0) toast.info('No hay sugerencias de compra para este proveedor por ahora.')
  } catch {
    toast.error('No se pudo calcular el sugerido.')
  } finally {
    cargandoSugerido.value = false
  }
}

async function convertirSugeridoEnCompra() {
  // Se pide el detalle completo de cada producto (el sugerido no trae
  // presentaciones/manejaLote) para que, si maneja lote, los campos
  // obligatorios aparezcan de una vez — si no, el backend rechaza la compra
  // sin que la UI muestre dónde falta completar.
  for (const s of sugerido.value) {
    const producto = await obtenerProductoPorId(s.productoId)
    lineas.value.push({
      productoId: producto.id,
      nombre: producto.nombre,
      presentacionId: null,
      presentaciones: producto.presentaciones,
      factor: 1,
      cantidad: s.cantidadSugerida,
      costoUnitario: 0,
      manejaLote: producto.manejaLote,
      numeroLote: '',
      fechaVencimiento: '',
      stockActual: s.stockActual,
    })
  }
  sugerido.value = []
  toast.success('Sugerido agregado a la compra: ajusta costos y confirma.')
}

async function enviarPorWhatsapp() {
  if (sugerido.value.length === 0) return
  try {
    const ok = await enviarPedidoProveedor(
      proveedorId.value,
      sugerido.value.map((s) => ({ productoId: s.productoId, cantidad: s.cantidadSugerida })),
    )
    if (ok.status === 200) toast.success('Pedido enviado a la cola de WhatsApp del proveedor.')
    else toast.error('El proveedor no tiene WhatsApp configurado.')
  } catch {
    toast.error('No se pudo enviar el pedido.')
  }
}

// ── Confirmar compra ──
const enviandoCompra = ref(false)
const errorCompra = ref('')
const resultadoCompra = ref(null)

function lineasInvalidas() {
  if (!proveedorId.value) return 'Elige un proveedor.'
  if (lineas.value.length === 0) return 'Agrega al menos un producto.'
  for (const l of lineas.value) {
    if (!l.cantidad || l.cantidad <= 0) return `Cantidad inválida en "${l.nombre}".`
    if (l.costoUnitario === '' || l.costoUnitario === null || l.costoUnitario < 0) return `Costo inválido en "${l.nombre}".`
    if (l.manejaLote && (!l.numeroLote?.trim() || !l.fechaVencimiento)) {
      return `"${l.nombre}" maneja lote: el número de lote y la fecha de vencimiento son obligatorios.`
    }
  }
  return null
}

async function confirmarCompra() {
  errorCompra.value = ''
  const problema = lineasInvalidas()
  if (problema) {
    errorCompra.value = problema
    return
  }

  enviandoCompra.value = true
  try {
    const comando = {
      proveedorId: proveedorId.value,
      nroFacturaProv: nroFacturaProv.value || null,
      sucursalId: null,
      detalles: lineas.value.map((l) => ({
        productoId: l.productoId,
        presentacionId: l.presentacionId,
        cantidad: Number(l.cantidad),
        costoUnitario: Number(l.costoUnitario),
        numeroLote: l.manejaLote ? l.numeroLote : null,
        fechaVencimiento: l.manejaLote ? l.fechaVencimiento : null,
      })),
    }
    const compra = await crearCompra(comando)

    // Stock actualizado: stock previo (capturado al agregar la línea) + lo comprado.
    resultadoCompra.value = {
      ...compra,
      detalles: compra.detalles.map((d) => {
        const linea = lineas.value.find((l) => l.productoId === d.productoId)
        return { ...d, nombre: linea?.nombre ?? '?', stockNuevo: (linea?.stockActual ?? 0) + d.cantidadBase }
      }),
    }
    toast.success(`Compra ${compra.numero} registrada`)
    lineas.value = []
    proveedorId.value = ''
    nroFacturaProv.value = ''
    await cargarListado()
  } catch (e) {
    const mensajes = e.response?.data?.errores?.map((x) => x.mensaje)
    errorCompra.value = mensajes?.join(' ') || 'No se pudo registrar la compra.'
  } finally {
    enviandoCompra.value = false
  }
}

function nuevaCompra() {
  resultadoCompra.value = null
  vista.value = 'nueva'
  if (proveedores.value.length === 0) cargarProveedores()
}

onMounted(() => {
  cargarListado()
})
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto flex w-full max-w-[720px] flex-col gap-6">
      <div class="flex items-center justify-between">
        <h2 class="font-display text-[24px] font-bold text-tinta">Compras</h2>
        <button
          v-if="vista === 'listado'"
          type="button"
          class="flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
          @click="nuevaCompra"
        >
          <Plus class="h-5 w-5" />
          Nueva compra
        </button>
        <button
          v-else
          type="button"
          class="min-h-11 rounded-s border border-linea px-4 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie-2"
          @click="vista = 'listado'"
        >
          Volver al listado
        </button>
      </div>

      <!-- ══ LISTADO ══ -->
      <div v-if="vista === 'listado'" class="overflow-hidden rounded border border-linea bg-superficie">
        <div class="overflow-x-auto">
          <table class="w-full border-collapse text-left">
            <thead>
              <tr class="border-b border-linea bg-superficie-2">
                <th class="px-4 py-3 text-[10px] font-bold uppercase tracking-wide text-tinta-3">N.º</th>
                <th class="px-4 py-3 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Fecha</th>
                <th class="px-4 py-3 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Proveedor</th>
                <th class="px-4 py-3 text-right text-[10px] font-bold uppercase tracking-wide text-tinta-3">Total</th>
                <th class="px-4 py-3 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Estado</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!cargandoListado && compras.length === 0">
                <td colspan="5" class="px-4 py-12 text-center text-[13.6px] text-tinta-3">
                  Todavía no registraste compras. Crea la primera con "Nueva compra".
                </td>
              </tr>
              <tr v-for="c in compras" :key="c.id" class="border-b border-linea last:border-b-0 hover:bg-marca-tenue">
                <td class="px-4 py-3 align-middle font-mono text-[12px] text-tinta-3">{{ c.numero }}</td>
                <td class="px-4 py-3 align-middle text-[13px] tabular-nums text-tinta">{{ dayjs(c.fecha).format('DD/MM/YY') }}</td>
                <td class="px-4 py-3 align-middle text-[13.6px] text-tinta">{{ c.proveedorNombre }}</td>
                <td class="px-4 py-3 text-right align-middle font-semibold tabular-nums text-tinta">{{ fmtBs(c.total) }}</td>
                <td class="px-4 py-3 align-middle">
                  <span class="inline-block rounded-chip bg-exito-tenue px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide text-exito">
                    {{ c.estado.toLowerCase() }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="flex items-center justify-between border-t border-linea px-4 py-3">
          <p class="text-[12.8px] text-tinta-3">Página <span class="tabular-nums">{{ pagina }}</span></p>
          <div class="flex gap-2">
            <button
              type="button"
              :disabled="pagina === 1"
              class="flex min-h-11 min-w-11 items-center justify-center rounded-s border border-linea text-tinta-2 hover:bg-superficie-2 disabled:opacity-40"
              @click="irAPagina(-1)"
            >
              ‹
            </button>
            <button
              type="button"
              :disabled="compras.length < 20"
              class="flex min-h-11 min-w-11 items-center justify-center rounded-s border border-linea text-tinta-2 hover:bg-superficie-2 disabled:opacity-40"
              @click="irAPagina(1)"
            >
              ›
            </button>
          </div>
        </div>
      </div>

      <!-- ══ RESULTADO DE LA ÚLTIMA COMPRA: stock actualizado ══ -->
      <div v-if="vista === 'listado' && resultadoCompra" class="rounded border border-linea bg-superficie p-6">
        <p class="font-display text-[15px] font-semibold text-tinta">Compra {{ resultadoCompra.numero }} · stock actualizado</p>
        <div class="mt-3 flex flex-col gap-2">
          <div v-for="d in resultadoCompra.detalles" :key="d.id" class="flex justify-between text-[13.6px]">
            <span class="text-tinta-2">{{ d.nombre }}</span>
            <span class="font-semibold tabular-nums text-marca">{{ d.stockNuevo }} uds</span>
          </div>
        </div>
      </div>

      <!-- ══ NUEVA COMPRA ══ -->
      <div v-else-if="vista === 'nueva'" class="flex flex-col gap-6">
        <div class="rounded border border-linea bg-superficie p-6">
          <div v-if="errorCompra" class="mb-4 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">{{ errorCompra }}</div>

          <div class="grid grid-cols-2 gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Proveedor</span>
              <select
                v-model="proveedorId"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              >
                <option value="">Selecciona…</option>
                <option v-for="p in proveedores" :key="p.id" :value="p.id">{{ p.nombre }}</option>
              </select>
            </label>
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">N.º factura del proveedor (opcional)</span>
              <input
                v-model="nroFacturaProv"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
            </label>
          </div>

          <button
            type="button"
            class="mt-3 text-[12px] font-semibold text-marca hover:underline"
            @click="mostrarNuevoProveedor = !mostrarNuevoProveedor"
          >
            {{ mostrarNuevoProveedor ? 'Cancelar' : '+ Nuevo proveedor' }}
          </button>

          <div v-if="mostrarNuevoProveedor" class="mt-3 rounded-s border border-linea bg-superficie-2 p-4">
            <div class="grid grid-cols-2 gap-3">
              <label class="col-span-2 flex flex-col gap-1 sm:col-span-1">
                <span class="text-[0.72rem] font-semibold text-tinta-2">Nombre</span>
                <input v-model="nuevoProveedor.nombre" type="text" class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] outline-none focus:border-marca" />
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.72rem] font-semibold text-tinta-2">WhatsApp (opcional)</span>
                <input v-model="nuevoProveedor.telefonoWhatsapp" type="text" placeholder="59170000000" class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 text-[13.6px] outline-none focus:border-marca" />
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.72rem] font-semibold text-tinta-2">Días de entrega</span>
                <input v-model="nuevoProveedor.leadTimeDias" type="number" min="0" class="min-h-10 rounded-s border-[1.5px] border-linea bg-superficie px-3 tabular-nums text-[13.6px] outline-none focus:border-marca" />
              </label>
            </div>
            <button
              type="button"
              :disabled="guardandoProveedor"
              class="mt-3 min-h-10 rounded-s bg-marca px-4 text-[13px] font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
              @click="guardarProveedorRapido"
            >
              Guardar proveedor
            </button>
          </div>
        </div>

        <!-- Sugerido de compra -->
        <div v-if="proveedorId" class="rounded border border-linea bg-superficie p-6">
          <div class="flex items-center justify-between">
            <p class="font-display text-[15px] font-semibold text-tinta">Sugerido de compra</p>
            <button type="button" class="text-[12px] font-semibold text-marca hover:underline" @click="cargarSugerido">
              {{ cargandoSugerido ? 'Calculando…' : 'Calcular sugerido' }}
            </button>
          </div>

          <div v-if="sugerido.length > 0" class="mt-3 flex flex-col gap-2">
            <div v-for="s in sugerido" :key="s.productoId" class="flex items-center justify-between text-[13.6px]">
              <span class="text-tinta-2">{{ s.productoNombre }}</span>
              <label class="flex items-center gap-2">
                <input v-model.number="s.cantidadSugerida" type="number" min="0" class="w-20 rounded-s border border-linea bg-superficie-2 px-2 py-1 text-right tabular-nums" />
                <span class="text-[11px] text-tinta-3">uds</span>
              </label>
            </div>
            <div class="mt-3 flex gap-3">
              <button type="button" class="flex-1 min-h-11 rounded-s border border-linea font-semibold text-tinta-2 hover:bg-superficie-2" @click="convertirSugeridoEnCompra">
                Convertir en compra
              </button>
              <button type="button" class="flex min-h-11 items-center gap-2 rounded-s border border-linea px-4 text-[13px] font-semibold text-tinta-2 hover:bg-superficie-2" @click="enviarPorWhatsapp">
                <Send class="h-4 w-4" />
                Enviar por WhatsApp
              </button>
            </div>
          </div>
        </div>

        <!-- Líneas -->
        <div class="rounded border border-linea bg-superficie p-6">
          <p class="mb-3 font-display text-[15px] font-semibold text-tinta">Productos</p>

          <div v-for="(l, i) in lineas" :key="i" class="mb-3 rounded-s border border-linea bg-superficie-2 p-4">
            <div class="flex items-start justify-between gap-3">
              <p class="font-medium text-tinta">{{ l.nombre }}</p>
              <button type="button" class="text-tinta-3 hover:text-peligro" aria-label="Quitar producto" @click="quitarLinea(i)">
                <Trash2 class="h-4 w-4" />
              </button>
            </div>

            <div class="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-4">
              <label v-if="l.presentaciones.length > 0" class="col-span-2 flex flex-col gap-1 sm:col-span-1">
                <span class="text-[0.68rem] font-semibold uppercase text-tinta-3">Presentación</span>
                <select v-model="l.presentacionId" class="min-h-10 rounded-s border border-linea bg-superficie px-2 text-[13px]" @change="alCambiarPresentacion(l)">
                  <option :value="null">Unidad</option>
                  <option v-for="p in l.presentaciones" :key="p.id" :value="p.id">{{ p.nombre }}</option>
                </select>
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.68rem] font-semibold uppercase text-tinta-3">Cantidad</span>
                <input v-model="l.cantidad" type="number" min="0.001" step="0.001" class="min-h-10 rounded-s border border-linea bg-superficie px-2 tabular-nums text-[13px]" />
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.68rem] font-semibold uppercase text-tinta-3">Costo unit. (Bs)</span>
                <input v-model="l.costoUnitario" type="number" min="0" step="0.01" class="min-h-10 rounded-s border border-linea bg-superficie px-2 tabular-nums text-[13px]" />
              </label>
              <div class="flex flex-col gap-1">
                <span class="text-[0.68rem] font-semibold uppercase text-tinta-3">Subtotal</span>
                <span class="flex min-h-10 items-center font-semibold tabular-nums text-marca">
                  {{ fmtBs((Number(l.cantidad) || 0) * (Number(l.costoUnitario) || 0)) }}
                </span>
              </div>
            </div>

            <div v-if="l.manejaLote" class="mt-3 grid grid-cols-2 gap-3 rounded-s bg-aviso-tenue p-3">
              <label class="flex flex-col gap-1">
                <span class="text-[0.68rem] font-semibold uppercase text-aviso">Número de lote *</span>
                <input v-model="l.numeroLote" type="text" class="min-h-10 rounded-s border border-linea bg-superficie px-2 text-[13px]" />
              </label>
              <label class="flex flex-col gap-1">
                <span class="text-[0.68rem] font-semibold uppercase text-aviso">Vencimiento *</span>
                <input v-model="l.fechaVencimiento" type="date" class="min-h-10 rounded-s border border-linea bg-superficie px-2 text-[13px]" />
              </label>
            </div>
          </div>

          <div class="relative">
            <input
              v-model="textoBuscarProducto"
              type="text"
              placeholder="Buscar producto para agregar…"
              class="min-h-11 w-full rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
            />
            <div v-if="resultadosBusqueda.length > 0" class="absolute z-10 mt-1 w-full rounded-s border border-linea bg-superficie shadow">
              <button
                v-for="r in resultadosBusqueda"
                :key="r.id"
                type="button"
                class="flex w-full items-center justify-between px-4 py-2.5 text-left text-[13.6px] hover:bg-marca-tenue"
                @click="agregarLinea(r)"
              >
                <span>{{ r.nombre }}</span>
                <span class="text-tinta-3">{{ fmtBs(r.precioBase) }}</span>
              </button>
            </div>
          </div>

          <div class="mt-4 flex justify-between border-t border-dashed border-linea pt-4">
            <span class="font-display text-[15px] font-semibold text-tinta">Total</span>
            <span class="font-display text-[19.2px] font-bold tabular-nums text-marca">{{ fmtBs(totalCompra) }}</span>
          </div>

          <button
            type="button"
            :disabled="enviandoCompra"
            class="mt-4 min-h-11 w-full rounded-s bg-marca font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
            @click="confirmarCompra"
          >
            {{ enviandoCompra ? 'Registrando…' : 'Confirmar compra' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
