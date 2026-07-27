import http from './http'

export function obtenerEmpresaActual() {
  return http.get('/empresa/actual').then((r) => r.data)
}
