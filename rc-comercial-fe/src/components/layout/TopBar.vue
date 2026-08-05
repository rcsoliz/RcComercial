<script setup>
import { computed, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { Settings, User } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { useCajaStore } from '@/stores/caja'

const auth = useAuthStore()
const caja = useCajaStore()

const inicialesUsuario = computed(() => {
  const partes = auth.nombreUsuario.trim().split(/\s+/).filter(Boolean)
  if (partes.length === 0) return ''
  return (partes[0][0] + (partes[partes.length - 1][0] || '')).toUpperCase()
})

onMounted(() => {
  if (!caja.yaConsultada) caja.cargarSesion()
})
</script>

<template>
  <!-- display (flex/hidden) lo define quien lo usa vía class, para no
       chocar con esa misma utilidad pasada desde afuera (AppShell). -->
  <header class="h-16 flex-shrink-0 items-center justify-between border-b border-linea bg-superficie px-6">
    <div class="flex flex-wrap items-center gap-3">
      <div class="flex items-center gap-2 rounded-chip bg-superficie-2 px-3 py-1.5">
        <span
          class="h-2 w-2 rounded-full"
          :class="caja.activa ? 'animate-pulse bg-exito' : 'bg-tinta-3'"
          aria-hidden="true"
        ></span>
        <span class="text-[11px] font-bold uppercase tracking-wide text-tinta-2">
          {{ caja.activa ? 'Caja activa' : 'Sin caja abierta' }}
        </span>
      </div>
      <div class="hidden h-4 w-px bg-linea sm:block" aria-hidden="true"></div>
      <div class="hidden items-center gap-1.5 sm:flex">
        <User class="h-4 w-4 text-tinta-3" />
        <span class="text-[13px] text-tinta-2">
          Operador: <span class="font-bold text-tinta">{{ auth.nombreUsuario || '—' }}</span>
        </span>
      </div>
    </div>
    <div class="flex items-center gap-4">
      <RouterLink
        :to="{ name: 'ajustes' }"
        class="text-tinta-2 transition-colors hover:text-marca"
        aria-label="Ajustes"
      >
        <Settings class="h-5 w-5" />
      </RouterLink>
      <div
        class="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-marca-tenue text-[11px] font-bold text-marca"
        :title="auth.nombreUsuario"
      >
        {{ inicialesUsuario }}
      </div>
    </div>
  </header>
</template>
