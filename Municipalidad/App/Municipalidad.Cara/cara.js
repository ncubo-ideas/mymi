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
(function () {
    'use strict';

    var P = window.Puente;
    var $ = P.$;

    // Que pregunta se esta mirando, y cuales ya se pidieron. Se piden POR DEMANDA: abrir la cara no
    // tiene por que costar seis vueltas al Host cuando la persona va a leer una.
    var abierta = 'por-que-pago-mas';
    var traidas = {};

    // ---------------------------------------------------------------- formato

    function texto(t) {
        return String(t === undefined || t === null ? '' : t)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    // COLONES SIN DECIMALES Y CON SEPARADOR DE MILES. Es como se lee un recibo municipal.
    function colones(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';
        return '₡' + String(Math.round(x)).replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
    }

    function plural(n, una, varias) { return n === 1 ? una : varias; }

    // LA URGENCIA LA DECIDE EL DOMINIO y aca solo se pinta. Una urgencia que esta pantalla no
    // conozca cae en neutra, que es lo unico honesto que se puede decir de ella.
    function parte(urgencia) {
        var u = String(urgencia || '').toLowerCase();
        if (u.indexOf('venc') === 0 || u === 'alta') return 'parte mala';
        if (u.indexOf('pron') === 0 || u === 'media' || u.indexOf('avis') === 0) return 'parte tibia';
        return 'parte';
    }

    function bloque(titulo, dentro) {
        return '<section class="bloque">' +
               (titulo ? '<h2>' + texto(titulo) + '</h2>' : '') + dentro + '</section>';
    }

    function nada(frase) { return '<p class="nota">' + texto(frase) + '</p>'; }

    // ---------------------------------------------------------------- las seis
    //
    // CADA UNA PINTA LO QUE EL DOMINIO YA REDACTO. El campo `Dice` de cada fila es la frase que
    // escribio el dominio; esta pantalla la ubica y le pone el color de su urgencia, pero no la
    // reescribe ni la resume. Es la misma regla que en los otros cuatro modulos.

    function porQuePagoMas(r) {
        var v = (r && r.variacion) || {};
        var rubros = v.rubros || [];
        if (!rubros.length) return nada('Todavía no hay dos comprobantes del mismo rubro para comparar.');

        return rubros.map(function (x) {
            if (!x.sePuedeVer)
                return '<div class="tarjeta"><div class="tarjeta__cabeza"><strong>' + texto(x.rubro) +
                       '</strong></div>' + nada(x.dice) + '</div>';

            return '<div class="' + (x.esSalto ? 'parte tibia' : 'tarjeta') + '">' +
                '<div class="tarjeta__cabeza"><strong>' + texto(x.rubro) + '</strong>' +
                '<span class="mono">' + colones(x.antes) + ' → ' + colones(x.ahora) + '</span></div>' +
                '<p>' + texto(x.dice) + '</p>' +
                (x.explicacion ? '<p class="nota">' + texto(x.explicacion) + '</p>' : '') +
                '<p class="nota mono">' + texto(x.desde) + ' → ' + texto(x.hasta) + '</p>' +
            '</div>';
        }).join('');
    }

    function queSeMeVence(r) {
        var v = (r && r.vence) || {};
        var porVencer = v.porVencer || [], vencidos = v.vencidos || [];
        if (!porVencer.length && !vencidos.length)
            return nada('No hay nada por vencerse ni vencido. Tu cuenta está al día.');

        var html = '';

        // LO VENCIDO VA PRIMERO, y el orden no es estetico: es lo unico de esta pantalla que ya
        // esta costando plata.
        if (vencidos.length)
            html += bloque('Ya venció', vencidos.map(function (x) {
                return '<div class="' + parte(x.urgencia) + '">' +
                    '<div class="tarjeta__cabeza"><strong>' + texto(x.rubro) + '</strong>' +
                    '<span class="mono">' + colones(x.monto) + '</span></div>' +
                    '<p>' + texto(x.dice) + '</p>' +
                    '<p class="nota mono">' + texto(x.periodo) + ' · venció ' + texto(x.vencio) +
                    ' · ' + x.dias + ' ' + plural(x.dias, 'día', 'días') +
                    (Number(x.costoProyectado) > 0 ? ' · mora proyectada ' + colones(x.costoProyectado) : '') +
                    '</p></div>';
            }).join(''));

        if (porVencer.length)
            html += bloque('Por vencer', porVencer.map(function (x) {
                return '<div class="' + parte(x.urgencia) + '">' +
                    '<div class="tarjeta__cabeza"><strong>' + texto(x.rubro) + '</strong>' +
                    '<span class="mono">' + colones(x.monto) + '</span></div>' +
                    '<p>' + texto(x.dice) + '</p>' +
                    '<p class="nota mono">' + texto(x.periodo) + ' · vence ' + texto(x.vence) +
                    ' · faltan ' + x.faltan + ' ' + plural(x.faltan, 'día', 'días') + '</p></div>';
            }).join(''));

        return html;
    }

    function cuantoApartar(r) {
        var v = (r && r.reserva) || {};
        if (!v.sePuedeProponer)
            return nada('Todavía no hay suficiente historia para proponer cuánto apartar por mes.');

        var html = '<div class="tarjeta">' +
            '<div class="tarjeta__cabeza"><strong>La propuesta</strong>' +
            '<span class="mono">' + colones(v.propuestoPorMes) + ' / mes</span></div>' +
            '<p>' + texto(v.diceLaPropuesta) + '</p>' +
            '<p class="nota mono">proyección del año ' + colones(v.proyeccionDelAnio) + '</p></div>';

        if (v.hayReservaAdoptada)
            html += '<div class="' + parte(v.urgencia) + '">' +
                '<div class="tarjeta__cabeza"><strong>Lo que adoptaste</strong>' +
                '<span class="mono">' + colones(v.adoptadoPorMes) + ' / mes</span></div>' +
                '<p>' + texto(v.diceElFaltante) + '</p>' +
                '<p class="nota mono">vence este mes ' + colones(v.venceEsteMes) +
                ' · falta ' + colones(v.faltante) + '</p></div>';
        else
            // ADOPTAR ES UNA ESCRITURA, y `consultar` solo alcanza [View]. La cara lo dice en vez de
            // ofrecer un boton que no puede cumplir.
            html += nada('Todavía no adoptaste una reserva. Se adopta desde la app de la municipalidad.');

        return html;
    }

    function sinComprobante(r) {
        var v = (r && r.sinComprobante) || {};
        if (!v.hayAlgunaConsulta)
            return nada('Todavía no se ha consultado el portal, así que no hay con qué comparar.');

        var periodos = v.periodos || [];
        var html = '<div class="' + (periodos.length ? 'parte tibia' : 'tarjeta') + '">' +
            '<p>' + texto(v.dice) + '</p>' +
            '<p class="nota mono">consultado el ' + texto(v.diaDeLaConsulta) + '</p></div>';

        if (periodos.length)
            html += periodos.map(function (x) {
                return '<div class="tarjeta"><div class="tarjeta__cabeza"><strong>' + texto(x.rubro) +
                    '</strong><span class="mono">' + texto(x.periodo) + '</span></div>' +
                    '<p class="nota">' + texto(x.dice) + '</p></div>';
            }).join('');

        return html;
    }

    function cuantoHePagado(r) {
        var v = (r && r.pagado) || {};
        if (!v.hayAlgunComprobante) return nada('Todavía no hay comprobantes en tu carpeta.');

        var html = '<div class="tarjeta">' +
            '<div class="tarjeta__cabeza"><strong>' + v.anio + '</strong>' +
            '<span class="mono">' + colones(v.totalDelAnio) + '</span></div>' +
            (v.elAnioPedidoNoTieneComprobantes
                ? nada('Ese año no tiene comprobantes.') : '') + '</div>';

        // LOS AÑOS QUE SE PUEDEN PEDIR los dice el dominio: la cara no los adivina de las fechas.
        var anios = v.aniosQueSePuedenPedir || [];
        if (anios.length > 1)
            html += '<div class="botones" id="anios">' + anios.map(function (a) {
                return '<button class="suave" type="button" data-anio="' + a + '"' +
                       (a === v.anio ? ' disabled' : '') + '>' + a + '</button>';
            }).join('') + '</div>';

        html += (v.series || []).map(function (s) {
            return '<div class="tarjeta"><div class="tarjeta__cabeza"><strong>' + texto(s.rubro) +
                '</strong><span class="mono">' + texto(s.estado) + '</span></div>' +
                '<p>' + texto(s.dice) + '</p>' +
                (s.lineas || []).map(function (l) {
                    return '<p class="nota mono">' + texto(l.periodo) + ' · ' + colones(l.monto) +
                        (Number(l.mora) > 0 ? ' (mora ' + colones(l.mora) + ')' : '') +
                        (l.cubre > 1 ? ' · cubre ' + l.cubre + ' períodos' : '') + '</p>';
                }).join('') + '</div>';
        }).join('');

        return html;
    }

    function queTanAlDia(r) {
        var v = (r && r.alDia) || {};
        var rubros = v.rubros || [];

        var html = '<div class="tarjeta"><p>' +
            (v.hayAlgunaConsulta
                ? 'El portal se consultó el ' + texto(v.diaDeLaUltimaConsulta) + '.'
                : 'Todavía no se ha consultado el portal.') +
            '</p><p class="nota mono">' + v.rubrosConHistoria + ' ' +
            plural(v.rubrosConHistoria, 'rubro con historia', 'rubros con historia') +
            ' · leído hasta el evento ' + v.ultimoEventoLeido + '</p></div>';

        if (rubros.length)
            html += bloque('Los rubros', rubros.map(function (x) {
                return '<div class="tarjeta"><div class="tarjeta__cabeza"><strong>' +
                    texto(x.rubro || x.nombre) + '</strong>' +
                    (x.estado ? '<span class="mono">' + texto(x.estado) + '</span>' : '') + '</div>' +
                    (x.dice ? '<p class="nota">' + texto(x.dice) + '</p>' : '') + '</div>';
            }).join(''));

        // LAS FINCAS vienen adentro de esta vista y no aparte: son el sujeto de todo lo demas, y
        // sacarlas a su propia pregunta las dejaria sin el contexto que las explica.
        var fincas = (v.fincas && v.fincas.fincas) || [];
        if (fincas.length)
            html += bloque('Tus fincas', fincas.map(function (f) {
                return '<div class="tarjeta"><div class="tarjeta__cabeza"><strong>' +
                    texto(f.numero) + '</strong>' +
                    (f.tieneAvaluo ? '<span class="mono">' + colones(f.avaluo) + '</span>' : '') +
                    '</div></div>';
            }).join(''));

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
    function pedirLa(cual) {
        if (traidas[cual]) return;
        traidas[cual] = true;

        var caja = $(cual);
        var argumentos = cual === 'cuanto-he-pagado' ? { anio: 0 } : {};

        P.pedir(cual, argumentos)
            .then(function (r) {
                if (r && r.hayDatos === false) {
                    P.apunte(caja, 'Todavía no hay nada de tu cuenta en este dispositivo.');
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

            P.pedir('cuanto-he-pagado', { anio: Number(b.getAttribute('data-anio')) })
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
        b.textContent = 'Actualizando…';

        P.refrescar(contar).then(function (r) {
            if (r) repintar();
            b.disabled = false;
            b.setAttribute('aria-busy', 'false');
            b.textContent = 'Actualizar';
        });
    }

    function arrancar() {
        P.listo();

        $('nav').addEventListener('click', function (e) {
            var a = e.target.closest('a[data-pregunta]');
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
