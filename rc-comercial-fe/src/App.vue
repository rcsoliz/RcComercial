<script setup>
import { onMounted, watch } from 'vue'
import { RouterView } from 'vue-router'
import { Toaster } from 'vue-sonner'
import { useAuthStore } from '@/stores/auth'
import { useCatalogoSync } from '@/composables/useCatalogoSync'
import { useSincronizacion } from '@/composables/useSincronizacion'

const auth = useAuthStore()
const catalogoSync = useCatalogoSync()
const sincronizacion = useSincronizacion()

onMounted(() => {
  if (auth.autenticado) catalogoSync.iniciar()
  // La cola de ventas offline se sincroniza sola apenas hay conexión, sin
  // importar en qué pantalla esté el usuario — no depende del login actual
  // para EXISTIR (las ventas ya quedaron guardadas), solo para despacharse.
  sincronizacion.iniciar()
})

watch(
  () => auth.autenticado,
  (autenticado) => (autenticado ? catalogoSync.iniciar() : catalogoSync.detener()),
)
</script>

<template>
  <RouterView />
  <Toaster position="top-center" rich-colors close-button />
</template>
