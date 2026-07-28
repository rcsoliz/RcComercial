import { ref, watch } from 'vue'
import { useConexion } from './useConexion'
import { sincronizarVentas } from '@/api/sync'
import { listarVentasPendientes, moverARechazada, quitarVentaPendiente } from '@/db/ventasDb'

const ESPERA_MINIMA_MS = 3_000
const ESPERA_MAXIMA_MS = 60_000

const sincronizando = ref(false)
const pendientesRestantes = ref(0)
let intentosFallidos = 0
let temporizador = null
let iniciado = false

/** Despacha la cola de ventas pendientes al backend (FIFO), en un solo lote. */
async function intentarSincronizar() {
  const pendientes = await listarVentasPendientes() // ya viene ordenado FIFO
  pendientesRestantes.value = pendientes.length
  if (pendientes.length === 0) return

  sincronizando.value = true
  try {
    const resultados = await sincronizarVentas(pendientes.map((v) => v.comando))
    for (const r of resultados) {
      if (r.estado === 'aceptada' || r.estado === 'duplicada') {
        await quitarVentaPendiente(r.id)
      } else {
        const original = pendientes.find((p) => p.id === r.id)
        if (original) await moverARechazada(original, r.motivo || 'Rechazada por el servidor.')
      }
    }
    intentosFallidos = 0
    pendientesRestantes.value = await countPendientes()
  } catch {
    // Fallo de red/servidor en el lote completo (no rechazo de un ítem
    // puntual): TODO sigue pendiente, se reintenta con backoff — nada se
    // pierde ni se marca rechazado por esto.
    intentosFallidos++
  } finally {
    sincronizando.value = false
  }
}

async function countPendientes() {
  const pendientes = await listarVentasPendientes()
  return pendientes.length
}

function programarReintento(enLinea) {
  clearTimeout(temporizador)
  const espera = Math.min(ESPERA_MINIMA_MS * 2 ** intentosFallidos, ESPERA_MAXIMA_MS)
  temporizador = setTimeout(async () => {
    if (enLinea.value) await intentarSincronizar()
    programarReintento(enLinea)
  }, espera)
}

/**
 * Sincroniza la cola de ventas offline (IndexedDB → POST /sync/ventas) apenas
 * vuelve la conexión, con reintentos de backoff si el lote entero falla.
 */
export function useSincronizacion() {
  const { enLinea } = useConexion()

  function iniciar() {
    if (iniciado) return
    iniciado = true
    countPendientes().then((n) => (pendientesRestantes.value = n))

    watch(enLinea, (esta) => {
      if (esta) {
        intentosFallidos = 0
        intentarSincronizar()
      }
    })

    programarReintento(enLinea)
  }

  return { iniciar, sincronizando, pendientesRestantes, intentarSincronizar }
}
