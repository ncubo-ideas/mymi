// EL TABLERO DE CARGAS, portado de Cargas.Wasm a JS plano.
//
// El protocolo NO esta aca: vive en puente.js, que lo pone el Host y es identico en todos los
// modulos. Este archivo es solo el dibujo -- y esa separacion es la que deja medir cuanto de una
// cara es «hablar con Gnothi» y cuanto es «ser la pantalla de esta institucion».
//
// LO QUE PIDE SON [View], no tools. Las dos tools declaradas son lo que el asistente puede
// preguntar; un tablero necesita tramos, fracciones de tanque y costos por moneda.
(function () {
    'use strict';

    var P = window.Puente;
    var $ = P.$, el = P.el;

    // La placa que se esta mirando. El dominio reparte TODO por vehiculo, y quedarse con el
    // primero escondia los demas sin decirlo.
    var placa = '';
    var unidades = { rendimiento: '', distancia: '' };

    // ---------------------------------------------------------------- formato
    //
    // Formato explicito SIEMPRE. La escala del decimal no sobrevive el journal -1452.50 entra,
    // 1452.5 sale del replay- asi que un toString pelado mostraria valores distintos antes y
    // despues de reabrir. Es el mismo criterio que la app suelta.

    function cifra(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';
        return String(Math.round(x * 100) / 100);
    }

    // Los enteros de distancia llevan separador de miles fino: «1 452 km». En la sans el fino
    // desaparecia a 12,5 px, y por eso toda cifra del tablero va en mono.
    function entero(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';
        return String(Math.round(x)).replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
    }

    function mayuscula(f) { return !f ? '' : f.charAt(0).toUpperCase() + f.slice(1); }
    function plural(n, una, varias) { return n === 1 ? una : varias; }

    // Un numero listo para un atributo de SVG, con punto decimal siempre: el atributo se lee en
    // cultura invariante y una coma lo rompe.
    function px(n) { return (Math.round(n * 100) / 100).toString(); }

    function simbolo(moneda) {
        return moneda === 'CRC' ? '₡' : moneda === 'USD' ? '$' : (moneda || '') + ' ';
    }

    // ---------------------------------------------------------------- el medidor
    //
    // LA AGUJA DE LA AUTONOMIA, dibujada como lo que todo el mundo ya sabe leer: un medidor de
    // gasolina de E a F. La autonomia no se consulta, se ojea: un arco con una aguja contesta
    // «¿me alcanza?» antes de que alguien lea la cifra, y la cifra queda adentro para cuando si
    // quiera el numero.
    //
    // SIN FRACCION NO HAY AGUJA: cuando no se puede estimar cuanto queda, el arco se dibuja
    // lleno y la lectura pasa a ser «el tanque lleno da ~X km», que eso si se sabe.

    var INICIO = 150, BARRIDO = 240, CX = 60, CY = 54, RADIO = 40;

    function rad(g) { return g * Math.PI / 180; }
    function xr(f, r) { return CX + r * Math.cos(rad(INICIO + BARRIDO * f)); }
    function yr(f, r) { return CY + r * Math.sin(rad(INICIO + BARRIDO * f)); }

    function arco(desde, hasta) {
        // El flag de arco largo depende del BARRIDO, no del radio: por encima de media vuelta el
        // path tiene que decirlo o SVG dibuja el complemento.
        var largo = (hasta - desde) * BARRIDO > 180 ? 1 : 0;
        return 'M ' + px(xr(desde, RADIO)) + ' ' + px(yr(desde, RADIO)) +
               ' A ' + RADIO + ' ' + RADIO + ' 0 ' + largo + ' 1 ' +
               px(xr(hasta, RADIO)) + ' ' + px(yr(hasta, RADIO));
    }

    function medidor(fraccion, descripcion, apagado) {
        var nivel = Number(fraccion) > 0 ? Math.min(1, Number(fraccion)) : 1;

        var d = '<path class="pista" d="' + arco(0, 1) + '" fill="none" stroke-width="7" stroke-linecap="round"/>';
        if (!apagado && nivel > 0)
            d += '<path class="carga" d="' + arco(0, nivel) + '" fill="none" stroke-width="7" ' +
                 'stroke-linecap="round" pathLength="1"/>';

        d += '<text class="marca-e" x="20" y="84" text-anchor="middle">E</text>' +
             '<text class="marca-f" x="100" y="84" text-anchor="middle">F</text>';

        if (!apagado)
            d += '<line class="aguja" x1="60" y1="54" x2="' + px(xr(nivel, 31)) + '" y2="' +
                 px(yr(nivel, 31)) + '" stroke-width="2.5" stroke-linecap="round"/>';

        d += '<circle class="eje" cx="60" cy="54" r="4" stroke-width="2"/>';

        return '<svg class="medidor' + (apagado ? ' medidor--apagado' : '') +
               '" viewBox="0 0 120 86" role="img" aria-label="' + atributo(descripcion) + '">' +
               d + '</svg>';
    }

    // ---------------------------------------------------------------- la chispa
    //
    // La trayectoria contra la linea base. SOLO LOS TRAMOS CALCULADOS: un tramo bloqueado no
    // tiene numero que poner, y su lugar es el bache de la ruta, no un hueco mudo en la curva.
    // Los calculados pero NO comparables se dibujan huecos: estan, pero no se leen contra la
    // banda.

    function chispa(tramos, habitual, pideAlgo, descripcion) {
        var calculados = (tramos || []).filter(function (t) { return t.estaCalculado; });
        if (!calculados.length) return '';

        var valores = calculados.map(function (t) { return Number(t.rendimiento); });
        var min = Math.min.apply(null, valores), max = Math.max.apply(null, valores);
        if (Number(habitual) > 0) { min = Math.min(min, Number(habitual)); max = Math.max(max, Number(habitual)); }

        // Aire arriba y abajo, con un piso: sin el, una serie casi plana -que ES lo que esta
        // forma tiene para contar- se aplastaria contra el borde.
        var aire = Math.max((max - min) * 0.2, 0.5);
        min -= aire; max += aire;

        function y(v) { return max === min ? 23 : 42 - ((v - min) / (max - min)) * 38; }

        var paso = calculados.length > 1 ? 292 / (calculados.length - 1) : 0;
        var puntos = calculados.map(function (t, i) {
            return { x: 4 + paso * i, y: y(Number(t.rendimiento)), comparable: t.esComparable };
        });

        var d = '';
        if (Number(habitual) > 0)
            d += '<line class="linea-base" x1="4" y1="' + px(y(Number(habitual))) +
                 '" x2="296" y2="' + px(y(Number(habitual))) + '" stroke-width="1"/>';

        // El trazo une solo los COMPARABLES: el punto hueco se ve, pero no tuerce la curva que
        // se lee contra la banda.
        var comparables = puntos.filter(function (p) { return p.comparable; });
        if (comparables.length > 1)
            d += '<polyline class="trazo' + (pideAlgo ? ' trazo--atencion' : '') + '" points="' +
                 comparables.map(function (p) { return px(p.x) + ',' + px(p.y); }).join(' ') +
                 '" pathLength="1"/>';

        puntos.forEach(function (p) {
            var clase = !p.comparable ? 'punto punto--hueco' : pideAlgo ? 'punto punto--atencion' : 'punto';
            d += '<circle class="' + clase + '" cx="' + px(p.x) + '" cy="' + px(p.y) + '" r="3"/>';
        });

        return '<svg class="chispa" viewBox="0 0 300 46" role="img" aria-label="' +
               atributo(descripcion) + '">' + d + '</svg>';
    }

    // ---------------------------------------------------------------- la tira
    //
    // El odometro, digito por ventanilla. Seis ventanillas cubren la vida de un carro. Sin
    // formato invariante un separador de miles se colaria entre las ventanillas y el contador
    // dejaria de alinear.

    function tira(kilometros, descripcion) {
        var ventanillas = 6;
        var crudo = String(Math.max(0, Math.trunc(Number(kilometros) || 0)));
        if (crudo.length > ventanillas) crudo = crudo.slice(crudo.length - ventanillas);

        var relleno = crudo;
        while (relleno.length < ventanillas) relleno = '0' + relleno;

        var desde = ventanillas - crudo.length;
        var celdas = '';
        for (var i = 0; i < relleno.length; i++)
            celdas += '<span class="tira-digito' + (i >= desde ? '' : ' tira-digito--tenue') + '">' +
                      relleno[i] + '</span>';

        return '<div class="tira" role="img" aria-label="' + atributo(descripcion) + '">' + celdas + '</div>';
    }

    function atributo(t) { return String(t || '').replace(/"/g, '&quot;'); }
    function texto(t) {
        return String(t === undefined || t === null ? '' : t)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    // ---------------------------------------------------------------- la franja

    // El tono lo decide la SEVERIDAD del dominio; aca solo se pinta.
    function tono(r) {
        if (!r.veredicto) return 'franja--neutra';
        if (r.severidad === 'urgente') return 'franja--problema';
        if (r.severidad === 'consulte pronto' || r.severidad === 'vigilar') return 'franja--atencion';
        if (!r.haySerie) return 'franja--neutra';
        return '';
    }

    function sello(r) {
        if (r.sugiereRegistrarLaCargaQueFalto) return 'sello--dudoso';
        if (r.severidad === 'urgente') return 'sello--problema';
        if (r.pideAlgo) return 'sello--atencion';
        if (!r.haySerie) return '';
        return 'sello--bien';
    }

    function pintarFranja(r, refrescando) {
        var caja = $('franja');
        caja.className = 'franja ' + tono(r || {});

        var cuerpo = '';
        if (r && r.paraSonia) {
            cuerpo = '<p class="franja-frase">' + texto(mayuscula(r.paraSonia)) + '.</p>';
            if (r.pideAlgo && r.accion)
                cuerpo += '<p class="franja-accion">' + texto(mayuscula(r.accion)) + '.</p>';
        } else if (refrescando) {
            // MIENTRAS SE VA A BUSCAR, la franja lo dice. Antes decia «Apretá Actualizar» con la
            // tecla ya en «Actualizando…»: pedia una accion que estaba en curso.
            cuerpo = '<p class="franja-frase">Buscando tus cargas en la estación…</p>';
        } else {
            cuerpo = '<p class="franja-frase">Todavía no hay cargas. Apretá <em>Actualizar</em>.</p>';
        }

        caja.innerHTML = '<span class="franja-luz"></span><div class="franja-texto">' + cuerpo + '</div>';
    }

    // ---------------------------------------------------------------- el cuadro

    function instrumento(clases, dentro) {
        return '<div class="instrumento' + (clases ? ' ' + clases : '') + '">' + dentro + '</div>';
    }

    function pintarCuadro(t) {
        var caja = $('cuadro');
        var r = t.rendimiento || {}, a = t.autonomia || {}, p = t.proxima || {};
        var o = t.odometro || {}, costos = t.costos || [];
        var html = '';

        // --- autonomia
        if (a.hay) {
            var descMedidor = p.hay
                ? 'Te quedan unos ' + entero(p.kilometrosRestantes) + ' kilómetros de los ' +
                  entero(a.kilometros) + ' de un tanque lleno'
                : 'Con el tanque lleno te alcanza para unos ' + entero(a.kilometros) + ' kilómetros';

            html += instrumento('instrumento--medidor',
                '<span class="rotulo">Autonomía</span>' +
                medidor(p.fraccionRestante, descMedidor, false) +
                '<div class="cifra">~' + entero(p.hay ? p.kilometrosRestantes : a.kilometros) +
                '<span class="unidad">km</span></div>' +
                '<div class="cifra-pie">' + (p.hay
                    ? 'lo que queda, estimado &middot; con el tanque lleno son <b class="mono">~' +
                      entero(a.kilometros) + ' km</b>'
                    : 'con el tanque lleno &middot; ' + cifra(a.capacidadEnLitros) + ' L a <b class="mono">' +
                      cifra(r.habitualComoSeVe) + ' ' + texto(r.simbolo) + '</b>') + '</div>');
        } else {
            html += instrumento('instrumento--medidor',
                '<span class="rotulo">Autonomía</span>' +
                medidor(0, 'Todavía no se puede estimar la autonomía', true) +
                '<div class="cifra">—</div>' +
                '<div class="cifra-pie">Hace falta un tramo cerrado entre dos tanques llenos para ' +
                'saber cuánto rinde, y de ahí sale la autonomía.</div>');
        }

        // --- rendimiento
        var dentro = '<div class="fila"><span class="rotulo crece">Rendimiento</span>' +
            (r.veredicto ? '<span class="sello ' + sello(r) + '">' + texto(r.veredicto) + '</span>' : '') +
            '</div>';

        if (Number(r.actual) > 0) {
            dentro += '<div class="fila"><span class="cifra">' + cifra(r.actualComoSeVe) +
                '<span class="unidad">' + texto(r.simbolo) + '</span></span>';

            if (r.haySerie && r.cambioPorcentualComoSeVe) {
                var clase = r.estaMejorando ? 'delta--mejor' : r.estaEmpeorando ? 'delta--peor' : 'delta--igual';
                dentro += '<span class="delta ' + clase + '">' +
                    (r.cambioPorcentualComoSeVe > 0 ? '▲' : '▼') + ' ' +
                    Math.abs(r.cambioPorcentualComoSeVe) + '%</span>';
            }
            dentro += '</div>';

            if (r.tramosCalculados > 0)
                dentro += chispa(t.tramos, r.habitual, r.pideAlgo, r.paraSonia);

            dentro += '<div class="cifra-pie">' +
                (Number(r.habitual) > 0
                    ? 'tu nivel de siempre: <b class="mono">' + cifra(r.habitualComoSeVe) + ' ' +
                      texto(r.simbolo) + '</b> &middot; ' : '') +
                'sobre ' + r.tramosCalculados + ' ' + plural(r.tramosCalculados, 'tramo', 'tramos') +
                ' entre tanques llenos' +
                (r.tramosBloqueados > 0 ? ', ' + r.tramosBloqueados + ' sin calcular' : '') +
                '</div>';
        } else {
            dentro += '<div class="cifra">—</div><div class="cifra-pie">' +
                (r.queFalta ? texto(mayuscula(r.queFalta)) + '.'
                            : 'Todavía no hay un tramo cerrado entre dos tanques llenos.') + '</div>';
        }
        html += instrumento('instrumento--ancho', dentro);

        // --- proxima carga
        html += instrumento(p.hay ? '' : 'instrumento--apagado',
            '<span class="rotulo">Próxima carga</span>' + (p.hay
                // «~35 días» y no «en ~35 días»: el «en» en la tipografia de las cifras se leia
                // como parte del numero, y el rotulo ya dice de que se trata.
                ? '<div class="cifra cifra--chica">~' + p.diasEstimados +
                  '<span class="unidad">' + plural(p.diasEstimados, 'día', 'días') + '</span></div>' +
                  '<div class="cifra-pie">estimado &middot; te quedan <b class="mono">~' +
                  entero(p.kilometrosRestantes) + ' km</b> y andás <b class="mono">' +
                  cifra(p.kilometrosPorDia) + ' km</b> por día</div>'
                : '<div class="cifra cifra--chica">—</div>' +
                  '<div class="cifra-pie">Hace falta la autonomía y tu promedio diario.</div>'));

        // --- costo por kilometro, UNO POR MONEDA
        //
        // El dominio reparte el costo por moneda porque sumar colones con dolares no significa
        // nada, y quedarse con costos[0] escondia la otra sin decirlo: la pantalla se veia
        // completa y correcta, que es la peor forma de perder un dato.
        if (costos.length) {
            costos.forEach(function (c) {
                html += instrumento('',
                    '<span class="rotulo">Costo por kilómetro</span>' +
                    '<div class="cifra cifra--chica">' + simbolo(c.moneda) + cifra(c.montoPorKilometro) + '</div>' +
                    '<div class="cifra-pie">por kilómetro &middot; sobre <b class="mono">' +
                    entero(c.kilometros) + ' km</b> con kilometraje anotado</div>');
            });
        } else {
            html += instrumento('instrumento--apagado',
                '<span class="rotulo">Costo por kilómetro</span>' +
                '<div class="cifra cifra--chica">—</div>' +
                '<div class="cifra-pie">Hace falta kilometraje anotado para repartir el gasto.</div>');
        }

        // --- odometro
        html += instrumento('instrumento--medio' + (o.hay ? '' : ' instrumento--apagado'),
            '<span class="rotulo">Odómetro</span>' + (o.hay
                ? tira(o.kilometrosComoSeVen, 'Unos ' + entero(o.kilometrosComoSeVen) + ' kilómetros, estimados') +
                  '<div class="cifra-pie">estimado &middot; ' + o.diasDesdeLaUltimaLectura + ' ' +
                  plural(o.diasDesdeLaUltimaLectura, 'día', 'días') + ' desde la última lectura, a <b class="mono">' +
                  cifra(o.porDiaComoSeVe) + ' ' + texto(o.simboloDeLaDistancia) + '</b> por día</div>'
                : '<div class="cifra cifra--chica">—</div>' +
                  '<div class="cifra-pie">Hace falta una lectura anotada y tu promedio diario.</div>'));

        caja.innerHTML = html;
    }

    // ---------------------------------------------------------------- lo que falta

    // El tablero apagado no explica POR QUE esta apagado, y el dominio sabe la respuesta. Es lo
    // que convierte un guion en una instruccion.
    function pintarFaltantes(r) {
        var caja = $('faltantes');
        var lista = (r && r.faltantes) || [];
        if (!lista.length) {
            P.apunte(caja, 'Nada: el tablero tiene todo lo que necesita para calcular.');
            return;
        }
        // QUE FALTA y QUE DESBLOQUEA, juntos: sin lo segundo, el faltante es una tarea sin
        // razon. La frase de la persona -- `paraSonia` -- la escribe el dominio, no esta
        // pantalla.
        caja.innerHTML = lista.map(function (f) {
            return '<div class="faltante"><b>' + texto(f.queFalta) + '</b>' +
                   (f.queDesbloquea ? ' &middot; ' + texto(f.queDesbloquea) : '') +
                   (f.paraSonia ? '<div>' + texto(f.paraSonia) + '</div>' : '') + '</div>';
        }).join('');
    }

    // ---------------------------------------------------------------- arranque

    function contar(t, malo) {
        var p = $('conexion');
        p.textContent = t;
        p.classList.toggle('malo', !!malo);
    }

    function pintarTablero() {
        if (!placa) return;
        P.pedir('tablero', { placa: placa })
            .then(function (t) { pintarFranja(t.rendimiento, false); pintarCuadro(t); })
            .catch(function (e) { P.apunte($('cuadro'), e.message, true); });

        P.pedir('faltantes', { placa: placa }).then(pintarFaltantes).catch(function () { /* nada */ });
    }

    function pintarTodo() {
        P.pedir('cabecera')
            .then(function (c) {
                if (c.hayDatos === false) {
                    pintarFranja(null, false);
                    P.apunte($('cuadro'), 'Todavía no hay cargas en este dispositivo.');
                    return;
                }
                unidades = c.unidades || unidades;

                var elegir = $('placa');
                var vehiculos = c.vehiculos || [];
                elegir.innerHTML = vehiculos.map(function (v) {
                    return '<option value="' + atributo(v.placa) + '">' + texto(v.nombre || v.placa) + '</option>';
                }).join('');
                // CON UN SOLO VEHICULO NO HAY NADA QUE ELEGIR, y la app suelta lo resuelve con dos nodos:
                // una placa fija y un selector, uno solo visible. Se conserva tal cual.
                var fija = $("placa-fija");
                elegir.hidden = vehiculos.length < 2;
                fija.hidden = vehiculos.length >= 2;
                if (vehiculos.length === 1)
                    fija.innerHTML = texto(vehiculos[0].placa) +
                        (vehiculos[0].nombre && vehiculos[0].nombre !== vehiculos[0].placa
                            ? ' <span class="apodo">' + texto(vehiculos[0].nombre) + '</span>' : '');

                if (!placa || !vehiculos.some(function (v) { return v.placa === placa; }))
                    placa = vehiculos.length ? vehiculos[0].placa : '';
                elegir.value = placa;

                pintarTablero();
            })
            .catch(function (e) { P.apunte($('cuadro'), e.message, true); });
    }

    function releer() {
        var b = $('releer');
        b.disabled = true;
        b.setAttribute('aria-busy', 'true');
        b.textContent = 'Actualizando…';
        pintarFranja(null, true);

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
        $('placa').addEventListener('change', function (e) { placa = e.target.value; pintarTablero(); });

        // PRIMERO SE PINTA LO QUE YA HAY, y despues se refresca. Al reves, abrir con el
        // publicador caido dejaria el tablero en blanco hasta que el pedido falle -- y tener el
        // dato propio sirve justamente para que eso no pase.
        pintarTodo();
        P.refrescar(contar).then(function (r) { if (r) pintarTodo(); });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', arrancar);
    else arrancar();
})();
