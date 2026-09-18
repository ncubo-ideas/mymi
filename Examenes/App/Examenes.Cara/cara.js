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
    var I = window.Idioma;
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

    // LOS NOMBRES DE LOS MESES SON DEL IDIOMA, igual que todo lo demas que esta cara dice con
    // sus propias palabras. Se leen del catalogo ya negociado, no de una lista fija.
    function cuando(iso) { var d = new Date(iso); return isNaN(d.getTime()) ? null : d; }
    function fecha(iso) {
        var d = cuando(iso);
        if (!d) return '';
        var nombres = (window.TextoDeLaCara[I.etiqueta] || {}).meses || [];
        return (d.getDate() < 10 ? '0' : '') + d.getDate() + ' ' + nombres[d.getMonth()] + ' ' + d.getFullYear();
    }
    function dia(iso) { var d = cuando(iso); return d ? d.toISOString().slice(0, 10) : String(iso); }
    // EL PLURAL, por clave y no pegando una 's': las dos lenguas del alcance tienen dos formas
    // y el catalogo lleva una clave por forma.
    function mediciones(n) {
        return n === 1 ? I.texto('tranquilo.medicion') : I.con('tranquilo.mediciones', { cuantas: n });
    }
    function resultados(n) {
        return n === 1 ? I.texto('historial.resultado') : I.con('historial.resultados', { cuantos: n });
    }
    // EL CONTEO DE «MERECE ATENCION» DICE LA UNIDAD, no un numero pelado: allá es
    // `atencion.conteo`/`atencion.conteoVarios` y acá tambien. Un «3» suelto al lado del rotulo
    // no dice de que son tres.
    function conteo(n) {
        return n === 1 ? I.texto('atencion.conteo') : I.con('atencion.conteoVarios', { cuantos: n });
    }

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
        return t === 'en-ascenso' ? '↑' : t === 'en-descenso' ? '↓' : '→';
    }

    // EL ESTADO SE DICE CON EL VOCABULARIO DE LA PANTALLA, no con el codigo del dominio.
    //
    // Esto imprimia `t.tendencia` crudo, y el dominio contesta ademas «indeterminada» y «no
    // aplica» (Dominio/Tendencia.cs): en la tarjeta se leia «→ indeterminada», que a una persona
    // no le dice nada y la app suelta nunca muestra. Es el mismo switch de EstadoBreve()
    // (MisExamenes.razor:1387), incluida la precedencia de «sugiere repetir». Las cuatro frases
    // salen del catalogo, con las MISMAS claves que usa la app suelta.
    //
    // LOS CODIGOS LLEVAN GUION -- «en-ascenso», «en-descenso» (Dominio/Tendencia.cs:34) --, que
    // es lo mismo que ya compara `flecha()` dos funciones mas arriba. Escritos con espacio, los
    // dos primeros no coincidian nunca y la tarjeta se quedaba muda salvo en «estable».
    function estadoBreve(l, t) {
        if (l && l.sugiereRepetirElExamen) return I.texto('atencion.posibleErrorDeMedicion');
        if (t.tendencia === 'en-ascenso') return I.texto('atencion.vieneAumentando');
        if (t.tendencia === 'en-descenso') return I.texto('atencion.vieneBajando');
        if (t.tendencia === 'estable') return I.texto('atencion.enSeguimiento');
        return '';
    }

    // «QUE CAMBIO» DICE UNA FRASE MAS PARA LA MISMA SERIE: cuando la tendencia es estable, allá
    // no dice «en seguimiento» sino «se mantiene estable» (MisExamenes.razor:864). Es la unica
    // diferencia entre las dos tarjetas, y esta escrita como una sola linea para que se vea.
    function estadoDelCambio(l, t) {
        return t.tendencia === 'estable' ? I.texto('atencion.seMantieneEstable') : estadoBreve(l, t);
    }

    function mayuscula(f) {
        return !f ? '' : f.charAt(0).toUpperCase() + f.slice(1);
    }

    // EL PISO Y EL TECHO SALEN DE LA FRASE DEL RANGO, no de las lineas de referencia.
    //
    // Es LimitesDeRango() de la app suelta (MisExamenes.razor:1449): la misma «hasta 220 mg/dL»
    // que la tarjeta ya muestra arriba. Importa que sea esta fuente y no `referencias`: las tres
    // series con meta -colesterol, LDL, trigliceridos- publican `partida`/`meta` y ninguna
    // publica `piso`, asi que pidiendo lineas se quedaban sin barra justo las del caso insignia.
    // Un solo numero en la frase sigue siendo «hasta X» y el piso se asume 0, igual que allá.
    function limitesDeRango(frase) {
        var numeros = [];
        String(frase || '').split(/\s+/).forEach(function (palabra) {
            var candidato = palabra.replace(/^[,.;]+|[,.;]+$/g, '').replace(',', '.');
            if (candidato !== '' && isFinite(Number(candidato))) numeros.push(Number(candidato));
        });
        if (!numeros.length) return null;
        if (numeros.length === 1) return { piso: null, techo: numeros[0] };
        return { piso: numeros[0], techo: numeros[numeros.length - 1] };
    }

    // LA BARRA CONTRA EL RANGO, en sus dos usos: la tarjeta que pide algo -- donde el limite es
    // lo unico que el modelo conoce, sin zonas que el dominio no define -- y la fila serena.
    function barraDeRango(valorFinal, frase, serena) {
        var lim = limitesDeRango(frase), actual = Number(valorFinal);
        if (!lim || !(lim.techo > 0) || !isFinite(actual)) return '';
        var piso = lim.piso === null ? 0 : lim.piso;
        if (!(lim.techo > piso)) return '';
        var p = (actual - piso) / (lim.techo - piso);
        p = p < 0 ? 0 : p > 1 ? 1 : p;
        return '<div class="inicio-atencion-rango-pista' + (serena ? ' inicio-atencion-rango-pista--serena' : '') + '">' +
               (serena ? '' : '<div class="inicio-atencion-rango-limite"></div>') +
               '<div class="inicio-atencion-rango-marca" style="left:' + px(p * 100) + '%"></div>' +
               '</div>';
    }

    // LA CIFRA DE CADA FORMA. Para un binario la cifra ES el ultimo resultado y para un nominal
    // ES la categoria: no hay unidad ni «de cuanto a cuanto». Vive acá porque las tres secciones
    // -- atencion, lo que esta bien, sin tendencia -- la escriben igual.
    function cifraDe(r, t) {
        if (r.presencia) return texto(r.presencia.ultimoResultado);
        if (r.constancia) return texto((r.constancia || {}).ultimaCategoria);
        return valor(t.valorFinal) + ' <span>' + texto(t.unidad) + '</span>';
    }

    function nombreDe(r, l, t) {
        return l.paraMostrar || t.paraMostrar || (r.presencia || {}).paraMostrar ||
               (r.constancia || {}).paraMostrar || r.analito;
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

        // LAS NOTAS DEL PIE TAMBIEN SON DEL IDIOMA. Eran las dos ultimas frases en espanol duro
        // que quedaban en esta cara -- ni el puerto ni la localizacion las habian tocado --, asi
        // que el dibujo hablaba espanol debajo de una pantalla en ingles.
        var notas = [];
        if (habitual) notas.push('<span class="forma-nota"><i class="muestra muestra--banda"></i>' +
            I.con('forma.nivelDeSiempre', { valor: valor(habitual.valor) + ' ' + texto(unidad) }) + '</span>');
        if (techo) notas.push('<span class="forma-nota"><i class="muestra muestra--techo"></i>' +
            I.con('forma.esperableHasta', { valor: valor(techo.valor) + ' ' + texto(unidad) }) + '</span>');

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
        if (sev === 'urgente') return I.texto('portada.urgente');
        if (sev === 'consulte-pronto') return I.texto('portada.consultePronto');
        return I.texto('portada.enGeneralBien');
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

    // LA TIRA DE UN BINARIO O DE UN NOMINAL, fuera de la tarjeta de atencion porque no es suya:
    // las tres secciones muestran el mismo dibujo cuando el analito tiene esa forma, y desde que
    // un negativo puede caer en «lo demas esta bien» esta funcion tiene dos llamadores.
    function tiraDe(r, l) {
        if (r.pruebas) {
            var pr = (r.pruebas || []).map(function (x) {
                return { fecha: x.fecha, marcada: x.esPositivo, texto: x.expresado };
            });
            var pos = pr.filter(function (x) { return x.marcada; }).length;
            if (!pr.length) return '';
            return tira(pr, (r.presencia || {}).positivasSeguidas,
                pos === 0
                    ? I.con('atencion.pruebasNinguna', { pruebas: pr.length })
                    : I.con('atencion.pruebasAlgunas', { presentes: pos, pruebas: pr.length }),
                pr[pr.length - 1].texto, l.paraSonia);
        }

        if (r.observaciones) {
            var ob = (r.observaciones || []).map(function (x) {
                return { fecha: x.fecha, marcada: !x.esEsperable, texto: x.expresado };
            });
            var c = r.constancia || {};
            var dentro = (c.mediciones || ob.length) - (c.noEsperables || 0);
            if (!ob.length) return '';
            return tira(ob, c.noEsperablesSeguidas,
                I.con('atencion.dentroDeLoEsperable',
                    { dentro: dentro, veces: (c.mediciones || ob.length) }),
                c.ultimaCategoria || (ob[ob.length - 1] || {}).texto, l.paraSonia);
        }

        return '';
    }

    function tarjetaAtencion(r) {
        var l = r.lectura || {}, t = r.hechos || {}, puntos = r.puntos || [];
        var dibujo = '';

        // SIN VEREDICTO NO HAY DIBUJO. Las formas se dibujan CONTRA una referencia, y esa
        // referencia es justamente lo que depende del dato que falta.
        if (!l.requiereElSexo) {
            // UN BINARIO Y UN NOMINAL COMPARTEN DIBUJO, y por eso no hay una forma nueva: lo
            // que la tira marca no es «positivo» sino «se salio de lo esperable», que es la misma
            // pregunta -- cuando pasa y cuantas veces seguidas -- sobre otro conjunto de valores.
            // La app suelta tambien las separa en TRES puertas explicitas (MisExamenes:1740).
            //
            // LA RAMA DEL NOMINAL faltaba, y sin ella la tarjeta caia por la puerta
            // continua y pedia `hechos.valorFinal`, que para un color no existe: la pantalla
            // imprimia literalmente **undefined** en la cifra grande, con la referencia y el
            // relato correctos abajo. Visto en el telefono el 2026-09-16 en «Color de la orina» y
            // «Color de las heces». Lo que faltaba era el dibujo y la cifra, no el reparto:
            // `repartir` ya mandaba `r.constancia` a atencion desde antes.
            //
            // LAS DOS RAMAS VIVEN EN `tiraDe`, que tambien usa la fila serena: desde que un
            // negativo puede caer en «lo demas esta bien», este dibujo tiene dos llamadores.
            if (r.pruebas || r.observaciones) {
                dibujo = tiraDe(r, l);

            } else if (l.forma === 'trayectoria-con-linea-base' && t.haySerie) {
                dibujo = trayectoria(puntos, r.referencias, l.pideAlgo, l.paraSonia);
            } else if (l.forma === 'posicion-en-banda') {
                dibujo = pista(puntos, r.referencias, false);

            // LA TERCERA FORMA FALTABA, y es la del caso insignia: colesterol, LDL y
            // trigliceridos son «progreso-hacia-meta» (SerieDeColesterol.cs:75) y caian en el
            // `else` mudo, asi que la tarjeta que mas pide salia sin dibujo. La app suelta la
            // pinta contra el UNICO limite que el modelo conoce -- el de la frase del rango --,
            // sin zonas normal/limite/alto que el dominio no define (MisExamenes.razor:424).
            //
            // EL CODIGO DE LA FORMA LLEVA GUIONES: «progreso-hacia-meta» (FormaDeLectura.cs:27).
            // Escrito con espacios la rama no entraba nunca, asi que la correccion no corregia
            // nada: la tarjeta insignia seguia saliendo sin barra.
            } else if (l.forma === 'progreso-hacia-meta') {
                var barra = barraDeRango(t.valorFinal, l.referenciaLegible, false);
                if (barra) dibujo = '<div class="inicio-atencion-rango">' + barra +
                    '<div class="inicio-atencion-rango-etiquetas">' +
                        '<span>' + I.con('atencion.tuResultado',
                            { valor: '<b>' + texto(valor(t.valorFinal) + ' ' + t.unidad) + '</b>' }) + '</span>' +
                        '<span>' + texto(I.con('atencion.referencia',
                            { referencia: l.referenciaLegible })) + '</span>' +
                    '</div></div>';
            }
        }

        var nombre = nombreDe(r, l, t);

        // TRES FORMAS, TRES CIFRAS: lo resuelve cifraDe(), que ahora comparten las tres
        // secciones. Para un nominal la cifra ES la categoria -- no hay unidad, no hay «de
        // cuanto a cuanto» -- y no hace falta `haySerie`: un color con una sola observacion ya
        // esta leido.
        var v = cifraDe(r, t);

        return '<div class="inicio-atencion-tarjeta">' +
            '<div class="inicio-atencion-cabeza">' +
                '<div class="inicio-atencion-titulo">' +
                    '<div class="inicio-atencion-nombre-grupo">' +
                        '<span class="inicio-atencion-nombre">' + texto(nombre) + '</span>' +
                        (estadoBreve(l, t) ? '<span class="inicio-atencion-estado">' + flecha(t.tendencia) + ' ' +
                            texto(estadoBreve(l, t)) + '</span>' : '') +
                    '</div>' +
                '</div>' +
                '<span class="inicio-atencion-valor">' + v + '</span>' +
            '</div>' +
            (cadena(t, puntos) ? '<p class="inicio-atencion-serie">' + cadena(t, puntos) + '</p>' : '') +
            (l.referenciaLegible ? '<p class="inicio-atencion-referencia">' +
                texto(I.con('atencion.referenciaMinuscula', { referencia: l.referenciaLegible })) + '</p>' : '') +
            dibujo +
            // LA CAJA DE LECTURA es la de la app suelta (MisExamenes.razor:459): un icono «i» y
            // el texto adentro. Esto usaba `inicio-atencion-detalle`, una clase que NO existe en
            // css/app.css -- la hoja es la misma de la app --, asi que el bloque salia suelto,
            // sin caja y sin icono. Y la frase va con mayuscula inicial, como allá.
            (l.paraSonia
                ? '<div class="inicio-atencion-lectura">' +
                      '<span class="inicio-atencion-lectura-icono" aria-hidden="true">i</span>' +
                      '<p class="inicio-atencion-lectura-texto">' +
                          '<span>' + texto(mayuscula(l.paraSonia)) + '.</span>' +
                          (l.pideAlgo && l.accion
                              ? '<span class="inicio-atencion-lectura-accion">' + texto(mayuscula(l.accion)) + '.</span>'
                              : '') +
                      '</p>' +
                  '</div>'
                : '') +
        '</div>';
    }

    // LA FILA SERENA. Orden y piezas de la app suelta (MisExamenes.razor:728): valor y chevron a
    // la derecha, la barra contra el rango, y DEBAJO la etiqueta del rango con el chip y la nota
    // de mediciones. Acá el chip y la nota estaban dentro de la misma linea y la frase del rango
    // -- «hasta 220 mg/dL», que es lo que explica donde cae la marca -- no aparecia.
    //
    // La nota NO dice el vocabulario corto de las tarjetas que piden algo: dice `TendenciaDicha`,
    // la frase que el dominio ya escribio para esta serie (MisExamenes.razor:1398). El codigo
    // crudo -- `t.tendencia` -- no va nunca a la pantalla.
    function itemTranquilo(r) {
        var l = r.lectura || {}, t = r.hechos || {};
        var esContinuo = !r.presencia && !r.constancia;

        // UN NEGATIVO SERENO LLEVA SU TIRA. Desde que un binario o un nominal pueden caer acá
        // -- antes iban todos a «merece atencion» -- la fila tiene que mostrar lo mismo que
        // mostraba alla: cuando se midio y cuantas veces seguidas, que es su unica forma. La
        // barra contra el rango no aplica: no hay valor continuo que ubicar.
        var barra = esContinuo ? barraDeRango(t.valorFinal, l.referenciaLegible, true) : '';
        var suTira = esContinuo ? '' : tiraDe(r, l);

        return '<li><div class="inicio-tranquilo-item">' +
            '<div class="inicio-tranquilo-cabeza">' +
                '<span class="inicio-tranquilo-nombre">' + texto(nombreDe(r, l, t)) + '</span>' +
                '<span class="inicio-tranquilo-cabeza-derecha">' +
                    '<span class="inicio-tranquilo-valor">' + cifraDe(r, t) + '</span>' +
                    '<span class="inicio-tranquilo-chevron" aria-hidden="true">›</span>' +
                '</span>' +
            '</div>' +
            suTira +
            (barra
                ? '<div class="inicio-tranquilo-rango">' + barra +
                      (l.referenciaLegible
                          ? '<div class="inicio-tranquilo-rango-etiquetas">' +
                                '<span>' + texto(l.referenciaLegible) + '</span>' +
                                '<span class="chip ' + insignia(l, t.haySerie) + '">' +
                                texto(I.texto('tranquilo.enRango')) + '</span>' +
                            '</div>'
                          : '') +
                      (esContinuo && t.haySerie
                          ? '<p class="inicio-tranquilo-nota">' + texto(I.con('tranquilo.nota', {
                                mediciones: mediciones(t.mediciones || 0),
                                tendencia: t.tendenciaDicha || ''
                            })) + '</p>'
                          : '') +
                  '</div>'
                : '<div class="inicio-tranquilo-rango-etiquetas">' +
                      '<span class="chip ' + insignia(l, t.haySerie) + '">' +
                      texto(I.texto('tranquilo.enRango')) + '</span>' +
                  '</div>') +
            (l.paraSonia ? '<p class="inicio-tranquilo-detalle">' + texto(l.paraSonia) + '.</p>' : '') +
        '</div></li>';
    }

    function tarjetaCambio(r) {
        var l = r.lectura || {}, t = r.hechos || {}, puntos = r.puntos || [];
        return '<div class="inicio-cambio-tarjeta inicio-cambio-tarjeta--' + tono(l) + '">' +
            '<div class="inicio-cambio-cabeza">' +
                '<span class="inicio-cambio-nombre">' + texto(l.paraMostrar || r.analito) + '</span>' +
                (estadoDelCambio(l, t) ? '<span class="inicio-cambio-estado inicio-cambio-estado--' + tono(l) + '">' +
                    flecha(t.tendencia) + ' ' + texto(estadoDelCambio(l, t)) + '</span>' : '') +
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
        var n = t.mediciones || (r.presencia || {}).mediciones || (r.constancia || {}).mediciones || 1;

        // LA FRASE ES LA DE LA APP SUELTA (TextoSinTendencia, MisExamenes.razor:1266), y allá se
        // usa A PROPOSITO en vez de `ParaSonia`: con una sola medicion la lectura del dominio
        // habla de un valor, no de que falta historia. Son DOS frases, no una: con mas de una
        // medicion y sin serie igual, prometer «tu proximo resultado» seria mentir.
        var frase = n <= 1 ? I.texto('sinTendencia.conUna') : I.texto('sinTendencia.conVarias');

        return '<div class="inicio-sintendencia-tarjeta">' +
            '<div class="inicio-sintendencia-cuerpo">' +
                '<p class="inicio-sintendencia-nombre">' + texto(nombreDe(r, l, t)) + '</p>' +
                '<div class="inicio-sintendencia-fila">' +
                    '<span class="inicio-sintendencia-valor">' + cifraDe(r, t) + '</span>' +
                    '<span class="chip chip--repetir">' + texto(mediciones(n)) + '</span>' +
                '</div>' +
                '<p class="inicio-sintendencia-texto">' + texto(frase) + '</p>' +
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

            // LAS TRES FORMAS SE REPARTEN IGUAL, y el criterio es el del dominio: pideAlgo manda,
            // despues «no hay serie», y lo demas esta bien (MisExamenes.razor:1720-1758).
            //
            // Un binario o un nominal caian SIEMPRE en atencion por ser binario o nominal, y eso
            // es clasificar por la forma del dato en vez de por lo que el dominio dictamino: un
            // «Nitritos: negativo» aparecia bajo «merece atencion», que es exactamente lo que una
            // pantalla de laboratorio no puede hacer.
            var haySerie = r.presencia ? !!r.presencia.haySerie
                         : r.constancia ? !!r.constancia.haySerie
                                        : !!t.haySerie;

            if (l.pideAlgo || l.severidad === 'urgente') { atencion.push(r); }
            else if (!haySerie) { sintendencia.push(r); }
            else { tranquilo.push(r); }

            // «QUE CAMBIO» NO ES UNA CUARTA SECCION EXCLUYENTE: es el ultimo salto de CADA serie
            // continua con dos puntos, este donde este (Cambios(), MisExamenes.razor:1300). Acá
            // se pedia ademas que no estuviera en atencion y que los dos ultimos puntos fueran
            // distintos, asi que justo el analito que pide algo nunca se veia cambiar.
            if (!r.presencia && !r.constancia && t.haySerie && puntos.length >= 2) cambios.push(r);
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
                '<p class="inicio-atencion-rotulo">' + texto(I.texto('atencion.rotulo')) + '</p>' +
                '<span class="inicio-atencion-conteo">' +
                    '<span class="inicio-atencion-conteo-punto" aria-hidden="true"></span>' +
                    '<span>' + texto(conteo(g.atencion.length)) + '</span></span>' +
            '</div>' + g.atencion.map(tarjetaAtencion).join(''));

        mostrar('tranquilo', g.tranquilo.length,
            '<p class="inicio-tranquilo-rotulo">' + texto(I.texto('tranquilo.rotulo')) + '</p>' +
            '<div class="inicio-tranquilo-tarjeta">' +
                '<p class="inicio-tranquilo-titulo">' + texto(I.texto('tranquilo.titulo')) + '</p>' +
                '<ul class="inicio-tranquilo-lista">' + g.tranquilo.map(itemTranquilo).join('') + '</ul>' +
            '</div>');

        mostrar('cambios', g.cambios.length,
            '<h2 class="seccion">' + texto(I.texto('cambios.titulo')) + '</h2>' +
            '<p class="inicio-cambios-intro">' + texto(I.texto('cambios.intro')) + '</p>' +
            g.cambios.map(tarjetaCambio).join(''));

        mostrar('sintendencia', g.sintendencia.length,
            '<p class="inicio-sintendencia-rotulo">' + texto(I.texto('sinTendencia.rotulo')) + '</p>' +
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
            // EL MISMO VALOR QUE LA APP, CON SU ENFASIS. `P.apunte` escribe por textContent, asi
            // que el <em> de «Apretá Actualizar» habria salido escrito; la app lo pinta como
            // marcado (MisExamenes.razor:1011, MarkupString). Dos redacciones para la misma
            // frase es lo que el guardian llama una segunda institucion.
            caja.innerHTML = '<p class="apunte">' + I.texto('historial.sinExamenes') + '</p>';
            return null;
        }

        caja.innerHTML =
            '<p class="inicio-historial-intro">' + texto(I.texto('historial.intro')) + '</p>' +
            '<ul class="inicio-historial-timeline">' + v.map(function (x, i) {
                return '<li class="inicio-historial-evento' +
                    (i === 0 ? ' inicio-historial-evento--reciente' : '') + '">' +
                    '<span class="inicio-historial-nodo" aria-hidden="true"></span>' +
                    '<div class="inicio-historial-fila">' +
                        '<span class="inicio-historial-fecha">' + fecha(x.fecha) + '</span>' +
                        '<span class="inicio-historial-cantidad">' + texto(resultados(x.cosas.length)) + '</span>' +
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
            '<p class="inicio-config-texto"><b>' + texto(p.paraSonia || I.texto('pedido.faltaUnDato')) + '</b>' +
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
        P.pedir('historial', { idioma: I.etiqueta })
            .then(function (r) { return pintarHistorial(r); })
            .catch(function (e) { P.apunte($('historial-cuerpo'), e.message, true); return null; })
            .then(function (masReciente) {
                return P.pedir('alertas', { idioma: I.etiqueta }).then(function (a) {
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
        P.pedir('analitos', { idioma: I.etiqueta })
            .then(function (r) {
                var nombres = (r && r.analitos) || [];
                if (!nombres.length) return null;

                var juntadas = [];
                return nombres.reduce(function (antes, a) {
                    return antes
                        .then(function () { return P.pedir('tendencia', { analito: a, idioma: I.etiqueta }); })
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
        I.pintarLoEstatico();
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
