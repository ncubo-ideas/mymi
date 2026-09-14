export function theLanguagesAskedFor() {
    if (navigator.languages && navigator.languages.length) return Array.from(navigator.languages);
    return [navigator.language || ''];
}

export function setTheDocumentsLanguage(tag) {
    document.documentElement.lang = tag;
}
