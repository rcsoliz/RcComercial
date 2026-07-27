import http from './http'

export function obtenerSesionAbierta() {
  return http.get('/caja/abierta').then((r) => r.data)
}

export function abrirCaja(montoInicial, sucursalId = null) {
  return http.post('/caja/abrir', { montoInicial, sucursalId }).then((r) => r.data)
}

export function cerrarCaja(sesionId, montoDeclarado) {
  return http.post('/caja/cerrar', { sesionId, montoDeclarado }).then((r) => r.data)
}

export function listarHistorialCaja(pagina = 1) {
  return http.get('/caja/historial', { params: { pagina } }).then((r) => r.data)
}
