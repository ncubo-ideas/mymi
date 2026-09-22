// EL PUENTE FALSO. Esto NO es de Gnothi: es lo único que se escribió para esta demostración.
//
// En el producto, `window.gnothiPuente` lo inyecta el Host -- Photino en el escritorio, el
// WebView en Android, WKWebView en iOS -- y del otro lado hay un motor, tres DLL y un journal.
// Acá hay un `setTimeout` y un objeto literal. La cara no nota la diferencia, y esa es
// exactamente la prueba: `puente.js` y `cara.js` son los archivos reales, sin una línea tocada.
//
// Los datos son inventados. Sonia no existe, el laboratorio Delfos es un mock, y ninguno de
// estos valores salió de una persona.
(function () {
    'use strict';

    // ------------------------------------------------------------------ el expediente inventado

    var VISITAS = ['2024-03-12', '2024-11-08', '2025-06-21', '2026-02-14', '2026-09-10'];

    function serie(fechas, valores, unidad) {
        return fechas.map(function (f, i) { return { fecha: f, valor: valores[i], unidad: unidad }; });
    }

    // LOS NOMBRES SON LOS DEL DOMINIO, no inventados: son los valores que el manifiesto declara
    // como válidos para el parámetro `analito` de las tools -- ColesterolTotal, ColesterolLdl,
    // Hemoglobina, AntigenoProstatico (numéricos), SangreEnOrina (presencia), ColorDeLaOrina
    // (categoría). La cara los recorre tal cual y muestra `lectura.paraMostrar`.
    //
    // Seis, elegidos para caer en las cuatro secciones que la cara reparte y para ejercitar las
    // TRES formas que el dominio distingue: serie continua, presencia y categoría.
    var ANALITOS = {
        'ColesterolTotal': {
            lectura: {
                paraMostrar: 'Colesterol total', severidad: 'consulte-pronto', pideAlgo: true,
                sugiereRepetirElExamen: false, requiereElSexo: false,
                forma: 'trayectoria con linea base',
                referenciaLegible: 'hasta 200 mg/dL',
                paraSonia: 'Subió de 182 a 214 mg/dL en 912 días, y en las dos últimas visitas pasó el techo esperable',
                accion: 'comentalo en tu próxima consulta'
            },
            hechos: {
                haySerie: true, mediciones: 5, tendencia: 'en-ascenso',
                valorInicial: 182, valorFinal: 214, unidad: 'mg/dL', paraMostrar: 'Colesterol total'
            },
            puntos: serie(VISITAS, [182, 191, 188, 205, 214], 'mg/dL'),
            referencias: [{ nombre: 'habitual', valor: 190 }, { nombre: 'techo', valor: 200 }]
        },

        'SangreEnOrina': {
            lectura: {
                paraMostrar: 'Sangre en la orina', severidad: 'consulte-pronto', pideAlgo: true,
                sugiereRepetirElExamen: true, requiereElSexo: false,
                forma: 'presencia en el tiempo',
                paraSonia: 'Apareció en las dos últimas pruebas seguidas',
                accion: 'repetí el examen'
            },
            presencia: { positivasSeguidas: 2, ultimoResultado: 'positivo' },
            pruebas: [
                { fecha: '2024-03-12', esPositivo: false, expresado: 'negativo' },
                { fecha: '2024-11-08', esPositivo: false, expresado: 'negativo' },
                { fecha: '2025-06-21', esPositivo: false, expresado: 'negativo' },
                { fecha: '2026-02-14', esPositivo: true, expresado: 'positivo' },
                { fecha: '2026-09-10', esPositivo: true, expresado: 'positivo' }
            ]
        },

        'ColorDeLaOrina': {
            lectura: {
                paraMostrar: 'Color de la orina', severidad: 'normal', pideAlgo: false,
                sugiereRepetirElExamen: false, requiereElSexo: false,
                forma: 'categoria en el tiempo',
                paraSonia: 'Cuatro de las cinco observaciones cayeron en lo esperable'
            },
            constancia: {
                mediciones: 5, noEsperables: 1, noEsperablesSeguidas: 1,
                ultimaCategoria: 'ámbar oscuro', paraMostrar: 'Color de la orina'
            },
            observaciones: [
                { fecha: '2024-03-12', esEsperable: true, expresado: 'amarillo claro' },
                { fecha: '2024-11-08', esEsperable: true, expresado: 'amarillo' },
                { fecha: '2025-06-21', esEsperable: true, expresado: 'amarillo claro' },
                { fecha: '2026-02-14', esEsperable: true, expresado: 'amarillo' },
                { fecha: '2026-09-10', esEsperable: false, expresado: 'ámbar oscuro' }
            ]
        },

        'ColesterolLdl': {
            lectura: {
                paraMostrar: 'Colesterol LDL', severidad: 'normal', pideAlgo: false,
                sugiereRepetirElExamen: false, requiereElSexo: false,
                forma: 'posicion en banda',
                paraSonia: 'Bajó de 128 a 96 mg/dL y entró en lo esperable'
            },
            hechos: {
                haySerie: true, mediciones: 5, tendencia: 'en-descenso',
                valorInicial: 128, valorFinal: 96, unidad: 'mg/dL', paraMostrar: 'Colesterol LDL'
            },
            puntos: serie(VISITAS, [128, 121, 114, 103, 96], 'mg/dL'),
            referencias: [{ nombre: 'piso', valor: 40 }, { nombre: 'techo', valor: 100 }]
        },

        'Hemoglobina': {
            lectura: {
                paraMostrar: 'Hemoglobina', severidad: 'normal', pideAlgo: false,
                sugiereRepetirElExamen: false, requiereElSexo: false,
                forma: 'posicion en banda',
                paraSonia: 'Estable en las cinco visitas'
            },
            hechos: {
                haySerie: true, mediciones: 5, tendencia: 'estable',
                valorInicial: 13.8, valorFinal: 14.1, unidad: 'g/dL', paraMostrar: 'Hemoglobina'
            },
            puntos: serie(VISITAS, [13.8, 14.0, 13.9, 14.1, 14.1], 'g/dL'),
            referencias: [{ nombre: 'piso', valor: 12 }, { nombre: 'techo', valor: 16 }]
        },


        'AntigenoProstatico': {
            lectura: {
                paraMostrar: 'Antígeno prostático', severidad: 'normal', pideAlgo: false,
                sugiereRepetirElExamen: false, requiereElSexo: false,
                forma: 'posicion en banda',
                paraSonia: 'Con una sola medición todavía no se puede decir para dónde viene'
            },
            hechos: {
                haySerie: false, mediciones: 1, tendencia: '',
                valorInicial: 1.2, valorFinal: 1.2, unidad: 'ng/mL', paraMostrar: 'Antígeno prostático'
            },
            puntos: serie(['2026-09-10'], [1.2], 'ng/mL'),
            referencias: [{ nombre: 'piso', valor: 0 }, { nombre: 'techo', valor: 4 }]
        }
    };

    var NOMBRES = Object.keys(ANALITOS);

    // El cuadro que devuelve `historial`: una celda por analito y por fecha en que hubo medición.
    // La cara las agrupa por día -- eso es desarmado, no decisión, y por eso llegan planas.
    function celdas() {
        var salida = [];
        NOMBRES.forEach(function (n) {
            var a = ANALITOS[n];
            var fechas = a.puntos ? a.puntos.map(function (p) { return p.fecha; })
                       : a.pruebas ? a.pruebas.map(function (p) { return p.fecha; })
                       : a.observaciones.map(function (o) { return o.fecha; });
            fechas.forEach(function (f) { salida.push({ fecha: f, analito: n }); });
        });
        return salida;
    }

    // Las frases del dominio dicen los números con letras, no con dígitos.
    var PALABRAS = ['cero','uno','dos','tres','cuatro','cinco','seis','siete','ocho','nueve','diez'];
    function enPalabras(n) { return PALABRAS[n] || String(n); }
    function mayus(s) { return s.charAt(0).toUpperCase() + s.slice(1); }

    // ------------------------------------------------------------------ «hay algo que no debería estar»
    //
    // LA REGLA ES LA DEL DOMINIO, y no es la misma que «pide algo».
    //
    // `Expediente.AnalitosFueraDeRango` (Examenes/Dominio/Expediente.cs:746) cuenta los analitos
    // cuya ULTIMA lectura es un hallazgo, y `LoQueEstaEnRango` es su contracara. La TENDENCIA NO
    // ENTRA: un colesterol que viene subiendo pero cuyo último valor sigue bajo el techo no cuenta
    // como fuera de rango, aunque la pantalla lo levante y aunque su lectura «pida algo». Son dos
    // preguntas distintas y el simulador las confundía -- derivaba el conteo de `lectura.pideAlgo`,
    // que es «¿esto amerita que la persona haga algo?».
    //
    // Cada forma responde lo suyo, igual que las tres sobrecargas de `UltimaEsHallazgo`:
    //
    //   continua  SerieContinua.cs:91  hay rango para esta persona Y el último no es normal.
    //                                  SIN RANGO NO HAY HALLAZGO, y eso no es «está bien»: es que
    //                                  no se puede afirmar que esté mal.
    //   binaria   SerieBinaria.cs:71   la última está presente. Literal.
    //   nominal   SerieNominal.cs:69   la categoría de hoy no es de las esperables.
    function referencia(a, nombre) {
        var r = (a.referencias || []).filter(function (x) { return x.nombre === nombre; })[0];
        return r ? r.valor : null;
    }

    function esHallazgo(n) {
        var a = ANALITOS[n];

        if (a.pruebas) return !!a.pruebas[a.pruebas.length - 1].esPositivo;
        if (a.observaciones) return !a.observaciones[a.observaciones.length - 1].esEsperable;

        // Continua. `requiereElSexo` es el caso de «no se puede determinar el rango».
        if (a.lectura.requiereElSexo) return false;

        // OJO CON `habitual`: es el «nivel de siempre» que la pantalla dibuja de fondo, no un
        // límite. Sólo `piso` y `techo` son el rango.
        var v = a.puntos[a.puntos.length - 1].valor;
        var techo = referencia(a, 'techo'), piso = referencia(a, 'piso');
        if (techo === null && piso === null) return false;
        return (techo !== null && v > techo) || (piso !== null && v < piso);
    }

    function hallazgos() { return NOMBRES.filter(esHallazgo); }

    // `haySerie` VA EN LAS TRES FORMAS, y se deriva: `HechosDeLaSerie.cs:55` lo define como
    // `CantidadDeMediciones >= 2`, ni más ni menos.
    //
    // No estaba en `presencia` ni en `constancia`, y la cara de 2026-09-21 lo lee para repartir
    // (`repartir()`, cara.js:595): sin él, `!!undefined` es falso y el «Color de la orina» -cinco
    // observaciones- caía en «todavía no podemos ver la tendencia», diciendo que no hay datos
    // suficientes con los datos ahí. La cara anterior no lo miraba, y por eso el campo faltante
    // no se notó hasta recopiarla.
    function conSerie(obj, cuantas) {
        var copia = {};
        for (var k in obj) if (Object.prototype.hasOwnProperty.call(obj, k)) copia[k] = obj[k];
        copia.haySerie = cuantas >= 2;
        return copia;
    }

    // ------------------------------------------------------------------ las cuatro [View]

    var VISTAS = {
        historial: function () {
            return { hayDatos: true, celdas: celdas() };
        },

        alertas: function () {
            // LOS QUE PIDEN ALGO SON LOS QUE LO DECLARAN, no una lista escrita a mano: si mañana
            // se siembra otro analito con `pideAlgo`, entra solo.
            var piden = NOMBRES.filter(function (n) { return ANALITOS[n].lectura.pideAlgo; });

            return {
                urgente: { severidad: 'consulte-pronto', pideAlgo: true, PideAlgo: true },
                piden: piden.map(function (n) { return ANALITOS[n].lectura; }),
                // LA CUENTA SE DERIVA, Y CON LA REGLA DEL DOMINIO. Decía «Seis de tus ocho», que
                // era la cuenta de cuando había ocho analitos sembrados -- y quedó a la vista,
                // grande, en el hero, contradiciendo a los seis que la propia pantalla pinta.
                //
                // `LoQueEstaEnRango` (Expediente.cs:718) cuenta los que NO son hallazgo, no los
                // que no «piden algo»: son seis analitos, tres con un hallazgo hoy -el colesterol
                // sobre el techo, la sangre en la orina presente, el color fuera de lo esperable-,
                // así que tres están donde tienen que estar.
                enRango: { paraSonia: mayus(enPalabras(NOMBRES.length - hallazgos().length)) +
                                      ' de tus ' + enPalabras(NOMBRES.length) +
                                      ' resultados están dentro de lo esperado.' },
                faltaElSexo: {
                    haceFalta: true,
                    paraSonia: 'Falta un dato tuyo: tu sexo.',
                    porQue: 'Algunos rangos de referencia cambian según el sexo. Se declara desde la ' +
                            'app del laboratorio — esta pantalla sólo lee.'
                }
            };
        },

        analitos: function () {
            return { hayDatos: true, analitos: NOMBRES };
        },

        tendencia: function (args) {
            var n = args && args.analito;
            var a = ANALITOS[n];
            if (!a) throw new Error("module 'examenes' does not expose that analyte");
            if (a.presencia) return { analito: n, lectura: a.lectura,
                presencia: conSerie(a.presencia, a.pruebas.length), pruebas: a.pruebas };
            if (a.constancia) return { analito: n, lectura: a.lectura,
                constancia: conSerie(a.constancia, a.observaciones.length), observaciones: a.observaciones };
            return { analito: n, lectura: a.lectura,
                hechos: conSerie(a.hechos, a.puntos.length), puntos: a.puntos, referencias: a.referencias };
        }
    };

    // ------------------------------------------------------------------ el estado del banco

    var hayRed = true;

    window.addEventListener('message', function (e) {
        var m = e.data || {};
        if (m.banco === 'red') hayRed = !!m.valor;

        // El banco aprieta ↻ por la persona. Es el MISMO botón de la cara -- no hay una segunda
        // entrada: se busca el control real y se lo pulsa.
        if (m.banco === 'releer') {
            var b = document.getElementById('releer');
            if (b) b.click();
        }
    });

    function avisar(fila) {
        try { parent.postMessage({ banco: 'puente', fila: fila }, '*'); } catch (e) { /* suelta */ }
    }

    // El retardo existe para que el inspector se lea: en el aparato real una [View] contra el
    // actor vivo tarda menos que esto.
    function pronto(fn) { setTimeout(fn, 18 + Math.random() * 22); }

    // ------------------------------------------------------------------ el puente

    window.gnothiPuente = {
        listo: function () {
            avisar({ puerta: 'listo', nombre: 'listo()', nota: 'la página avisa que existe; recién ahora se le pregunta' });
        },

        // La firma es la del Host, exacta: (id, vista, argumentosJSON). No lleva el nombre del
        // módulo a propósito -- si lo llevara, una cara podría preguntar por el journal de otra
        // institución y ninguna regla río abajo lo frenaría.
        consultar: function (id, vista, argumentosJson) {
            var t0 = performance.now();
            var args = {};
            try { args = JSON.parse(argumentosJson || '{}'); } catch (e) { /* queda vacío */ }

            pronto(function () {
                var ms = Math.round(performance.now() - t0);
                try {
                    var fn = VISTAS[vista];
                    if (!fn) throw new Error("module 'examenes' does not expose '" + vista + "'");
                    var payload = fn(args);
                    avisar({
                        puerta: 'view', nombre: vista, args: args, ms: ms, ok: true,
                        json: JSON.stringify(payload)
                    });
                    window.gnothi.respuesta(id, JSON.stringify(payload));
                } catch (e) {
                    avisar({ puerta: 'view', nombre: vista, args: args, ms: ms, ok: false, error: e.message });
                    window.gnothi.respuesta(id, null, e.message);
                }
            });
        },

        // EL CARTERO es lo único que sale a la red, y lo hace Gnothi con SU credencial. La cara
        // no tiene con qué: por eso apagar la red se nota acá y en ningún otro lado.
        pull: function (id, cursor) {
            var t0 = performance.now();
            setTimeout(function () {
                var ms = Math.round(performance.now() - t0);
                if (!hayRed) {
                    avisar({
                        puerta: 'deliver', nombre: 'pull', args: { cursor: cursor }, ms: ms, ok: false,
                        error: 'no se pudo alcanzar al publicador'
                    });
                    window.gnothi.respuesta(id, null, 'no se pudo alcanzar al publicador');
                    return;
                }
                var r = { resultado: '0 sobres nuevos · el expediente ya estaba al día' };
                avisar({
                    puerta: 'deliver', nombre: 'pull', args: { cursor: cursor }, ms: ms, ok: true,
                    json: JSON.stringify(r)
                });
                window.gnothi.respuesta(id, JSON.stringify(r));
            }, hayRed ? 620 : 380);
        }
    };

    // LO QUE EL ASISTENTE SÍ PUEDE PREGUNTAR: las DOS tools que declara el manifiesto que se
    // instala, `resumen` y `lista_de_examenes`. Viven acá porque salen del mismo dominio, pero
    // pasan por otra puerta -- el shell se las pide por `postMessage`, como el Dispatcher.
    //
    // Hasta la v13 acá había CINCO tools (`que_me_falta_medir`, `evaluar_tendencia`,
    // `velocidad_de`, `presencia_de`, `constancia_de`) copiadas del manifiesto equivocado. Eso no
    // era sólo un dato de más: con `evaluar_tendencia` entre ellas, el remate del recorrido -«el
    // modelo no puede darte la tendencia»- era FALSO, porque ahí estaba la tendencia.
    //
    // Los cuerpos siguen a ConsultasDelModulo.cs, campo por campo, y se derivan de los mismos
    // datos sembrados que pinta la cara: así las dos puertas no se pueden contradecir.
    //
    // FIJATE EN LO QUE NO DEVUELVEN. `lista_de_examenes` da filas de EXAMEN -nombre, fecha,
    // resultado-, no la serie de un analito: ni un punto, ni la unidad, ni la línea de referencia,
    // ni el «nivel de siempre», ni si eso sube o baja. Eso es `InterpretacionDeAnalito` y
    // `TendenciaDeAnalito`, que son [View] -- lo dice el propio dominio: «la vista que NO tiene
    // equivalente en una pantalla de resultados: un laboratorio entrega valores, no tendencias».
    // El modelo no las ve nunca.

    // A QUÉ EXAMEN PERTENECE CADA ANALITO. Un examen agrupa varios analitos, y por eso la lista
    // que ve el modelo es más gruesa que el cuadro que ve la pantalla.
    var EXAMEN_DE = {
        ColesterolTotal: 'Perfil lipídico', ColesterolLdl: 'Perfil lipídico',
        Hemoglobina: 'Hemograma',
        SangreEnOrina: 'Urianálisis', ColorDeLaOrina: 'Urianálisis',
        AntigenoProstatico: 'Antígeno prostático'
    };

    // LAS FILAS, ARMADAS UNA SOLA VEZ: una por examen y fecha, con lo que ese examen informó ese
    // día. `ExamenEnLista` es {Nombre, Fecha, Resultado, AportaSerieContinua}.
    function examenes() {
        var porClave = {};
        NOMBRES.forEach(function (n) {
            var a = ANALITOS[n], examen = EXAMEN_DE[n] || n;
            var filas = a.puntos
                ? a.puntos.map(function (p) { return { fecha: p.fecha, dice: p.valor + ' ' + p.unidad }; })
                : a.pruebas
                ? a.pruebas.map(function (p) { return { fecha: p.fecha, dice: p.expresado }; })
                : a.observaciones.map(function (o) { return { fecha: o.fecha, dice: o.expresado }; });

            filas.forEach(function (f) {
                var clave = examen + '|' + f.fecha;
                if (!porClave[clave])
                    porClave[clave] = { nombre: examen, fecha: f.fecha, partes: [],
                                        aportaSerieContinua: false };
                porClave[clave].partes.push(a.lectura.paraMostrar + ' ' + f.dice);
                if (a.puntos) porClave[clave].aportaSerieContinua = true;
            });
        });

        // LO MÁS RECIENTE PRIMERO, que es como una app de exámenes lista lo suyo.
        return Object.keys(porClave).map(function (k) {
            var e = porClave[k];
            return { nombre: e.nombre, fecha: e.fecha, resultado: e.partes.join(' · '),
                     aportaSerieContinua: e.aportaSerieContinua };
        }).sort(function (a, b) {
            return a.fecha === b.fecha ? a.nombre.localeCompare(b.nombre) : (a.fecha < b.fecha ? 1 : -1);
        });
    }

    window.__tools = {
        // UNA LÍNEA DE RESUMEN: conteos, nada más. Es el bloque útil más chico.
        //
        // `analitosFueraDeRango` es EXACTAMENTE lo que cuenta `Expediente.AnalitosFueraDeRango`:
        // los que tienen un hallazgo HOY. No los que «piden algo» -- una tendencia en ascenso
        // dentro del rango pide algo y no está fuera de rango, y llamarle a eso «fuera de rango»
        // sería justo la clase de mentira que este recorrido existe para no decir.
        resumen: function () {
            return {
                hayDatos: true,
                examenes: examenes().length,
                analitosFueraDeRango: hallazgos().length,
                ultimoEventoLeido: 1284
            };
        },

        // LA LISTA DE EXÁMENES. Filas planas: exactamente lo que una ventana pinta como tabla.
        lista_de_examenes: function () {
            return { hayDatos: true, examenes: examenes() };
        }
    };
})();
