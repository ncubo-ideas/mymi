export function theLanguagesAskedFor() {
    if (navigator.languages && navigator.languages.length) return Array.from(navigator.languages);
    return [navigator.language || ''];
}

export function setTheDocumentsLanguage(tag) {
    document.documentElement.lang = tag;
}

export function setTheWordsOfTheFailureBanner(wentWrong, reload) {
    const banner = document.getElementById('blazor-error-ui');
    if (!banner) return;

    const said = banner.querySelector('.said');
    if (said) said.textContent = wentWrong;

    const again = banner.querySelector('.reload');
    if (again) again.textContent = reload;
}
