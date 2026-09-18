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

    // EL NOMBRE DE LA MUNICIPALIDAD NO SE TRADUCE: es voz de la institucion, no texto de esta
    // caratula, asi que vive aca y no en los catalogos. Lo que no esta en el archivo no se puede
    // traducir por descuido.
    //
    // Lo escribe index.html en la banda, y esta aca ademas porque idioma.js es quien repone
    // document.title despues de negociar.
    var LA_INSTITUCION = 'MONTEVERDE Municipal Services';

    document.documentElement.lang = etiqueta;

    function pintarLoEstatico() {
        var nodos = document.querySelectorAll('[data-texto]');
        for (var i = 0; i < nodos.length; i++)
            nodos[i].textContent = texto(nodos[i].getAttribute('data-texto'));

        var etiquetados = document.querySelectorAll('[data-texto-aria]');
        for (var j = 0; j < etiquetados.length; j++)
            etiquetados[j].setAttribute('aria-label', texto(etiquetados[j].getAttribute('data-texto-aria')));

        document.title = LA_INSTITUCION;
    }

    return {
        etiqueta: etiqueta,
        institucion: LA_INSTITUCION,
        texto: texto,
        con: con,
        pintarLoEstatico: pintarLoEstatico
    };
})();
