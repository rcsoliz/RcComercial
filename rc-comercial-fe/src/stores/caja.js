import { defineStore } from 'pinia'
import { toast } from 'vue-sonner'
import { obtenerSesionAbierta, abrirCaja } from '@/api/caja'

const CLAVE_SESION_CAJA_CACHE = 'syscenters-caja-abierta-cache'

// Estado de caja compartido: antes vivía solo dentro de VentaView, pero el
// header (Caja activa/Operador) ahora se muestra en todas las vistas vía
// AppShell, así que necesita esta info sin depender de haber entrado a /venta.
export const useCajaStore = defineStore('caja', {
  state: () => ({
    sesion: null,
    cargando: true,
    yaConsultada: false,
  }),

  getters: {
    activa: (state) => !!state.sesion,
  },

  actions: {
    async cargarSesion() {
      this.cargando = true
      try {
        this.sesion = await obtenerSesionAbierta()
        if (this.sesion) localStorage.setItem(CLAVE_SESION_CAJA_CACHE, JSON.stringify(this.sesion))
        else localStorage.removeItem(CLAVE_SESION_CAJA_CACHE)
      } catch {
        // Sin red no se puede confirmar el estado real: si ya se había
        // abierto en línea, seguimos con esa última sesión conocida en vez
        // de bloquear el POS entero por no tener conexión.
        const cache = localStorage.getItem(CLAVE_SESION_CAJA_CACHE)
        this.sesion = cache ? JSON.parse(cache) : null
        if (this.sesion) toast.info('Sin conexión: se usa la última sesión de caja conocida.')
      } finally {
        this.cargando = false
        this.yaConsultada = true
      }
    },

    async abrir(montoInicial) {
      const sesion = await abrirCaja(montoInicial, null)
      this.sesion = sesion
      localStorage.setItem(CLAVE_SESION_CAJA_CACHE, JSON.stringify(sesion))
      return sesion
    },
  },
})
