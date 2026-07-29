import http from './http'

export function listarSucursales(estado = 'activos') {
  return http.get('/sucursales', { params: { estado } }).then((r) => r.data)
}

export function crearSucursal(comando) {
  return http.post('/sucursales', comando).then((r) => r.data)
}

export function editarSucursal(id, comando) {
  return http.put(`/sucursales/${id}`, comando).then((r) => r.data)
}

export function desactivarSucursal(id) {
  return http.delete(`/sucursales/${id}`)
}
