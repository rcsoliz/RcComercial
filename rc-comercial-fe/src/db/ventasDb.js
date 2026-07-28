import { openDB } from 'idb'

const NOMBRE_DB = 'syscenters-ventas-offline'
const VERSION_DB = 2
const ALMACEN_PENDIENTES = 'pendientes'
const ALMACEN_RECHAZADAS = 'rechazadas'
const ALMACEN_RANGO = 'rango'
const CLAVE_RANGO = 'actual'
const UMBRAL_RESERVAR_MAS = 50

function abrirDb() {
  return openDB(NOMBRE_DB, VERSION_DB, {
    upgrade(db) {
      if (!db.objectStoreNames.contains(ALMACEN_PENDIENTES)) {
        db.createObjectStore(ALMACEN_PENDIENTES, { keyPath: 'id' })
      }
      if (!db.objectStoreNames.contains(ALMACEN_RECHAZADAS)) {
        db.createObjectStore(ALMACEN_RECHAZADAS, { keyPath: 'id' })
      }
      if (!db.objectStoreNames.contains(ALMACEN_RANGO)) {
        // IndexedDB (a diferencia de localStorage) sí serializa transacciones
        // "readwrite" contra el mismo store entre pestañas del mismo origen:
        // dos pestañas pidiendo "el siguiente número" a la vez nunca leen el
        // mismo valor, porque la segunda transacción espera a que la primera
        // confirme antes de poder leer.
        db.createObjectStore(ALMACEN_RANGO)
      }
    },
  })
}

export async function establecerRango(inicio, fin) {
  const db = await abrirDb()
  await db.put(ALMACEN_RANGO, { inicio, fin, siguienteLibre: inicio }, CLAVE_RANGO)
}

export async function numerosDisponibles() {
  const db = await abrirDb()
  const r = await db.get(ALMACEN_RANGO, CLAVE_RANGO)
  return r ? Math.max(0, r.fin - r.siguienteLibre + 1) : 0
}

export async function faltaReservarMas() {
  return (await numerosDisponibles()) < UMBRAL_RESERVAR_MAS
}

/** Toma el siguiente número del rango local (formato "00000123"), o null si no queda ninguno. Atómico entre pestañas. */
export async function tomarSiguienteNumero() {
  const db = await abrirDb()
  const tx = db.transaction(ALMACEN_RANGO, 'readwrite')
  const rango = await tx.store.get(CLAVE_RANGO)
  if (!rango || rango.siguienteLibre > rango.fin) {
    await tx.done
    return null
  }
  const numero = rango.siguienteLibre
  await tx.store.put({ ...rango, siguienteLibre: numero + 1 }, CLAVE_RANGO)
  await tx.done
  return String(numero).padStart(8, '0')
}

/** venta: { id, numero, creadoEn, total, resumenItems, comando } */
export async function encolarVentaPendiente(venta) {
  const db = await abrirDb()
  await db.put(ALMACEN_PENDIENTES, venta)
}

/** Orden FIFO: la más antigua primero. */
export async function listarVentasPendientes() {
  const db = await abrirDb()
  const todas = await db.getAll(ALMACEN_PENDIENTES)
  return todas.sort((a, b) => a.creadoEn.localeCompare(b.creadoEn))
}

export async function contarVentasPendientes() {
  const db = await abrirDb()
  return db.count(ALMACEN_PENDIENTES)
}

export async function quitarVentaPendiente(id) {
  const db = await abrirDb()
  await db.delete(ALMACEN_PENDIENTES, id)
}

/** Nunca se descarta en silencio: pasa de "pendientes" a "rechazadas" con motivo, visible en Ventas por revisar. */
export async function moverARechazada(venta, motivo) {
  const db = await abrirDb()
  const tx = db.transaction([ALMACEN_PENDIENTES, ALMACEN_RECHAZADAS], 'readwrite')
  await tx.objectStore(ALMACEN_PENDIENTES).delete(venta.id)
  await tx.objectStore(ALMACEN_RECHAZADAS).put({ ...venta, motivo, rechazadaEn: new Date().toISOString() })
  await tx.done
}

export async function listarVentasRechazadas() {
  const db = await abrirDb()
  const todas = await db.getAll(ALMACEN_RECHAZADAS)
  return todas.sort((a, b) => b.rechazadaEn.localeCompare(a.rechazadaEn))
}

export async function contarVentasRechazadas() {
  const db = await abrirDb()
  return db.count(ALMACEN_RECHAZADAS)
}

/** Vuelve a intentar sincronizar una rechazada: la regresa a "pendientes" para que useSincronizacion la retome. */
export async function reintentarVentaRechazada(id) {
  const db = await abrirDb()
  const venta = await db.get(ALMACEN_RECHAZADAS, id)
  if (!venta) return
  const { motivo, rechazadaEn, ...pendiente } = venta
  const tx = db.transaction([ALMACEN_PENDIENTES, ALMACEN_RECHAZADAS], 'readwrite')
  await tx.objectStore(ALMACEN_RECHAZADAS).delete(id)
  await tx.objectStore(ALMACEN_PENDIENTES).put(pendiente)
  await tx.done
}
