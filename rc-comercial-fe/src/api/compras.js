import http from './http'

export function listarCompras(pagina = 1) {
  return http.get('/compras', { params: { pagina } }).then((r) => r.data)
}

export function crearCompra(comando) {
  return http.post('/compras', comando).then((r) => r.data)
}

export function obtenerSugeridoCompra(proveedorId, sucursalId = null) {
  return http.get('/compras/sugerido', { params: { proveedorId, sucursalId } }).then((r) => r.data)
}

export function enviarPedidoProveedor(proveedorId, detalles) {
  return http.post('/compras/pedido-proveedor', { proveedorId, detalles })
}
