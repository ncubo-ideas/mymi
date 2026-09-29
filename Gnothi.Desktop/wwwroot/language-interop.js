// THE DOCUMENT'S LANGUAGE, SET FROM C#. A module of its own and not an eval, for the same reason it
// is one in Municipalidad.Wasm: the content policy a hosted module is served under admits
// WebAssembly and does NOT admit eval, so an `eval` that works here today is a pattern somebody
// copies tomorrow where it does not.
//
// Who decides the language is C#, not this function: here only what was already resolved gets
// written. The page does not read navigator.language on its own -- two resolvers would be two
// sources of truth.
export function setLanguage(tag) {
    document.documentElement.lang = tag;
}
