import http from './http'

export function crearVehiculo(comando) {
  return http.post('/vehiculos', comando).then((r) => r.data)
}

export function editarVehiculo(id, comando) {
  return http.put(`/vehiculos/${id}`, comando).then((r) => r.data)
}

export function desactivarVehiculo(id) {
  return http.delete(`/vehiculos/${id}`)
}

export function obtenerHistorialVehiculo(id) {
  return http.get(`/vehiculos/${id}/historial`).then((r) => r.data)
}
