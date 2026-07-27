import http from './http'

export function obtenerPanelHoy() {
  return http.get('/panel/hoy').then((r) => r.data)
}

export function obtenerPanelAlertas() {
  return http.get('/panel/alertas').then((r) => r.data)
}

export function obtenerPanelHistorico(desde, hasta) {
  return http.get('/panel/historico', { params: { desde, hasta } }).then((r) => r.data)
}
