// Rango de numeración de venta reservado por este dispositivo (localStorage:
// es un objeto único y pequeño, no necesita IndexedDB). Permite asignar un
// número de venta real sin red mientras el bloque no se agote.
const CLAVE_RANGO = 'syscenters-rango-numeracion'
const CLAVE_DISPOSITIVO = 'syscenters-dispositivo-id'
const UMBRAL_RESERVAR_MAS = 50

export function obtenerDispositivoId() {
  let id = localStorage.getItem(CLAVE_DISPOSITIVO)
  if (!id) {
    id = crypto.randomUUID()
    localStorage.setItem(CLAVE_DISPOSITIVO, id)
  }
  return id
}

function leerRango() {
  const raw = localStorage.getItem(CLAVE_RANGO)
  return raw ? JSON.parse(raw) : null
}

function guardarRango(rango) {
  localStorage.setItem(CLAVE_RANGO, JSON.stringify(rango))
}

export function establecerRango(inicio, fin) {
  guardarRango({ inicio, fin, siguienteLibre: inicio })
}

export function numerosDisponibles() {
  const r = leerRango()
  return r ? Math.max(0, r.fin - r.siguienteLibre + 1) : 0
}

export function faltaReservarMas() {
  return numerosDisponibles() < UMBRAL_RESERVAR_MAS
}

/** Toma el siguiente número del rango local (formato "00000123"), o null si no queda ninguno. */
export function tomarSiguienteNumero() {
  const r = leerRango()
  if (!r || r.siguienteLibre > r.fin) return null
  guardarRango({ ...r, siguienteLibre: r.siguienteLibre + 1 })
  return String(r.siguienteLibre).padStart(8, '0')
}
