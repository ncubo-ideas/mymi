// LA CARA PREGUNTA; LA SUPERFICIE CONTESTA.
//
// En la forma hospedada la pagina ES la superficie: Gnothi le pregunta por `window.gnothi.call`
// y ella contesta. Aca es al reves, y es toda la diferencia de la forma hibrida: esta pagina no
// tiene motor ni dominio -- viajan al lado, en dominio/*.dll, y corren sobre el motor de Gnothi.
//
// EL VERBO ES `consultar`, y su respuesta NO vuelve por retorno. Los metodos que Android inyecta
// con @JavascriptInterface son SINCRONOS y la pagina se congelaria mientras el Host trabaja; iOS
// ni siquiera puede devolver un valor. Por eso todo el contrato contesta por callback, y la
// respuesta aterriza en `window.gnothi.respuesta`.
//
// `consultar` NO lleva el nombre del modulo a proposito: una cara solo puede preguntar por el
// suyo. Ver WebModule.Serve.
//
// Y LO QUE PIDE NO SON LAS TOOLS: son [View]. Las tools declaradas son lo que el asistente puede
// preguntar -- dos, gruesas --; una pantalla necesita puntos y lineas de referencia.
//
// ------------------------------------------------------------------------------------------
//
// ESTA PANTALLA ES LA DE LA APP SUELTA, portada. El markup, las clases y los cinco dibujos
// salen de Examenes.Wasm/Pages/MisExamenes.razor y de Examenes.Wasm/Formas/*.razor. Donde hay
// una diferencia esta dicha en el comentario de al lado; no hay ninguna silenciosa.
(function () {
    'use strict';

    var pendientes = {};
    var siguiente = 0;

    // El buzon donde el Host deja las respuestas. Es el MISMO nombre que usa la forma hospedada
    // -- el mecanismo no cambia, cambia quien pregunta.
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
                    'esta pagina solo corre dentro de Gnothi, en la forma hibrida: ' +
                    'no hay puente, y sin puente no tiene a quien preguntarle'));
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

    function pedir(vista, argumentos) {
        return consultar(vista, argumentos).then(function (json) { return JSON.parse(json); });
    }

    // ---------------------------------------------------------------- utilidades

    function $(id) { return document.getElementById(id); }

    function el(tag, clase, texto) {
        var n = document.createElement(tag);
        if (clase) n.className = clase;
        if (texto !== undefined && texto !== null) n.textContent = texto;
        return n;
    }

    function apunte(caja, texto, malo) {
        caja.innerHTML = '';
        caja.appendChild(el('p', malo ? 'apunte malo' : 'apunte', texto));
    }

    // FORMATO EXPLICITO SIEMPRE. Medido en el negocio de facturas: la escala del decimal no
    // sobrevive el journal -5571.50 entra, 5571.5 sale del replay- asi que un toString pelado
    // mostraria valores distintos antes y despues de reabrir la app. Es el mismo `Valor()` de
    // la app suelta, con "0.##".
    function valor(n) {
        var x = Number(n);
        if (!isFinite(x)) return String(n);
        return String(Math.round(x * 100) / 100);
    }

    var MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
    function cuando(iso) { var d = new Date(iso); return isNaN(d.getTime()) ? null : d; }
    function diaYMes(iso) { var d = cuando(iso); return d ? (d.getDate() < 10 ? '0' : '') + d.getDate() + ' ' + MESES[d.getMonth()] : ''; }
    function mesYAnio(iso) { var d = cuando(iso); return d ? MESES[d.getMonth()] + ' ' + d.getFullYear() : ''; }
    function anio(iso) { var d = cuando(iso); return d ? d.getFullYear() : ''; }
    function dia(iso) { var d = cuando(iso); return d ? d.toISOString().slice(0, 10) : String(iso); }

    function mayuscula(frase) {
        return !frase ? '' : frase.charAt(0).toUpperCase() + frase.slice(1);
    }

    function plural(n, una, varias) { return n === 1 ? una : varias; }

    // Un numero listo para un atributo de SVG, con punto decimal siempre: el atributo se lee
    // en cultura invariante y una coma lo rompe.
    function px(n) { return (Math.round(n * 10) / 10).toString(); }

    function svg(alto, etiqueta, cuerpo) {
        return '<svg viewBox="0 0 320 ' + alto + '" role="img" aria-label="' +
            String(etiqueta || '').replace(/"/g, '&quot;') + '">' + cuerpo + '</svg>';
    }

    function pie(notas, extremos) {
        return '<div class="forma-pie' + (extremos ? ' forma-pie--extremos' : '') + '">' +
            notas.join('') + '</div>';
    }

    function nota(texto, clase) {
        return '<span class="forma-nota' + (clase ? ' ' + clase : '') + '">' + texto + '</span>';
    }

    function referencia(lista, nombre) {
        for (var i = 0; i < (lista || []).length; i++)
            if (lista[i].nombre === nombre) return lista[i];
        return null;
    }

    // ---------------------------------------------------------------- las cinco formas
    //
    // UNA FORMA POR ANALITO, y no es decoracion: el dominio dice cual le toca a cada uno.
    // Dibujarlos igual seria la misma tabla que se rechazo en el dominio, un piso mas arriba.

    // LA FORMA DEL ANTIGENO PROSTATICO: una trayectoria contra la linea base de la persona.
    //
    // No es «un grafico de la serie». Lo que hace legible este dibujo es la banda: el nivel en
    // el que esa persona vive. Un 3.0 pegado a la banda no dice nada; el mismo 3.0 lejos de
    // ella es el hallazgo. Sin la banda esto seria una linea que sube, que es justo lo que una
    // tabla ya decia.
    function trayectoria(puntos, referencias, pideAlgo, descripcion) {
        var habitual = referencia(referencias, 'habitual');
        var techo = referencia(referencias, 'techo');
        var unidad = puntos.length ? (puntos[0].unidad || '') : '';

        var todos = puntos.map(function (p) { return Number(p.valor); })
            .concat((referencias || []).map(function (r) { return Number(r.valor); }));
        var crudoMin = Math.min.apply(null, todos), crudoMax = Math.max.apply(null, todos);

        // Aire arriba y abajo para que ni el punto ni las lineas queden pegados al borde.
        var aire = crudoMax === crudoMin ? 1 : (crudoMax - crudoMin) * 0.18;
        var min = crudoMin - aire, max = crudoMax + aire;

        // El grosor visual de la banda. NO sale del dominio porque no es un umbral clinico: es
        // cuanto ancho hace falta para que se lea como una franja donde la persona vive.
        var margen = (max - min) * 0.14;

        function y(v) {
            if (max === min) return 48;
            return 84 - ((v - min) / (max - min)) * 72;   // el eje va al reves que el SVG
        }
        function x(i) { return puntos.length <= 1 ? 160 : 20 + (280 * i / (puntos.length - 1)); }

        var d = '';
        if (habitual) {
            var arriba = y(Number(habitual.valor) + margen);
            var abajo = y(Number(habitual.valor) - margen);
            d += '<rect x="0" y="' + px(arriba) + '" width="320" height="' +
                 px(abajo - arriba) + '" class="banda"/>';
            d += '<line x1="0" y1="' + px(y(Number(habitual.valor))) + '" x2="320" y2="' +
                 px(y(Number(habitual.valor))) + '" class="linea-base"/>';
        }
        if (techo) {
            d += '<line x1="0" y1="' + px(y(Number(techo.valor))) + '" x2="320" y2="' +
                 px(y(Number(techo.valor))) + '" class="linea-techo"/>';
        }

        // pathLength="1" deja que el dibujado del trazo se anime con una fraccion exacta y no
        // con una longitud adivinada: el navegador reescala el dasharray al largo real.
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
        if (habitual) notas.push(nota('<i class="muestra muestra--banda"></i>tu nivel de siempre, ' +
            valor(habitual.valor) + ' ' + unidad));
        if (techo) notas.push(nota('<i class="muestra muestra--techo"></i>esperable hasta ' +
            valor(techo.valor) + ' ' + unidad));

        return '<div class="forma forma--trayectoria">' + svg(96, descripcion, d) +
               pie(notas, false) + '</div>';
    }

    // LA FORMA DEL COLESTEROL: un progreso, no una serie.
    //
    // Lo que importa aca no es de donde viene toda la vida, es de donde arranco el episodio y
    // cuanto falta. Por eso el eje NO es el tiempo: es el valor, y va de la partida a la meta.
    // Dibujar esto como una linea de tiempo seria copiar la forma del antigeno en un analito
    // que no se lee asi.
    function progreso(puntos, referencias, descripcion) {
        var partida = referencia(referencias, 'partida');
        var meta = referencia(referencias, 'meta');
        var actual = puntos.length ? Number(puntos[puntos.length - 1].valor) : 0;
        var unidad = puntos.length ? (puntos[0].unidad || '') : '';

        var hayEpisodio = !!partida && !!meta && Number(partida.valor) > Number(meta.valor);
        var yaLlego = !!meta && actual <= Number(meta.valor);

        var p;
        if (!hayEpisodio) p = yaLlego ? 1 : 0;
        else {
            p = (Number(partida.valor) - actual) / (Number(partida.valor) - Number(meta.valor));
            p = p < 0 ? 0 : p > 1 ? 1 : p;
        }
        var ahora = 12 + p * 296;

        var d = '<rect x="12" y="20" width="296" height="12" rx="6" class="pista"/>';

        // LO RECORRIDO en verde y LO QUE FALTA en ambar, siempre los dos. Sin el tramo ambar,
        // alguien que todavia no empezo a bajar veia una pista gris vacia que no decia nada.
        if (hayEpisodio) d += '<rect x="12" y="20" width="' + px(p * 296) + '" height="12" rx="6" class="avance"/>';
        if (!yaLlego) d += '<rect x="' + px(ahora) + '" y="20" width="' + px((1 - p) * 296) +
                           '" height="12" rx="6" class="pendiente"/>';

        d += '<line x1="308" y1="10" x2="308" y2="42" class="hito-meta"/>';
        d += '<circle cx="' + px(ahora) + '" cy="26" r="7" class="' +
             (yaLlego ? 'marca marca--llego' : 'marca') + '"/>';

        var izquierda = (hayEpisodio ? 'venías de ' + valor(partida.valor) : 'hoy') +
            ' <b>' + valor(actual) + ' ' + unidad + '</b>';
        var derecha = 'meta <b>' + valor(meta ? meta.valor : actual) + ' ' + unidad + '</b>';

        return '<div class="forma forma--progreso">' + svg(52, descripcion, d) +
               pie([nota(izquierda), nota(derecha, 'forma-nota--der')], true) + '</div>';
    }

    // LA FORMA DE LA HEMOGLOBINA: donde caes dentro de una banda con piso Y techo.
    //
    // Es el unico analito del demo al que le conviene quedarse quieto: alejarse es peor en las
    // dos direcciones. Por eso su dibujo no es una flecha ni un progreso -- es una posicion.
    function posicion(puntos, referencias, descripcion) {
        var piso = referencia(referencias, 'piso');
        var techo = referencia(referencias, 'techo');
        var actual = puntos.length ? Number(puntos[puntos.length - 1].valor) : 0;
        var unidad = puntos.length ? (puntos[0].unidad || '') : '';

        var dentro = !!piso && !!techo && actual >= Number(piso.valor) && actual <= Number(techo.valor);

        // El eje muestra la banda con aire a los lados, para que un valor por fuera se vea
        // FUERA y no pegado al borde.
        var aire = (!piso || !techo) ? 1 : (Number(techo.valor) - Number(piso.valor)) * 0.35;
        var min = (piso ? Number(piso.valor) : actual) - aire;
        var max = (techo ? Number(techo.valor) : actual) + aire;

        function donde(v) {
            if (max === min) return 160;
            var p = (v - min) / (max - min);
            return 12 + (p < 0 ? 0 : p > 1 ? 1 : p) * 296;
        }

        var izq = donde(piso ? Number(piso.valor) : min);
        var der = donde(techo ? Number(techo.valor) : max);
        var marca = donde(actual);

        var d = '<rect x="12" y="18" width="296" height="16" rx="3" class="fuera"/>' +
                '<rect x="' + px(izq) + '" y="18" width="' + px(der - izq) + '" height="16" rx="3" class="dentro"/>' +
                '<line x1="' + px(marca) + '" y1="10" x2="' + px(marca) + '" y2="42" class="' +
                (dentro ? 'aguja' : 'aguja aguja--fuera') + '"/>';

        return '<div class="forma forma--banda">' + svg(52, descripcion, d) + pie([
            nota(valor(piso ? piso.valor : 0) + ' ' + unidad),
            nota('<b>' + valor(actual) + ' ' + unidad + '</b>', 'forma-nota--centro'),
            nota(valor(techo ? techo.valor : 0) + ' ' + unidad, 'forma-nota--der')
        ], true) + '</div>';
    }

    // LA FORMA DE LOS BINARIOS Y LOS NOMINALES: estuvo, no estuvo, volvio a estar.
    //
    // No es una curva y no puede serlo -- no hay eje vertical, porque no existe «mas positivo»
    // y las categorias no se ordenan: ambar no es «mas» que amarillo palido. Lo que se dibuja
    // son las fechas y si en cada una habia o no habia.
    //
    // Y ESO ES UN DIBUJO, no una frase. Ver tres pruebas seguidas marcadas dice algo que la
    // ultima sola no dice: que ya se repitio y volvio a salir.
    //
    // ACA LAS DOS FORMAS SON UNA SOLA FUNCION, y en la app suelta son dos componentes. Alla la
    // separacion la exige el tipo -- PruebaEnSerie y ObservacionEnSerie son dos clases -- y el
    // comentario dice «por eso son dos componentes y no uno con un flag». En JS no hay tipos
    // que separar: el que llama ya tradujo su vocabulario, y duplicar el dibujo seria copiar
    // sin ganar la distincion.
    function tira(marcas, seguidas, conteo, ultimo, descripcion) {
        function x(i) {
            if (marcas.length <= 1) return 160;
            var desde = cuando(marcas[0].fecha), hasta = cuando(marcas[marcas.length - 1].fecha);
            var recorrido = (desde && hasta) ? (hasta - desde) : 0;

            // Las marcas se reparten POR FECHA y no en pasos iguales: dos seguidas con un anio
            // en medio y dos con un mes no cuentan lo mismo. Cuando todas caen el mismo dia se
            // distribuyen parejas, que es lo unico honesto que se puede hacer con eso.
            var p = recorrido <= 0
                ? i / (marcas.length - 1)
                : (cuando(marcas[i].fecha) - desde) / recorrido;
            return 12 + p * 296;
        }

        var d = '<line x1="12" y1="23" x2="308" y2="23" class="hilo"/>';

        // La racha del final, cuando la hay: dos o mas seguidas es otra cosa que una.
        if (seguidas >= 2)
            d += '<line x1="' + px(x(marcas.length - seguidas)) + '" y1="23" x2="' +
                 px(x(marcas.length - 1)) + '" y2="23" class="racha"/>';

        for (var i = 0; i < marcas.length; i++)
            d += '<circle cx="' + px(x(i)) + '" cy="23" r="' + (marcas[i].marcada ? 6.5 : 5) +
                 '" class="' + (marcas[i].marcada ? 'prueba prueba--presente' : 'prueba') + '"/>';

        return '<div class="forma forma--presencia">' + svg(46, descripcion, d) + pie([
            nota(marcas.length ? mesYAnio(marcas[0].fecha) : ''),
            nota(conteo, 'forma-nota--centro'),
            nota('<b>' + (ultimo || '') + '</b>', 'forma-nota--der')
        ], true) + '</div>';
    }

    // ---------------------------------------------------------------- la tarjeta de serie

    // EL CHIP. Es el mismo `Insignia()` de la app suelta, con las mismas precedencias: repetir
    // gana sobre todo, despues el rojo reservado, despues lo que pide algo, y una serie de un
    // solo punto se marca como tal en vez de decir «estable».
    function insignia(lectura, haySerie) {
        if (lectura.sugiereRepetirElExamen) return 'chip--repetir';
        if (lectura.severidad === 'urgente') return 'chip--problema';
        if (lectura.pideAlgo) return 'chip--atencion';
        if (!haySerie) return 'chip--sinserie';
        return 'chip--estable';
    }

    function cabezaDeSerie(nombre, chipTexto, chipClase, relato, valorGrande, unidad, ref) {
        var izq = '<div>' +
            '<span class="serie-nombre">' + nombre + '</span> ' +
            '<span class="chip ' + chipClase + '">' + chipTexto + '</span>' +
            (relato ? '<div class="serie-relato">' + relato + '</div>' : '') +
            '</div>';

        var der = '<div>' +
            '<div class="serie-valor">' + valorGrande +
            (unidad ? ' <span>' + unidad + '</span>' : '') + '</div>' +
            // NINGUN NUMERO SIN SU REFERENCIA. La escribe el dominio, no esta pantalla.
            (ref ? '<div class="serie-referencia">' + ref + '</div>' : '') +
            '</div>';

        return '<div class="serie-cabeza">' + izq + der + '</div>';
    }

    // La consecuencia va abajo, separada por una linea: es una conclusion, no un dato mas.
    //
    // EL ESPACIO DESPUES DEL PUNTO es la unica letra en que esto se aparta del razor. Alla el
    // separador es `.` pelado, y cuando el analito no pide nada PERO trae accion -- el cloro de
    // una sola medicion -- las dos frases salen pegadas: «…para donde viene.repeti el examen».
    // Medido en el telefono el 2026-09-11.
    function lecturaDeSerie(lectura) {
        if (!lectura.paraSonia) return '';
        var accion = lectura.accion || '';
        return '<div class="serie-lectura' + (lectura.pideAlgo ? ' serie-lectura--atencion' : '') + '">' +
            lectura.paraSonia + (lectura.pideAlgo ? ': ' : '. ') +
            '<b>' + accion + '</b></div>';
    }

    function tarjetaDeSerie(r) {
        var lectura = r.lectura || {};
        var tarjeta = el('article', 'serie');

        // --- presencia: un binario
        if (r.presencia) {
            var p = r.presencia;
            var pruebas = (r.pruebas || []).map(function (x) {
                return { fecha: x.fecha, marcada: x.esPositivo, texto: x.expresado };
            });
            var positivas = pruebas.filter(function (x) { return x.marcada; }).length;

            tarjeta.innerHTML =
                cabezaDeSerie(p.paraMostrar, p.presencia, insignia(lectura, p.haySerie),
                    p.haySerie ? p.positivas + ' de ' + p.mediciones + ' pruebas en ' + p.dias + ' ' +
                        plural(p.dias, 'día', 'días') : '',
                    p.ultimoResultado, '', lectura.referenciaLegible) +
                (p.haySerie ? tira(pruebas, p.positivasSeguidas,
                    positivas === 0 ? pruebas.length + ' pruebas, ninguna'
                                    : positivas + ' de ' + pruebas.length,
                    pruebas.length ? pruebas[pruebas.length - 1].texto : '',
                    lectura.paraSonia) : '') +
                lecturaDeSerie(lectura);
            return tarjeta;
        }

        // --- categoria: un nominal
        if (r.constancia) {
            var c = r.constancia;
            var obs = (r.observaciones || []).map(function (x) {
                return { fecha: x.fecha, marcada: x.esHallazgo, texto: x.expresado };
            });
            var afuera = obs.filter(function (x) { return x.marcada; }).length;

            tarjeta.innerHTML =
                cabezaDeSerie(c.paraMostrar, c.constancia, insignia(lectura, c.haySerie),
                    c.haySerie ? (c.mediciones - c.noEsperables) + ' de ' + c.mediciones +
                        ' veces dentro de lo esperable, en ' + c.dias + ' ' +
                        plural(c.dias, 'día', 'días') : '',
                    c.ultimaCategoria, '', lectura.referenciaLegible) +
                (c.haySerie ? tira(obs, c.noEsperablesSeguidas,
                    afuera === 0 ? obs.length + ' veces, siempre lo esperable'
                                 : afuera + ' de ' + obs.length + ' fuera de lo esperable',
                    obs.length ? obs[obs.length - 1].texto : '',
                    lectura.paraSonia) : '') +
                lecturaDeSerie(lectura);
            return tarjeta;
        }

        // --- continua: trayectoria, progreso o banda
        var t = r.hechos || {};
        var puntos = r.puntos || [];
        var refs = r.referencias || [];

        var dibujo = '';
        // SIN VEREDICTO NO HAY DIBUJO. Las formas se dibujan CONTRA una referencia -la banda de
        // la hemoglobina, el techo del antigeno- y esa referencia es justamente lo que depende
        // del dato que falta. El valor sigue a la vista arriba; lo que se retiene es la lectura.
        //
        // Y la trayectoria necesita serie; la meta y la banda se dibujan con un solo punto,
        // porque no hablan del tiempo sino de donde estas parado.
        if (!lectura.requiereElSexo && (t.haySerie || lectura.forma !== 'trayectoria con linea base')) {
            if (lectura.forma === 'trayectoria con linea base')
                dibujo = trayectoria(puntos, refs, lectura.pideAlgo, lectura.paraSonia);
            else if (lectura.forma === 'progreso hacia meta')
                dibujo = progreso(puntos, refs, lectura.paraSonia);
            else if (lectura.forma === 'posicion en banda')
                dibujo = posicion(puntos, refs, lectura.paraSonia);
        }

        tarjeta.innerHTML =
            cabezaDeSerie(t.paraMostrar || r.analito, t.tendencia || '', insignia(lectura, t.haySerie),
                t.haySerie ? 'pasó de ' + valor(t.valorInicial) + ' a ' + valor(t.valorFinal) + ' ' +
                    (t.unidad || '') + ' en ' + t.dias + ' ' + plural(t.dias, 'día', 'días') +
                    ', sobre ' + t.mediciones + ' mediciones' : '',
                valor(t.valorFinal), t.unidad, lectura.referenciaLegible) +
            dibujo +
            lecturaDeSerie(lectura);
        return tarjeta;
    }

    // ---------------------------------------------------------------- la portada

    // La carita del landing: el circulo, los dos ojos y la boca. Es lo que hace que la pantalla
    // se reconozca de un vistazo. La diferencia entre «bien» y «mirá esto» son dos paths, no
    // dos dibujos.
    function cara(contenta) {
        return '<svg viewBox="0 0 24 24" aria-hidden="true">' +
            '<circle cx="12" cy="12" r="10" fill="none" stroke-width="1.6"/>' +
            '<circle cx="8.5" cy="10" r="0.6" fill="currentColor" stroke="none"/>' +
            '<circle cx="15.5" cy="10" r="0.6" fill="currentColor" stroke="none"/>' +
            (contenta
                ? '<path d="M8 14.5c1 1.3 2.4 2 4 2s3-.7 4-2" fill="none" stroke-width="1.6" stroke-linecap="round"/>'
                : '<path d="M8.5 15.5h7" fill="none" stroke-width="1.6" stroke-linecap="round"/>') +
            '</svg>';
    }

    function tono(severidad) {
        return severidad === 'urgente' ? 'veredicto--problema' : 'veredicto--atencion';
    }

    // La cabecera y las alertas llegan por separado, y la portada las necesita a las dos. Se
    // guardan y se pinta cuando ya estan ambas: pintar media portada y completarla despues
    // haria saltar la tarjeta delante de quien la esta leyendo.
    var loDeArriba = { cabecera: null, alertas: null, masVieja: null };

    function pintarPortada() {
        var c = loDeArriba.cabecera, a = loDeArriba.alertas;
        if (!c || !a) return;

        var caja = $('portada');
        caja.innerHTML = '';

        if (c.hayDatos === false) {
            apunte(caja, 'Todavía no hay exámenes en este dispositivo.');
            return;
        }

        var urgente = a.urgente || {}, enRango = a.enRango || {};
        var html = '';

        // EL BLOQUE QUE TRANQUILIZA. No se dibuja cuando la severidad es «urgente»: el rojo lo
        // reserva el dominio para lo que no puede esperar, y poner un «todo está bien» al lado
        // de eso lo contradice. Fuera de ese caso el verde convive con el ambar.
        if (enRango.hayAlgo && urgente.severidad !== 'urgente') {
            // LA FRASE ES DEL DOMINIO. Aca se decide UNA cosa -si va de titular o de detalle- y
            // eso si es presentacion: con algo pidiendo atencion el conteo titula, porque «Todo
            // está bien» encima de «mostrále esto a tu médico este mes» se contradice.
            html += '<div class="veredicto veredicto--bien">' + cara(true) + '<div>' +
                '<p class="veredicto-titulo">' +
                (urgente.pideAlgo ? enRango.paraSonia : 'Todo está bien') + '</p>' +
                (urgente.pideAlgo ? '' : '<p class="veredicto-detalle">' + enRango.paraSonia + '</p>') +
                '</div></div>';
        }

        if (urgente.pideAlgo) {
            var cuales = (a.piden || []).slice().sort(function (u, v) {
                return String(u.paraMostrar).localeCompare(String(v.paraMostrar), 'es');
            });
            html += '<div class="veredicto ' + tono(urgente.severidad) + '">' + cara(false) + '<div>' +
                '<p class="veredicto-titulo">' + mayuscula(urgente.accion || '') + '.</p>' +
                (cuales.length ? '<div class="portada-cuales">' + cuales.map(function (l) {
                    return '<span class="portada-cual">' + l.paraMostrar + '</span>';
                }).join('') + '</div>' : '') +
                '</div></div>';
        }

        // NI VERDE NI AMBAR: no hay nada en rango que contar y nada que pida algo. Es el
        // expediente recien abierto, y decirlo es mejor que una tarjeta vacia.
        if (!enRango.hayAlgo && !urgente.pideAlgo)
            html += '<p class="veredicto-titulo">Todavía no hay nada que leer.</p>';

        var patas = [
            '<span><b>' + c.cantidad + '</b> ' + plural(c.cantidad, 'examen', 'exámenes') + '</span>',
            '<span><b>' + (enRango.analitos || 0) + '</b> ' +
                plural(enRango.analitos, 'analito con historia', 'analitos con historia') + '</span>'
        ];
        if (loDeArriba.masVieja) patas.push('<span>desde <b>' + mesYAnio(loDeArriba.masVieja) + '</b></span>');
        // «leído hasta el evento N» es el MISMO tipo de dato que el resto del pie -el registro
        // de lo que se viene juntando- y por eso termina aca en vez de pegado al boton.
        patas.push('<span>leído hasta el evento <b>' + c.ultimoEvento + '</b></span>');
        html += '<div class="portada-pie">' + patas.join('') + '</div>';

        caja.innerHTML = html;
    }

    // EL PEDIDO DEL DATO QUE FALTA. Aca NO es editable, y esa es una diferencia con la app
    // suelta: declarar el sexo es una escritura, y esta cara no tiene por donde escribir --
    // `consultar` solo alcanza [View]. Lo que puede hacer es decir que falta y por que.
    function pintarPedido(a) {
        var caja = $('pedido');
        caja.innerHTML = '';
        var p = a && a.faltaElSexo;
        if (!p || !p.haceFalta) return;

        var d = el('div', 'pedido');
        d.appendChild(el('p', 'pedido-que', p.paraSonia || 'Falta un dato tuyo'));
        if (p.porQue) d.appendChild(el('p', 'pedido-porque', p.porQue));
        caja.appendChild(d);
    }

    // ---------------------------------------------------------------- el historial

    // UNA CRONOLOGIA DE VISITAS, no una tabla. Las trayectorias ya se ven arriba; lo que esta
    // seccion aporta es QUE PASO CADA DIA, que es como se recuerda una visita al laboratorio:
    // varios analitos juntos, no una grilla.
    function pintarHistorial(r) {
        var caja = $('historial');
        var celdas = (r && r.celdas) || [];
        if (!celdas.length) {
            apunte(caja, 'Todavía no hay exámenes. Apretá Actualizar.');
            return;
        }

        // El dominio manda la fecha en cada celda, ya colapsada por dia, asi que reconstruir
        // las visitas es juntar las distintas en orden. No es una decision, es un desarmado.
        var porFecha = {};
        var orden = [];
        celdas.forEach(function (c) {
            var clave = dia(c.fecha);
            if (!porFecha[clave]) { porFecha[clave] = { fecha: c.fecha, cosas: [] }; orden.push(clave); }
            porFecha[clave].cosas.push(c);
        });
        orden.sort(function (u, v) { return u < v ? 1 : u > v ? -1 : 0; });   // de la mas nueva

        caja.innerHTML = '<ol class="visitas">' + orden.map(function (clave) {
            var visita = porFecha[clave];
            return '<li class="visita">' +
                '<div class="visita-fecha">' +
                    '<span class="visita-dia">' + diaYMes(visita.fecha) + '</span>' +
                    '<span class="visita-ano">' + anio(visita.fecha) + '</span>' +
                '</div>' +
                '<ul class="visita-cosas">' + visita.cosas.map(function (c) {
                    return '<li class="visita-cosa">' +
                        '<span class="visita-que">' + c.paraMostrar + '</span>' +
                        '<span class="visita-cuanto">' + c.expresado + '</span></li>';
                }).join('') + '</ul></li>';
        }).join('') + '</ol>';
    }

    // ---------------------------------------------------------------- las series

    // Que pintada es la vigente. Las tarjetas de arriba se reemplazan de un saque y no la
    // necesitan; las series se arman de a una, y por eso si: la pantalla se pinta dos veces --
    // lo que ya hay, y otra vez cuando el cartero vuelve -- asi que la segunda pasada limpia la
    // lista mientras la primera todavia esta preguntando analito por analito. Sin esta guarda
    // las dos cadenas siguen agregando y cada serie sale DUPLICADA.
    var ultimaPasada = 0;

    function pintarSeries(analitos, pasada) {
        var caja = $('series');
        caja.innerHTML = '';
        if (!analitos || !analitos.length) {
            apunte(caja, 'Todavía no hay una serie que seguir.');
            return;
        }
        // De a uno, en orden, y cada uno se pinta apenas llega: son varias preguntas al Host y
        // esperar a todas dejaria la seccion vacia mientras tanto.
        analitos.reduce(function (antes, a) {
            return antes
                .then(function () { return pedir('tendencia', { analito: a }); })
                .then(function (r) { if (pasada === ultimaPasada) caja.appendChild(tarjetaDeSerie(r)); })
                .catch(function () { /* un analito sin serie no rompe los demas */ });
        }, Promise.resolve());
    }

    // ---------------------------------------------------------------- el cartero

    // PEDIRLE AL CARTERO, que es Gnothi. La cara NO sale a la red -- no puede, y no le hace
    // falta: le pide a Gnothi que traiga lo nuevo, y en esta forma Gnothi ademas se lo entrega
    // a la superficie, que es la unica que puede asentarlo.
    //
    // El cursor va en 0 y no es un descuido: el cursor de verdad lo pone el actor saliendo del
    // journal, del otro lado del puente. Esta pagina no sabe por donde iba ni tiene por que.
    //
    // Y EL RESULTADO SE DICE, aunque sea en el pie: sin esto, «no hay examenes» significa a la
    // vez que el laboratorio no publico nada, que el cartero no pudo y que no hay puente.
    function contar(texto, malo) {
        var p = $('conexion');
        p.textContent = texto;
        p.classList.toggle('malo', !!malo);
    }

    function refrescar() {
        return new Promise(function (resolver) {
            if (!window.gnothiPuente || !window.gnothiPuente.pull) {
                contar('sin puente: esta pantalla no corre dentro de Gnothi', true);
                resolver(null); return;
            }
            contar('pidiéndole a Gnothi que traiga lo nuevo…');

            var id = 'p' + (++siguiente);
            var t0 = Date.now();
            pendientes[id] = {
                resolver: function (json) {
                    var dicho = json;
                    try {
                        var r = JSON.parse(json);
                        dicho = r && r.resultado ? r.resultado : json;
                    } catch (e) { /* se muestra crudo */ }
                    contar('al día, en ' + (Date.now() - t0) + ' ms · ' + String(dicho).slice(0, 200));
                    resolver(json);
                },
                rechazar: function (e) {
                    contar('no se pudo traer lo nuevo (' + (Date.now() - t0) + ' ms): ' +
                        (e && e.message ? e.message : e), true);
                    resolver(null);
                }
            };
            try { window.gnothiPuente.pull(id, '0'); }
            catch (e) {
                delete pendientes[id];
                contar('pull tiró: ' + (e && e.message ? e.message : e), true);
                resolver(null);
            }
        });
    }

    // ---------------------------------------------------------------- arranque

    function pintarTodo() {
        var pasada = ++ultimaPasada;

        pedir('cabecera')
            .then(function (r) { loDeArriba.cabecera = r; pintarPortada(); })
            .catch(function (e) { apunte($('portada'), e.message, true); });

        pedir('alertas')
            .then(function (r) { loDeArriba.alertas = r; pintarPedido(r); pintarPortada(); })
            .catch(function () { /* sin avisos la portada espera, no se rompe */ });

        // La lista completa solo aporta UNA cosa a esta pantalla: desde cuando hay expediente,
        // que va en el pie de la portada. El historial lo pinta la cronologia de visitas.
        pedir('examenes')
            .then(function (r) {
                var filas = (r && (r.examenes || r.lista)) || (Array.isArray(r) ? r : []);
                var vieja = null;
                filas.forEach(function (f) {
                    var d = cuando(f.fecha);
                    if (d && (!vieja || d < vieja)) vieja = d;
                });
                loDeArriba.masVieja = vieja;
                pintarPortada();
            })
            .catch(function () { /* el pie se queda sin el «desde», nada mas */ });

        pedir('historial').then(pintarHistorial)
            .catch(function (e) { apunte($('historial'), e.message, true); });

        pedir('analitos')
            .then(function (r) { pintarSeries(r && r.analitos, pasada); })
            .catch(function (e) { apunte($('series'), e.message, true); });
    }

    function releer() {
        var boton = $('releer');
        boton.disabled = true;
        boton.setAttribute('aria-busy', 'true');
        boton.textContent = 'Actualizando…';
        refrescar().then(function (r) {
            if (r) pintarTodo();
            boton.disabled = false;
            boton.setAttribute('aria-busy', 'false');
            boton.textContent = 'Actualizar';
        });
    }

    function arrancar() {
        // `listo()` primero y siempre: hasta que la pagina lo diga, el Host no le pregunta nada.
        // Aca ademas no hay actor que hidratar -- el actor vive del otro lado -- asi que se
        // avisa apenas el documento existe.
        if (window.gnothiPuente && window.gnothiPuente.listo) window.gnothiPuente.listo();

        $('releer').addEventListener('click', releer);

        // PRIMERO SE PINTA LO QUE YA HAY, y despues se refresca. Al reves, abrir con el
        // publicador caido dejaria la pantalla en blanco hasta que el pedido falle -- y tener el
        // dato propio sirve justamente para que eso no pase.
        pintarTodo();
        refrescar().then(function (r) { if (r) pintarTodo(); });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', arrancar);
    else arrancar();
})();
