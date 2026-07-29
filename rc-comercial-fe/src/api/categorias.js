import http from './http'

export function listarCategorias() {
  return http.get('/categorias').then((r) => r.data)
}

export function crearCategoria(comando) {
  return http.post('/categorias', comando).then((r) => r.data)
}

export function actualizarCategoria(id, comando) {
  return http.put(`/categorias/${id}`, comando)
}

export function desactivarCategoria(id) {
  return http.delete(`/categorias/${id}`)
}
