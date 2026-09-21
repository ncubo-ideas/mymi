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

    // LO QUE ESTA LINEA DICE ES DEL IDIOMA DE QUIEN MIRA, no del Host.
    //
    // Estas seis frases estaban escritas en español acá adentro, y el pie de una cara en inglés
    // terminaba con «al día, en 69 ms» debajo de una pantalla entera en inglés. Medido el
    // 2026-09-18 en Monteverde y en La Guaria.
    //
    // El puente NO puede tener catálogo propio: lo pone el Host, es el mismo archivo en los cinco
    // módulos, y hacerlo cargar textos lo volvería otra cosa. Así que lo dice LA CARA, que ya
    // negoció el idioma y ya tiene sus dos catálogos: publica `window.TextoDelPuente` antes de
    // que este archivo cargue, y acá se lee tarde -- en cada uso -- para no depender del orden.
    //
    // SIN ELLA SIGUE HABLANDO, en español, que es el idioma del registro de las cinco
    // instituciones. Una cara vieja que no publique nada no se queda muda.
    //
    // EL NOMBRE DE LA LLAVE NO SE TRADUCE, ni siquiera en el módulo que está escrito en inglés:
    // es el cable, como `window.gnothi` y `consultar`.
    var respaldo = {
        sinPuente:      'esta página solo corre dentro de Mymi, en la forma híbrida: ' +
                        'no hay puente, y sin puente no tiene a quién preguntarle',
        sinPuenteCorto: 'sin puente: esta pantalla no corre dentro de Mymi',
        pidiendo:       'pidiéndole a Mymi que traiga lo nuevo…',
        alDia:          'al día, en {ms} ms · {dicho}',
        noAlcance:      'no se pudo llegar a tu institución: revisá tu conexión y volvé a intentar',
        noSePudo:       'no se pudo traer lo nuevo: volvé a intentar en un momento'
    };

    // POR QUE NO SE PUDO, EN DOS CASOS Y NO EN SIETE.
    //
    // Hasta el 2026-09-21 esta línea mostraba el texto de la excepción tal cual -- «no se pudo
    // traer lo nuevo (1204 ms): HttpRequestException: Connection failure» --, que es jerga, está
    // en inglés pase lo que pase, y en rojo parece que la app se rompió. Lo vio Geaninna en
    // Delfos, y salía igual en las cinco caras: la frase de alrededor ya estaba traducida, el
    // hueco no.
    //
    // DOS CASOS, PORQUE SON LOS DOS QUE CAMBIAN LO QUE ELLA PUEDE HACER: no se llegó a la
    // institución -- y entonces esperar y volver a intentar sirve -- o falló otra cosa, y no hay
    // nada que ella pueda hacer. Distinguir más pediría que el Host mandara un código, y eso es
    // otro contrato.
    //
    // LEE UN TEXTO QUE COMPONE NUESTRO PROPIO HOST (WebModule.Attend, «Tipo: mensaje»), y aun así
    // no depende de él: un tipo que no esté en la lista cae en la frase genérica. Esa es la
    // propiedad que hace aceptable el acoplamiento -- si el formato cambia, lo peor que pasa es
    // que la persona lea la frase de siempre, nunca que vuelva la jerga.
    //
    // Y LOS MILISEGUNDOS SE FUERON de la línea roja. En un fallo no le dicen nada a nadie; van a
    // la consola, con el texto técnico, que es donde sirven.
    var sinAlcance = [
        'HttpRequestException',    // no hubo con quién hablar
        'TaskCanceledException',   // el timeout de HttpClient llega así
        'TimeoutException',
        'SocketException',
        'WebException'
    ];

    function porQue(e, cuanto) {
        var crudo = e && e.message ? String(e.message) : String(e);

        // LO QUE YA DIJO EL PUENTE NO SE VUELVE A DECIR. `sinPuente` es una frase llana y ya
        // traducida; pasarla por acá la cambiaría por la genérica, que dice menos.
        if (e && e.delPuente) return crudo;

        // El log del Host ya lo tiene; esto es para quien abra devtools contra la cara.
        if (window.console && console.warn)
            console.warn('Mymi · no se pudo' + (cuanto ? ' (' + cuanto + ' ms)' : '') + ': ' + crudo);

        var tipo = crudo.split(':')[0].trim();
        return dice(sinAlcance.indexOf(tipo) >= 0 ? 'noAlcance' : 'noSePudo');
    }

    function dice(clave, huecos) {
        var suyas = window.TextoDelPuente || {};
        var frase = typeof suyas[clave] === 'string' ? suyas[clave] : respaldo[clave];
        for (var hueco in huecos)
            if (Object.prototype.hasOwnProperty.call(huecos, hueco))
                frase = frase.split('{' + hueco + '}').join(huecos[hueco]);
        return frase;
    }

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
                var sinEl = new Error(dice('sinPuente'));
                sinEl.delPuente = true;     // ya está en llano: porQue() la deja pasar
                rechazar(sinEl);
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
                decir(dice('sinPuenteCorto'), true);
                resolver(null); return;
            }
            decir(dice('pidiendo'));

            var id = 'p' + (++siguiente);
            var t0 = Date.now();
            pendientes[id] = {
                resolver: function (json) {
                    var dicho = json;
                    try {
                        var r = JSON.parse(json);
                        dicho = r && r.resultado ? r.resultado : json;
                    } catch (e) { /* se muestra crudo */ }
                    decir(dice('alDia', { ms: Date.now() - t0, dicho: String(dicho).slice(0, 200) }));
                    resolver(json);
                },
                rechazar: function (e) {
                    decir(porQue(e, Date.now() - t0), true);
                    resolver(null);
                }
            };
            try { window.gnothiPuente.pull(id, '0'); }
            // Y SI `pull` TIRA DE UNA, lo mismo: para la persona es el mismo hecho -- no llegó lo
            // nuevo --, y cuál de las dos formas falló se lee en la consola.
            catch (e) {
                delete pendientes[id];
                decir(porQue(e, Date.now() - t0), true);
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

        // QUE DECIRLE A LA PERSONA CUANDO UNA CONSULTA FALLA. Las caras imprimían `e.message`, y
        // eso es el texto de una excepción de .NET: «LanguageException: Upgrade 'semilla' differs
        // from the one previously applied to this actor…», con código adentro, en rojo y en
        // inglés. Quince lugares en las cinco caras, y ninguna pantalla debería enseñar eso.
        //
        // LA FRASE LA DA EL PUENTE Y EL LUGAR LO ELIGE LA CARA, que por eso esto devuelve un
        // string en vez de escribir en el DOM: una cara lo pone en su caja de lectura, otra en su
        // contador, y esa decisión es de ella.
        porQue: porQue,

        // Preguntar por una [View] y recibir el objeto ya parseado.
        pedir: function (vista, argumentos) {
            return consultar(vista, argumentos).then(function (json) { return JSON.parse(json); });
        },

        $: $, el: el, apunte: apunte
    };
})();
