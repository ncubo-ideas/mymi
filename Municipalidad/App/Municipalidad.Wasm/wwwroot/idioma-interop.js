export function idiomas() {
    if (navigator.languages && navigator.languages.length) return Array.from(navigator.languages);
    return [navigator.language || ''];
}

export function fijarIdiomaDelDocumento(etiqueta) {
    document.documentElement.lang = etiqueta;
}

export function fijarTextosDeLaCascara(errorInesperado, recargar) {
    const caja = document.getElementById('blazor-error-ui');
    if (!caja) return;

    const mensaje = caja.querySelector('.mensaje');
    if (mensaje) mensaje.textContent = errorInesperado;

    const recarga = caja.querySelector('.reload');
    if (recarga) recarga.textContent = recargar;
}
