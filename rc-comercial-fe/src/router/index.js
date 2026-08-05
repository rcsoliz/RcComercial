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
    path: '/cambiar-password',
    name: 'cambiar-password',
    component: () => import('@/views/CambiarPasswordView.vue'),
    meta: { requiereAuth: true },
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
      { path: 'vehiculos/:id', name: 'vehiculos-historial', component: () => import('@/views/VehiculoHistorialView.vue'), meta: { titulo: 'Historial del vehículo' }, props: true },
      { path: 'proformas', name: 'proformas', component: () => import('@/views/ProformasView.vue'), meta: { titulo: 'Proformas', permiso: Permisos.ProformasCrear } },
      { path: 'proformas/nueva', name: 'proformas-nueva', component: () => import('@/views/ProformaFormView.vue'), meta: { titulo: 'Nueva proforma', permiso: Permisos.ProformasCrear } },
      { path: 'proformas/:id', name: 'proformas-editar', component: () => import('@/views/ProformaFormView.vue'), meta: { titulo: 'Proforma', permiso: Permisos.ProformasCrear }, props: true },
      { path: 'ajustes', name: 'ajustes', component: () => import('@/views/AjustesView.vue'), meta: { titulo: 'Ajustes' } },
      { path: 'usuarios', name: 'usuarios', component: () => import('@/views/UsuariosView.vue'), meta: { titulo: 'Usuarios', permiso: Permisos.AdminUsuarios } },
      { path: 'usuarios/nuevo', name: 'usuarios-nuevo', component: () => import('@/views/UsuarioFormView.vue'), meta: { titulo: 'Nuevo usuario', permiso: Permisos.AdminUsuarios } },
      { path: 'usuarios/:id', name: 'usuarios-editar', component: () => import('@/views/UsuarioFormView.vue'), meta: { titulo: 'Editar usuario', permiso: Permisos.AdminUsuarios }, props: true },
      { path: 'roles', name: 'roles', component: () => import('@/views/RolesView.vue'), meta: { titulo: 'Roles y permisos', permiso: Permisos.AdminRoles } },
      { path: 'catalogos', name: 'catalogos', component: () => import('@/views/CatalogosView.vue'), meta: { titulo: 'Categorías y marcas', permiso: Permisos.ProductosCrearEditar } },
      { path: 'sucursales', name: 'sucursales', component: () => import('@/views/SucursalesView.vue'), meta: { titulo: 'Sucursales', permiso: Permisos.AdminSucursales } },
      { path: 'configuracion', name: 'configuracion', component: () => import('@/views/ConfiguracionView.vue'), meta: { titulo: 'Configuración', permiso: Permisos.AdminConfiguracion } },
      { path: 'plataforma', name: 'plataforma', component: () => import('@/views/PlataformaView.vue'), meta: { titulo: 'Plataforma', soloSuperadmin: true } },
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

  // Primer ingreso / contraseña restablecida: no se puede usar nada más
  // hasta cambiarla (el backend también lo bloquea, ver Program.cs).
  if (auth.autenticado && auth.debeCambiarPassword && to.name !== 'cambiar-password') {
    return { name: 'cambiar-password' }
  }
  if (to.name === 'cambiar-password' && auth.autenticado && !auth.debeCambiarPassword) {
    return { path: '/pos' }
  }

  if (to.meta.permiso && !auth.tienePermiso(to.meta.permiso)) {
    return { name: 'pos' }
  }

  // /plataforma es el back-office del proveedor SaaS: ni siquiera un Dueño
  // normal entra acá, solo es_superadmin (el backend lo exige igual, con la
  // policy SoloPlataforma — esto es nada más para no mostrar la pantalla).
  if (to.meta.soloSuperadmin && !auth.esSuperadmin) {
    return { name: 'pos' }
  }

  if (to.name === 'login' && auth.autenticado) {
    return { path: '/pos' }
  }

  return true
})

export default router
