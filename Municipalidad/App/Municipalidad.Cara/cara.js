// LAS SEIS PREGUNTAS DE LA MUNICIPALIDAD, portadas de Municipalidad.Wasm a JS plano.
//
// El protocolo NO esta aca: vive en puente.js, que lo pone el Host y es identico en todos los
// modulos. Este archivo es solo el dibujo.
//
// ESTE MODULO NO ES UN TABLERO. Los otros cuatro tienen una pantalla de inicio que lo muestra
// todo; este tiene SEIS PREGUNTAS, cada una su propia pagina en la app suelta. Una cara hibrida es
// un solo index.html, sin router, asi que las seis viven juntas y aca se muestra una a la vez.
//
// LO QUE PIDE SON [View], no tools. Las dos tools declaradas son lo que el asistente puede
// preguntar; estas seis son lo que la pantalla necesita para pintar.
//
// LA APP SUELTA ES LA FUENTE DE VERDAD, y esta cara la sigue en tres cosas que antes no seguia:
//
//  - EL IDIOMA. La etiqueta se negocia con el aparato en idioma.js y viaja en CADA pedido, asi que
//    la frase que compone el dominio vuelve en el idioma de la persona y no en el del registro.
//  - LAS PALABRAS. Todo lo visible sale del catalogo, copiado del de la app suelta: dos pantallas
//    del mismo modulo que dicen distinto son dos apps.
//  - LAS CIFRAS. `Frases.Monto` escribe SIN simbolo de moneda, sin separador de miles y con la
//    coma del idioma. Esta cara escribia `₡36 000` mientras la frase de al lado -- compuesta por el
//    dominio, en la misma tarjeta -- decia `36000`: la misma plata escrita de dos maneras en el
//    mismo renglon.
//
// Y LOS CODIGOS DEL DOMINIO NO SE IMPRIMEN. `cambio-de-tarifa` y `con-historia` son identidad y
// cruzan el puente sin tocarse; lo que una persona lee sale de `etiqueta.*`, que es la misma tabla
// que usa la app suelta (LaRedaccion.Etiqueta).
(function () {
    'use strict';

    var P = window.Puente;
    var I = window.Idioma;
    var $ = P.$;

    // LO ESTATICO SE DICE YA, y no en arrancar(): el markup de arriba ya esta parseado cuando este
    // script corre, y esperar a DOMContentLoaded seria pintar en un idioma para repintar en el otro.
    I.pintarLoEstatico();

    // Que pregunta se esta mirando, y cuales ya se pidieron. Se piden POR DEMANDA: abrir la cara no
    // tiene por que costar seis vueltas al Host cuando la persona va a leer una.
    var abierta = 'por-que-pago-mas';
    var traidas = {};

    // ---------------------------------------------------------------- formato

    function texto(t) {
        return String(t === undefined || t === null ? '' : t)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    // UNA FRASE DEL CATALOGO, escapada. Lo que viene del registro pasa por `texto()` igual: aca no
    // hay distincion de confianza, hay una sola manera de meter texto en el HTML.
    function T(clave) { return texto(I.texto(clave)); }

    function C(clave, huecos) { return texto(I.con(clave, huecos)); }

    // LAS POCAS QUE TRAEN MARCADO. Son frases de este catalogo -- las escribio la institucion, con
    // su <strong> adentro --, no datos del journal, asi que entran tal cual. Los huecos se escapan
    // en quien llama, porque esos si vienen de la vista.
    function M(clave, huecos) { return I.con(clave, huecos || {}); }

    // EL MISMO RENDERIZADOR QUE `Frases.Monto`, del otro lado del puente: los digitos que trae la
    // columna tipada, SIN simbolo de moneda y SIN separador de miles, con la coma del idioma. La
    // cifra de la tarjeta y la de la frase que la explica tienen que ser el mismo texto.
    function monto(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';
        var escrito = String(Math.round(x * 100) / 100);
        return I.lengua === 'en' ? escrito : escrito.replace('.', ',');
    }

    // `Frases.Porcentaje`: un decimal, y el espacio antes del signo es del espanol.
    function porcentaje(p) {
        var x = Number(p);
        if (!isFinite(x)) return '—';
        var escrito = String(Math.round(x * 1000) / 10);
        return (I.lengua === 'en' ? escrito : escrito.replace('.', ',')) +
               (I.lengua === 'en' ? '%' : ' %');
    }

    // `Frases.Dias`: entero, para que «1 días» no aparezca nunca en ningun idioma.
    function dias(n) {
        return Number(n) === 1 ? I.texto('comun.unDia') : I.con('comun.dias', { dias: n });
    }

    // LA URGENCIA LA DECIDE EL DOMINIO y aca solo se pinta. Los codigos son los que emite este
    // modulo -- ninguna | aviso | urgente -- y no prefijos adivinados: `parte()` matcheaba 'venc',
    // 'alta' y 'media', que esta institucion no escribe nunca, asi que TODO caia en neutra y un
    // periodo urgente se veia igual que uno tranquilo.
    function parte(urgencia) {
        var u = String(urgencia || '').toLowerCase();
        if (u === 'urgente') return 'parte mala';
        if (u === 'aviso') return 'parte tibia';
        return 'tarjeta';
    }

    // COMO SE DICE UN CODIGO DEL DOMINIO, con el mismo criterio que `LaRedaccion.Etiqueta`: un
    // codigo sin dicho se dice con su propio codigo, que es lo que esta pantalla mostraba antes de
    // que existiera la tabla.
    function comoSeDice(codigo) {
        if (!codigo) return '';
        var dicho = I.texto('etiqueta.' + codigo);
        return dicho.charAt(0) === '{' ? codigo : dicho;
    }

    // El sello de la app suelta. 'ninguna' se dice con la cadena vacia y entonces no se dibuja: un
    // sello que dijera «ninguna» en cada renglon volveria ruido la calma.
    function sello(codigo, clase) {
        var dicho = comoSeDice(codigo);
        if (!dicho) return '';
        return '<span class="sello sello--' + texto(clase || codigo) + '">' + texto(dicho) + '</span>';
    }

    function llave(nombre) { return '<span class="llave">' + texto(nombre) + '</span>'; }

    function periodo(dicho) { return '<span class="periodo">' + dicho + '</span>'; }

    function bloque(titulo, dentro) {
        return '<section class="bloque">' +
               (titulo ? '<h2>' + texto(titulo) + '</h2>' : '') + dentro + '</section>';
    }

    function nada(frase) { return '<p class="nota">' + texto(frase) + '</p>'; }

    // Una nota ya armada, con sus cifras adentro.
    function nota(armada) { return '<p class="nota">' + armada + '</p>'; }

    // LA FRASE DEL DOMINIO, dicha y no rearmada. `Apagada` es cuando explica una ausencia en vez de
    // contestar la pregunta; es la misma parte que la app suelta (Partes/Frase.razor).
    function frase(dice, apagada) {
        return '<p class="frase' + (apagada ? ' frase--apagada' : '') + '">' + texto(dice) + '</p>';
    }

    // EL AVISO DE LA APP SUELTA, con su markup real (Partes/Aviso.razor). Las clases estan en la
    // hoja de la institucion -- que viaja tal cual --, asi que no hay layout nuevo que inventar:
    // son las advertencias de doctrina que sin esto se perdian al portar.
    function aviso(tono, titulo, cuerpo) {
        return '<div class="aviso aviso--' + tono + '">' +
               '<h3>' + texto(titulo) + '</h3>' +
               '<div class="aviso__cuerpo">' + cuerpo + '</div></div>';
    }

    // ---------------------------------------------------------------- las seis
    //
    // CADA UNA PINTA LO QUE EL DOMINIO YA REDACTO. El campo `dice` de cada fila es la frase que
    // escribio el dominio; esta pantalla la ubica y le pone el color de su urgencia, pero no la
    // reescribe ni la resume. Es la misma regla que en los otros cuatro modulos.

    function porQuePagoMas(r) {
        var v = (r && r.variacion) || {};
        var rubros = v.rubros || [];

        // LOS UMBRALES CON QUE SE MIDIO, que nadie de esta pantalla eligio. La app suelta los dice
        // arriba de todo, y sin ellos «salto» es una palabra sin escala.
        var html = nota(C('porQuePagoMas.umbrales', {
            salto: porcentaje(v.umbralDeSalto),
            ruido: porcentaje(v.ruido)
        }));

        html += rubros.map(function (x) {
            if (!x.sePuedeVer)
                return '<div class="tarjeta"><div class="tarjeta__cabeza">' + llave(x.rubro) +
                       '</div>' + frase(x.dice, true) + '</div>';

            return '<div class="tarjeta">' +
                '<div class="tarjeta__cabeza">' + llave(x.rubro) +
                sello(x.explicacion, x.esSalto ? 'salto' : 'quieto') + '</div>' +
                frase(x.dice) +
                '<p class="nota mono">' +
                    T('porQuePagoMas.periodos') + ' ' + texto(x.desde) + ' → ' + texto(x.hasta) +
                    ' · ' + T('porQuePagoMas.porPeriodo') + ' ' + monto(x.antes) + ' → ' + monto(x.ahora) +
                    ' · ' + T('porQuePagoMas.elCambio') + ' ' + porcentaje(x.proporcion) +
                '</p></div>';
        }).join('');

        if (!v.algunoSePuedeVer)
            html += aviso('tibia', I.texto('porQuePagoMas.sinVariacion.titulo'),
                '<p>' + T('porQuePagoMas.sinVariacion.cuerpo') + '</p>');

        return html;
    }

    function queSeMeVence(r) {
        var v = (r && r.vence) || {};
        var porVencer = v.porVencer || [], vencidos = v.vencidos || [];

        var html = nota(C('queSeMeVence.umbrales', {
            dias: dias(v.diasDeAviso),
            tasa: porcentaje(v.tasaConQueSeProyecto)
        }));

        // POR VENCER PRIMERO, en el orden de la app suelta: es lo unico de esta pantalla con lo que
        // todavia se puede hacer algo.
        //
        // Y CADA LISTA DICE LO SUYO CUANDO ESTA VACIA. Antes, con las dos vacias, esta cara escribia
        // «Tu cuenta está al día», que es justo lo que el dominio se niega a decir: el portal es la
        // autoridad sobre la deuda, y esta app solo sabe lo que ha visto.
        html += bloque(I.texto('queSeMeVence.porVencer'), porVencer.length
            ? porVencer.map(function (x) {
                return '<div class="' + parte(x.urgencia) + '">' +
                    '<div class="tarjeta__cabeza">' + llave(x.rubro) + periodo(texto(x.periodo)) +
                    sello(x.urgencia) + '</div>' +
                    frase(x.dice) +
                    '<p class="nota mono">' +
                        T('queSeMeVence.vence') + ' ' + texto(x.vence) +
                        ' · ' + T('queSeMeVence.faltan') + ' ' + texto(dias(x.faltan)) +
                        ' · ' + T('queSeMeVence.monto') + ' ' + monto(x.monto) +
                    '</p></div>';
            }).join('')
            : nada(I.con('queSeMeVence.nadaPorVencer', {
                dia: v.preguntadoEl,
                dias: dias(v.diasDeAviso)
            })));

        html += bloque(I.texto('queSeMeVence.yaVencidos'), vencidos.length
            ? vencidos.map(function (x) {
                return '<div class="' + parte(x.urgencia) + '">' +
                    '<div class="tarjeta__cabeza">' + llave(x.rubro) + periodo(texto(x.periodo)) +
                    sello(x.urgencia) + '</div>' +
                    frase(x.dice) +
                    '<p class="nota mono">' +
                        T('queSeMeVence.vencio') + ' ' + texto(x.vencio) +
                        ' · ' + T('queSeMeVence.deAtraso') + ' ' + texto(dias(x.dias)) +
                        ' · ' + T('queSeMeVence.monto') + ' ' + monto(x.monto) +
                        ' · ' + T('queSeMeVence.proyeccionDeMora') + ' ' + monto(x.costoProyectado) +
                    '</p></div>';
            }).join('')
            : nada(I.texto('queSeMeVence.nadaVencido')));

        html += aviso('quieta', I.texto('queSeMeVence.noSeSuman'),
            '<p>' + T('queSeMeVence.noSeSumanCuerpo') + '</p>');

        html += nota(C('comun.contestadoAl', { dia: v.preguntadoEl }));

        return html;
    }

    function cuantoApartar(r) {
        var v = (r && r.reserva) || {};

        var html = bloque(I.texto('cuantoApartar.laPropuesta'), '<div class="tarjeta">' +
            (v.sePuedeProponer
                ? '<div class="tarjeta__cabeza">' + periodo(T('cuantoApartar.porMes')) +
                  '<span class="cifra">' + monto(v.propuestoPorMes) + '</span></div>'
                : '') +
            frase(v.diceLaPropuesta, !v.sePuedeProponer) +
            (v.sePuedeProponer
                ? '<p class="nota mono">' + C('cuantoApartar.proyeccionDe', { anio: v.anio }) +
                  ' ' + monto(v.proyeccionDelAnio) + '</p>'
                : '') +
            '</div>');

        // LO ADOPTADO ES DE LA PERSONA, y su frase tambien la compone el dominio -- diga que falta
        // tanto, o diga por que todavia no hay faltante que ver. Antes esta cara reemplazaba las dos
        // por texto propio, que es la unica cosa que una cara no puede hacer con una frase ajena.
        html += bloque(I.texto('cuantoApartar.loAdoptado'), '<div class="' + parte(v.urgencia) + '">' +
            '<div class="tarjeta__cabeza">' +
            (v.hayReservaAdoptada
                ? periodo(T('cuantoApartar.adoptadoPorMes')) +
                  '<span class="cifra">' + monto(v.adoptadoPorMes) + '</span>'
                : '') +
            sello(v.urgencia) + '</div>' +
            frase(v.diceElFaltante, !v.hayReservaAdoptada) +
            (v.hayReservaAdoptada
                ? '<p class="nota mono">' +
                      T('cuantoApartar.leFalta') + ' ' + monto(v.faltante) +
                      ' (' + porcentaje(v.proporcionDelFaltante) + ')' +
                      ' · ' + T('cuantoApartar.venceEsteMes') + ' ' + monto(v.venceEsteMes) +
                  '</p>'
                // ADOPTAR ES UNA ESCRITURA, y `consultar` solo alcanza [View]. La cara lo dice en vez
                // de ofrecer un boton que no puede cumplir.
                : nada(I.texto('cara.adoptarNoEstaAqui'))) +
            '</div>' +
            nota(C('cuantoApartar.umbral', { umbral: porcentaje(v.umbralConQueSeAvisa) })));

        html += nota(C('comun.contestadoAl', { dia: v.preguntadoEl }));

        return html;
    }

    function sinComprobante(r) {
        var v = (r && r.sinComprobante) || {};

        // NO PODER COTEJAR ES UN TERCER ESTADO y no es estar en orden. La frase de por que no se
        // puede la escribio el dominio; el aviso que lo subraya es el de la app suelta.
        if (!v.hayAlgunaConsulta)
            return aviso('tibia', I.texto('sinComprobante.sinConsulta'),
                frase(v.dice, true) + '<p>' + M('sinComprobante.sinConsultaCuerpo') + '</p>');

        var periodos = v.periodos || [];

        var html = '<div class="tarjeta"><div class="tarjeta__cabeza">' +
            sello(v.estado) +
            periodo(C('sinComprobante.cotejadoContra', { dia: v.diaDeLaConsulta })) + '</div>' +
            frase(v.dice) + '</div>';

        html += periodos.map(function (x) {
            return '<div class="tarjeta"><div class="tarjeta__cabeza">' + llave(x.rubro) +
                periodo(texto(x.periodo)) + '</div>' + frase(x.dice) + '</div>';
        }).join('');

        if (periodos.length)
            html += aviso('quieta', I.texto('sinComprobante.noEsDeuda'),
                '<p>' + T('sinComprobante.noEsDeudaCuerpo') + '</p>');

        html += nota(C('sinComprobante.fotoConFecha', { dia: v.diaDeLaConsulta }));

        return html;
    }

    function cuantoHePagado(r) {
        var v = (r && r.pagado) || {};
        var series = v.series || [];

        var html = '<div class="tarjeta"><div class="tarjeta__cabeza">' +
            periodo(C('cuantoHePagado.pagadoEn', { anio: v.anio })) +
            '<span class="cifra">' + monto(v.totalDelAnio) + '</span></div>' +
            (!v.hayAlgunComprobante
                ? nada(I.texto('cuantoHePagado.sinNingunComprobante'))
                : v.elAnioPedidoNoTieneComprobantes
                    ? nada(I.con('cuantoHePagado.anioSinComprobantes', { anio: v.anio }))
                    : '') +
            '</div>';

        // LOS AÑOS QUE SE PUEDEN PEDIR los dice el dominio: la cara no los adivina de las fechas.
        var anios = v.aniosQueSePuedenPedir || [];
        if (anios.length > 1)
            html += '<div class="botones" id="anios">' + anios.map(function (a) {
                return '<button class="suave" type="button" data-anio="' + a + '"' +
                       (a === v.anio ? ' disabled' : '') + '>' + a + '</button>';
            }).join('') + '</div>';

        html += aviso('quieta', I.texto('cuantoHePagado.incluyeLaMora'),
            '<p>' + T('cuantoHePagado.incluyeLaMoraCuerpo') + '</p>');

        var hayInferida = false;

        html += series.map(function (s) {
            var lineas = s.lineas || [];

            return '<div class="tarjeta"><div class="tarjeta__cabeza">' + llave(s.rubro) +
                periodo(texto(comoSeDice(s.estado))) + '</div>' +
                frase(s.dice, lineas.length === 0) +
                lineas.map(function (l) {
                    if (l.inferido) hayInferida = true;

                    return '<p class="nota mono">' + texto(l.periodo) +
                        (l.inferido
                            ? ' <span class="sello sello--inferido">' + T('cuantoHePagado.inferido') + '</span>'
                            : '') +
                        ' · ' + monto(l.monto) +
                        (Number(l.mora) > 0
                            ? ' (' + T('cuantoHePagado.mora') + ' ' + monto(l.mora) + ')'
                            : '') +
                        // UNA LINEA PUEDE CUBRIR VARIOS PERIODOS, y entonces su monto no se compara
                        // con el de otra linea: para eso esta «por periodo», que es la columna con
                        // que la app suelta compara dos epocas.
                        (Number(l.cubre) > 1
                            ? ' · ' + T('cuantoHePagado.cubre') + ' ' + texto(l.cubre) +
                              ' · ' + T('cuantoHePagado.porPeriodo') + ' ' + monto(l.porPeriodo)
                            : '') +
                        '</p>';
                }).join('') + '</div>';
        }).join('');

        if (hayInferida)
            html += aviso('tibia', I.texto('cuantoHePagado.loQueSeInfirio'),
                '<p>' + T('cuantoHePagado.loQueSeInfirioCuerpo') + '</p>');

        return html;
    }

    function queTanAlDia(r) {
        var v = (r && r.alDia) || {};
        var rubros = v.rubros || [];
        var fincas = (v.fincas && v.fincas.fincas) || [];

        var html = bloque(I.texto('queTanAlDia.hastaDondeLeyo'),
            '<div class="tarjeta"><dl class="renglones">' +
                '<dt>' + T('queTanAlDia.ultimoHecho') + '</dt>' +
                '<dd class="cifra">' + texto(v.ultimoEventoLeido) + '</dd>' +
                '<dt>' + T('queTanAlDia.rubrosConComprobante') + '</dt>' +
                '<dd>' + C('comun.deTotal', {
                    cuantos: v.rubrosConHistoria, total: rubros.length
                }) + '</dd>' +
                '<dt>' + T('queTanAlDia.ultimaConsulta') + '</dt>' +
                '<dd>' + (v.hayAlgunaConsulta
                    ? texto(v.diaDeLaUltimaConsulta)
                    : T('queTanAlDia.ninguna')) + '</dd>' +
            '</dl></div>');

        // QUE LLEGO DE CADA RUBRO. La vista manda `nombre` y `tieneHistoria` y nada mas -- no hay
        // `estado` ni `dice` en esta fila --, asi que el sello lo compone la pantalla igual que la
        // app suelta. Antes se pedian dos campos que esta vista nunca tuvo, y la fila salia muda.
        html += bloque(I.texto('queTanAlDia.queLlego'), rubros.map(function (x) {
            return '<div class="tarjeta"><div class="tarjeta__cabeza">' + llave(x.nombre) +
                '<span class="sello sello--' +
                (x.tieneHistoria ? 'nada-sin-comprobante' : 'sin-consulta') + '">' +
                (x.tieneHistoria
                    ? T('queTanAlDia.conComprobantes')
                    : T('queTanAlDia.sinComprobantes')) + '</span></div>' +
                (x.tieneHistoria ? '' : nada(I.texto('queTanAlDia.rubroSinComprobantes'))) +
                '</div>';
        }).join(''));

        // QUE FALTA, que es la mitad honesta de esta pregunta: un numero que sube solo no dice si
        // falta algo. Los tres huecos ya viajaban en la vista y esta cara no los miraba.
        var falta = '';

        if (v.noFaltaNadaQueEstaPantallaSepaMirar)
            falta += aviso('quieta', I.texto('queTanAlDia.noFaltaNada'),
                '<p>' + T('queTanAlDia.noFaltaNadaCuerpo') + '</p>');

        if (!v.hayReservaAdoptada)
            falta += aviso('tibia', I.texto('queTanAlDia.faltaLaReserva'),
                '<p>' + T('queTanAlDia.faltaLaReservaCuerpo') +
                ' <a href="#" data-pregunta="cuanto-apartar">' +
                T('queTanAlDia.faltaLaReservaEnlace') + '</a>.</p>');

        if (!v.hayAlgunaConsulta)
            falta += aviso('tibia', I.texto('queTanAlDia.faltaUnaConsulta'),
                '<p>' + T('queTanAlDia.faltaUnaConsultaCuerpo') +
                ' <a href="#" data-pregunta="sin-comprobante">' + T('sinComprobante.titulo') + '</a> ' +
                M('queTanAlDia.faltaUnaConsultaCierre') + '</p>');

        if (Number(v.fincasSinAvaluoPublicado) > 0)
            falta += aviso('tibia', I.texto('queTanAlDia.faltaUnAvaluo'), '<p>' +
                M(fincas.length === 1
                    ? 'queTanAlDia.faltaUnAvaluoCuerpo.una'
                    : 'queTanAlDia.faltaUnAvaluoCuerpo.varias', {
                        cuantas: texto(v.fincasSinAvaluoPublicado),
                        total: texto(fincas.length)
                    }) + '</p>');

        // LAS FINCAS vienen adentro de esta vista y no aparte: son el sujeto de todo lo demas, y
        // sacarlas a su propia pregunta las dejaria sin el contexto que las explica.
        falta += fincas.map(function (f) {
            return '<div class="tarjeta"><div class="tarjeta__cabeza">' + llave(f.numero) +
                periodo(f.tieneAvaluo
                    ? C('porQuePagoMas.finca.avaluo', { monto: monto(f.avaluo) })
                    : T('porQuePagoMas.finca.sinAvaluo')) + '</div></div>';
        }).join('');

        html += bloque(I.texto('queTanAlDia.queFalta'), falta);

        html += nota(C('comun.contestadoAl', { dia: v.preguntadoEl }));

        return html;
    }

    var PINTORES = {
        'por-que-pago-mas': porQuePagoMas,
        'que-se-me-vence': queSeMeVence,
        'cuanto-apartar': cuantoApartar,
        'sin-comprobante': sinComprobante,
        'cuanto-he-pagado': cuantoHePagado,
        'que-tan-al-dia': queTanAlDia
    };

    // ---------------------------------------------------------------- el switch

    function mostrar(cual) {
        abierta = cual;

        var secciones = document.querySelectorAll('.pregunta');
        for (var i = 0; i < secciones.length; i++)
            secciones[i].hidden = secciones[i].getAttribute('data-pregunta') !== cual;

        var enlaces = document.querySelectorAll('#nav a');
        for (var j = 0; j < enlaces.length; j++)
            enlaces[j].classList.toggle('active', enlaces[j].getAttribute('data-pregunta') === cual);

        pedirLa(cual);
    }

    // SE PIDE UNA VEZ Y SE GUARDA. Volver a una pregunta ya vista no vuelve a cruzar el puente: lo
    // que hay en pantalla sigue siendo valido hasta que el cartero traiga algo, y ahi se borra todo.
    //
    // EL IDIOMA VIAJA EN CADA PEDIDO. La superficie cae al del registro cuando no le dicen, asi que
    // no mandarlo era pedir siempre en espanol; el Host liga los argumentos por nombre e ignora los
    // que sobran, y por eso va parejo en las seis aunque una de ellas no redacte ninguna frase.
    function pedirLa(cual) {
        if (traidas[cual]) return;
        traidas[cual] = true;

        var caja = $(cual);
        var argumentos = { idioma: I.etiqueta };
        if (cual === 'cuanto-he-pagado') argumentos.anio = 0;

        P.pedir(cual, argumentos)
            .then(function (r) {
                if (r && r.hayDatos === false) {
                    P.apunte(caja, I.texto('cara.sinNada'));
                    return;
                }
                caja.innerHTML = PINTORES[cual](r);
                if (cual === 'cuanto-he-pagado') cablearLosAnios();
            })
            .catch(function (e) {
                traidas[cual] = false;   // que se pueda reintentar al volver
                P.apunte(caja, e.message, true);
            });
    }

    // EL AÑO ES LA UNICA ELECCION DE ESTA CARA, y la lista la da el dominio.
    function cablearLosAnios() {
        var caja = $('anios');
        if (!caja) return;

        caja.addEventListener('click', function (e) {
            var b = e.target.closest('button[data-anio]');
            if (!b) return;

            P.pedir('cuanto-he-pagado', {
                anio: Number(b.getAttribute('data-anio')),
                idioma: I.etiqueta
            })
                .then(function (r) {
                    $('cuanto-he-pagado').innerHTML = cuantoHePagado(r);
                    cablearLosAnios();
                })
                .catch(function (err) { P.apunte($('cuanto-he-pagado'), err.message, true); });
        });
    }

    // ---------------------------------------------------------------- arranque

    function contar(t, malo) {
        var p = $('conexion');
        p.textContent = t;
        p.classList.toggle('malo', !!malo);
    }

    function repintar() {
        traidas = {};
        mostrar(abierta);
    }

    function releer() {
        var b = $('releer');
        b.disabled = true;
        b.setAttribute('aria-busy', 'true');
        b.textContent = I.texto('arranque.trayendo');

        P.refrescar(contar).then(function (r) {
            if (r) repintar();
            b.disabled = false;
            b.setAttribute('aria-busy', 'false');
            b.textContent = I.texto('arranque.traer');
        });
    }

    function arrancar() {
        P.listo();

        // EL CLICK SE ESCUCHA EN EL DOCUMENTO Y NO EN EL NAV, porque la app suelta cruza de una
        // pregunta a otra desde adentro de una respuesta -- «se adopta en cuánto apartar» --, y esos
        // enlaces se dibujan despues. Sigue sin haber router: `data-pregunta` es lo unico que
        // engancha, y el href es solo para que el navegador lo trate como un enlace.
        document.addEventListener('click', function (e) {
            var a = e.target.closest ? e.target.closest('a[data-pregunta]') : null;
            if (!a) return;
            e.preventDefault();   // no hay a donde navegar: la cara no tiene router
            mostrar(a.getAttribute('data-pregunta'));
        });

        $('releer').addEventListener('click', releer);

        // PRIMERO SE PINTA LO QUE YA HAY, y despues se refresca. Al reves, abrir con el publicador
        // caido dejaria la pantalla en blanco hasta que el pedido falle.
        mostrar(abierta);
        P.refrescar(contar).then(function (r) { if (r) repintar(); });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', arrancar);
    else arrancar();
})();
