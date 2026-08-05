import http from './http'

export function listarProformas(texto, pagina = 1, estado = null, clienteId = null) {
  return http
    .get('/proformas', { params: { buscar: texto || undefined, pagina, estado: estado || undefined, clienteId: clienteId || undefined } })
    .then((r) => r.data)
}

export function obtenerProformaPorId(id) {
  return http.get(`/proformas/${id}`).then((r) => r.data)
}

export function crearProforma(comando) {
  return http.post('/proformas', comando).then((r) => r.data)
}

export function rechazarProforma(id, motivoRechazo) {
  return http.post(`/proformas/${id}/rechazar`, { id, motivoRechazo }).then((r) => r.data)
}

export function convertirProformaEnVenta(id, comando) {
  return http.post(`/proformas/${id}/convertir`, { proformaId: id, ...comando }).then((r) => r.data)
}
