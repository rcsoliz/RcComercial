import http from './http'

export function listarCategorias() {
  return http.get('/categorias').then((r) => r.data)
}
