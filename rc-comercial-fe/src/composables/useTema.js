import { ref } from 'vue'

const CLAVE = 'syscenters-tema'

const tema = ref(document.documentElement.getAttribute('data-tema') === 'noche' ? 'noche' : 'dia')

function aplicar(valor) {
  tema.value = valor
  if (valor === 'noche') {
    document.documentElement.setAttribute('data-tema', 'noche')
  } else {
    document.documentElement.removeAttribute('data-tema')
  }
  localStorage.setItem(CLAVE, valor)
}

export function useTema() {
  function alternar() {
    aplicar(tema.value === 'noche' ? 'dia' : 'noche')
  }

  function establecer(valor) {
    aplicar(valor === 'noche' ? 'noche' : 'dia')
  }

  return { tema, alternar, establecer }
}
