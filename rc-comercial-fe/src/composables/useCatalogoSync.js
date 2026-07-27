import { ref } from 'vue'
import { useConexion } from './useConexion'
import { sincronizarCatalogo } from '@/db/catalogoDb'

const INTERVALO_MS = 15 * 60 * 1000

let temporizador = null
const sincronizando = ref(false)
const ultimaSincronizacion = ref(null)

async function sincronizarSiHayConexion(enLinea) {
  if (!enLinea.value) return
  sincronizando.value = true
  try {
    await sincronizarCatalogo()
    ultimaSincronizacion.value = new Date()
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
