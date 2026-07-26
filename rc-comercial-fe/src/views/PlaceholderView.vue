<script setup>
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <div class="p-6">
    <h1 class="font-display text-[19.2px] font-bold text-tinta">{{ route.meta.titulo }}</h1>
    <p class="mt-2 text-tinta-2">Esta pantalla se construye en una próxima sesión.</p>
  </div>
</template>
