import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

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
      { path: '', redirect: '/panel' },
      { path: 'panel', name: 'panel', component: () => import('@/views/PlaceholderView.vue'), meta: { titulo: 'Panel del dueño' } },
      { path: 'pos', name: 'pos', component: () => import('@/views/PlaceholderView.vue'), meta: { titulo: 'Punto de venta' } },
      { path: 'productos', name: 'productos', component: () => import('@/views/PlaceholderView.vue'), meta: { titulo: 'Productos' } },
      { path: 'compras', name: 'compras', component: () => import('@/views/PlaceholderView.vue'), meta: { titulo: 'Compras' } },
      { path: 'ajustes', name: 'ajustes', component: () => import('@/views/PlaceholderView.vue'), meta: { titulo: 'Ajustes' } },
    ],
  },
  { path: '/:pathMatch(.*)*', redirect: '/panel' },
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

  if (to.name === 'login' && auth.autenticado) {
    return { path: '/panel' }
  }

  return true
})

export default router
