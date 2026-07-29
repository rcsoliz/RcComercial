import { defineStore } from 'pinia'
import http from '@/api/http'
import { decodificarPayloadJwt } from '@/utils/jwt'

const CLAVE = 'syscenters-sesion'

function cargar() {
  try {
    const raw = localStorage.getItem(CLAVE)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

function guardar(sesion) {
  if (sesion) localStorage.setItem(CLAVE, JSON.stringify(sesion))
  else localStorage.removeItem(CLAVE)
}

export const useAuthStore = defineStore('auth', {
  state: () => {
    const sesion = cargar()
    return {
      accessToken: sesion?.accessToken ?? null,
      refreshToken: sesion?.refreshToken ?? null,
    }
  },

  getters: {
    autenticado: (state) => !!state.accessToken,

    permisos: (state) => {
      const payload = state.accessToken ? decodificarPayloadJwt(state.accessToken) : null
      const permiso = payload?.permiso
      if (!permiso) return []
      return Array.isArray(permiso) ? permiso : [permiso]
    },

    // El backend también lo exige (OnTokenValidated en Program.cs): esto es
    // solo para redirigir antes de que el usuario choque contra un 401.
    debeCambiarPassword: (state) => {
      const payload = state.accessToken ? decodificarPayloadJwt(state.accessToken) : null
      return payload?.debe_cambiar_password === 'true'
    },

    // Back-office del proveedor SaaS (/plataforma): el backend también lo
    // exige (policy SoloPlataforma) — esto es solo para mostrar/ocultar UI.
    esSuperadmin: (state) => {
      const payload = state.accessToken ? decodificarPayloadJwt(state.accessToken) : null
      return payload?.es_superadmin === 'true'
    },
  },

  actions: {
    tienePermiso(codigo) {
      return this.permisos.includes(codigo)
    },

    _persistir() {
      guardar(this.accessToken ? { accessToken: this.accessToken, refreshToken: this.refreshToken } : null)
    },

    async iniciarSesion(usuarioLogin, password) {
      const { data } = await http.post('/auth/login', { usuarioLogin, password })
      this.accessToken = data.accessToken
      this.refreshToken = data.refreshToken
      this._persistir()
    },

    async cambiarPasswordObligatorio(passwordActual, passwordNueva) {
      const { data } = await http.post('/auth/cambiar-password-obligatorio', { passwordActual, passwordNueva })
      this.accessToken = data.accessToken
      this.refreshToken = data.refreshToken
      this._persistir()
    },

    async refrescarSesion() {
      if (!this.refreshToken) throw new Error('sin_refresh_token')
      const { data } = await http.post('/auth/refresh', { refreshToken: this.refreshToken })
      this.accessToken = data.accessToken
      this.refreshToken = data.refreshToken
      this._persistir()
    },

    cerrarSesion() {
      this.accessToken = null
      this.refreshToken = null
      this._persistir()
    },
  },
})
