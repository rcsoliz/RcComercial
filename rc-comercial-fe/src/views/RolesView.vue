<script setup>
import { computed, onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import { AlertTriangle, Lock, Plus, ShieldCheck } from 'lucide-vue-next'
import { crearRol, editarRol, listarPermisosCatalogo, listarRoles } from '@/api/roles'

const roles = ref([])
const permisos = ref([])
const cargando = ref(true)
const guardando = ref(false)
const errorGeneral = ref('')

const rolSeleccionado = ref(null) // null = nada elegido; { ...rol } = viendo/editando
const modoNuevo = ref(false)

const nombre = ref('')
const permisosElegidos = ref(new Set())

const permisosPorModulo = computed(() => {
  const grupos = new Map()
  for (const p of permisos.value) {
    if (!grupos.has(p.modulo)) grupos.set(p.modulo, [])
    grupos.get(p.modulo).push(p)
  }
  return [...grupos.entries()]
})

const editando = computed(() => modoNuevo.value || (rolSeleccionado.value && !rolSeleccionado.value.esSistema))
const soloLectura = computed(() => rolSeleccionado.value?.esSistema === true)

async function cargar() {
  cargando.value = true
  try {
    const [rls, prms] = await Promise.all([listarRoles(), listarPermisosCatalogo()])
    roles.value = rls
    permisos.value = prms
  } finally {
    cargando.value = false
  }
}

function nombrePermiso(id) {
  return permisos.value.find((p) => p.id === id)?.nombre ?? `#${id}`
}

function abrirNuevo() {
  modoNuevo.value = true
  rolSeleccionado.value = null
  nombre.value = ''
  permisosElegidos.value = new Set()
  errorGeneral.value = ''
}

function abrirRol(rol) {
  modoNuevo.value = false
  rolSeleccionado.value = rol
  nombre.value = rol.nombre
  permisosElegidos.value = new Set(rol.permisoIds)
  errorGeneral.value = ''
}

function cerrarPanel() {
  modoNuevo.value = false
  rolSeleccionado.value = null
}

function alternarPermiso(id) {
  if (permisosElegidos.value.has(id)) permisosElegidos.value.delete(id)
  else permisosElegidos.value.add(id)
  // Set no es reactivo por referencia: forzar el trigger.
  permisosElegidos.value = new Set(permisosElegidos.value)
}

async function guardar() {
  errorGeneral.value = ''
  if (!nombre.value.trim()) {
    errorGeneral.value = 'Ponle un nombre al rol.'
    return
  }
  guardando.value = true
  try {
    const comando = { nombre: nombre.value.trim(), permisoIds: [...permisosElegidos.value] }
    if (modoNuevo.value) {
      await crearRol(comando)
      toast.success('Rol creado')
    } else {
      await editarRol(rolSeleccionado.value.id, { id: rolSeleccionado.value.id, ...comando })
      toast.success('Rol guardado')
    }
    cerrarPanel()
    await cargar()
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorGeneral.value = mensajes?.join(' ') || 'No se pudo guardar el rol.'
  } finally {
    guardando.value = false
  }
}

onMounted(cargar)
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto flex w-full max-w-[900px] flex-col gap-6">
      <div class="flex items-center justify-between">
        <h2 class="font-display text-[24px] font-bold text-tinta">Roles y permisos</h2>
        <button
          type="button"
          class="flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
          @click="abrirNuevo"
        >
          <Plus class="h-5 w-5" />
          Nuevo rol
        </button>
      </div>

      <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">Cargando…</div>

      <template v-else>
        <div class="overflow-hidden rounded border border-linea bg-superficie">
          <ul>
            <li
              v-for="r in roles"
              :key="r.id"
              class="flex cursor-pointer items-center justify-between border-b border-linea px-6 py-4 transition-colors last:border-b-0 hover:bg-marca-tenue"
              tabindex="0"
              @click="abrirRol(r)"
              @keydown.enter="abrirRol(r)"
            >
              <div class="flex items-center gap-3">
                <component :is="r.esSistema ? Lock : ShieldCheck" class="h-4 w-4 text-tinta-3" />
                <div>
                  <p class="font-display text-[15px] font-semibold text-tinta">{{ r.nombre }}</p>
                  <p class="text-[12px] text-tinta-3">{{ r.permisoIds.length }} permisos</p>
                </div>
              </div>
              <span
                class="rounded-chip px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide"
                :class="r.esSistema ? 'bg-superficie-2 text-tinta-3' : 'bg-marca-tenue text-marca'"
              >
                {{ r.esSistema ? 'de sistema · solo lectura' : 'propio' }}
              </span>
            </li>
          </ul>
        </div>

        <div v-if="modoNuevo || rolSeleccionado" class="rounded border border-linea bg-superficie p-6">
          <div class="mb-5 flex items-center justify-between">
            <h3 class="font-display text-[17px] font-bold text-tinta">
              {{ modoNuevo ? 'Nuevo rol' : soloLectura ? rolSeleccionado.nombre : 'Editar rol' }}
            </h3>
            <button type="button" class="text-[13px] font-semibold text-tinta-2 hover:underline" @click="cerrarPanel">
              Cerrar
            </button>
          </div>

          <div v-if="soloLectura" class="mb-5 flex items-start gap-2 rounded-s bg-superficie-2 px-4 py-3 text-[13px] text-tinta-2">
            <AlertTriangle class="mt-0.5 h-4 w-4 flex-shrink-0 text-tinta-3" />
            Los roles de sistema (Dueño, Encargado, Vendedor) son de solo lectura: así "Vendedor" significa lo mismo
            en cualquier empresa. Crea un rol propio si necesitas otra combinación.
          </div>

          <div v-if="errorGeneral" class="mb-5 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
            {{ errorGeneral }}
          </div>

          <label v-if="!soloLectura" class="mb-5 flex flex-col gap-1.5">
            <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre del rol</span>
            <input
              v-model="nombre"
              type="text"
              class="min-h-11 max-w-[320px] rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
            />
          </label>

          <div class="flex flex-col gap-5">
            <div v-for="[modulo, items] in permisosPorModulo" :key="modulo">
              <p class="mb-2 text-[11px] font-bold uppercase tracking-wide text-tinta-3">{{ modulo }}</p>
              <div class="grid grid-cols-1 gap-2 sm:grid-cols-2">
                <template v-if="soloLectura">
                  <p
                    v-for="p in items.filter((p) => rolSeleccionado.permisoIds.includes(p.id))"
                    :key="p.id"
                    class="flex items-center gap-2 text-[13.6px] text-tinta"
                  >
                    <span class="h-1.5 w-1.5 rounded-full bg-marca"></span>
                    {{ p.nombre }}
                    <span v-if="p.esSensible" class="rounded-chip bg-peligro-tenue px-1.5 py-0.5 text-[10px] font-bold uppercase text-peligro">
                      sensible
                    </span>
                  </p>
                </template>
                <template v-else>
                  <label
                    v-for="p in items"
                    :key="p.id"
                    class="check flex cursor-pointer items-center gap-2 rounded-s border border-linea px-3 py-2.5 text-[13.6px] text-tinta transition-colors hover:bg-superficie-2"
                    :class="{ 'border-marca bg-marca-tenue': permisosElegidos.has(p.id) }"
                  >
                    <input
                      type="checkbox"
                      class="h-4 w-4"
                      :checked="permisosElegidos.has(p.id)"
                      @change="alternarPermiso(p.id)"
                    />
                    {{ p.nombre }}
                    <span v-if="p.esSensible" class="ml-auto rounded-chip bg-peligro-tenue px-1.5 py-0.5 text-[10px] font-bold uppercase text-peligro">
                      sensible
                    </span>
                  </label>
                </template>
              </div>
            </div>
          </div>

          <div v-if="!soloLectura" class="mt-6 flex justify-end gap-3 border-t border-linea pt-5">
            <button
              type="button"
              class="min-h-11 rounded-s border border-linea px-5 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie-2"
              @click="cerrarPanel"
            >
              Cancelar
            </button>
            <button
              type="button"
              :disabled="guardando"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
              @click="guardar"
            >
              {{ guardando ? 'Guardando…' : 'Guardar rol' }}
            </button>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>
