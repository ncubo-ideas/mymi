export function idiomas() {
    if (navigator.languages && navigator.languages.length) return Array.from(navigator.languages);
    return [navigator.language || ''];
}

export function fijarIdiomaDelDocumento(etiqueta) {
    document.documentElement.lang = etiqueta;
}
