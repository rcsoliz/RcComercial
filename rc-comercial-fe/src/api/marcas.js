import http from './http'

export function listarMarcas() {
  return http.get('/marcas').then((r) => r.data)
}
