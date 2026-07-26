import http from './http'

export function crearVenta(comando) {
  return http.post('/ventas', comando).then((r) => r.data)
}
