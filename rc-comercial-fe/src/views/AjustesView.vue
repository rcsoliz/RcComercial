<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import { UserCog, ShieldCheck, Tag, Building, Settings2, ChevronRight } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { Permisos } from '@/utils/permisos'

const auth = useAuthStore()

const secciones = [
  { to: '/usuarios', label: 'Usuarios', desc: 'Altas, roles, restablecer contraseñas', icon: UserCog, permiso: Permisos.AdminUsuarios },
  { to: '/roles', label: 'Roles y permisos', desc: 'Qué puede hacer cada rol', icon: ShieldCheck, permiso: Permisos.AdminRoles },
  { to: '/catalogos', label: 'Categorías y marcas', desc: 'Catálogo de productos', icon: Tag, permiso: Permisos.ProductosCrearEditar },
  { to: '/sucursales', label: 'Sucursales', desc: 'Locales de tu negocio', icon: Building, permiso: Permisos.AdminSucursales },
  { to: '/configuracion', label: 'Configuración', desc: 'Datos de la empresa, ventas, WhatsApp', icon: Settings2, permiso: Permisos.AdminConfiguracion },
]

const seccionesVisibles = computed(() => secciones.filter((s) => auth.tienePermiso(s.permiso)))
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto flex w-full max-w-[640px] flex-col gap-6">
      <h2 class="font-display text-[24px] font-bold text-tinta">Ajustes</h2>

      <div v-if="seccionesVisibles.length === 0" class="rounded border border-linea bg-superficie px-6 py-16 text-center text-[13.6px] text-tinta-2">
        No tienes acceso a ninguna sección de administración.
      </div>

      <div v-else class="overflow-hidden rounded border border-linea bg-superficie">
        <RouterLink
          v-for="s in seccionesVisibles"
          :key="s.to"
          :to="s.to"
          class="flex items-center gap-4 border-b border-linea px-5 py-4 transition-colors last:border-b-0 hover:bg-marca-tenue"
        >
          <div class="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-s bg-superficie-2">
            <component :is="s.icon" class="h-5 w-5 text-tinta-2" />
          </div>
          <div class="min-w-0 flex-1">
            <p class="font-display text-[15px] font-semibold text-tinta">{{ s.label }}</p>
            <p class="text-[12.6px] text-tinta-2">{{ s.desc }}</p>
          </div>
          <ChevronRight class="h-4 w-4 flex-shrink-0 text-tinta-3" />
        </RouterLink>
      </div>
    </div>
  </div>
</template>
