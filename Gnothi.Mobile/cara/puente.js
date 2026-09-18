// EL PUENTE DE LA CARA HIBRIDA. Este archivo es IDENTICO en todos los modulos, byte por byte.
//
// Vive acá -- Gnothi.Mobile/cara/ -- y no en el módulo, porque el protocolo es del Host: una
// institución no debería tener que acertarle sola al orden de los callbacks para que su pantalla
// funcione. `armar-bundle.ps1` lo copia a `web/` al armar cualquier paquete híbrido.
//
// LO QUE PONE, Y NADA MAS: preguntar, recibir, y pedirle al cartero. El dibujo es de cada cara,
// y por eso no hay una sola línea de presentación acá adentro.
//
// ------------------------------------------------------------------------------------------
//
// LA CARA PREGUNTA; LA SUPERFICIE CONTESTA.
//
// En la forma hospedada la página ES la superficie: Gnothi le pregunta por `window.gnothi.call`
// y ella contesta. Acá es al revés, y es toda la diferencia de la forma híbrida: esta página no
// tiene motor ni dominio -- viajan al lado, en dominio/*.dll, y corren sobre el motor de Gnothi.
//
// EL VERBO ES `consultar`, y su respuesta NO vuelve por retorno. Los métodos que Android inyecta
// con @JavascriptInterface son SÍNCRONOS y la página se congelaría mientras el Host trabaja; iOS
// ni siquiera puede devolver un valor. Por eso todo el contrato contesta por callback, y la
// respuesta aterriza en `window.gnothi.respuesta`.
//
// `consultar` NO lleva el nombre del módulo a propósito: una cara solo puede preguntar por el
// suyo. Si lo llevara, una cara podría preguntar por el journal de otra institución y ninguna
// regla río abajo lo frenaría. Ver WebModule.Serve.
//
// Y LO QUE PIDE NO SON LAS TOOLS: son [View]. Las tools declaradas son lo que el asistente puede
// preguntar -- gruesas, en el manifiesto, y la persona las apaga; una pantalla necesita puntos y
// líneas de referencia, y eso el modelo no lo ve nunca.
window.Puente = (function () {
    'use strict';

    var pendientes = {};
    var siguiente = 0;

    // El buzón donde el Host deja las respuestas. Es el MISMO nombre que usa la forma hospedada
    // -- el mecanismo no cambia, cambia quién pregunta.
    window.gnothi = window.gnothi || {};
    window.gnothi.respuesta = function (id, json, error) {
        var espera = pendientes[id];
        if (!espera) return;
        delete pendientes[id];
        if (error) espera.rechazar(new Error(error)); else espera.resolver(json);
    };

    function consultar(vista, argumentos) {
        return new Promise(function (resolver, rechazar) {
            if (!window.gnothiPuente || !window.gnothiPuente.consultar) {
                rechazar(new Error(
                    'esta página solo corre dentro de Mymi, en la forma híbrida: ' +
                    'no hay puente, y sin puente no tiene a quién preguntarle'));
                return;
            }
            var id = 'c' + (++siguiente);
            pendientes[id] = { resolver: resolver, rechazar: rechazar };
            try {
                window.gnothiPuente.consultar(id, vista, JSON.stringify(argumentos || {}));
            } catch (e) {
                delete pendientes[id];
                rechazar(e);
            }
        });
    }

    // PEDIRLE AL CARTERO, que es Gnothi. La cara NO sale a la red -- no puede, y no le hace
    // falta: le pide a Gnothi que traiga lo nuevo, y en esta forma Gnothi además se lo entrega a
    // la superficie, que es la única que puede asentarlo.
    //
    // El cursor va en 0 y no es un descuido: el cursor de verdad lo pone el actor saliendo del
    // journal, del otro lado del puente. Esta página no sabe por dónde iba ni tiene por qué.
    //
    // `decir` recibe el estado en palabras. Sin eso, «no hay datos», «el cartero no pudo» y «no
    // hay puente» se ven exactamente igual, y en un teléfono que inunda el log esa línea es el
    // único instrumento que queda.
    function refrescar(decir) {
        decir = decir || function () {};
        return new Promise(function (resolver) {
            if (!window.gnothiPuente || !window.gnothiPuente.pull) {
                decir('sin puente: esta pantalla no corre dentro de Mymi', true);
                resolver(null); return;
            }
            decir('pidiéndole a Mymi que traiga lo nuevo…');

            var id = 'p' + (++siguiente);
            var t0 = Date.now();
            pendientes[id] = {
                resolver: function (json) {
                    var dicho = json;
                    try {
                        var r = JSON.parse(json);
                        dicho = r && r.resultado ? r.resultado : json;
                    } catch (e) { /* se muestra crudo */ }
                    decir('al día, en ' + (Date.now() - t0) + ' ms · ' + String(dicho).slice(0, 200));
                    resolver(json);
                },
                rechazar: function (e) {
                    decir('no se pudo traer lo nuevo (' + (Date.now() - t0) + ' ms): ' +
                        (e && e.message ? e.message : e), true);
                    resolver(null);
                }
            };
            try { window.gnothiPuente.pull(id, '0'); }
            catch (e) {
                delete pendientes[id];
                decir('pull tiró: ' + (e && e.message ? e.message : e), true);
                resolver(null);
            }
        });
    }

    // `listo()` primero y siempre: hasta que la página lo diga, el Host no le pregunta nada. En
    // la forma híbrida además no hay actor que hidratar -- vive del otro lado -- así que se avisa
    // apenas el documento existe.
    function listo() {
        if (window.gnothiPuente && window.gnothiPuente.listo) window.gnothiPuente.listo();
    }

    // ---------------------------------------------------------------- lo mínimo del DOM
    // Tres funciones que toda cara escribe igual. No son presentación: son no repetirlas.

    function $(id) { return document.getElementById(id); }

    function el(tag, clase, texto) {
        var n = document.createElement(tag);
        if (clase) n.className = clase;
        if (texto !== undefined && texto !== null) n.textContent = texto;
        return n;
    }

    function apunte(caja, texto, malo) {
        if (!caja) return;
        caja.innerHTML = '';
        caja.appendChild(el('p', malo ? 'apunte malo' : 'apunte', texto));
    }

    return {
        listo: listo,
        refrescar: refrescar,

        // Preguntar por una [View] y recibir el objeto ya parseado.
        pedir: function (vista, argumentos) {
            return consultar(vista, argumentos).then(function (json) { return JSON.parse(json); });
        },

        $: $, el: el, apunte: apunte
    };
})();
