import http from './http'

export function buscarClientes(buscar, estado = 'activos', pagina = 1) {
  return http.get('/clientes', { params: { buscar, estado, pagina } }).then((r) => r.data)
}

/**
 * Paralela a buscarClientes() (esa la usa SelectorCliente.vue en el POS y no
 * cambia): esta es para el listado de ClientesView, con paginado real (con
 * total) y filtro de estado.
 * estado: 'activos' (default) | 'inactivos' | 'todos'.
 */
export function listarClientesPaginado(texto, pagina = 1, estado = 'activos') {
  return http
    .get('/clientes/listado', { params: { buscar: texto || undefined, pagina, estado } })
    .then((r) => r.data)
}

export function obtenerClientePorId(id) {
  return http.get(`/clientes/${id}`).then((r) => r.data)
}

export function crearCliente(comando) {
  return http.post('/clientes', comando).then((r) => r.data)
}

export function editarCliente(id, comando) {
  return http.put(`/clientes/${id}`, comando).then((r) => r.data)
}

export function desactivarCliente(id) {
  return http.delete(`/clientes/${id}`)
}

export function listarVentasPorCliente(clienteId, pagina = 1) {
  return http.get(`/clientes/${clienteId}/ventas`, { params: { pagina } }).then((r) => r.data)
}
