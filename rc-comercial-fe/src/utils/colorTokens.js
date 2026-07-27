// Lee el valor RESUELTO (hex/rgb) de un token de tokens.css en el DOM actual.
// Necesario porque ApexCharts no acepta var(--x) en todas sus props de color;
// así seguimos usando "solo tokens" (nunca un hex inventado en el JS).
export function leerColorToken(nombre) {
  return getComputedStyle(document.documentElement).getPropertyValue(nombre).trim()
}
