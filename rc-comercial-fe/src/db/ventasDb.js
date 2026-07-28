import { openDB } from 'idb'

const NOMBRE_DB = 'syscenters-ventas-offline'
const VERSION_DB = 1
const ALMACEN_PENDIENTES = 'pendientes'
const ALMACEN_RECHAZADAS = 'rechazadas'

function abrirDb() {
  return openDB(NOMBRE_DB, VERSION_DB, {
    upgrade(db) {
      if (!db.objectStoreNames.contains(ALMACEN_PENDIENTES)) {
        db.createObjectStore(ALMACEN_PENDIENTES, { keyPath: 'id' })
      }
      if (!db.objectStoreNames.contains(ALMACEN_RECHAZADAS)) {
        db.createObjectStore(ALMACEN_RECHAZADAS, { keyPath: 'id' })
      }
    },
  })
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
