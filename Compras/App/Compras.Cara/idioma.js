// EN QUE IDIOMA ABRE ESTA CARA, decidido con la MISMA REGLA que la app suelta: la primera de
// `navigator.languages` que este modulo hable, y si ninguna, el idioma del registro del
// supermercado. Es la regla de Compras.Wasm/Textos.cs -- LasQueHay = {"es-CR","en"}, caida a
// es-CR -- escrita otra vez porque aca no hay .NET que la corra, no porque sea otra regla.
//
// LA ETIQUETA VIAJA A LA SUPERFICIE. Lo que esta pantalla escribe sale de estos catalogos; lo
// que compone el DOMINIO -- «venís gastando 8% más que en agosto» -- lo dice la superficie, y
// para decirlo en el idioma correcto hay que pasarselo: `idioma: I.etiqueta` en cada pedido.
// Adentro de Gnothi no hay navegador que la superficie pueda consultar, y ese es justamente el
// punto de la forma hibrida.
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

    // UNA CLAVE QUE FALTA SE DICE CON SU PROPIO NOMBRE y no revienta la pantalla: una tarjeta a
    // la que le falta un rotulo sigue sirviendo, y el hueco se ve. Es la misma decision que toma
    // Frases.Diga del lado de .NET.
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

    var lengua = typeof catalogo.idioma === 'string' ? catalogo.idioma : 'es';

    // LA ETIQUETA DEL DOCUMENTO LA PONE ESTO, no el markup: el bundle se sirve igual para los dos
    // idiomas, asi que un `lang` escrito a mano seria falso para la mitad de quienes lo abren.
    document.documentElement.lang = etiqueta;

    // EL <title> NO PASA POR ACA a proposito: «La Guaria y vos» es voz de la institucion y no se
    // traduce, igual que en la app suelta, donde vive en Textos.TituloDelModulo y fuera del
    // catalogo -- justamente donde quien traduzca no lo ve.
    function pintarLoEstatico() {
        var nodos = document.querySelectorAll('[data-texto]');
        for (var i = 0; i < nodos.length; i++)
            nodos[i].textContent = texto(nodos[i].getAttribute('data-texto'));

        // CON MARCADO, Y SOLO EL NUESTRO. Hay frases del catalogo que traen su propio <strong> --
        // el aviso de que se esta guardando en este telefono es una --, y por textContent saldrian
        // escritas en vez de aplicadas. Lo que entra aca es SIEMPRE una frase de estos archivos,
        // nunca nada del registro del supermercado.
        var conMarcado = document.querySelectorAll('[data-texto-html]');
        for (var h = 0; h < conMarcado.length; h++)
            conMarcado[h].innerHTML = texto(conMarcado[h].getAttribute('data-texto-html'));

        var etiquetados = document.querySelectorAll('[data-texto-aria]');
        for (var j = 0; j < etiquetados.length; j++)
            etiquetados[j].setAttribute('aria-label', texto(etiquetados[j].getAttribute('data-texto-aria')));

        var titulados = document.querySelectorAll('[data-texto-titulo]');
        for (var k = 0; k < titulados.length; k++)
            titulados[k].setAttribute('title', texto(titulados[k].getAttribute('data-texto-titulo')));
    }

    return {
        etiqueta: etiqueta,
        lengua: lengua,
        texto: texto,
        con: con,
        pintarLoEstatico: pintarLoEstatico
    };
})();
