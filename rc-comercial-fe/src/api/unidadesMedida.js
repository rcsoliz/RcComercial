import http from './http'

export function listarUnidadesMedida() {
  return http.get('/unidades-medida').then((r) => r.data)
}
