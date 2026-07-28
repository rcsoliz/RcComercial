import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { Permisos } from '@/utils/permisos'

const routes = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { publica: true },
  },
  {
    path: '/',
    component: () => import('@/layouts/AppShell.vue'),
    meta: { requiereAuth: true },
    children: [
      { path: '', redirect: '/pos' },
      { path: 'panel', name: 'panel', component: () => import('@/views/PanelView.vue'), meta: { titulo: 'Panel del dueño', permiso: Permisos.ReportesVer } },
      { path: 'pos', name: 'pos', component: () => import('@/views/VentaView.vue'), meta: { titulo: 'Punto de venta' } },
      { path: 'pos/revisar', name: 'ventas-revisar', component: () => import('@/views/VentasRevisarView.vue'), meta: { titulo: 'Ventas por revisar' } },
      { path: 'productos', name: 'productos', component: () => import('@/views/ProductosView.vue'), meta: { titulo: 'Productos' } },
      { path: 'productos/nuevo', name: 'productos-nuevo', component: () => import('@/views/ProductoFormView.vue'), meta: { titulo: 'Nuevo producto' } },
      { path: 'productos/:id', name: 'productos-editar', component: () => import('@/views/ProductoFormView.vue'), meta: { titulo: 'Editar producto' }, props: true },
      { path: 'caja', name: 'caja', component: () => import('@/views/CajaView.vue'), meta: { titulo: 'Caja', permiso: Permisos.CajaAbrirCerrar } },
      { path: 'compras', name: 'compras', component: () => import('@/views/ComprasView.vue'), meta: { titulo: 'Compras', permiso: Permisos.ComprasCrear } },
      { path: 'clientes', name: 'clientes', component: () => import('@/views/ClientesView.vue'), meta: { titulo: 'Clientes' } },
      { path: 'clientes/nuevo', name: 'clientes-nuevo', component: () => import('@/views/ClienteFormView.vue'), meta: { titulo: 'Nuevo cliente' } },
      { path: 'clientes/:id', name: 'clientes-editar', component: () => import('@/views/ClienteFormView.vue'), meta: { titulo: 'Editar cliente' }, props: true },
      { path: 'proveedores', name: 'proveedores', component: () => import('@/views/ProveedoresView.vue'), meta: { titulo: 'Proveedores' } },
      { path: 'proveedores/nuevo', name: 'proveedores-nuevo', component: () => import('@/views/ProveedorFormView.vue'), meta: { titulo: 'Nuevo proveedor' } },
      { path: 'proveedores/:id', name: 'proveedores-editar', component: () => import('@/views/ProveedorFormView.vue'), meta: { titulo: 'Editar proveedor' }, props: true },
      { path: 'ajustes', name: 'ajustes', component: () => import('@/views/PlaceholderView.vue'), meta: { titulo: 'Ajustes' } },
    ],
  },
  { path: '/:pathMatch(.*)*', redirect: '/pos' },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.requiereAuth && !auth.autenticado) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.permiso && !auth.tienePermiso(to.meta.permiso)) {
    return { name: 'pos' }
  }

  if (to.name === 'login' && auth.autenticado) {
    return { path: '/pos' }
  }

  return true
})

export default router
