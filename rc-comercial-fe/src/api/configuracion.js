import http from './http'

export function obtenerConfiguracion() {
  return http.get('/configuracion').then((r) => r.data)
}

export function actualizarConfiguracion(comando) {
  return http.put('/configuracion', comando)
}

export function editarEmpresa(comando) {
  return http.put('/configuracion/empresa', comando).then((r) => r.data)
}
