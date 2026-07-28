import { openDB } from 'idb'
import http from '@/api/http'

const NOMBRE_DB = 'syscenters-catalogo'
const VERSION_DB = 1
const ALMACEN_PRODUCTOS = 'productos'
const ALMACEN_META = 'meta'

function abrirDb() {
  return openDB(NOMBRE_DB, VERSION_DB, {
    upgrade(db) {
      if (!db.objectStoreNames.contains(ALMACEN_PRODUCTOS)) {
        const tienda = db.createObjectStore(ALMACEN_PRODUCTOS, { keyPath: 'id' })
        tienda.createIndex('nombre', 'nombre')
        tienda.createIndex('codigoBarras', 'codigoBarras')
      }
      if (!db.objectStoreNames.contains(ALMACEN_META)) {
        db.createObjectStore(ALMACEN_META)
      }
    },
  })
}

/** Trae el catálogo del backend (GET /sync/catalogo) y lo persiste completo en IndexedDB. */
export async function sincronizarCatalogo() {
  const { data } = await http.get('/sync/catalogo')
  const db = await abrirDb()

  const tx = db.transaction([ALMACEN_PRODUCTOS, ALMACEN_META], 'readwrite')
  await tx.objectStore(ALMACEN_PRODUCTOS).clear()
  for (const producto of data.productos) {
    await tx.objectStore(ALMACEN_PRODUCTOS).put(producto)
  }
  await tx.objectStore(ALMACEN_META).put(data.version, 'version')
  await tx.objectStore(ALMACEN_META).put(new Date().toISOString(), 'sincronizadoEn')
  await tx.done

  return data
}

export async function obtenerMetaCatalogo() {
  const db = await abrirDb()
  const [version, sincronizadoEn] = await Promise.all([
    db.get(ALMACEN_META, 'version'),
    db.get(ALMACEN_META, 'sincronizadoEn'),
  ])
  return { version: version ?? null, sincronizadoEn: sincronizadoEn ?? null }
}

/** Búsqueda instantánea en el catálogo local: coincidencia por nombre, código o código de barras. */
export async function buscarEnCatalogoLocal(texto) {
  const db = await abrirDb()
  const todos = await db.getAll(ALMACEN_PRODUCTOS)

  const q = texto?.trim().toLowerCase()
  if (!q) return todos.sort((a, b) => a.nombre.localeCompare(b.nombre)).slice(0, 20)

  return todos
    .filter(
      (p) =>
        p.nombre.toLowerCase().includes(q) ||
        p.codigo?.toLowerCase().includes(q) ||
        p.codigoBarras?.toLowerCase().includes(q),
    )
    .slice(0, 20)
}

export async function obtenerProductoLocalPorId(id) {
  const db = await abrirDb()
  return db.get(ALMACEN_PRODUCTOS, id)
}

export async function obtenerProductoLocalPorCodigoBarras(codigo) {
  const db = await abrirDb()
  const producto = await db.getFromIndex(ALMACEN_PRODUCTOS, 'codigoBarras', codigo)
  if (producto) return producto
  // También puede ser el código de barras de una presentación, no del producto base.
  const todos = await db.getAll(ALMACEN_PRODUCTOS)
  return todos.find((p) => p.presentaciones.some((pr) => pr.codigoBarras === codigo)) ?? null
}

export async function contarProductosLocal() {
  const db = await abrirDb()
  return db.count(ALMACEN_PRODUCTOS)
}

/**
 * Descuento OPTIMISTA de stock local tras una venta offline: el backend es
 * quien de verdad valida/descuenta al sincronizar, esto es solo para que el
 * cajero vea el catálogo local reflejar la venta de inmediato (evita vender
 * "de más" por no ver el descuento hasta la próxima sincronización).
 */
export async function descontarStockOptimista(productoId, cantidadBase) {
  const db = await abrirDb()
  const tx = db.transaction(ALMACEN_PRODUCTOS, 'readwrite')
  const producto = await tx.store.get(productoId)
  if (producto) {
    producto.stockTotal = Math.max(0, (producto.stockTotal ?? 0) - cantidadBase)
    await tx.store.put(producto)
  }
  await tx.done
}
