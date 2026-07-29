import http from './http'

export function buscarUsuarios(buscar, estado = 'activos', pagina = 1) {
  return http.get('/usuarios', { params: { buscar, estado, pagina } }).then((r) => r.data)
}

export function obtenerUsuarioPorId(id) {
  return http.get(`/usuarios/${id}`).then((r) => r.data)
}

export function crearUsuario(comando) {
  return http.post('/usuarios', comando).then((r) => r.data)
}

export function editarUsuario(id, comando) {
  return http.put(`/usuarios/${id}`, comando).then((r) => r.data)
}

export function restablecerPassword(id) {
  return http.post(`/usuarios/${id}/restablecer-password`).then((r) => r.data)
}

export function desactivarUsuario(id) {
  return http.delete(`/usuarios/${id}`)
}
