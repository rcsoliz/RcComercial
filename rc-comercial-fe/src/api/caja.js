import http from './http'

export function obtenerSesionAbierta() {
  return http.get('/caja/abierta').then((r) => r.data)
}

export function abrirCaja(montoInicial, sucursalId = null) {
  return http.post('/caja/abrir', { montoInicial, sucursalId }).then((r) => r.data)
}
