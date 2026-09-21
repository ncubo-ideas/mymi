// LA PANTALLA DEL DUEÑO, portada de Concesionario.Wasm/Sections/Inicio.razor a JS plano.
//
// El protocolo NO esta aca: vive en puente.js, que lo pone el Host y es identico en todos los
// modulos. Este archivo es solo el dibujo.
//
// LO QUE PIDE SON [View], no tools. Las dos tools declaradas son lo que el asistente puede
// preguntar; esta pantalla necesita las nueve operaciones con su estado y las estadias del año.
//
// Y LO QUE DICE NO ESTA ESCRITO ACA: cada frase propia sale de text/, por su clave, y cada frase
// del dominio llega ya dicha en la vista. El idioma lo eligio language.js leyendo el aparato, y
// viaja como un parametro mas de cada [View] -- `language` --, igual que en Cargas.
(function () {
    'use strict';

    var P = window.Puente;
    var L = window.Language;
    var $ = P.$;

    L.paintTheStaticText();

    // ---------------------------------------------------------------- el diccionario
    //
    // EL DOMINIO HABLA EN UN VOCABULARIO CERRADO Y LA PANTALLA EN EL IDIOMA DE LA PERSONA, y la
    // traduccion es de la pantalla y no del dominio: `DealershipCarAPI` devuelve "Overdue" y
    // "FluidChange" porque ese es su vocabulario cerrado, y ponerle acentos seria mezclar la
    // etiqueta con su presentacion.
    //
    // Los dichos ya NO estan escritos en este archivo: viven en text/es-CR.js y text/en.js con las
    // mismas claves -- `board.operation.*`, `board.operation.short.*`, `board.dueState.*` -- que
    // Wording/TheSpanishCatalogue.cs y TheEnglishCatalogue.cs, de donde estan copiados. Tener el
    // diccionario dos veces era tener dos pantallas que se desincronizan sin que nada avise.

    // Separa un identificador en palabras, para lo que el diccionario no tenga. Es el `Spaced`
    // de Wording.Phrases: mejor «Wear Part Replacement» que «WearPartReplacement».
    function espaciado(s) {
        return String(s || '').replace(/([a-z0-9])([A-Z])/g, '$1 $2');
    }

    // COMO SE DICE UN MIEMBRO DE UN CONJUNTO CERRADO. Es `Phrases.HowItIsSaid`: un codigo sin
    // dicho se separa en cada mayuscula en vez de mostrar la clave entre llaves, porque un codigo
    // que el catalogo no alcanza sigue siendo legible y una llave no lo es.
    function dicho(familia, codigo) {
        if (!codigo) return '';

        var clave = familia + '.' + codigo;
        var frase = L.text(clave);

        // La ausencia se reconoce por la clave entera y no por la primera llave: una frase puede
        // empezar legitimamente por un hueco -- «{count} vencidos» --, y confundir las dos cosas
        // haria que una frase buena se dijera separada en mayusculas.
        return frase === '{' + clave + '}' ? espaciado(codigo) : frase;
    }

    function operacion(k) { return dicho('board.operation', k); }
    function operacionCorta(k) { return dicho('board.operation.short', k); }
    function estado(s) { return dicho('board.dueState', s); }

    // El tono del semaforo, decidido por el ESTADO del dominio. Un estado que esta pantalla no
    // conozca cae en neutro, que es lo unico honesto que se puede decir de el.
    var TONOS = {
        Overdue: 'danger', Missed: 'danger',
        Due: 'warn', DueSoon: 'warn', Deferred: 'warn',
        NotDue: 'ok', Completed: 'ok'
    };

    function tono(s) { return TONOS[s] || 'mute'; }

    // ---------------------------------------------------------------- formato

    function texto(t) {
        return String(t === undefined || t === null ? '' : t)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }
    function atributo(t) { return String(t || '').replace(/"/g, '&quot;'); }

    // UN ENTERO CON EL SEPARADOR DE MILES DEL IDIOMA, que es `Phrases.Grouped`: punto en español
    // -- «142.380 km» -- y coma en ingles -- «142,380 km» --. Llevaba un espacio fino, que no es
    // el de ninguno de los dos y hacia que la misma cifra se leyera distinto en las dos pantallas.
    function entero(n) {
        var x = Number(n);
        if (!isFinite(x)) return '--';

        var separador = L.language === 'en' ? ',' : '.';
        var signo = x < 0 ? '-' : '';

        return signo + String(Math.abs(Math.round(x)))
            .replace(/\B(?=(\d{3})+(?!\d))/g, separador);
    }

    // UN DIA DEL REGISTRO DICHO CORTO, que es `Phrases.AShortDay`: el orden lo pone el catalogo
    // -- `day.short` -- y el mes sale de `month.short.*`, donde setiembre es «set» en español y
    // «Sep» en ingles.
    //
    // SE PARTE A MANO Y NO CON `new Date`, y no es un gusto: `new Date('2026-09-14')` es medianoche
    // UTC, y en Costa Rica -- UTC-6 -- eso se lee como el 13. Un dia del registro no tiene hora ni
    // zona; ponerle una era correrle la fecha un dia hacia atras a toda la linea de tiempo.
    //
    // Un texto que no calce con el molde se devuelve intacto, por la misma razon que en Phrases:
    // inventarle un dia seria peor que dejarlo ver.
    function fecha(iso) {
        if (!iso) return '';

        var molde = /^(\d{4})-(\d{2})-(\d{2})$/.exec(String(iso).slice(0, 10));
        if (!molde) return String(iso);

        var mes = Number(molde[2]);
        if (mes < 1 || mes > 12) return String(iso);

        return L.fill('day.short', {
            day: String(Number(molde[3])),
            month: L.text('month.short.' + mes),
            year: molde[1]
        });
    }

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

    // EL TONO DE LA PANTALLA ENTERA, y lo decide lo PEOR que haya. Es el `Tono` de Inicio.razor, y
    // no tiene rama para lo que no se hizo: un «sin hacer» se cuenta en su pildora, pero no manda
    // sobre el titular.
    function tonoDelCarro(h) {
        return h.overdue > 0 ? 'danger' : h.due > 0 ? 'warn' : 'ok';
    }

    // ---------------------------------------------------------------- el estado

    function pintarEstado(h) {
        var caja = $('estado');
        caja.setAttribute('aria-busy', 'false');

        var programadas = h.scheduled || 0;
        var alDia = h.upToDate || 0;
        var matiz = tonoDelCarro(h);

        // EL TITULAR ES EL DE LA APP SUELTA, y son tres palabras y no un conteo repetido: los
        // numeros ya estan en las pildoras de abajo, y decirlos dos veces hacia que el titular
        // compitiera con ellas en vez de resumirlas.
        var titular = h.overdue > 0
            ? L.text('home.headlineAttention')
            : h.due > 0 ? L.text('home.headlineSomething') : L.text('home.headlineOk');

        var conteos = '';
        if (h.overdue > 0) conteos += '<span class="d-pill d-pill--danger">' +
            texto(L.fill(h.overdue === 1 ? 'home.overdueOne' : 'home.overdueMany',
                { count: entero(h.overdue) })) + '</span>';
        if (h.due > 0) conteos += '<span class="d-pill d-pill--warn">' +
            texto(L.fill(h.due === 1 ? 'home.dueOne' : 'home.dueMany',
                { count: entero(h.due) })) + '</span>';
        if (h.missed > 0) conteos += '<span class="d-pill d-pill--mute">' +
            texto(L.fill('home.missed', { count: entero(h.missed) })) + '</span>';
        if (alDia > 0) conteos += '<span class="d-pill d-pill--ok">' +
            texto(L.fill('home.upToDate', { count: entero(alDia) })) + '</span>';

        caja.innerHTML =
            '<div class="d-card__head">' +
                '<span class="d-card__title">' + texto(L.text('home.stateTitle')) + '</span>' +
                '<span class="d-pill d-pill--' + matiz + '">' + texto(titular) + '</span>' +
            '</div>' +
            '<div class="d-health">' +
                '<div class="d-health__score">' +
                    '<span class="d-health__n">' + texto(entero(alDia)) + '</span>' +
                    '<span class="d-health__of">/' + texto(entero(programadas)) + '</span>' +
                '</div>' +
                '<div class="d-health__body"><p class="d-health__what">' +
                    texto(programadas
                        ? L.text('home.healthWhat')
                        : L.text('face.nothingScheduled')) +
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
                    '<div><div class="d-next__kicker">' +
                    texto(L.text('home.nothingPending')) + '</div>' +
                    '<div class="d-next__title">' + texto(L.text('home.allGood')) + '</div>' +
                    '<div class="d-next__more"><span class="d-note">' +
                    texto(L.fill('home.nothingDue', { count: entero(h.scheduled || 0) })) +
                    '</span></div></div>' +
                '</section>';
            return;
        }

        // LA RAZON LA ESCRIBE EL DOMINIO, no esta pantalla. `Says` es lo que la operacion tiene
        // para decir de si misma; sin eso, aca habria una frase de plantilla igual para las
        // nueve. Y viene en el idioma que se le pidio a la vista, no en el del registro.
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
                    '<div class="d-next__kicker">' + texto(L.text('home.nextKicker')) + '</div>' +
                    '<div class="d-next__title">' + texto(operacion(siguiente)) + '</div>' +
                    (suya && suya.says
                        ? '<div class="d-says"><span class="d-says__label">' +
                          texto(L.text('home.saysLabel')) + '</span>' +
                          texto(suya.says) + '</div>'
                        : '') +
                '</div>' +
            '</section>';
    }

    // ---------------------------------------------------------------- los numeros

    // LOS DOS AZULEJOS SIEMPRE, como en Inicio.razor. Antes desaparecian cuando el dato faltaba, y
    // un azulejo ausente no dice nada: el que dice algo es el que esta y muestra «--» o «sin
    // lectura», porque nombra el hueco en vez de esconderlo.
    function pintarNumeros(v) {
        var conOdometro = !!(v && v.odometerKnown);
        var observado = (v && v.odometerObservedAt) || '';

        $('numeros').innerHTML =
            '<div class="d-tile">' +
                '<div><span class="d-tile__v">' +
                texto(conOdometro ? entero(v.odometer) : '--') + '</span>' +
                '<span class="d-tile__u">' +
                texto(conOdometro ? v.odometerUnit : '') + '</span></div>' +
                '<div class="d-tile__k">' + texto(L.text('home.odometerTile')) + '</div>' +
            '</div>' +
            // LA LECTURA ES `odometerObservedAt` Y NO `deliveredAt`, que es el dia de la ENTREGA
            // del carro: el azulejo decia «ultima lectura que envio tu concesionario» y mostraba
            // la fecha en que se lo entregaron, que en este registro es de hace años.
            '<div class="d-tile">' +
                '<div class="d-tile__v" style="font-size:15px;letter-spacing:-.2px">' +
                texto(observado ? fecha(observado) : L.text('home.noReading')) + '</div>' +
                '<div class="d-tile__k">' + texto(L.text('home.readingTile')) + '</div>' +
            '</div>';
    }

    // ---------------------------------------------------------------- el vistazo

    // LAS NUEVE, cada una con su punto de color. Sin porcentaje, porque no hay: una operacion
    // esta o no esta al dia, y un numero inventado ahi seria peor que el punto.
    function pintarVistazo(ops, estados) {
        var caja = $('vistazo');
        caja.setAttribute('aria-busy', 'false');

        var lista = (ops && ops.operations) || [];
        if (!lista.length) {
            P.apunte(caja, L.text('face.noOperations'));
            return;
        }

        caja.innerHTML = '<div class="d-dots">' + lista.map(function (o) {
            var s = estados[o.kind] || '';
            return '<div class="d-dot" title="' + atributo(estado(s)) + '">' +
                '<span class="d-dot__ring d-dot__ring--' + tono(s) + '">' +
                glifo(dibujo(s), 18, 2.6) + '</span>' +
                '<span class="d-dot__k">' + texto(operacionCorta(o.kind)) + '</span></div>';
        }).join('') + '</div>';
    }

    // ---------------------------------------------------------------- el historial

    // LAS ESTADIAS COMO LAS MANDA `TheStaysOfThisVehicle`, y no como esta pantalla se las
    // imaginaba: leia `stays.stays` y `stays.visits`, que no existen, asi que el año entero se veia
    // vacio aunque el taller hubiera registrado cada entrada. Las filas estan en `rows`, y cada una
    // trae `branch`, `subject`, `enteredAt`, `coveredDays` y su propia frase.
    function pintarHistorial(h) {
        var caja = $('historial');
        caja.setAttribute('aria-busy', 'false');

        var estadias = (h && h.stays) || {};
        var filas = estadias.rows || [];

        if (!filas.length) {
            // NO SE DICE «tu carro no estuvo en el taller»: que no haya ninguna registrada no es
            // prueba de que no entro. Lo primero nombra el periodo con las palabras de la app
            // suelta; lo segundo es la frase que compuso el dominio, que nombra sus dos limites.
            P.apunte(caja, L.text('history.staysNoneTitle'));
            if (estadias.says) P.apunte(caja, estadias.says);
            return;
        }

        // SIN TONO: una estadia cerrada es un hecho del registro y no un veredicto del plan. Antes
        // caia en `Completed` por omision y se pintaba verde, que es un semaforo donde no hay
        // ninguno. El punto queda en su estilo base, que es neutro.
        caja.innerHTML = '<div class="d-line">' + filas.map(function (e) {
            return '<div class="d-step">' +
                '<span class="d-step__bullet"></span>' +
                '<div><div class="d-step__when">' + texto(fecha(e.enteredAt)) + '</div>' +
                '<div class="d-step__title">' + texto(e.subject || e.branch) + '</div>' +
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

    // EL SALUDO ES EL <h1> DE Inicio.razor, y lo compone el estado del carro: quien abre la
    // pantalla lee primero como esta su carro y no como se llama la app.
    function saludo(h) {
        var modelo = (h.specification && h.specification.model) || '';
        var carro = modelo
            ? L.fill('home.yourModel', { model: modelo })
            : L.text('home.yourCar');

        return h.overdue > 0
            ? L.fill('home.greetingAttention', { car: carro })
            : h.due > 0
                ? L.fill('home.greetingSomething', { car: carro })
                : L.fill('home.greetingOk', { car: carro });
    }

    // QUIEN ES ESTE CARRO, en una linea. No esta en la pantalla de la app suelta -- alla vive en la
    // pestaña «Mi vehículo», y esta cara no tiene pestañas --, pero los rotulos si son los suyos.
    // La placa sale de `platePublished`/`asPublished`: esta pantalla leia `plate.plate`, que no
    // existe, asi que la placa nunca aparecia.
    function identidad(h) {
        var p = h.plate || {};
        var partes = [];

        if (p.platePublished && p.asPublished)
            partes.push(L.text('vehicle.rowPlate') + ' ' + p.asPublished);

        if (h.specification && h.specification.model) partes.push(h.specification.model);

        if (h.vin) partes.push(L.text('vehicle.rowVin') + ' ' + h.vin);

        return partes.join(' · ');
    }

    function pintarTodo() {
        // EL IDIOMA VIAJA EN CADA PEDIDO. La superficie no tiene navegador -- corre en proceso
        // adentro de Gnothi -- y quien si lo es ya leyo la eleccion del aparato: sin esto, las
        // frases del dominio vuelven siempre en el idioma del registro del concesionario.
        var enSuIdioma = { language: L.tag };

        P.pedir('header', enSuIdioma)
            .then(function (h) {
                if (h.hasData === false) {
                    $('subtitulo').textContent = L.text('face.nothingHereYet');
                    P.apunte($('estado'),
                        L.fill('face.pressRefresh', { refresh: L.text('home.refresh') }));
                    return;
                }

                $('saludo').textContent = saludo(h);
                $('subtitulo').textContent = identidad(h);

                // El estado y el vistazo necesitan las dos vistas, asi que se piden juntas y se
                // pintan cuando estan ambas: pintar media tarjeta y completarla despues haria
                // saltar la pantalla delante de quien la esta leyendo.
                P.pedir('standing', enSuIdioma)
                    .then(function (s) {
                        pintarEstado(h);
                        pintarSigue(h, (s.operations && s.operations.operations) || [], s.dueStates || {});
                        pintarNumeros(s.vehicle);
                        pintarVistazo(s.operations, s.dueStates || {});
                    })
                    .catch(function (e) { P.apunte($('estado'), P.porQue(e), true); });
            })
            .catch(function (e) { P.apunte($('estado'), P.porQue(e), true); });

        P.pedir('history', enSuIdioma).then(pintarHistorial)
            .catch(function (e) { P.apunte($('historial'), P.porQue(e), true); });
    }

    function releer() {
        var b = $('releer');
        b.disabled = true;
        b.setAttribute('aria-busy', 'true');
        b.textContent = L.text('home.refreshFetching');

        P.refrescar(contar).then(function (r) {
            if (r) pintarTodo();
            b.disabled = false;
            b.setAttribute('aria-busy', 'false');
            b.textContent = L.text('home.refresh');
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
