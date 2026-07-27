import { ref } from 'vue'

// Estado compartido a nivel de módulo: un solo verificador de conexión para
// toda la app (igual que useTema), no uno por componente que lo use.
const enLinea = ref(navigator.onLine)
let temporizador = null
let intentosFallidos = 0
let iniciado = false

const ESPERA_EN_LINEA_MS = 15_000
const ESPERA_MINIMA_OFFLINE_MS = 2_000
const ESPERA_MAXIMA_OFFLINE_MS = 30_000

async function verificar() {
  if (!navigator.onLine) {
    enLinea.value = false
    intentosFallidos++
    programarSiguiente()
    return
  }

  try {
    const controlador = new AbortController()
    const corte = setTimeout(() => controlador.abort(), 4000)
    const respuesta = await fetch('/health', { method: 'GET', cache: 'no-store', signal: controlador.signal })
    clearTimeout(corte)
    // fetch() no rechaza por códigos de error HTTP (un 502/503 del backend
    // caído detrás del proxy "responde" igual): sin este chequeo se
    // reportaría "en línea" con el backend abajo.
    if (!respuesta.ok) throw new Error('salud no ok')
    enLinea.value = true
    intentosFallidos = 0
  } catch {
    enLinea.value = false
    intentosFallidos++
  }

  programarSiguiente()
}

function programarSiguiente() {
  clearTimeout(temporizador)
  const espera = enLinea.value
    ? ESPERA_EN_LINEA_MS
    : Math.min(ESPERA_MINIMA_OFFLINE_MS * 2 ** intentosFallidos, ESPERA_MAXIMA_OFFLINE_MS)
  temporizador = setTimeout(verificar, espera)
}

function iniciar() {
  if (iniciado) return
  iniciado = true

  window.addEventListener('online', () => {
    intentosFallidos = 0
    verificar()
  })
  window.addEventListener('offline', () => {
    enLinea.value = false
    programarSiguiente()
  })

  verificar()
}

export function useConexion() {
  iniciar()
  return { enLinea }
}
