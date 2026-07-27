import http from './http'

export function listarProveedores() {
  return http.get('/proveedores').then((r) => r.data)
}

export function crearProveedor(comando) {
  return http.post('/proveedores', comando).then((r) => r.data)
}
