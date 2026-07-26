<script setup>
import { Moon, Sun } from 'lucide-vue-next'
import { useTema } from '@/composables/useTema'

const { tema, alternar } = useTema()
</script>

<template>
  <button
    type="button"
    class="flex h-11 w-11 items-center justify-center rounded-s text-tinta-2 transition-colors hover:bg-superficie-2"
    :aria-label="tema === 'noche' ? 'Cambiar a tema día' : 'Cambiar a tema noche'"
    @click="alternar"
  >
    <Sun v-if="tema === 'noche'" class="h-5 w-5" />
    <Moon v-else class="h-5 w-5" />
  </button>
</template>
