<script setup>
import { computed, onMounted, ref } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { LogOut, ShieldAlert } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { obtenerEmpresaActual } from '@/api/empresa'
import TemaToggle from '@/components/ui/TemaToggle.vue'
import IndicadorConexion from '@/components/ui/IndicadorConexion.vue'
import TopBar from '@/components/layout/TopBar.vue'
import { navegacion } from './navegacion'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const navegacionVisible = computed(() =>
  navegacion.filter((item) => !item.permiso || auth.tienePermiso(item.permiso)),
)
const navegacionMovil = computed(() => navegacionVisible.value.filter((item) => !item.soloEscritorio))

// Nombre real de la empresa del usuario logueado, no un texto fijo — cada
// tenant debe ver el suyo (incluida "SysCenterS Plataforma" si es superadmin).
const nombreEmpresa = ref('')
// Rubros "de servicio" (talleres, consultorías) no manejan stock: el menú
// dice "Servicios" en vez de "Productos" — ver navegacion.js (labelServicio).
const esTipoServicio = ref(false)
onMounted(async () => {
  try {
    const empresa = await obtenerEmpresaActual()
    nombreEmpresa.value = empresa.nombre
    esTipoServicio.value = empresa.esTipoServicio
  } catch {
    // Sin conexión al montar el shell: se deja vacío, no es crítico.
  }
})

function etiquetaNav(item) {
  return esTipoServicio.value && item.labelServicio ? item.labelServicio : item.label
}

function cerrarSesion() {
  auth.cerrarSesion()
  router.push('/login')
}
</script>

<template>
  <div class="min-h-dvh bg-papel">
    <aside class="fixed left-0 top-0 hidden h-dvh w-64 flex-col border-r border-linea bg-papel md:flex">
      <div class="flex flex-col gap-3 p-6">
        <h1 class="font-display text-[17px] font-bold text-marca">{{ nombreEmpresa || 'SysCenterS' }}</h1>
        <IndicadorConexion />
      </div>

      <nav class="flex flex-1 flex-col gap-1 px-4" aria-label="Navegación principal">
        <RouterLink
          v-for="item in navegacionVisible"
          :key="item.to"
          :to="item.to"
          class="flex items-center gap-3 rounded-s px-4 py-3 text-tinta-2 transition-colors hover:bg-superficie-2"
          :class="{ 'bg-marca-tenue font-bold text-marca': route.path.startsWith(item.to) }"
        >
          <component :is="item.icon" class="h-5 w-5" />
          {{ etiquetaNav(item) }}
        </RouterLink>
      </nav>

      <!-- Back-office del proveedor SaaS: separado a propósito del resto de
           la navegación (borde punteado + acento peligro), visible solo para
           es_superadmin — no es "una pantalla más de la empresa". -->
      <div v-if="auth.esSuperadmin" class="px-4 pb-2">
        <RouterLink
          to="/plataforma"
          class="flex items-center gap-3 rounded-s border border-dashed border-peligro/40 px-4 py-3 text-peligro transition-colors hover:bg-peligro-tenue"
          :class="{ 'bg-peligro-tenue font-bold': route.path.startsWith('/plataforma') }"
        >
          <ShieldAlert class="h-5 w-5" />
          Plataforma
        </RouterLink>
      </div>

      <div class="flex items-center gap-2 border-t border-linea p-4">
        <button
          class="flex flex-1 items-center gap-3 rounded-s px-4 py-3 text-tinta-2 hover:bg-superficie-2"
          @click="cerrarSesion"
        >
          <LogOut class="h-5 w-5" />
          Cerrar sesión
        </button>
        <TemaToggle />
      </div>
    </aside>

    <TemaToggle class="fixed right-2 top-2 z-40 bg-papel md:hidden" />
    <IndicadorConexion compacto class="fixed left-2 top-2 z-40 md:hidden" />

    <TopBar class="fixed left-64 right-0 top-0 z-30 hidden md:flex" />

    <main class="min-h-dvh pb-28 md:ml-64 md:pb-0 md:pt-16">
      <RouterView />
    </main>

    <nav
      class="fixed bottom-0 left-0 z-50 flex w-full items-center justify-around border-t border-linea bg-superficie py-1 md:hidden"
      style="border-radius: 10px 10px 0 0"
      aria-label="Navegación móvil"
    >
      <RouterLink
        v-for="item in navegacionMovil"
        :key="item.to"
        :to="item.to"
        class="flex min-h-11 min-w-11 flex-col items-center justify-center gap-1 rounded px-3 py-2 text-tinta-2"
        :class="{ 'bg-marca-tenue text-marca': route.path.startsWith(item.to) }"
      >
        <component :is="item.icon" class="h-5 w-5" />
        <span class="text-[9px] font-bold uppercase tracking-wide">{{ etiquetaNav(item) }}</span>
      </RouterLink>
    </nav>
  </div>
</template>
