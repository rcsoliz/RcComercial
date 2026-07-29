<script setup>
import { onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import dayjs from 'dayjs'
import { Building2, Check, Copy, Plus } from 'lucide-vue-next'
import { cambiarActivoEmpresa, crearEmpresaPlataforma, listarEmpresasPlataforma } from '@/api/plataforma'

const vista = ref('listado') // 'listado' | 'alta'

// ── Listado ──
const empresas = ref([])
const estado = ref('activos')
const cargando = ref(true)

async function cargar() {
  cargando.value = true
  try {
    empresas.value = await listarEmpresasPlataforma(estado.value)
  } finally {
    cargando.value = false
  }
}

function elegirEstado(valor) {
  estado.value = valor
  cargar()
}

async function alternarActivo(empresa) {
  try {
    await cambiarActivoEmpresa(empresa.id, !empresa.activo)
    toast.success(empresa.activo ? 'Empresa suspendida' : 'Empresa reactivada')
    await cargar()
  } catch {
    toast.error('No se pudo cambiar el estado de la empresa.')
  }
}

function fmtFecha(f) {
  return f ? dayjs(f).format('DD/MM/YYYY') : 'Nunca'
}

// ── Wizard de alta (3 pasos) ──
const paso = ref(1)
const guardando = ref(false)
const errorGeneral = ref('')
const resultado = ref(null) // { dueno, passwordTemporal } tras crear
const copiado = ref(false)

const RUBROS = [
  { id: 1, nombre: 'Almacén / Tienda de barrio' },
  { id: 2, nombre: 'Farmacia' },
  { id: 3, nombre: 'Ferretería' },
  { id: 4, nombre: 'Licorería' },
  { id: 5, nombre: 'Minimarket' },
]

const form = ref({
  nombreEmpresa: '',
  nit: '',
  rubroId: 1,
  telefonoWhatsapp: '',
  nombreSucursal: 'Sucursal Central',
  nombreDueno: '',
  usuarioLoginDueno: '',
  telefonoWhatsappDueno: '',
})

function abrirAlta() {
  vista.value = 'alta'
  paso.value = 1
  errorGeneral.value = ''
  resultado.value = null
  form.value = {
    nombreEmpresa: '', nit: '', rubroId: 1, telefonoWhatsapp: '',
    nombreSucursal: 'Sucursal Central',
    nombreDueno: '', usuarioLoginDueno: '', telefonoWhatsappDueno: '',
  }
}

function volverAlListado() {
  vista.value = 'listado'
  cargar()
}

function siguientePaso() {
  errorGeneral.value = ''
  if (paso.value === 1 && !form.value.nombreEmpresa.trim()) {
    errorGeneral.value = 'Ponle un nombre a la empresa.'
    return
  }
  if (paso.value === 2 && !form.value.nombreSucursal.trim()) {
    errorGeneral.value = 'Ponle un nombre a la sucursal.'
    return
  }
  paso.value++
}

function pasoAnterior() {
  errorGeneral.value = ''
  paso.value--
}

async function confirmarAlta() {
  errorGeneral.value = ''
  if (!form.value.nombreDueno.trim() || !form.value.usuarioLoginDueno.trim()) {
    errorGeneral.value = 'Faltan datos del dueño.'
    return
  }
  guardando.value = true
  try {
    const creado = await crearEmpresaPlataforma({
      nombreEmpresa: form.value.nombreEmpresa.trim(),
      nit: form.value.nit.trim() || null,
      rubroId: Number(form.value.rubroId),
      telefonoWhatsapp: form.value.telefonoWhatsapp.trim() || null,
      nombreSucursal: form.value.nombreSucursal.trim(),
      nombreDueno: form.value.nombreDueno.trim(),
      usuarioLoginDueno: form.value.usuarioLoginDueno.trim(),
      telefonoWhatsappDueno: form.value.telefonoWhatsappDueno.trim() || null,
    })
    resultado.value = creado
    paso.value = 4
  } catch (error) {
    const mensajes = error.response?.data?.errores?.map((e) => e.mensaje)
    errorGeneral.value = mensajes?.join(' ') || 'No se pudo crear la empresa.'
  } finally {
    guardando.value = false
  }
}

async function copiarCredenciales() {
  const texto = `Usuario: ${resultado.value.dueno.usuarioLogin}\nContraseña temporal: ${resultado.value.passwordTemporal}`
  try {
    await navigator.clipboard.writeText(texto)
    copiado.value = true
    toast.success('Credenciales copiadas')
    setTimeout(() => (copiado.value = false), 2000)
  } catch {
    toast.error('No se pudo copiar. Selecciónalas y cópialas a mano.')
  }
}

onMounted(cargar)
</script>

<template>
  <div class="p-4 md:p-6">
    <div class="mx-auto flex w-full max-w-[820px] flex-col gap-6">
      <!-- ═══ LISTADO ═══ -->
      <template v-if="vista === 'listado'">
        <div class="mb-2 flex flex-wrap items-center justify-between gap-4">
          <div>
            <h2 class="font-display text-[24px] font-bold text-tinta">Plataforma</h2>
            <p class="text-[13px] text-tinta-2">Empresas (tenants) dadas de alta en el sistema.</p>
          </div>
          <button
            type="button"
            class="flex min-h-11 items-center gap-2 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
            @click="abrirAlta"
          >
            <Plus class="h-5 w-5" />
            Nueva empresa
          </button>
        </div>

        <div class="flex gap-1 self-start rounded-s bg-superficie-2 p-1">
          <button
            v-for="opcion in [
              { valor: 'activos', label: 'Activas' },
              { valor: 'inactivos', label: 'Suspendidas' },
              { valor: 'todos', label: 'Todas' },
            ]"
            :key="opcion.valor"
            type="button"
            class="min-h-9 rounded-chip px-3 text-[12.6px] font-semibold transition-colors"
            :class="estado === opcion.valor ? 'bg-superficie text-marca shadow-sm' : 'text-tinta-2 hover:text-tinta'"
            @click="elegirEstado(opcion.valor)"
          >
            {{ opcion.label }}
          </button>
        </div>

        <div v-if="cargando" class="rounded border border-linea bg-superficie p-8 text-center text-tinta-2">Cargando…</div>

        <div v-else-if="empresas.length === 0" class="flex flex-col items-center gap-3 rounded border border-linea bg-superficie px-6 py-16 text-center">
          <Building2 class="h-10 w-10 text-tinta-3" />
          <p class="text-[13.6px] text-tinta-2">No hay empresas en este filtro.</p>
        </div>

        <div v-else class="overflow-hidden rounded border border-linea bg-superficie">
          <div class="overflow-x-auto">
            <table class="w-full border-collapse text-left">
              <thead>
                <tr class="border-b border-linea bg-superficie-2">
                  <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Empresa</th>
                  <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Rubro</th>
                  <th class="px-6 py-3.5 text-right text-[10px] font-bold uppercase tracking-wide text-tinta-3">Usuarios</th>
                  <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Última venta</th>
                  <th class="px-6 py-3.5 text-[10px] font-bold uppercase tracking-wide text-tinta-3">Estado</th>
                  <th class="px-6 py-3.5"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="e in empresas" :key="e.id" class="border-b border-linea last:border-b-0">
                  <td class="px-6 py-4 align-middle">
                    <p class="font-display text-[15px] font-semibold text-tinta">{{ e.nombre }}</p>
                    <p v-if="e.nit" class="mt-0.5 font-mono text-[11px] text-tinta-3">{{ e.nit }}</p>
                  </td>
                  <td class="px-6 py-4 align-middle text-[13.6px] text-tinta-2">{{ e.rubroNombre }}</td>
                  <td class="px-6 py-4 text-right align-middle tabular-nums text-tinta-2">{{ e.nroUsuarios }}</td>
                  <td class="px-6 py-4 align-middle text-[13px] text-tinta-2">{{ fmtFecha(e.ultimaVenta) }}</td>
                  <td class="px-6 py-4 align-middle">
                    <span
                      class="inline-block rounded-chip px-2.5 py-0.5 text-[11px] font-bold uppercase tracking-wide"
                      :class="e.activo ? 'bg-exito-tenue text-exito' : 'bg-peligro-tenue text-peligro'"
                    >
                      {{ e.activo ? 'activa' : 'suspendida' }}
                    </span>
                  </td>
                  <td class="px-6 py-4 align-middle">
                    <button
                      type="button"
                      class="text-[12px] font-semibold hover:underline"
                      :class="e.activo ? 'text-peligro' : 'text-exito'"
                      @click="alternarActivo(e)"
                    >
                      {{ e.activo ? 'Suspender' : 'Reactivar' }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </template>

      <!-- ═══ WIZARD DE ALTA ═══ -->
      <template v-else>
        <div class="flex items-center justify-between">
          <h2 class="font-display text-[24px] font-bold text-tinta">Nueva empresa</h2>
          <button type="button" class="text-[13px] font-semibold text-tinta-2 hover:underline" @click="volverAlListado">
            {{ paso === 4 ? 'Volver al listado' : 'Cancelar' }}
          </button>
        </div>

        <div v-if="paso <= 3" class="flex items-center gap-2">
          <div v-for="n in 3" :key="n" class="flex items-center gap-2">
            <div
              class="flex h-8 w-8 items-center justify-center rounded-full text-[12.6px] font-bold"
              :class="n <= paso ? 'bg-marca text-sobre-marca' : 'bg-superficie-2 text-tinta-3'"
            >
              {{ n }}
            </div>
            <span class="text-[12.6px] font-semibold" :class="n <= paso ? 'text-tinta' : 'text-tinta-3'">
              {{ { 1: 'Empresa', 2: 'Sucursal', 3: 'Dueño' }[n] }}
            </span>
            <div v-if="n < 3" class="h-px w-8 bg-linea"></div>
          </div>
        </div>

        <div class="rounded border border-linea bg-superficie p-6">
          <div v-if="errorGeneral" class="mb-5 rounded-s bg-peligro-tenue px-4 py-3 text-[13px] text-peligro">
            {{ errorGeneral }}
          </div>

          <!-- Paso 1: Empresa -->
          <div v-if="paso === 1" class="flex flex-col gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre / razón social</span>
              <input
                v-model="form.nombreEmpresa"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
            </label>
            <div class="grid grid-cols-2 gap-4">
              <label class="flex flex-col gap-1.5">
                <span class="text-[0.8rem] font-semibold text-tinta-2">NIT (opcional)</span>
                <input
                  v-model="form.nit"
                  type="text"
                  class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
                />
              </label>
              <label class="flex flex-col gap-1.5">
                <span class="text-[0.8rem] font-semibold text-tinta-2">Rubro</span>
                <select
                  v-model="form.rubroId"
                  class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
                >
                  <option v-for="r in RUBROS" :key="r.id" :value="r.id">{{ r.nombre }}</option>
                </select>
              </label>
            </div>
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">WhatsApp de la empresa (opcional)</span>
              <input
                v-model="form.telefonoWhatsapp"
                type="text"
                placeholder="+59171234567"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
            </label>
          </div>

          <!-- Paso 2: Sucursal -->
          <div v-else-if="paso === 2" class="flex flex-col gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre de la sucursal inicial</span>
              <input
                v-model="form.nombreSucursal"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
              <span class="text-[11.6px] text-tinta-3">
                Se pueden agregar más sucursales después, desde Ajustes de la empresa.
              </span>
            </label>
          </div>

          <!-- Paso 3: Dueño -->
          <div v-else-if="paso === 3" class="flex flex-col gap-4">
            <label class="flex flex-col gap-1.5">
              <span class="text-[0.8rem] font-semibold text-tinta-2">Nombre del dueño</span>
              <input
                v-model="form.nombreDueno"
                type="text"
                class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
              />
            </label>
            <div class="grid grid-cols-2 gap-4">
              <label class="flex flex-col gap-1.5">
                <span class="text-[0.8rem] font-semibold text-tinta-2">Usuario de acceso</span>
                <input
                  v-model="form.usuarioLoginDueno"
                  type="text"
                  autocomplete="off"
                  class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 text-tinta outline-none focus:border-marca focus:bg-superficie"
                />
              </label>
              <label class="flex flex-col gap-1.5">
                <span class="text-[0.8rem] font-semibold text-tinta-2">WhatsApp del dueño (opcional)</span>
                <input
                  v-model="form.telefonoWhatsappDueno"
                  type="text"
                  placeholder="+59171234567"
                  class="min-h-11 rounded-s border-[1.5px] border-linea bg-superficie-2 px-3 tabular-nums text-tinta outline-none focus:border-marca focus:bg-superficie"
                />
              </label>
            </div>
            <p class="text-[11.6px] text-tinta-3">
              Se genera una contraseña temporal: el dueño deberá cambiarla al entrar por primera vez.
            </p>
          </div>

          <!-- Paso 4: Credenciales -->
          <div v-else class="flex flex-col items-center gap-4 text-center">
            <div class="flex h-14 w-14 items-center justify-center rounded-full bg-exito-tenue">
              <Check class="h-7 w-7 text-exito" />
            </div>
            <div>
              <p class="font-display text-[19.2px] font-bold text-tinta">Empresa creada</p>
              <p class="mt-1 text-[13.6px] text-tinta-2">
                Entrégale estas credenciales a <strong class="text-tinta">{{ form.nombreDueno }}</strong> por un canal
                seguro. No se van a volver a mostrar.
              </p>
            </div>
            <div class="w-full max-w-[360px] rounded-s border border-linea bg-superficie-2 p-4 text-left">
              <p class="text-[11px] font-bold uppercase tracking-wide text-tinta-3">Usuario</p>
              <p class="font-mono text-[15px] font-semibold text-tinta">{{ resultado.dueno.usuarioLogin }}</p>
              <p class="mt-3 text-[11px] font-bold uppercase tracking-wide text-tinta-3">Contraseña temporal</p>
              <p class="select-all font-mono text-[17px] font-bold tracking-wide text-tinta">{{ resultado.passwordTemporal }}</p>
            </div>
            <button
              type="button"
              class="flex min-h-11 items-center gap-2 rounded-s border border-linea bg-superficie px-4 text-[13px] font-semibold text-tinta-2 hover:bg-superficie-2"
              @click="copiarCredenciales"
            >
              <Copy class="h-4 w-4" />
              {{ copiado ? 'Copiadas' : 'Copiar usuario y contraseña' }}
            </button>
          </div>

          <div v-if="paso <= 3" class="mt-6 flex justify-between border-t border-linea pt-5">
            <button
              type="button"
              :disabled="paso === 1"
              class="min-h-11 rounded-s border border-linea px-5 text-[13.6px] font-semibold text-tinta-2 hover:bg-superficie-2 disabled:opacity-40"
              @click="pasoAnterior"
            >
              Atrás
            </button>
            <button
              v-if="paso < 3"
              type="button"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
              @click="siguientePaso"
            >
              Siguiente: {{ { 1: 'Sucursal', 2: 'Dueño' }[paso] }}
            </button>
            <button
              v-else
              type="button"
              :disabled="guardando"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover disabled:opacity-60"
              @click="confirmarAlta"
            >
              {{ guardando ? 'Creando…' : 'Crear empresa' }}
            </button>
          </div>
          <div v-else class="mt-6 flex justify-end border-t border-linea pt-5">
            <button
              type="button"
              class="min-h-11 rounded-s bg-marca px-5 font-display font-bold text-sobre-marca hover:bg-marca-hover"
              @click="volverAlListado"
            >
              Listo
            </button>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>
