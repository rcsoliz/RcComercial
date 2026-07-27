// Decodifica el payload de un JWT sin verificar firma: solo se usa para leer
// claims ya confiables (el token vino de nuestro propio backend) y mostrar/
// ocultar botones en la UI. La autorización real siempre la valida el backend.
export function decodificarPayloadJwt(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    )
    return JSON.parse(json)
  } catch {
    return null
  }
}
