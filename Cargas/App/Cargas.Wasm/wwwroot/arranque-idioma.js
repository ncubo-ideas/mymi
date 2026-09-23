(function () {
    'use strict';

    var LAS_QUE_HAY = ['es-CR', 'en'];
    var LA_DEL_REGISTRO = 'es-CR';

    var ARRANQUE = {
        'es-CR': {
            titulo: 'Tablero · Estaciones Faro',
            cargando: 'Cargando',
            fallo: 'Ocurrió un error inesperado.',
            falloCorto: 'Ocurrió un error.',
            recargar: 'Recargar',
            descartar: 'Descartar este aviso'
        },
        'en': {
            titulo: 'Dashboard · Estaciones Faro',
            cargando: 'Loading',
            fallo: 'Something unexpected went wrong.',
            falloCorto: 'Something went wrong.',
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
    document.title = dice.titulo;

    var raiz = document.documentElement.style;
    raiz.setProperty('--texto-cargando', '"' + dice.cargando + '"');
    raiz.setProperty('--texto-fallo', '"' + dice.falloCorto + '"');

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
