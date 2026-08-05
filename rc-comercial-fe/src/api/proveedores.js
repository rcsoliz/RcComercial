import http from './http'

export function listarProveedores(buscar, estado = 'activos', pagina = 1) {
  return http.get('/proveedores', { params: { buscar, estado, pagina } }).then((r) => r.data)
}

/**
 * Paralela a listarProveedores() (esa la usa el <select> de "elegir
 * proveedor" en ComprasView y no cambia): esta es para el listado de
 * ProveedoresView, con paginado real (con total) y filtro de estado.
 * estado: 'activos' (default) | 'inactivos' | 'todos'.
 */
export function listarProveedoresPaginado(texto, pagina = 1, estado = 'activos') {
  return http
    .get('/proveedores/listado', { params: { buscar: texto || undefined, pagina, estado } })
    .then((r) => r.data)
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
