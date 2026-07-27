import http from './http'

export function buscarProductos(texto, pagina = 1) {
  return http.get('/productos', { params: { buscar: texto || undefined, pagina } }).then((r) => r.data)
}

export function obtenerProductoPorId(id) {
  return http.get(`/productos/${id}`).then((r) => r.data)
}

export async function obtenerProductoPorCodigoBarras(codigoBarras) {
  try {
    const { data } = await http.get(`/productos/por-codigo/${encodeURIComponent(codigoBarras)}`)
    return data
  } catch (error) {
    if (error.response?.status === 404) return null
    throw error
  }
}

export function crearProducto(comando) {
  return http.post('/productos', comando).then((r) => r.data)
}

export function actualizarProducto(id, comando) {
  return http.put(`/productos/${id}`, comando).then((r) => r.data)
}

export function cambiarPrecio(id, nuevoPrecio, presentacionId = null) {
  return http.put(`/productos/${id}/precio`, { presentacionId, nuevoPrecio })
}

export function desactivarProducto(id) {
  return http.delete(`/productos/${id}`)
}
