import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import router from '@/router'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
})

let refrescando = null

http.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.accessToken) {
    config.headers.Authorization = `Bearer ${auth.accessToken}`
  }
  return config
})

http.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config

    if (
      error.response?.status !== 401 ||
      !original ||
      original._reintentado ||
      original.url?.includes('/auth/')
    ) {
      return Promise.reject(error)
    }

    original._reintentado = true
    const auth = useAuthStore()

    if (!refrescando) {
      refrescando = auth.refrescarSesion().finally(() => {
        refrescando = null
      })
    }

    try {
      await refrescando
      original.headers.Authorization = `Bearer ${auth.accessToken}`
      return http(original)
    } catch (refreshError) {
      auth.cerrarSesion()
      router.push({ name: 'login' })
      return Promise.reject(refreshError)
    }
  },
)

export default http
