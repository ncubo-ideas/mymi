// LA CARA DEL LABORATORIO, portada de Examenes.Wasm/Pages/MisExamenes.razor a JS plano.
//
// El protocolo NO esta aca: vive en puente.js, que lo pone el Host y es identico en todos los
// modulos. Este archivo es solo el dibujo.
//
// ESTA CARA SE REHIZO CONTRA EL DISEÑO NUEVO. La primera version portaba el anterior -`portada`,
// `veredicto`, `serie`, `visita`- y el merge del 2026-09-14 rediseño la pantalla entera. Una cara
// portada del diseño viejo no es una cara vieja: es otra institucion.
//
// LO QUE PIDE SON [View], no tools. Las dos tools declaradas son lo que el asistente puede
// preguntar; una pantalla necesita puntos, lineas de referencia y la lectura de cada serie.
(function () {
    'use strict';

    var P = window.Puente;
    var $ = P.$;

    // ---------------------------------------------------------------- formato

    function texto(t) {
        return String(t === undefined || t === null ? '' : t)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }
    function atributo(t) { return String(t || '').replace(/"/g, '&quot;'); }

    // FORMATO EXPLICITO SIEMPRE. La escala del decimal no sobrevive el journal -5571.50 entra,
    // 5571.5 sale del replay- asi que un toString pelado mostraria valores distintos antes y
    // despues de reabrir. Es el mismo `Valor()` de la app suelta, con "0.##".
    function valor(n) {
        var x = Number(n);
        if (!isFinite(x)) return String(n);
        return String(Math.round(x * 100) / 100);
    }

    var MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
    function cuando(iso) { var d = new Date(iso); return isNaN(d.getTime()) ? null : d; }
    function fecha(iso) {
        var d = cuando(iso);
        if (!d) return '';
        return (d.getDate() < 10 ? '0' : '') + d.getDate() + ' ' + MESES[d.getMonth()] + ' ' + d.getFullYear();
    }
    function dia(iso) { var d = cuando(iso); return d ? d.toISOString().slice(0, 10) : String(iso); }
    function plural(n, una, varias) { return n === 1 ? una : varias; }

    // Un numero listo para un atributo de SVG, con punto decimal siempre: el atributo se lee en
    // cultura invariante y una coma lo rompe.
    function px(n) { return (Math.round(n * 10) / 10).toString(); }

    function referencia(lista, nombre) {
        for (var i = 0; i < (lista || []).length; i++)
            if (lista[i].nombre === nombre) return lista[i];
        return null;
    }

    // LA FLECHA SIGUE A LA TENDENCIA, que es semantica del dominio -«en ascenso», «en descenso»- y
    // no al signo de un numero. En una unidad invertida el numero sube cuando la cosa mejora.
    function flecha(t) {
        return t === 'en ascenso' ? '↑' : t === 'en descenso' ? '↓' : '→';
    }

    // EL CHIP, con las mismas precedencias que `Insignia()`: repetir gana sobre todo, despues el
    // rojo reservado, despues lo que pide algo, y una serie de un solo punto se marca como tal.
    function insignia(lectura, haySerie) {
        if (lectura.sugiereRepetirElExamen) return 'chip--repetir';
        if (lectura.severidad === 'urgente') return 'chip--problema';
        if (lectura.pideAlgo) return 'chip--atencion';
        if (!haySerie) return 'chip--sinserie';
        return 'chip--estable';
    }

    function tono(lectura) {
        if (lectura.severidad === 'urgente') return 'problema';
        if (lectura.pideAlgo) return 'atencion';
        return 'neutro';
    }

    // ---------------------------------------------------------------- las formas
    //
    // UNA FORMA POR ANALITO, y no es decoracion: el dominio dice cual le toca a cada uno.
    // Dibujarlas todas igual seria la misma tabla que se rechazo en el dominio, un piso mas
    // arriba. Son las de Examenes.Wasm/Formas/*.razor, con su matematica intacta.
    //
    // EL DISEÑO NUEVO USA TRES, no cinco: la trayectoria y la pista de rango en las tarjetas, y la
    // tira en los binarios. `ProgresoHaciaMeta` salio del inicio en la ronda 7 y vive ahora en el
    // detalle, asi que esta cara tampoco lo dibuja.

    // LA TRAYECTORIA: la serie contra la banda del nivel de siempre. Sin la banda esto seria una
    // linea que sube, que es justo lo que una tabla ya decia.
    function trayectoria(puntos, referencias, pideAlgo, descripcion) {
        var habitual = referencia(referencias, 'habitual');
        var techo = referencia(referencias, 'techo');
        var unidad = puntos.length ? (puntos[0].unidad || '') : '';

        var todos = puntos.map(function (p) { return Number(p.valor); })
            .concat((referencias || []).map(function (r) { return Number(r.valor); }));
        var crudoMin = Math.min.apply(null, todos), crudoMax = Math.max.apply(null, todos);

        var aire = crudoMax === crudoMin ? 1 : (crudoMax - crudoMin) * 0.18;
        var min = crudoMin - aire, max = crudoMax + aire;

        // El grosor visual de la banda. NO sale del dominio porque no es un umbral clinico: es
        // cuanto ancho hace falta para que se lea como una franja donde la persona vive.
        var margen = (max - min) * 0.14;

        function y(v) { return max === min ? 48 : 84 - ((v - min) / (max - min)) * 72; }
        function x(i) { return puntos.length <= 1 ? 160 : 20 + (280 * i / (puntos.length - 1)); }

        var d = '';
        if (habitual) {
            var arriba = y(Number(habitual.valor) + margen), abajo = y(Number(habitual.valor) - margen);
            d += '<rect x="0" y="' + px(arriba) + '" width="320" height="' + px(abajo - arriba) + '" class="banda"/>' +
                 '<line x1="0" y1="' + px(y(Number(habitual.valor))) + '" x2="320" y2="' +
                 px(y(Number(habitual.valor))) + '" class="linea-base"/>';
        }
        if (techo)
            d += '<line x1="0" y1="' + px(y(Number(techo.valor))) + '" x2="320" y2="' +
                 px(y(Number(techo.valor))) + '" class="linea-techo"/>';

        // pathLength="1" deja que el dibujado se anime con una fraccion exacta y no con una
        // longitud adivinada: el navegador reescala el dasharray al largo real.
        d += '<polyline points="' + puntos.map(function (p, i) {
            return px(x(i)) + ',' + px(y(Number(p.valor)));
        }).join(' ') + '" class="trazo" pathLength="1"/>';

        for (var i = 0; i < puntos.length; i++) {
            var ultimo = i === puntos.length - 1;
            var clase = !ultimo ? 'punto' : (pideAlgo ? 'punto punto--atencion' : 'punto punto--final');
            d += '<circle cx="' + px(x(i)) + '" cy="' + px(y(Number(puntos[i].valor))) +
                 '" r="' + (ultimo ? 5 : 3) + '" class="' + clase + '"/>';
        }

        var notas = [];
        if (habitual) notas.push('<span class="forma-nota"><i class="muestra muestra--banda"></i>tu nivel de siempre, ' +
            valor(habitual.valor) + ' ' + texto(unidad) + '</span>');
        if (techo) notas.push('<span class="forma-nota"><i class="muestra muestra--techo"></i>esperable hasta ' +
            valor(techo.valor) + ' ' + texto(unidad) + '</span>');

        return '<div class="forma forma--trayectoria">' +
               '<svg viewBox="0 0 320 96" role="img" aria-label="' + atributo(descripcion) + '">' + d + '</svg>' +
               '<div class="forma-pie">' + notas.join('') + '</div></div>';
    }

    // LA TIRA de los binarios: estuvo, no estuvo, volvio a estar. No es una curva y no puede serlo
    // -- no hay eje vertical, porque no existe «mas positivo». Las marcas se reparten POR FECHA:
    // dos seguidas con un año en medio y dos con un mes no cuentan lo mismo.
    function tira(marcas, seguidas, conteo, ultimo, descripcion) {
        function x(i) {
            if (marcas.length <= 1) return 160;
            var desde = cuando(marcas[0].fecha), hasta = cuando(marcas[marcas.length - 1].fecha);
            var recorrido = (desde && hasta) ? (hasta - desde) : 0;
            var p = recorrido <= 0 ? i / (marcas.length - 1)
                                   : (cuando(marcas[i].fecha) - desde) / recorrido;
            return 12 + p * 296;
        }

        var d = '<line x1="12" y1="23" x2="308" y2="23" class="hilo"/>';
        if (seguidas >= 2)
            d += '<line x1="' + px(x(marcas.length - seguidas)) + '" y1="23" x2="' +
                 px(x(marcas.length - 1)) + '" y2="23" class="racha"/>';

        for (var i = 0; i < marcas.length; i++)
            d += '<circle cx="' + px(x(i)) + '" cy="23" r="' + (marcas[i].marcada ? 6.5 : 5) +
                 '" class="' + (marcas[i].marcada ? 'prueba prueba--presente' : 'prueba') + '"/>';

        return '<div class="forma forma--presencia">' +
               '<svg viewBox="0 0 320 46" role="img" aria-label="' + atributo(descripcion) + '">' + d + '</svg>' +
               '<div class="forma-pie forma-pie--extremos">' +
               '<span class="forma-nota">' + (marcas.length ? fecha(marcas[0].fecha) : '') + '</span>' +
               '<span class="forma-nota forma-nota--centro">' + texto(conteo) + '</span>' +
               '<span class="forma-nota forma-nota--der"><b>' + texto(ultimo) + '</b></span>' +
               '</div></div>';
    }

    // LA PISTA DE RANGO del diseño nuevo: donde cae el valor dentro de lo esperado, como una barra
    // con una marca. Es lo que reemplazo al grafico en las tarjetas serenas -- para un valor en
    // rango la pregunta no es «para donde viene» sino «que tan adentro estoy».
    function pista(puntos, referencias, serena) {
        var piso = referencia(referencias, 'piso') || referencia(referencias, 'habitual');
        var techo = referencia(referencias, 'techo') || referencia(referencias, 'meta');
        if (!piso || !techo || !puntos.length) return '';

        var actual = Number(puntos[puntos.length - 1].valor);
        var a = Number(piso.valor), b = Number(techo.valor);
        if (b === a) return '';

        var p = (actual - a) / (b - a);
        p = p < 0 ? 0 : p > 1 ? 1 : p;

        return '<div class="inicio-tranquilo-rango">' +
               '<div class="inicio-atencion-rango-pista' + (serena ? ' inicio-atencion-rango-pista--serena' : '') + '">' +
               '<div class="inicio-atencion-rango-marca" style="left:' + px(p * 100) + '%"></div>' +
               '</div></div>';
    }

    // EL MICRO-GRAFICO de «que cambio»: una polilinea sin ejes ni escala visible. No es un grafico
    // chico, es otra cosa -- lo que muestra es la FORMA del cambio, y el numero ya esta al lado.
    function micro(puntos) {
        if (!puntos || puntos.length < 2) return '';
        var v = puntos.map(function (p) { return Number(p.valor); });
        var min = Math.min.apply(null, v), max = Math.max.apply(null, v);
        var alto = 28, ancho = 72, margen = 3;
        function y(n) { return max === min ? alto / 2 + margen : margen + alto - ((n - min) / (max - min)) * alto; }
        var pts = v.map(function (n, i) {
            return px(margen + (v.length === 1 ? ancho / 2 : i * ancho / (v.length - 1))) + ',' + px(y(n));
        }).join(' ');
        return '<svg class="inicio-cambio-grafico" viewBox="0 0 ' + (ancho + 2 * margen) + ' ' +
               (alto + 2 * margen) + '" aria-hidden="true">' +
               '<polyline points="' + pts + '" fill="none" stroke-width="2.5"/></svg>';
    }

    // ---------------------------------------------------------------- el hero

    // EL SALUDO LO DECIDE LA SEVERIDAD, y es del dominio: no es una frase amable por omision.
    function saludo(sev) {
        if (sev === 'urgente') return 'Hay algo que conviene revisar pronto.';
        if (sev === 'consulte pronto') return 'Conviene comentar un resultado con tu médico.';
        return 'En general, tus resultados vienen bien.';
    }

    function pintarHero(a, masReciente) {
        var urgente = (a && a.urgente) || {}, enRango = (a && a.enRango) || {};
        $('saludo').textContent = saludo(urgente.severidad);
        $('secundario').textContent = enRango.paraSonia || '';

        // LA FECHA DEL ULTIMO CHEQUEO es la de la visita mas reciente, no un dato aparte.
        if (masReciente) {
            $('chequeo-fecha').textContent = fecha(masReciente);
            $('chequeo').hidden = false;
        }
    }

    // ---------------------------------------------------------------- las tarjetas

    // LA CADENA DE VALORES: «4.1 → 5.2». Solo con dos o mas mediciones, porque con una no hay
    // cadena que mostrar.
    // LOS DOS EXTREMOS DE LA TENDENCIA, que es la misma correccion que la tarjeta de cambios.
    //
    // Esto tomaba los DOS ULTIMOS PUNTOS de la serie y los ponia al lado de un chip que habla de
    // toda la serie. Resultado visto en el telefono el 2026-09-14 y todavia el 16: «212 → 212»
    // arriba de «↑ en ascenso» -- y con la frase del dominio abajo diciendo, en la misma tarjeta,
    // «subio de 182 a 212 mg/dL en 734 dias». La pantalla se contradecia a si misma dos renglones
    // despues.
    //
    // `ValorInicial` y `ValorFinal` son los extremos de la ventana sobre la que el dominio
    // dictamino, y es lo que empareja la app suelta (Tendencia.razor:259, MisExamenes.razor:1801).
    // Los puntos siguen sirviendo para DIBUJAR la forma; para la cifra no.
    function cadena(t, puntos) {
        if (!t || !t.haySerie) return '';
        if (t.valorInicial === undefined || t.valorFinal === undefined) return '';
        return valor(t.valorInicial) + ' → ' + valor(t.valorFinal);
    }

    function tarjetaAtencion(r) {
        var l = r.lectura || {}, t = r.hechos || {}, puntos = r.puntos || [];
        var dibujo = '';

        // SIN VEREDICTO NO HAY DIBUJO. Las formas se dibujan CONTRA una referencia, y esa
        // referencia es justamente lo que depende del dato que falta.
        if (!l.requiereElSexo) {
            if (r.pruebas) {
                var pr = (r.pruebas || []).map(function (x) {
                    return { fecha: x.fecha, marcada: x.esPositivo, texto: x.expresado };
                });
                var pos = pr.filter(function (x) { return x.marcada; }).length;
                if (pr.length) dibujo = tira(pr, (r.presencia || {}).positivasSeguidas,
                    pos === 0 ? pr.length + ' pruebas, ninguna' : pos + ' de ' + pr.length,
                    pr.length ? pr[pr.length - 1].texto : '', l.paraSonia);
            } else if (l.forma === 'trayectoria con linea base' && t.haySerie) {
                dibujo = trayectoria(puntos, r.referencias, l.pideAlgo, l.paraSonia);
            } else if (l.forma === 'posicion en banda') {
                dibujo = pista(puntos, r.referencias, false);
            }
        }

        var nombre = l.paraMostrar || t.paraMostrar || r.analito;
        var v = r.presencia ? texto(r.presencia.ultimoResultado)
                            : valor(t.valorFinal) + ' <span>' + texto(t.unidad) + '</span>';

        return '<div class="inicio-atencion-tarjeta">' +
            '<div class="inicio-atencion-cabeza">' +
                '<div class="inicio-atencion-titulo">' +
                    '<div class="inicio-atencion-nombre-grupo">' +
                        '<span class="inicio-atencion-nombre">' + texto(nombre) + '</span>' +
                        (t.tendencia ? '<span class="inicio-atencion-estado">' + flecha(t.tendencia) + ' ' +
                            texto(t.tendencia) + '</span>' : '') +
                    '</div>' +
                '</div>' +
                '<span class="inicio-atencion-valor">' + v + '</span>' +
            '</div>' +
            (cadena(t, puntos) ? '<p class="inicio-atencion-serie">' + cadena(t, puntos) + '</p>' : '') +
            (l.referenciaLegible ? '<p class="inicio-atencion-referencia">referencia: ' +
                texto(l.referenciaLegible) + '</p>' : '') +
            dibujo +
            (l.paraSonia ? '<p class="inicio-atencion-detalle">' + texto(l.paraSonia) +
                (l.pideAlgo && l.accion ? ': <b>' + texto(l.accion) + '</b>' : '.') + '</p>' : '') +
        '</div>';
    }

    function itemTranquilo(r) {
        var l = r.lectura || {}, t = r.hechos || {}, puntos = r.puntos || [];
        return '<li><div class="inicio-tranquilo-item">' +
            '<div class="inicio-tranquilo-cabeza">' +
                '<span class="inicio-tranquilo-nombre">' + texto(l.paraMostrar || t.paraMostrar || r.analito) + '</span>' +
                '<span class="inicio-tranquilo-cabeza-derecha">' +
                    '<span class="inicio-tranquilo-valor">' + valor(t.valorFinal) +
                    ' <small>' + texto(t.unidad) + '</small></span>' +
                '</span>' +
            '</div>' +
            pista(puntos, r.referencias, true) +
            '<div class="inicio-tranquilo-rango-etiquetas">' +
                '<span class="chip ' + insignia(l, t.haySerie) + '">en rango</span>' +
                '<p class="inicio-tranquilo-nota">' + (t.mediciones || 0) + ' ' +
                plural(t.mediciones, 'medición', 'mediciones') +
                (t.tendencia ? ' · ' + texto(t.tendencia) : '') + '</p>' +
            '</div>' +
            (l.paraSonia ? '<p class="inicio-tranquilo-detalle">' + texto(l.paraSonia) + '.</p>' : '') +
        '</div></li>';
    }

    function tarjetaCambio(r) {
        var l = r.lectura || {}, t = r.hechos || {}, puntos = r.puntos || [];
        return '<div class="inicio-cambio-tarjeta inicio-cambio-tarjeta--' + tono(l) + '">' +
            '<div class="inicio-cambio-cabeza">' +
                '<span class="inicio-cambio-nombre">' + texto(l.paraMostrar || r.analito) + '</span>' +
                (t.tendencia ? '<span class="inicio-cambio-estado inicio-cambio-estado--' + tono(l) + '">' +
                    flecha(t.tendencia) + ' ' + texto(t.tendencia) + '</span>' : '') +
            '</div>' +
            // LAS CIFRAS SON LAS DE LA TENDENCIA, no los dos ultimos puntos de la serie.
            //
            // Esto decia `puntos[n-2] → puntos[n-1]`, y producia renglones que se contradicen
            // solos: visto en el telefono el 2026-09-14, «212 → 212» con el chip «↑ en ascenso»
            // al lado. Los datos no estaban mal -- el veredicto es sobre la SERIE y esos eran los
            // dos ultimos, que pueden ser iguales dentro de una serie que sube --, pero el
            // renglon queda ilegible, y una pantalla de laboratorio que se contradice no se puede
            // defender diciendo que tecnicamente ambas cosas son ciertas.
            //
            // El dominio ya entrega los extremos de SU ventana, la misma sobre la que dictamino:
            // `ValorInicial` y `ValorFinal`. Es lo que empareja la app suelta -- ver
            // Tendencia.razor:259 y MisExamenes.razor:1801, las dos veces con esos dos campos.
            '<div class="inicio-cambio-cifras">' + valor(t.valorInicial) + ' → <b>' +
                valor(t.valorFinal) + '</b> <span>' + texto(t.unidad) + '</span></div>' +
            micro(puntos) +
            (l.paraSonia ? '<p class="inicio-cambio-conclusion">' + texto(l.paraSonia) + '.</p>' : '') +
        '</div>';
    }

    function tarjetaSinTendencia(r) {
        var l = r.lectura || {}, t = r.hechos || {};
        var n = t.mediciones || 1;
        return '<div class="inicio-sintendencia-tarjeta">' +
            '<div class="inicio-sintendencia-cuerpo">' +
                '<p class="inicio-sintendencia-nombre">' + texto(l.paraMostrar || r.analito) + '</p>' +
                '<div class="inicio-sintendencia-fila">' +
                    '<span class="inicio-sintendencia-valor">' + valor(t.valorFinal) +
                    ' <span>' + texto(t.unidad) + '</span></span>' +
                    '<span class="chip chip--repetir">' + n + ' ' + plural(n, 'medición', 'mediciones') + '</span>' +
                '</div>' +
                '<p class="inicio-sintendencia-texto">' +
                texto(l.paraSonia || 'Con una sola medición todavía no se puede decir para dónde viene.') +
                '</p>' +
            '</div>' +
        '</div>';
    }

    // ---------------------------------------------------------------- el reparto
    //
    // CADA ANALITO CAE EN UNA SOLA SECCION, y quien decide es el dominio: si pide algo va a
    // atencion; si tiene una sola medicion, a sin-tendencia; si cambio entre las dos ultimas,
    // ademas aparece en cambios; y el resto, en lo que esta bien.
    var ultimaPasada = 0;

    function repartir(lecturas) {
        var atencion = [], tranquilo = [], cambios = [], sintendencia = [];

        lecturas.forEach(function (r) {
            if (!r || !r.lectura) return;
            var l = r.lectura, t = r.hechos || {}, puntos = r.puntos || [];

            // Un binario o un nominal no tienen tendencia ni valor continuo: van con su tira, y
            // van arriba porque son los que el dominio marca cuando aparecen.
            if (r.presencia || r.constancia) { atencion.push(r); return; }

            if (l.pideAlgo || l.severidad === 'urgente') { atencion.push(r); return; }
            if (!t.haySerie || (t.mediciones || 0) < 2) { sintendencia.push(r); return; }

            if (puntos.length >= 2 &&
                Number(puntos[puntos.length - 1].valor) !== Number(puntos[puntos.length - 2].valor))
                cambios.push(r);

            tranquilo.push(r);
        });

        return { atencion: atencion, tranquilo: tranquilo, cambios: cambios, sintendencia: sintendencia };
    }

    function mostrar(id, hay, html) {
        var s = $(id);
        s.hidden = !hay;
        if (hay) s.innerHTML = html;
    }

    function pintarSecciones(g) {
        mostrar('atencion', g.atencion.length,
            '<div class="inicio-atencion-encabezado">' +
                '<p class="inicio-atencion-rotulo">merece atención</p>' +
                '<span class="inicio-atencion-conteo">' +
                    '<span class="inicio-atencion-conteo-punto" aria-hidden="true"></span>' +
                    g.atencion.length + '</span>' +
            '</div>' + g.atencion.map(tarjetaAtencion).join(''));

        mostrar('tranquilo', g.tranquilo.length,
            '<p class="inicio-tranquilo-rotulo">lo demás está bien</p>' +
            '<div class="inicio-tranquilo-tarjeta">' +
                '<p class="inicio-tranquilo-titulo">Estos resultados están dentro de lo esperado.</p>' +
                '<ul class="inicio-tranquilo-lista">' + g.tranquilo.map(itemTranquilo).join('') + '</ul>' +
            '</div>');

        mostrar('cambios', g.cambios.length,
            '<h2 class="seccion">Qué cambió desde tu último chequeo</h2>' +
            '<p class="inicio-cambios-intro">Así han cambiado tus resultados recientes.</p>' +
            g.cambios.map(tarjetaCambio).join(''));

        mostrar('sintendencia', g.sintendencia.length,
            '<p class="inicio-sintendencia-rotulo">todavía no podemos ver la tendencia</p>' +
            g.sintendencia.map(tarjetaSinTendencia).join(''));
    }

    // ---------------------------------------------------------------- el historial

    // UNA CRONOLOGIA DE VISITAS, no una tabla: lo que esta seccion aporta es QUE PASO CADA DIA,
    // que es como se recuerda una ida al laboratorio. El dominio manda las celdas ya colapsadas
    // por dia; agruparlas es desarmado, no decision.
    function visitas(celdas) {
        var porFecha = {}, orden = [];
        (celdas || []).forEach(function (c) {
            var clave = dia(c.fecha);
            if (!porFecha[clave]) { porFecha[clave] = { fecha: c.fecha, cosas: [] }; orden.push(clave); }
            porFecha[clave].cosas.push(c);
        });
        orden.sort(function (u, v) { return u < v ? 1 : u > v ? -1 : 0; });
        return orden.map(function (k) { return porFecha[k]; });
    }

    function pintarHistorial(r) {
        var caja = $('historial-cuerpo');
        var v = visitas(r && r.celdas);
        if (!v.length) {
            P.apunte(caja, 'Todavía no hay exámenes. Apretá Actualizar.');
            return null;
        }

        caja.innerHTML =
            '<p class="inicio-historial-intro">Aquí podés ver tus resultados anteriores.</p>' +
            '<ul class="inicio-historial-timeline">' + v.map(function (x, i) {
                return '<li class="inicio-historial-evento' +
                    (i === 0 ? ' inicio-historial-evento--reciente' : '') + '">' +
                    '<span class="inicio-historial-nodo" aria-hidden="true"></span>' +
                    '<div class="inicio-historial-fila">' +
                        '<span class="inicio-historial-fecha">' + fecha(x.fecha) + '</span>' +
                        '<span class="inicio-historial-cantidad">' + x.cosas.length + ' ' +
                        plural(x.cosas.length, 'resultado', 'resultados') + '</span>' +
                    '</div></li>';
            }).join('') + '</ul>';

        return v[0].fecha;
    }

    // ---------------------------------------------------------------- el pedido

    // EL DATO QUE FALTA. Aca NO es editable, y esa es una diferencia con la app suelta: declarar
    // el sexo es una ESCRITURA, y `consultar` solo alcanza [View].
    function pintarPedido(a) {
        var caja = $('pedido');
        caja.innerHTML = '';
        var p = a && a.faltaElSexo;
        if (!p || !p.haceFalta) return;

        caja.innerHTML = '<div class="inicio-config-fila">' +
            '<p class="inicio-config-texto"><b>' + texto(p.paraSonia || 'Falta un dato tuyo') + '</b>' +
            (p.porQue ? '<br>' + texto(p.porQue) : '') + '</p></div>';
    }

    // ---------------------------------------------------------------- arranque

    function contar(t, malo) {
        var p = $('conexion');
        p.textContent = t;
        p.classList.toggle('malo', !!malo);
    }

    function pintarTodo() {
        var pasada = ++ultimaPasada;

        // EL HERO NECESITA LAS DOS: la severidad sale de `alertas` y la fecha del ultimo chequeo
        // del historial. Se encadenan porque pintar medio hero y completarlo despues haria saltar
        // el titular delante de quien lo esta leyendo.
        P.pedir('historial')
            .then(function (r) { return pintarHistorial(r); })
            .catch(function (e) { P.apunte($('historial-cuerpo'), e.message, true); return null; })
            .then(function (masReciente) {
                return P.pedir('alertas').then(function (a) {
                    if (pasada !== ultimaPasada) return;
                    pintarHero(a, masReciente);
                    pintarPedido(a);
                });
            })
            .catch(function () { /* el hero se queda con su frase por omision */ });

        // LAS SERIES, DE A UNA Y EN ORDEN. Cada una es una pregunta al Host; se juntan TODAS antes
        // de repartir, porque una tarjeta no puede saber a que seccion va sin ver al resto.
        //
        // Y CADA PASADA TIENE SU NUMERO: la pantalla se pinta dos veces -lo que ya hay, y otra vez
        // cuando vuelve el cartero-, y sin la guarda las dos cadenas repartirian sobre las mismas
        // secciones.
        P.pedir('analitos')
            .then(function (r) {
                var nombres = (r && r.analitos) || [];
                if (!nombres.length) return null;

                var juntadas = [];
                return nombres.reduce(function (antes, a) {
                    return antes
                        .then(function () { return P.pedir('tendencia', { analito: a }); })
                        .then(function (x) { juntadas.push(x); })
                        .catch(function () { /* un analito sin serie no rompe los demas */ });
                }, Promise.resolve()).then(function () { return juntadas; });
            })
            .then(function (juntadas) {
                if (!juntadas || pasada !== ultimaPasada) return;
                pintarSecciones(repartir(juntadas));
            })
            .catch(function (e) { contar(e.message, true); });
    }

    function releer() {
        var b = $('releer');
        b.disabled = true;
        b.setAttribute('aria-busy', 'true');
        b.classList.add('gira');

        P.refrescar(contar).then(function (r) {
            if (r) pintarTodo();
            b.disabled = false;
            b.setAttribute('aria-busy', 'false');
            b.classList.remove('gira');
        });
    }

    function arrancar() {
        P.listo();
        $('releer').addEventListener('click', releer);

        // PRIMERO SE PINTA LO QUE YA HAY, y despues se refresca. Al reves, abrir con el publicador
        // caido dejaria la pantalla en blanco hasta que el pedido falle -- y tener el dato propio
        // sirve justamente para que eso no pase.
        pintarTodo();
        P.refrescar(contar).then(function (r) { if (r) pintarTodo(); });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', arrancar);
    else arrancar();
})();
