import { LayoutDashboard, ShoppingCart, Package, Wallet, Truck, Users, Building2, Settings } from 'lucide-vue-next'
import { Permisos } from '@/utils/permisos'

// soloEscritorio: no ocupa slot en la barra inferior móvil (DESIGN.md: 4-5
// pestañas máximo ahí) — se usan igual desde el sidebar de escritorio. Son
// pantallas de trastienda (gestión de clientes/proveedores), no algo que se
// use a mitad de una venta desde el celular.
export const navegacion = [
  { to: '/panel', label: 'Panel', icon: LayoutDashboard, permiso: Permisos.ReportesVer },
  { to: '/pos', label: 'Venta', icon: ShoppingCart, permiso: null },
  { to: '/productos', label: 'Productos', icon: Package, permiso: null },
  { to: '/caja', label: 'Caja', icon: Wallet, permiso: Permisos.CajaAbrirCerrar },
  { to: '/compras', label: 'Compras', icon: Truck, permiso: Permisos.ComprasCrear },
  { to: '/clientes', label: 'Clientes', icon: Users, permiso: null, soloEscritorio: true },
  { to: '/proveedores', label: 'Proveedores', icon: Building2, permiso: Permisos.ComprasCrear, soloEscritorio: true },
  { to: '/ajustes', label: 'Ajustes', icon: Settings, permiso: null },
]
