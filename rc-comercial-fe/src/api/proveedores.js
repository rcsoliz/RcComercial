import http from './http'

export function listarProveedores(buscar, estado = 'activos', pagina = 1) {
  return http.get('/proveedores', { params: { buscar, estado, pagina } }).then((r) => r.data)
}

export function obtenerProveedorPorId(id) {
  return http.get(`/proveedores/${id}`).then((r) => r.data)
}

export function crearProveedor(comando) {
  return http.post('/proveedores', comando).then((r) => r.data)
}

export function editarProveedor(id, comando) {
  return http.put(`/proveedores/${id}`, comando).then((r) => r.data)
}

export function desactivarProveedor(id) {
  return http.delete(`/proveedores/${id}`)
}
