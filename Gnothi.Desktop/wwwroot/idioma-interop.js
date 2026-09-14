// EL IDIOMA DEL DOCUMENTO, PUESTO DESDE C#. Un modulo propio y no un eval, por lo mismo que lo
// es en Municipalidad.Wasm: la politica de contenido con que se sirve un modulo hospedado admite
// WebAssembly y NO admite eval, asi que un `eval` que hoy anda aca es un patron que alguien copia
// manana donde no anda.
//
// Quien decide el idioma es C#, no esta funcion: aca solo se escribe lo que ya se resolvio. La
// pagina no lee navigator.language por su cuenta -- dos resolutores serian dos fuentes de verdad.
export function fijarIdioma(etiqueta) {
    document.documentElement.lang = etiqueta;
}
