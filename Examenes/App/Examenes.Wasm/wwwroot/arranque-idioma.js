(function () {
    'use strict';

    var LAS_QUE_HAY = ['es-CR', 'en'];
    var LA_DEL_REGISTRO = 'es-CR';

    // EL NOMBRE DE LA INSTITUCION Y EL TITULO DEL MODULO NO SE TRADUCEN: son voz de la
    // institucion, asi que el titulo del documento esta escrito UNA vez y no una por idioma.
    // Lo que no esta en la tabla de abajo no se puede traducir por descuido.
    var LA_INSTITUCION = 'Laboratorio Clínico Delfos';
    var EL_TITULO = 'Mis exámenes · ' + LA_INSTITUCION;

    var ARRANQUE = {
        'es-CR': {
            cargando: 'Cargando',
            fallo: 'Ocurrió un error inesperado.',
            recargar: 'Recargar',
            descartar: 'Descartar este aviso'
        },
        'en': {
            cargando: 'Loading',
            fallo: 'Something unexpected went wrong.',
            recargar: 'Reload',
            descartar: 'Dismiss this notice'
        }
    };

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

    var etiqueta = laPrimeraQueHablamos();
    var dice = ARRANQUE[etiqueta] || ARRANQUE[LA_DEL_REGISTRO];

    document.documentElement.lang = etiqueta;
    document.title = EL_TITULO;

    // EL ARO DE CARGA se pinta con una variable de CSS porque su texto es `content:` de un
    // pseudo-elemento: no hay nodo al que ponerle textContent, y Blazor lo pisa con el
    // porcentaje en cuanto empieza a bajar el payload.
    document.documentElement.style.setProperty('--texto-cargando', '"' + dice.cargando + '"');

    function decirElCanalDeFallos() {
        var caja = document.getElementById('blazor-error-ui');
        if (!caja) return;

        var frase = caja.querySelector('.frase');
        var recargar = caja.querySelector('.reload');
        var descartar = caja.querySelector('.dismiss');

        if (frase) frase.textContent = dice.fallo;
        if (recargar) recargar.textContent = dice.recargar;
        if (descartar) descartar.setAttribute('aria-label', dice.descartar);
    }

    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', decirElCanalDeFallos);
    else decirElCanalDeFallos();
})();
