// Identidad del dispositivo (informativa/telemetría): no participa en la
// atomicidad del rango de numeración — esa vive en ventasDb.js (IndexedDB,
// serializa entre pestañas). localStorage alcanza acá porque es un valor
// que se escribe una sola vez y una colisión rarísima entre dos pestañas
// en su primer arranque no tiene consecuencia real.
const CLAVE_DISPOSITIVO = 'syscenters-dispositivo-id'

export function obtenerDispositivoId() {
  let id = localStorage.getItem(CLAVE_DISPOSITIVO)
  if (!id) {
    id = crypto.randomUUID()
    localStorage.setItem(CLAVE_DISPOSITIVO, id)
  }
  return id
}
