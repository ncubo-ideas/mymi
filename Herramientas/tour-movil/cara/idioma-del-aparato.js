// EL IDIOMA DEL APARATO SIMULADO. Esto NO es del módulo: es de la lámina, como `simulador.js`.
//
// `idioma.js` negocia con `navigator.languages` y elige `es-CR` o `en` (regla 11 de
// Metodologia/Guias/reglas-de-localizacion.md: «la Wasm sigue a la persona»). Y esa misma regla
// dice, con todas las letras, que **no hay ningún canal** que le diga a la carátula qué idioma
// quiere la persona: está listado en «lo que falta construir».
//
// Así que la lámina NO puede pedirle a la cara que hable en inglés, y no debe inventar el canal
// que falta: mostraría al producto haciendo algo que el producto no hace.
//
// Lo que sí puede hacer, y es lo correcto, es simular el APARATO. La lámina dibuja una
// computadora o un teléfono; el idioma del aparato es parte de lo que dibuja. Se redefine
// `navigator.languages` ANTES de que `idioma.js` lo lea, y la cara negocia como negociaría de
// verdad en un aparato configurado así. La regla se cumple: la carátula sigue a la persona.
//
// VA PRIMERO EN EL <head>, antes de `texto/*.js` y de `idioma.js`, porque idioma.js resuelve el
// catálogo al cargarse y no lo vuelve a mirar.
(function () {
    'use strict';

    var pedido = /[?&]lang=(es|en)/.exec(location.search);
    if (!pedido) return;

    var lenguas = pedido[1] === 'en' ? ['en-US', 'en'] : ['es-CR', 'es'];

    // `navigator.languages` es de sólo lectura, así que se reemplaza el getter. Si el navegador no
    // deja -- algunos viejos --, no pasa nada: la cara negocia con el idioma real y sigue andando.
    try {
        Object.defineProperty(navigator, 'languages', { get: function () { return lenguas; }, configurable: true });
        Object.defineProperty(navigator, 'language',  { get: function () { return lenguas[0]; }, configurable: true });
    } catch (e) { /* el aparato simulado se queda con el idioma del navegador */ }
})();
