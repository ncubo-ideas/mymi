// LA PANTALLA DEL DUEÑO, portada de Concesionario.Wasm/Sections/Inicio.razor a JS plano.
//
// El protocolo NO esta aca: vive en puente.js, que lo pone el Host y es identico en todos los
// modulos. Este archivo es solo el dibujo.
//
// LO QUE PIDE SON [View], no tools. Las dos tools declaradas son lo que el asistente puede
// preguntar; esta pantalla necesita las nueve operaciones con su estado y las estadias del año.
(function () {
    'use strict';

    var P = window.Puente;
    var $ = P.$;

    // ---------------------------------------------------------------- el diccionario
    //
    // EL DOMINIO HABLA EN INGLES Y LA PANTALLA EN ESPAÑOL, y la traduccion es de la pantalla y
    // no del dominio: `DealershipCarAPI` devuelve "Overdue" y "FluidChange" porque ese es su
    // vocabulario cerrado, y ponerle acentos seria mezclar la etiqueta con su presentacion.
    //
    // Es el mismo diccionario de Parts/ComoSeLee.cs, traido tal cual.

    var LAS_NUEVE = {
        Inspection: 'Revisión', FluidChange: 'Cambio de líquidos', FilterChange: 'Cambio de filtro',
        WearPartReplacement: 'Piezas de desgaste', TimedComponent: 'Componente por tiempo',
        SoftwareUpdate: 'Actualización de software', RecallRemedy: 'Llamado a revisión',
        Diagnostic: 'Diagnóstico', Repair: 'Reparación'
    };

    var CORTO = {
        Inspection: 'Revisión', FluidChange: 'Líquidos', FilterChange: 'Filtro',
        WearPartReplacement: 'Desgaste', TimedComponent: 'Por tiempo',
        SoftwareUpdate: 'Software', RecallRemedy: 'Llamado',
        Diagnostic: 'Diagnóstico', Repair: 'Reparación'
    };

    var ESTADOS = {
        Overdue: 'Vencido', Due: 'Toca ahora', DueSoon: 'Se acerca', NotDue: 'Al día',
        Deferred: 'Pospuesto', Completed: 'Realizado', Missed: 'No se hizo',
        Superseded: 'Reemplazado'
    };

    // El tono del semaforo, decidido por el ESTADO del dominio. Un estado que esta pantalla no
    // conozca cae en neutro, que es lo unico honesto que se puede decir de el.
    var TONOS = {
        Overdue: 'danger', Missed: 'danger',
        Due: 'warn', DueSoon: 'warn', Deferred: 'warn',
        NotDue: 'ok', Completed: 'ok'
    };

    // Separa un identificador en palabras, para lo que el diccionario no tenga. Es el `Spaced`
    // de AppState: mejor «Wear Part Replacement» que «WearPartReplacement».
    function espaciado(s) {
        return String(s || '').replace(/([a-z0-9])([A-Z])/g, '$1 $2');
    }

    function operacion(k) { return LAS_NUEVE[k] || espaciado(k); }
    function operacionCorta(k) { return CORTO[k] || espaciado(k); }
    function estado(s) { return ESTADOS[s] || espaciado(s); }
    function tono(s) { return TONOS[s] || 'mute'; }

    // ---------------------------------------------------------------- formato

    function texto(t) {
        return String(t === undefined || t === null ? '' : t)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }
    function atributo(t) { return String(t || '').replace(/"/g, '&quot;'); }

    // Los enteros con separador de miles fino: «142 380 km».
    function entero(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';
        return String(Math.round(x)).replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
    }

    var MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
    function fecha(iso) {
        if (!iso) return '';
        var d = new Date(iso);
        if (isNaN(d.getTime())) return String(iso).slice(0, 10);
        return d.getDate() + ' ' + MESES[d.getMonth()] + ' ' + d.getFullYear();
    }
    function plural(n, una, varias) { return n === 1 ? una : varias; }

    // ---------------------------------------------------------------- los dibujitos
    //
    // Cuatro glifos, de trazo y nunca de relleno, para que hereden el color de su punto. Son los
    // de Parts/Glyph.razor, reducidos a los que esta pantalla usa.

    var GLIFOS = {
        alert: '<path d="M12 8v5"/><circle cx="12" cy="16.5" r="1.1" fill="currentColor" stroke="none"/>' +
               '<path d="M10.3 3.6 2.6 17.4A2 2 0 0 0 4.3 20.4h15.4a2 2 0 0 0 1.7-3L13.7 3.6a2 2 0 0 0-3.4 0z"/>',
        clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5.5l3.5 2"/>',
        calendar: '<rect x="3.5" y="5" width="17" height="15.5" rx="2.5"/><path d="M3.5 10h17M8 3v4M16 3v4"/>',
        check: '<path d="M4.5 12.5 9.5 17.5 19.5 6.5"/>',

        // EL QUE FALTABA, y su ausencia costo un semaforo mal pintado: sin un glifo para «no se»,
        // el respaldo de `glifo` y el de `dibujo` no tenian mas remedio que devolver el visto.
        question: '<circle cx="12" cy="12" r="9"/><path d="M9.4 9.4a2.7 2.7 0 1 1 3.3 3.1v1.4"/>' +
                  '<circle cx="12" cy="17" r="1.1" fill="currentColor" stroke="none"/>'
    };

    // EL GLIFO DE UN ESTADO, y el respaldo NO es el visto.
    //
    // Decia `return 'check'` para todo lo que no reconocia -- incluido `undefined` --, y eso hizo
    // que un dato que no llegaba se viera igual que «todo bien». Paso de verdad: el Host
    // proyectaba mal el diccionario de estados, la busqueda daba `undefined`, y la tarjeta pintaba
    // un visto verde debajo de una pildora que decia «1 te toca ahora». El bug del Host esta
    // arreglado, pero el respaldo que lo disfrazo tambien tenia que cambiar.
    //
    // Un semaforo no puede tener un color por omision: el unico fallo que no se le permite es
    // decir que todo esta bien cuando no lo sabe. `check` ahora hay que ganarselo nombrando un
    // estado que de verdad lo sea.
    // Los que dan visto son los SIETE del dominio que lo merecen -- `DueState.cs`: NotDue,
    // Completed, Superseded --, escritos con su nombre y no adivinados.
    function dibujo(s) {
        if (s === 'Overdue' || s === 'Missed') return 'alert';
        if (s === 'Due' || s === 'DueSoon') return 'clock';
        if (s === 'Deferred') return 'calendar';
        if (s === 'NotDue' || s === 'Completed' || s === 'Superseded') return 'check';
        return 'question';
    }

    // Y EL MISMO RESPALDO, UN PISO MAS ABAJO: esto tambien decia `GLIFOS.check`. Dos redes que
    // atrapaban lo desconocido y las dos lo pintaban de verde.
    function glifo(nombre, tam, grosor) {
        return '<svg viewBox="0 0 24 24" width="' + (tam || 18) + '" height="' + (tam || 18) +
               '" fill="none" stroke="currentColor" stroke-width="' + (grosor || 2.6) +
               '" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
               (GLIFOS[nombre] || GLIFOS.question) + '</svg>';
    }

    // ---------------------------------------------------------------- el estado

    function pintarEstado(h, s) {
        var caja = $('estado');
        caja.setAttribute('aria-busy', 'false');

        var programadas = h.scheduled || 0;
        var alDia = h.upToDate || 0;

        // EL TITULAR LO DECIDE LO PEOR QUE HAYA, y en ese orden: primero lo vencido, despues lo
        // que toca ahora, despues lo que no se hizo. Un «todo al día» arriba de un vencido se
        // contradice solo.
        var titular, matiz;
        if (h.overdue > 0) { titular = h.overdue + ' ' + plural(h.overdue, 'vencido', 'vencidos'); matiz = 'danger'; }
        else if (h.due > 0) { titular = h.due + ' te ' + plural(h.due, 'toca', 'tocan') + ' ahora'; matiz = 'warn'; }
        else if (h.missed > 0) { titular = h.missed + ' sin hacer'; matiz = 'mute'; }
        else { titular = 'Todo al día'; matiz = 'ok'; }

        var conteos = '';
        if (h.overdue > 0) conteos += '<span class="d-pill d-pill--danger">' + h.overdue + ' ' +
            plural(h.overdue, 'vencido', 'vencidos') + '</span>';
        if (h.due > 0) conteos += '<span class="d-pill d-pill--warn">' + h.due + ' te ' +
            plural(h.due, 'toca', 'tocan') + ' ahora</span>';
        if (h.missed > 0) conteos += '<span class="d-pill d-pill--mute">' + h.missed + ' sin hacer</span>';
        if (alDia > 0) conteos += '<span class="d-pill d-pill--ok">' + alDia + ' al día</span>';

        caja.innerHTML =
            '<div class="d-card__head">' +
                '<span class="d-card__title">Estado de tu vehículo</span>' +
                '<span class="d-pill d-pill--' + matiz + '">' + texto(titular) + '</span>' +
            '</div>' +
            '<div class="d-health">' +
                '<div class="d-health__score">' +
                    '<span class="d-health__n">' + alDia + '</span>' +
                    '<span class="d-health__of">/' + programadas + '</span>' +
                '</div>' +
                '<div class="d-health__body"><p class="d-health__what">' +
                    (programadas
                        ? 'de las ' + programadas + ' operaciones que tu concesionario programó están al día.'
                        : 'Tu concesionario todavía no programó operaciones para este vehículo.') +
                '</p></div>' +
            '</div>' +
            (conteos ? '<div class="d-health__counts">' + conteos + '</div>' : '');
    }

    // ---------------------------------------------------------------- lo que sigue

    function pintarSigue(h, ops, estados) {
        var caja = $('sigue');
        var siguiente = h.nextDueKind || '';

        if (!siguiente) {
            caja.innerHTML =
                '<section class="d-next d-next--ok">' +
                    '<span class="d-next__icon">' + glifo('check', 22, 2.4) + '</span>' +
                    '<div><div class="d-next__kicker">Nada pendiente</div>' +
                    '<div class="d-next__title">No hay nada que agendar ahora mismo.</div></div>' +
                '</section>';
            return;
        }

        // LA RAZON LA ESCRIBE EL DOMINIO, no esta pantalla. `Says` es lo que la operacion tiene
        // para decir de si misma; sin eso, aca habria una frase de plantilla igual para las
        // nueve.
        var suya = null;
        (ops || []).forEach(function (o) { if (o.kind === siguiente) suya = o; });

        // EL TONO SALE DE `dueStates`, que es el veredicto del dominio por operacion. Una
        // operacion tiene DOS cuentas regresivas -por distancia y por tiempo- y ninguna de las dos
        // es el veredicto: ReadDueState si. Adivinarlo pintaba el visto verde al lado de una
        // pildora que decia «te toca ahora».
        var t = tono(estados[siguiente]);

        caja.innerHTML =
            '<section class="d-next d-next--' + t + '">' +
                '<span class="d-next__icon">' + glifo(dibujo(estados[siguiente]), 22, 2.4) + '</span>' +
                '<div>' +
                    '<div class="d-next__kicker">Lo que sigue</div>' +
                    '<div class="d-next__title">' + texto(operacion(siguiente)) + '</div>' +
                    (suya && suya.says
                        ? '<div class="d-says"><span class="d-says__label">Lo que dice tu registro</span>' +
                          texto(suya.says) + '</div>'
                        : '') +
                '</div>' +
            '</section>';
    }

    // ---------------------------------------------------------------- los numeros

    function pintarNumeros(v) {
        var caja = $('numeros');
        var html = '';

        if (v && v.odometerKnown) {
            html += '<div class="d-tile">' +
                '<div><span class="d-tile__v">' + entero(v.odometer) + '</span>' +
                '<span class="d-tile__u">' + texto(v.odometerUnit) + '</span></div>' +
                '<div class="d-tile__k">han recorrido juntos</div></div>';
        }

        if (v && v.deliveryRecorded) {
            html += '<div class="d-tile">' +
                '<div class="d-tile__v" style="font-size:15px;letter-spacing:-.2px">' +
                texto(fecha(v.deliveredAt)) + '</div>' +
                '<div class="d-tile__k">última lectura que envió tu concesionario</div></div>';
        }

        caja.innerHTML = html;
    }

    // ---------------------------------------------------------------- el vistazo

    // LAS NUEVE, cada una con su punto de color. Sin porcentaje, porque no hay: una operacion
    // esta o no esta al dia, y un numero inventado ahi seria peor que el punto.
    function pintarVistazo(ops, estados) {
        var caja = $('vistazo');
        caja.setAttribute('aria-busy', 'false');

        var lista = (ops && ops.operations) || [];
        if (!lista.length) {
            P.apunte(caja, 'Todavía no hay operaciones programadas para este vehículo.');
            return;
        }

        caja.innerHTML = '<div class="d-dots">' + lista.map(function (o) {
            var s = estados[o.kind] || "";
            return '<div class="d-dot" title="' + atributo(estado(s)) + '">' +
                '<span class="d-dot__ring d-dot__ring--' + tono(s) + '">' +
                glifo(dibujo(s), 18, 2.6) + '</span>' +
                '<span class="d-dot__k">' + texto(operacionCorta(o.kind)) + '</span></div>';
        }).join('') + '</div>';
    }

    // ---------------------------------------------------------------- el historial

    function pintarHistorial(h) {
        var caja = $('historial');
        caja.setAttribute('aria-busy', 'false');

        var estadias = (h && h.stays && (h.stays.stays || h.stays.visits)) || [];
        if (!estadias.length) {
            P.apunte(caja, 'Tu carro no estuvo en el taller este año.');
            return;
        }

        caja.innerHTML = '<div class="d-line">' + estadias.map(function (e) {
            var s = e.standing || e.dueState || 'Completed';
            return '<div class="d-step">' +
                '<span class="d-step__bullet d-step__bullet--' + tono(s) + '"></span>' +
                '<div><div class="d-step__when d-step__when--' + tono(s) + '">' +
                texto(fecha(e.day || e.at || e.when)) + '</div>' +
                '<div class="d-step__title">' + texto(operacion(e.kind) || e.title || 'Estadía') + '</div>' +
                (e.says ? '<div class="d-step__sub">' + texto(e.says) + '</div>' : '') +
                '</div></div>';
        }).join('') + '</div>';
    }

    // ---------------------------------------------------------------- arranque

    function contar(t, malo) {
        var p = $('conexion');
        p.textContent = t;
        p.classList.toggle('malo', !!malo);
    }

    function pintarTodo() {
        P.pedir('header')
            .then(function (h) {
                if (h.hasData === false) {
                    $('subtitulo').textContent = 'Todavía no hay nada de tu carro en este dispositivo.';
                    P.apunte($('estado'), 'Apretá Actualizar para que Gnothi traiga lo que publicó tu concesionario.');
                    return;
                }

                var p = h.plate || {};
                $('subtitulo').textContent =
                    (p.plate ? 'Placa ' + p.plate + ' · ' : '') +
                    (h.specification && h.specification.model ? h.specification.model + ' · ' : '') +
                    'VIN ' + (h.vin || '');

                // El estado y el vistazo necesitan las dos vistas, asi que se piden juntas y se
                // pintan cuando estan ambas: pintar media tarjeta y completarla despues haria
                // saltar la pantalla delante de quien la esta leyendo.
                P.pedir('standing')
                    .then(function (s) {
                        pintarEstado(h, s);
                        pintarSigue(h, (s.operations && s.operations.operations) || [], s.dueStates || {});
                        pintarNumeros(s.vehicle);
                        pintarVistazo(s.operations, s.dueStates || {});
                    })
                    .catch(function (e) { P.apunte($('estado'), e.message, true); });
            })
            .catch(function (e) { P.apunte($('estado'), e.message, true); });

        P.pedir('history').then(pintarHistorial)
            .catch(function (e) { P.apunte($('historial'), e.message, true); });
    }

    function releer() {
        var b = $('releer');
        b.disabled = true;
        b.setAttribute('aria-busy', 'true');
        b.textContent = 'Actualizando…';

        P.refrescar(contar).then(function (r) {
            if (r) pintarTodo();
            b.disabled = false;
            b.setAttribute('aria-busy', 'false');
            b.textContent = 'Actualizar';
        });
    }

    function arrancar() {
        P.listo();
        $('releer').addEventListener('click', releer);

        // PRIMERO SE PINTA LO QUE YA HAY, y despues se refresca. Al reves, abrir con el
        // publicador caido dejaria la pantalla en blanco hasta que el pedido falle -- y tener el
        // dato propio sirve justamente para que eso no pase.
        pintarTodo();
        P.refrescar(contar).then(function (r) { if (r) pintarTodo(); });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', arrancar);
    else arrancar();
})();
