import http from './http'

export function buscarProductos(texto) {
  return http.get('/productos', { params: { buscar: texto || undefined } }).then((r) => r.data)
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
