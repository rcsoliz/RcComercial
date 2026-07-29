import http from './http'

export function listarRoles() {
  return http.get('/roles').then((r) => r.data)
}

export function listarPermisosCatalogo() {
  return http.get('/roles/permisos').then((r) => r.data)
}

export function crearRol(comando) {
  return http.post('/roles', comando).then((r) => r.data)
}

export function editarRol(id, comando) {
  return http.put(`/roles/${id}`, comando).then((r) => r.data)
}
