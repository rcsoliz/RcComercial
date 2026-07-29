import http from './http'

export function listarEmpresasPlataforma(estado = 'activos') {
  return http.get('/plataforma/empresas', { params: { estado } }).then((r) => r.data)
}

export function crearEmpresaPlataforma(comando) {
  return http.post('/plataforma/empresas', comando).then((r) => r.data)
}

export function cambiarActivoEmpresa(id, activo) {
  return http.patch(`/plataforma/empresas/${id}/activo`, { activo })
}
