// EL IDIOMA DE ESTA CARA, negociado con el aparato igual que lo hace la app suelta.
//
// La app suelta lee `navigator.languages` -- por JSInterop, en `Textos.cs` -- y elige entre las dos
// que habla, con el espanol del registro como caida. Aca se hace lo MISMO y con la misma regla:
// una cara hibrida corre adentro de un WebView, asi que el aparato si esta y preguntarselo no ata
// nada al navegador que no estuviera atado ya.
//
// LO QUE NO SE NEGOCIA es la frase del dominio: esa la compone la superficie, que recibe la
// etiqueta como un parametro mas de la vista. Este archivo solo decide en que idioma habla la
// PANTALLA, y le pasa la etiqueta a quien redacta.
//
// La unica diferencia con el mismo archivo de Cargas es `data-texto-html`: esta institucion
// escribe frases con <strong> adentro -- son suyas, del catalogo, no del registro -- y pintarlas
// con textContent mostraria las etiquetas en crudo.
window.Idioma = (function () {
    'use strict';

    var LAS_QUE_HAY = ['es-CR', 'en'];
    var LA_DEL_REGISTRO = 'es-CR';

    function primaria(etiqueta) {
        if (!etiqueta) return '';
        var limpia = String(etiqueta).trim().toLowerCase();
        var guion = limpia.indexOf('-');
        return guion < 0 ? limpia : limpia.slice(0, guion);
    }

    function loQuePidioElDispositivo() {
        try {
            if (navigator.languages && navigator.languages.length)
                return Array.prototype.slice.call(navigator.languages);
            return [navigator.language || ''];
        } catch (e) {
            return [];
        }
    }

    function laPrimeraQueHablamos() {
        var pedidas = loQuePidioElDispositivo();
        for (var i = 0; i < pedidas.length; i++) {
            var lengua = primaria(pedidas[i]);
            for (var j = 0; j < LAS_QUE_HAY.length; j++)
                if (primaria(LAS_QUE_HAY[j]) === lengua) return LAS_QUE_HAY[j];
        }
        return LA_DEL_REGISTRO;
    }

    var todos = window.TextoDeLaCara || {};
    var etiqueta = laPrimeraQueHablamos();
    var catalogo = todos[etiqueta] || todos[LA_DEL_REGISTRO] || {};

    function texto(clave) {
        var partes = String(clave).split('.');
        var nodo = catalogo;
        for (var i = 0; i < partes.length; i++) {
            if (!nodo || typeof nodo !== 'object') return '{' + clave + '}';
            nodo = nodo[partes[i]];
        }
        return typeof nodo === 'string' ? nodo : '{' + clave + '}';
    }

    function con(clave, huecos) {
        var dicho = texto(clave);
        for (var hueco in huecos)
            if (Object.prototype.hasOwnProperty.call(huecos, hueco))
                dicho = dicho.split('{' + hueco + '}').join(huecos[hueco]);
        return dicho;
    }

    // LA COMA DECIMAL ES DEL IDIOMA y no del dato, asi que quien escribe una cifra necesita saber
    // en cual esta. Es la subetiqueta primaria, la misma con que `Frases.De` elige catalogo del
    // otro lado del puente: las dos mitades de la pantalla escriben 1234,5 o 1234.5 juntas.
    var lengua = typeof catalogo.idioma === 'string' ? catalogo.idioma : 'es';

    document.documentElement.lang = etiqueta;

    function pintarLoEstatico() {
        var nodos = document.querySelectorAll('[data-texto]');
        for (var i = 0; i < nodos.length; i++)
            nodos[i].textContent = texto(nodos[i].getAttribute('data-texto'));

        // LAS QUE TRAEN MARCADO. Son del catalogo de esta cara -- las escribe la institucion, no el
        // registro -- asi que entran tal cual. Ningun dato del journal pasa por aqui.
        var conMarcado = document.querySelectorAll('[data-texto-html]');
        for (var k = 0; k < conMarcado.length; k++)
            conMarcado[k].innerHTML = texto(conMarcado[k].getAttribute('data-texto-html'));

        var etiquetados = document.querySelectorAll('[data-texto-aria]');
        for (var j = 0; j < etiquetados.length; j++)
            etiquetados[j].setAttribute('aria-label', texto(etiquetados[j].getAttribute('data-texto-aria')));
    }

    return {
        etiqueta: etiqueta,
        lengua: lengua,
        texto: texto,
        con: con,
        pintarLoEstatico: pintarLoEstatico
    };
})();
