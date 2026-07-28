import { ref } from 'vue'
import { useConexion } from './useConexion'
import { sincronizarCatalogo } from '@/db/catalogoDb'
import { reservarRango } from '@/api/sync'
import { establecerRango, faltaReservarMas, obtenerDispositivoId } from '@/db/rangoNumeracion'

const INTERVALO_MS = 15 * 60 * 1000
const TAMANO_RANGO = 500

let temporizador = null
const sincronizando = ref(false)
const ultimaSincronizacion = ref(null)

async function reservarRangoSiHaceFalta() {
  if (!faltaReservarMas()) return
  try {
    const rango = await reservarRango(null, obtenerDispositivoId(), TAMANO_RANGO)
    establecerRango(rango.inicio, rango.fin)
  } catch {
    // Sin permiso o sin red real pese a "en línea": el dispositivo sigue con
    // el rango que ya tenía (o sin rango, y usará el respaldo por venta).
  }
}

async function sincronizarSiHayConexion(enLinea) {
  if (!enLinea.value) return
  sincronizando.value = true
  try {
    await sincronizarCatalogo()
    ultimaSincronizacion.value = new Date()
    await reservarRangoSiHaceFalta()
  } catch {
    // Sin red o backend caído: seguimos sirviendo lo que ya haya en IndexedDB.
  } finally {
    sincronizando.value = false
  }
}

/**
 * Sincroniza el catálogo (GET /sync/catalogo → IndexedDB) al iniciar sesión y
 * cada 15 min mientras haya conexión. Un solo temporizador para toda la app.
 */
export function useCatalogoSync() {
  const { enLinea } = useConexion()

  function iniciar() {
    if (temporizador) return
    sincronizarSiHayConexion(enLinea)
    temporizador = setInterval(() => sincronizarSiHayConexion(enLinea), INTERVALO_MS)
  }

  function detener() {
    clearInterval(temporizador)
    temporizador = null
  }

  return { iniciar, detener, sincronizando, ultimaSincronizacion }
}
