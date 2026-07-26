import { defineStore } from 'pinia'
import { uuid7 } from '@/utils/uuid7'

function claveItem(productoId, presentacionId) {
  return `${productoId}|${presentacionId ?? 'base'}`
}

export const useVentaStore = defineStore('venta', {
  // Estado serializable a propósito (sin clases, sin funciones): es la base
  // de la cola offline de la Fase 8. Toda mutación vive en actions, nunca en
  // los componentes — VentaView solo lee este estado y llama estas actions.
  state: () => ({
    id: uuid7(),
    clienteId: null,
    descuento: 0,
    items: [],
    pagos: [],
    receta: null,
  }),

  getters: {
    vacia: (state) => state.items.length === 0,

    subtotal: (state) => state.items.reduce((acc, i) => acc + i.cantidad * i.precioUnitario, 0),

    totalDescuentos: (state) => state.descuento + state.items.reduce((acc, i) => acc + (i.descuento || 0), 0),

    total() {
      return Math.max(0, this.subtotal - this.totalDescuentos)
    },

    totalPagado: (state) => state.pagos.reduce((acc, p) => acc + p.monto, 0),

    diferenciaPago() {
      return Math.round((this.total - this.totalPagado) * 100) / 100
    },

    requiereReceta: (state) => state.items.some((i) => i.esControlado),

    recetaCompleta() {
      return !this.requiereReceta || this.receta !== null
    },
  },

  actions: {
    iniciarNueva() {
      this.id = uuid7()
      this.clienteId = null
      this.descuento = 0
      this.items = []
      this.pagos = []
      this.receta = null
    },

    agregarProducto(producto, presentacion = null, cantidad = 1) {
      const clave = claveItem(producto.id, presentacion?.id ?? null)
      const existente = this.items.find((i) => i.clave === clave)

      if (existente) {
        existente.cantidad += cantidad
        return
      }

      this.items.push({
        clave,
        productoId: producto.id,
        presentacionId: presentacion?.id ?? null,
        nombre: producto.nombre,
        presentacionNombre: presentacion?.nombre ?? null,
        factor: presentacion?.factor ?? 1,
        codigo: producto.codigo ?? null,
        cantidad,
        precioUnitario: presentacion?.precio ?? producto.precioBase,
        descuento: 0,
        esControlado: !!producto.esControlado,
        manejaLote: !!producto.manejaLote,
      })
    },

    cambiarCantidad(clave, delta) {
      const item = this.items.find((i) => i.clave === clave)
      if (!item) return
      const nueva = item.cantidad + delta
      if (nueva <= 0) {
        this.items = this.items.filter((i) => i.clave !== clave)
      } else {
        item.cantidad = nueva
      }
    },

    quitarItem(clave) {
      this.items = this.items.filter((i) => i.clave !== clave)
    },

    establecerReceta(datos) {
      this.receta = datos
    },

    agregarPago(pago) {
      this.pagos.push(pago)
    },

    quitarPago(indice) {
      this.pagos.splice(indice, 1)
    },

    limpiarPagos() {
      this.pagos = []
    },

    /** Arma el payload exacto de CrearVentaCommand (backend). */
    aComandoCrearVenta() {
      return {
        id: this.id,
        clienteId: this.clienteId,
        descuento: this.descuento,
        detalles: this.items.map((i) => ({
          productoId: i.productoId,
          presentacionId: i.presentacionId,
          cantidad: i.cantidad,
          precioUnitario: i.precioUnitario,
          descuento: i.descuento,
        })),
        pagos: this.pagos.map((p) => ({
          metodo: p.metodo,
          monto: p.monto,
          referenciaQr: p.referenciaQr ?? null,
        })),
        receta: this.receta,
      }
    },
  },
})
