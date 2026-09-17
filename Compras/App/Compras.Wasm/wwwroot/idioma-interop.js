export function idiomas() {
    if (navigator.languages && navigator.languages.length) return Array.from(navigator.languages);
    return [navigator.language || ''];
}

export function fijarIdiomaDelDocumento(etiqueta) {
    document.documentElement.lang = etiqueta;
}

export function fijarTextosDeLaCascara(errorInesperado, recargar, errorEnLaPantalla) {
    const caja = document.getElementById('blazor-error-ui');
    if (caja) {
        const mensaje = caja.querySelector('.mensaje');
        if (mensaje) mensaje.textContent = errorInesperado;

        const recarga = caja.querySelector('.reload');
        if (recarga) recarga.textContent = recargar;
    }

    document.documentElement.style.setProperty(
        '--texto-error-en-la-pantalla', JSON.stringify(errorEnLaPantalla));
}
