<script setup>
import { onMounted, watch } from 'vue'
import { RouterView } from 'vue-router'
import { Toaster } from 'vue-sonner'
import { useAuthStore } from '@/stores/auth'
import { useCatalogoSync } from '@/composables/useCatalogoSync'

const auth = useAuthStore()
const catalogoSync = useCatalogoSync()

onMounted(() => {
  if (auth.autenticado) catalogoSync.iniciar()
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
