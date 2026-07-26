import { defineStore } from 'pinia'
import http from '@/api/http'

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
  },

  actions: {
    _persistir() {
      guardar(this.accessToken ? { accessToken: this.accessToken, refreshToken: this.refreshToken } : null)
    },

    async iniciarSesion(usuarioLogin, password) {
      const { data } = await http.post('/auth/login', { usuarioLogin, password })
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
