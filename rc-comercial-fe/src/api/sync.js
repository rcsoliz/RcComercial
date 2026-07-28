import http from './http'

export function reservarRango(sucursalId, dispositivoId, tamano = 500) {
  return http.post('/sync/reservar-rango', { sucursalId, dispositivoId, tamano }).then((r) => r.data)
}

export function sincronizarVentas(ventas) {
  return http.post('/sync/ventas', { ventas }).then((r) => r.data)
}
