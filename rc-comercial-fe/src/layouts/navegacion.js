import { LayoutDashboard, ShoppingCart, Package, Truck, Settings } from 'lucide-vue-next'

export const navegacion = [
  { to: '/panel', label: 'Panel', icon: LayoutDashboard },
  { to: '/pos', label: 'Venta', icon: ShoppingCart },
  { to: '/productos', label: 'Productos', icon: Package },
  { to: '/compras', label: 'Compras', icon: Truck },
  { to: '/ajustes', label: 'Ajustes', icon: Settings },
]
