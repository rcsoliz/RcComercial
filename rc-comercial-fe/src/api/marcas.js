import http from './http'

export function listarMarcas() {
  return http.get('/marcas').then((r) => r.data)
}

export function crearMarca(comando) {
  return http.post('/marcas', comando).then((r) => r.data)
}

export function actualizarMarca(id, comando) {
  return http.put(`/marcas/${id}`, comando)
}

export function desactivarMarca(id) {
  return http.delete(`/marcas/${id}`)
}
