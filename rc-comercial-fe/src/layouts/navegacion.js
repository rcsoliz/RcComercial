import { LayoutDashboard, ShoppingCart, Package, Wallet, Truck, Settings } from 'lucide-vue-next'
import { Permisos } from '@/utils/permisos'

export const navegacion = [
  { to: '/panel', label: 'Panel', icon: LayoutDashboard, permiso: Permisos.ReportesVer },
  { to: '/pos', label: 'Venta', icon: ShoppingCart, permiso: null },
  { to: '/productos', label: 'Productos', icon: Package, permiso: null },
  { to: '/caja', label: 'Caja', icon: Wallet, permiso: Permisos.CajaAbrirCerrar },
  { to: '/compras', label: 'Compras', icon: Truck, permiso: Permisos.ComprasCrear },
  { to: '/ajustes', label: 'Ajustes', icon: Settings, permiso: null },
]
