// LA PORTADA DE COMPRAS, portada de Compras.Wasm/Pages/MisCompras.razor a JS plano.
//
// El protocolo NO esta aca: vive en puente.js, que lo pone el Host y es identico en todos los
// modulos. Este archivo es solo el dibujo.
//
// LO QUE PIDE SON [View], no tools. Las dos tools declaradas son lo que el asistente puede
// preguntar; esta pantalla necesita la canasta del mes con sus dos comparaciones, los precios
// que se movieron y el ritmo de cada producto.
//
// HABLA LOS DOS IDIOMAS DE LA APP SUELTA. Ninguna frase se escribe aca adentro: las de la
// pantalla salen del catalogo -- copiado clave por clave de Compras.Wasm/wwwroot/texto -- y las
// del DOMINIO llegan ya dichas por la superficie, a la que se le manda `idioma: I.etiqueta` en
// cada pedido porque adentro de Gnothi no hay navegador que pueda preguntarselo.
(function () {
    'use strict';

    var P = window.Puente;
    var I = window.Idioma;
    var $ = P.$;

    // LO ESTATICO SE DICE YA, y no en arrancar(): el markup de arriba ya esta parseado cuando
    // este script corre, y esperar a DOMContentLoaded seria pintar en un idioma para repintar en
    // el otro.
    I.pintarLoEstatico();

    // EL CUERPO DE LA TARJETA, no la tarjeta. La hoja de la institucion espera `card > card-body`
    // -- es de Bootstrap, y `app.css` la pisa para ponerle la sombra y el radio de La Guaria --
    // asi que escribir sobre la `section` borraria el nodo que da el padding.
    function cuerpo(id) {
        var s = $(id);
        return (s && s.querySelector(".card-body")) || s;
    }

    // ---------------------------------------------------------------- formato

    function texto(t) {
        return String(t === undefined || t === null ? '' : t)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    // Un rotulo del catalogo, escapado y listo para entrar en el HTML.
    function rotulo(clave) { return texto(I.texto(clave)); }

    // LA FRASE DEL CATALOGO SE ESCAPA PRIMERO Y SE LLENA DESPUES, y ese orden es la regla: la
    // sentencia es nuestra pero varios huecos llevan marcado propio, y al reves ese marcado
    // saldria escrito en vez de aplicado. Lo que venga del registro lo escapa quien arma el hueco.
    function frase(clave, huecos) {
        var dicho = texto(I.texto(clave));
        for (var hueco in huecos)
            if (Object.prototype.hasOwnProperty.call(huecos, hueco))
                dicho = dicho.split('{' + hueco + '}').join(huecos[hueco]);
        return dicho;
    }

    // LA EXCEPCION, Y ES UNA SOLA CLASE: hay frases del catalogo que TRAEN su propio marcado --
    // `inicio.representanEl` lleva el <strong class="mk-carrito-cifra"> que la hoja de la
    // institucion pinta --, y escaparlas mostraria la etiqueta escrita. Lo que entra por aca es
    // siempre una frase de texto/*.js; los huecos los escapa quien los arma.
    function fraseConMarcado(clave, huecos) {
        var dicho = I.texto(clave);
        for (var hueco in huecos)
            if (Object.prototype.hasOwnProperty.call(huecos, hueco))
                dicho = dicho.split('{' + hueco + '}').join(huecos[hueco]);
        return dicho;
    }

    // EL SIMBOLO NO SE TRADUCE: es la moneda con la que el super cobra. Es la misma constante que
    // Frases.SimboloDeLaMoneda.
    var SIMBOLO = '₡';

    function miles(entero, separador) {
        return String(entero).replace(/\B(?=(\d{3})+(?!\d))/g, separador);
    }

    // COLONES, SIN DECIMALES Y CON SEPARADOR DE MILES, exactamente como Frases.Monto: se redondea
    // al colon ALEJANDOSE DEL CERO -- que es lo que hace MidpointRounding.AwayFromZero y NO lo que
    // hace Math.round con un negativo -- y el separador es del idioma: el punto en Costa Rica y la
    // coma en ingles. Antes era un espacio fino igual en los dos, que es la forma que no usa
    // ninguno de los dos y ademas discrepaba con la cifra que la superficie manda ya escrita en la
    // misma pantalla.
    function colones(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';

        var redondeado = x < 0 ? -Math.round(-x) : Math.round(x);
        return SIMBOLO + (redondeado < 0 ? '-' : '') +
               miles(Math.abs(redondeado), I.lengua === 'en' ? ',' : '.');
    }

    // UN CONTEO, SIN SEPARADOR: es como lo escribe la app suelta -- `cantidad.ToString()` --, y el
    // separador queda reservado para la plata, que es donde cambia con el idioma.
    function entero(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';
        return String(Math.round(x));
    }

    // Una proporcion dicha como porcentaje, como Frases.Porcentaje: un decimal, ninguno si es
    // entero, la coma decimal del idioma, y el espacio antes del signo solo en espanol.
    function porcentaje(p) {
        var x = Number(p);
        if (!isFinite(x)) return '—';

        var escrito = String(Math.round(x * 1000) / 10);
        if (I.lengua !== 'en') escrito = escrito.replace('.', ',');
        return escrito + (I.lengua === 'en' ? '%' : ' %');
    }

    // Cuantos dias, dicho entero para que «1 días» no aparezca nunca. Es Frases.Dias, que en la
    // app suelta es codigo y no catalogo; aca la palabra tiene que estar escrita, y esta en
    // `plural.dia` / `plural.dias`.
    function dias(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';
        x = Math.round(x);
        return x + ' ' + I.texto(x === 1 ? 'plural.dia' : 'plural.dias');
    }

    function mesCorto(m) { return I.texto('comun.mes.' + Number(m)); }
    function mesLargo(m) { return I.texto('comun.mesLargo.' + Number(m)); }

    // LA FECHA SE PARTE A MANO Y NO CON new Date(iso). Un «2026-05-09» pelado el navegador lo lee
    // como UTC, y al oeste de Greenwich eso lo corre un dia para atras: una garantia que vence el
    // 1 aparecia venciendo el 30. Del mismo aaaa-mm-dd sale el aaaa-mm-ddTHH:mm:ss con que viaja
    // un DateTime, asi que un solo lector sirve para los dos.
    function partesDeFecha(iso) {
        var hallado = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(iso === null || iso === undefined ? '' : iso));
        if (!hallado) return null;
        return { anio: Number(hallado[1]), mes: Number(hallado[2]), dia: Number(hallado[3]) };
    }

    // «9 de mayo de 2026» en espanol, «May 9, 2026» en ingles: el orden es del catalogo y no de
    // esta funcion, que es lo que hace que las dos pantallas lo escriban igual.
    function fecha(iso) {
        var d = partesDeFecha(iso);
        if (!d) return '';
        return I.con('comun.fechaLarga', { numero: d.dia, mes: mesLargo(d.mes), anio: d.anio });
    }

    // «enero de 2025» desde el aaaa-mm con que el mes VIAJA. El guion es la forma de maquina; en
    // pantalla se lee, y en el idioma de quien mira.
    function mesDeAnio(aaaaMm) {
        var partes = String(aaaaMm || '').split('-');
        if (partes.length < 2) return '';
        var mes = Number(partes[1]);
        if (!(mes >= 1 && mes <= 12)) return '';
        return I.con('comun.mesDeAnio', { mes: mesLargo(mes), anio: partes[0] });
    }

    var CODIGO_DEL_DIA = ['domingo', 'lunes', 'martes', 'miercoles', 'jueves', 'viernes', 'sabado'];

    // El dia de la semana de una fecha ya partida. Se construye con (anio, mes, dia) y no con la
    // cadena, por la misma razon de zona horaria de arriba.
    function diaDeLaSemana(d) {
        var cual = new Date(d.anio, d.mes - 1, d.dia).getDay();
        return I.texto('comun.dia.' + CODIGO_DEL_DIA[cual]);
    }

    // «válida hasta el viernes 5» cuando quedan dias, «último día» cuando es hoy. Mismo reparto
    // que Vigencia() en MisCompras.razor.
    function vigencia(promo) {
        if (Number(promo.diasQueQuedan) === 0) return I.texto('inicio.ultimoDia');

        var d = partesDeFecha(promo.validaHasta);
        if (d) return I.con('inicio.validaHasta', { dia: diaDeLaSemana(d), numero: d.dia });

        return I.con('inicio.quedanDias', { dias: dias(promo.diasQueQuedan) });
    }

    function dato(etiqueta, valor, extra) {
        return '<div class="dato"><span>' + texto(etiqueta) + '</span><b>' + valor +
               (extra ? ' ' + extra : '') + '</b></div>';
    }

    function apunte(dicho) {
        return '<span class="apunte" style="font-weight:400"> · ' + dicho + '</span>';
    }

    // EL SIGNO DEL CAMBIO, y el color NO sigue al signo: sigue a lo que significa. En un
    // supermercado gastar MAS es la noticia mala aunque el numero suba, asi que subir va en rojo
    // y bajar en verde -- al reves que en un grafico financiero. Es la misma regla que la app.
    //
    // Y LO QUE SE DICE ES PLATA, NO UN PORCENTAJE: la app suelta pone «₡12 400 · menos que en
    // ago», que es `Math.Abs(mes.CambioEnElTotal)` con su rotulo. El porcentaje que habia aca
    // salia de `cambio`, otro campo, y las dos pantallas contestaban distinto sobre el mismo mes.
    function cambioEnPlata(cambio) {
        var x = Number(cambio);
        if (!isFinite(x) || x === 0)
            return '<span class="delta--igual">' + rotulo('cara.igual') + '</span>';

        return '<span class="' + (x > 0 ? 'delta--sube' : 'delta--baja') + '">' +
               (x > 0 ? '▲' : '▼') + ' ' + texto(colones(Math.abs(x))) + '</span>';
    }

    // ---------------------------------------------------------------- la portada

    // EL CARRUSEL, LA FRANJA DE CIFRAS Y EL RENGLON DE ESTADO, que es con lo que abre la app.
    //
    // LOS BANNERS LLEGAN ELEGIDOS Y YA ESCRITOS. Esta funcion no decide cual senal se muestra ni
    // en que orden: eso lo hace `LaPortadaDeLaCara` del lado de la superficie, porque son reglas
    // del producto (una novedad vieja no es noticia, una promo de algo que ella no compra no se
    // fuerza). Aca solo se dibuja lo que vino.
    // LAS FRASES DEL CARRUSEL, con las MISMAS claves que `ArmarHeroBanners` de la app suelta.
    //
    // Hasta el 2026-09-18 el banner llegaba con `kicker`, `titulo`, `secundario` y `cta` ya
    // escritos desde `LaPortadaDeLaCara`, en espanol duro: en ingles el carrusel quedaba en
    // espanol arriba de una pantalla en ingles. Ahora la superficie manda los INGREDIENTES --
    // que producto, cuantos ritmos cambiaron, cuantos dias le quedan a la promo -- y la frase se
    // arma aca. QUE banner se muestra y en que orden lo sigue decidiendo ella: es regla de
    // producto y las dos caras la comparten.
    //
    // El gancho y el detalle de una promocion NO pasan por catalogo: los escribio el
    // supermercado para esa promocion, como el nombre de un producto.
    function frasesDelBanner(b) {
        if (b.variante === 'nuevo')
            return {
                kicker: I.texto('hero.nuevo'),
                titulo: I.con('hero.nuevoTitulo', { producto: b.producto }),
                cta: I.texto('hero.verlo'),
                secundario: '',
            };

        if (b.variante === 'habitual')
            return {
                kicker: I.texto('hero.bajoDePrecio'),
                titulo: I.con('hero.bajoDePrecioTitulo', { producto: b.producto }),
                cta: I.texto('hero.verlo'),
                secundario: '',
            };

        if (b.variante === 'promo')
            return {
                kicker: I.texto('hero.puedeInteresarte'),
                titulo: b.gancho,
                cta: '',
                secundario: b.detalle + ' · ' + vigencia(b),
            };

        // EL NEUTRO SON DOS, y los separa si ya hay compras leidas: sin nada todavia, la
        // bienvenida; con historial pero sin ninguna senal, como vienen las compras.
        if (!b.hayCompras)
            return {
                kicker: '',
                titulo: I.texto('hero.bienvenida'),
                cta: '',
                secundario: I.texto('hero.bienvenidaBajada'),
            };

        return {
            kicker: '',
            titulo: I.texto('hero.asiVienen'),
            cta: '',
            secundario: b.cuantosRitmos > 0
                ? I.con(b.cuantosRitmos === 1 ? 'hero.casiTodoHabitualUno' : 'hero.casiTodoHabitualVarios',
                        { cuantos: entero(b.cuantosRitmos) })
                : I.texto('hero.todoComoSiempre'),
        };
    }

    function pintarPortada(c) {
        var cifras = $('cifras');
        var pista = $('lgHomeCarrusel');
        var puntos = $('carrusel-puntos');
        var envoltorio = $('carrusel-envoltorio');

        if (!c || c.hayDatos === false) {
            cifras.textContent = I.texto('comun.sinCompras');
            return;
        }

        var desde = mesDeAnio(c.primerMes);
        cifras.textContent =
            I.con(c.cantidad === 1 ? 'inicio.compraTuya' : 'inicio.comprasTuyas', { cuantas: entero(c.cantidad) }) +
            (desde ? ' ' + I.con('inicio.desde', { cuando: desde }) : '') + ' · ' + colones(c.total);

        $('estado-cambio').textContent = c.cambios > 0
            ? I.con(c.cambios === 1 ? 'inicio.cambioReciente' : 'inicio.cambiosRecientes',
                    { cuantos: entero(c.cambios) })
            : I.texto('inicio.todoComoSiempre');
        $('estado-cambio').className = 'lg-home-estado-cambio' +
            (c.cambios > 0 ? '' : ' lg-home-estado-cambio--calmo');

        var banners = c.banners || [];
        if (banners.length === 0) { envoltorio.hidden = true; return; }

        envoltorio.hidden = false;
        pista.innerHTML = banners.map(function (b) {
            var dicho = frasesDelBanner(b);

            var precio = (b.precioAntes !== null && b.precioAntes !== undefined &&
                          b.precioAhora !== null && b.precioAhora !== undefined)
                ? '<span class="lg-home-banner-precio">' +
                      '<span class="lg-home-banner-precio-antes">' + colones(b.precioAntes) + '</span>' +
                      '<span aria-hidden="true">&rarr;</span>' +
                      '<span class="lg-home-banner-precio-ahora">' + colones(b.precioAhora) + '</span>' +
                  '</span>'
                : (dicho.secundario ? '<p class="lg-home-banner-secundario">' + texto(dicho.secundario) + '</p>' : '');

            // EL CTA NO ES UN ENLACE. En la app lleva a `producto/<nombre>`, que es una ruta; esta
            // cara no tiene router, asi que se dibuja igual -- la marca lo pide -- pero sin href.
            return '<div class="lg-home-banner lg-home-banner--' + texto(b.variante) +
                        (b.foto ? '' : ' lg-home-banner--sin-imagen') + '">' +
                   '<div class="lg-home-banner-texto">' +
                       (dicho.kicker ? '<span class="lg-home-banner-kicker">' + texto(dicho.kicker) + '</span>' : '') +
                       '<h1>' + texto(dicho.titulo) + '</h1>' +
                       precio +
                       (dicho.cta ? '<span class="lg-home-banner-cta">' + texto(dicho.cta) + ' &rarr;</span>' : '') +
                   '</div>' +
                   (b.foto ? '<img class="lg-home-banner-imagen" src="' + texto(b.foto) + '" alt="" aria-hidden="true" />' : '') +
                   '</div>';
        }).join('');

        puntos.innerHTML = banners.length > 1
            ? banners.map(function (_, i) {
                  return '<span class="lg-home-punto' + (i === 0 ? ' activo' : '') + '"></span>';
              }).join('')
            : '';

        pintarRutina(c.rutina);
        pintarCarrito(c.canasta);
        pintarOportunidad(c.habitual);
    }

    // ¿EN QUE SE LLENA TU CARRITO? Lo que manda es la FRASE -- «tus N categorias principales
    // representan el X%» --, no la barra: eso se lee en dos segundos y el resto es apoyo. Los
    // departamentos vienen ya ordenados del que mas pesa al que menos, del dominio.
    //
    // TRES PRINCIPALES Y EL RESTO EN «OTROS», que es el corte de la app.
    function pintarCarrito(k) {
        var caja = $('carrito');
        if (!k || !k.hayCompras || !(k.departamentos || []).length) { caja.hidden = true; return; }

        var todos = k.departamentos;
        var top = todos.slice(0, 3);
        var otros = todos.slice(3);
        var propTop = top.reduce(function (s, d) { return s + Number(d.proporcionDelTotal || 0); }, 0);
        var propOtros = otros.reduce(function (s, d) { return s + Number(d.proporcionDelTotal || 0); }, 0);
        var montoOtros = otros.reduce(function (s, d) { return s + Number(d.monto || 0); }, 0);

        var cuantas = top.length === 1
            ? I.texto('inicio.categoriaPrincipal')
            : I.con('inicio.categoriasPrincipales', { cuantas: entero(top.length) });

        function grow(p) { return Math.max(1, Math.round(Number(p || 0) * 1000)); }

        caja.hidden = false;
        caja.querySelector('.card-body').innerHTML =
            '<div class="mk-carrito-cabeza">' +
                '<div class="mk-carrito-titulos">' +
                    '<h2 class="mk-carrito-titulo">' + rotulo('inicio.enQueSeLlena') + '</h2>' +
                    // EL MES VA CORTO -- «sep/2026» --, que es como lo escribe la app suelta con
                    // `comun.mes.N`. Aca decia «setiembre/2026», el largo, y en la misma pantalla
                    // el titulo del mes ya usa el largo: dos formas del mismo mes a dos dedos.
                    '<p class="mk-carrito-periodo">' + texto(mesCorto(k.mes)) + '/' + texto(k.anio) +
                        ' &middot; ' + colones(k.gastoTotal) + '</p>' +
                '</div>' +
                '<img class="mk-carrito-ilustracion" src="assets/hero-canasta-3d.png" alt="" aria-hidden="true" />' +
            '</div>' +
            '<p class="mk-carrito-destacado">' +
                fraseConMarcado('inicio.representanEl', {
                    cuantas: texto(cuantas),
                    porcentaje: texto(porcentaje(propTop))
                }) + '</p>' +
            '<div class="mk-carrito-barra" aria-hidden="true">' +
                top.map(function (d, i) {
                    return '<span class="mk-carrito-segmento mk-carrito-segmento--' + (i + 1) +
                           '" style="flex-grow:' + grow(d.proporcionDelTotal) + '"></span>';
                }).join('') +
                (otros.length ? '<span class="mk-carrito-segmento mk-carrito-segmento--otros" style="flex-grow:' +
                    grow(propOtros) + '"></span>' : '') +
            '</div>' +
            '<div class="mk-carrito-datos">' +
                top.map(function (d, i) {
                    return '<div class="mk-carrito-dato">' +
                           '<span class="mk-carrito-punto mk-carrito-punto--' + (i + 1) + '" aria-hidden="true"></span>' +
                           '<span class="mk-carrito-dato-texto">' +
                               '<span class="mk-carrito-dato-nombre">' + texto(d.titulo) + '</span>' +
                               '<span class="mk-carrito-dato-cifras">' + colones(d.monto) + ' &middot; ' +
                                   porcentaje(d.proporcionDelTotal) + '</span>' +
                           '</span></div>';
                }).join('') +
                (otros.length
                    ? '<div class="mk-carrito-dato mk-carrito-dato--otros">' +
                      '<span class="mk-carrito-punto mk-carrito-punto--otros" aria-hidden="true"></span>' +
                      '<span class="mk-carrito-dato-texto">' +
                          '<span class="mk-carrito-dato-nombre">' + rotulo('inicio.otros') + '</span>' +
                          '<span class="mk-carrito-dato-cifras">' + colones(montoOtros) + ' &middot; ' +
                              porcentaje(propOtros) + '</span>' +
                      '</span></div>'
                    : '') +
            '</div>';
    }

    // TU HABITUAL: el mini-afiche de la mejor bajada entre lo que ella lleva siempre. Los dos
    // circulos de fondo son de la hoja -- `blob-a` y `blob-b` -- y van vacios a proposito.
    function pintarOportunidad(h) {
        var caja = $('oportunidad');
        if (!h) { caja.hidden = true; return; }

        caja.hidden = false;
        caja.innerHTML =
            '<span class="mk-oportunidad-blob-a" aria-hidden="true"></span>' +
            '<span class="mk-oportunidad-blob-b" aria-hidden="true"></span>' +
            (h.foto ? '<img class="mk-oportunidad-foto" src="' + texto(h.foto) + '" alt="" />' : '') +
            '<div class="mk-oportunidad-que">' +
                '<div class="mk-oportunidad-kicker-fila">' +
                    '<span class="mk-oportunidad-kicker">' + rotulo('inicio.tuHabitual') + '</span>' +
                    '<span class="mk-oportunidad-badge">' + rotulo('inicio.mejorPrecio') + '</span>' +
                '</div>' +
                '<strong class="mk-oportunidad-nombre">' + texto(h.descripcion) + '</strong>' +
                '<div class="mk-oportunidad-precio"><span>' + rotulo('inicio.ahora') +
                    '</span><strong>' + colones(h.ahora) + '</strong></div>' +
                '<div class="mk-oportunidad-ahorro">' +
                    '<span class="precio-antes">' + colones(h.antes) + '</span> &middot; ' +
                    '<strong>' + frase('inicio.menos', { monto: texto(colones(h.ahorro)) }) + '</strong>' +
                '</div>' +
            '</div>';
    }

    // UN CAMBIO EN TU RUTINA: grid de dos columnas, foto fija y contenido. Si el producto no tiene
    // foto el <img> no se dibuja y la columna queda vacia -- el texto no se corre a ocuparla
    // porque su grid-column esta fijo en 2.
    function pintarRutina(r) {
        var caja = $('rutina');
        if (!r) { caja.hidden = true; return; }

        // El corte viaja como aaaa-mm y el nombre del mes lo pone el catalogo: «cambió desde
        // enero» / «changed since January».
        var partes = String(r.desdeMes || '').split('-');
        var desde = partes.length > 1 && Number(partes[1]) >= 1 && Number(partes[1]) <= 12
            ? mesLargo(partes[1]) : '';

        caja.hidden = false;
        caja.innerHTML =
            (r.foto ? '<img class="mk-cambio-foto" src="' + texto(r.foto) + '" alt="" />' : '') +
            '<span class="mk-cambio-que">' +
                '<span class="mk-cambio-nombre">' +
                    '<strong>' + texto(r.descripcion) + '</strong>' +
                    '<span class="mk-cambio-pill">' + rotulo('inicio.cambioPill') + '</span>' +
                '</span>' +
                '<span class="mk-cambio-linea">' +
                    '<span>' + texto(r.antes) + '</span>' +
                    '<span aria-hidden="true">&rarr;</span>' +
                    '<span>' + texto(r.ahora) + '</span>' +
                '</span>' +
                (desde ? '<span class="mk-cambio-desde">' +
                    frase('inicio.cambioDesde', { mes: texto(desde) }) + '</span>' : '') +
            '</span>';
    }

    // ---------------------------------------------------------------- la firma

    // LA FIRMA DE LA SEMANA. Es lo que hace que la pantalla la reconozca a ella: cuantas compras
    // se miraron y cuantos productos vuelven siempre. La frase la escribe el dominio.
    function pintarFirma(c) {
        var caja = cuerpo('firma');
        caja.setAttribute('aria-busy', 'false');

        if (!c || c.hayDatos === false) {
            // LA TECLA SE NOMBRA SOLA, y no a mano: aca decia «Apretá Actualizar» y el boton dice
            // «Buscar compras nuevas». Renombrarlo una vez ya habia dejado la frase mintiendo, asi
            // que el nombre entra como hueco y sale del mismo catalogo que rotula la tecla.
            caja.innerHTML = '<p class="apunte">' +
                frase('cara.sinComprasAca', { boton: '<em>' + rotulo('inicio.buscarNuevas') + '</em>' }) +
                '</p>';
            return;
        }

        // LA CANTIDAD Y EL TOTAL YA NO VAN ACA: viven arriba, en la franja de «87 compras tuyas
        // desde enero de 2025 · ₡3.217.096», que es donde la app los pone. Repetirlos era lo que
        // hacia que esta tarjeta se leyera como un panel de cifras y no como «tus habituales».
        var f = c.firma || {}, r = c.rango || {};
        caja.innerHTML =
            (f.paraElAsistente ? '<p class="apunte" style="margin:0 0 12px">' + texto(f.paraElAsistente) + '</p>' : '') +
            (f.cuantosHabituales ? dato(I.texto('cara.productosQueVuelven'), entero(f.cuantosHabituales)) : '') +
            (f.comprasMiradas ? dato(I.texto('cara.comprasMiradas'), entero(f.comprasMiradas)) : '') +
            (r.mesesConCompras ? dato(I.texto('cara.mesesConCompras'), entero(r.mesesConCompras)) : '');
    }

    // ---------------------------------------------------------------- el mes

    function pintarMes(c) {
        var caja = cuerpo('mes');
        caja.setAttribute('aria-busy', 'false');

        if (!c || c.hayDatos === false) {
            P.apunte(caja, I.texto('cara.sinMesCerrado'));
            return;
        }

        var m = c.mes || {}, anterior = c.contraElMesAnterior || {}, anio = c.contraElAnioPasado || {};

        // EL MES LO ELIGE EL DOMINIO -- el ultimo con compras -- y no esta pantalla. Pedirle a la
        // persona que elija un mes seria pedirle que sepa cual tiene datos.
        caja.innerHTML =
            '<p class="mk-titulo" style="margin:0 0 10px">' +
                texto(I.con('comun.mesDeAnio', { mes: mesLargo(m.mes), anio: m.anio })) + '</p>' +
            dato(I.texto('cara.gastoDelMes'), colones(m.gastoTotal)) +
            dato(I.texto('cara.enComida'), colones(m.gastoComestible)) +
            dato(I.texto('cara.enLoDemas'), colones(m.gastoNoComestible)) +
            dato(I.texto(m.cantidadDeCompras === 1 ? 'comun.compra' : 'comun.comprasPlural'),
                 entero(m.cantidadDeCompras)) +
            // CONTRA EL MES ANTERIOR, EN PLATA Y CONTRA UN MES CON NOMBRE: la app suelta pone
            // «₡12.400 · menos que en ago», y el mes corto sale de `mesAntes`. Antes se decia un
            // porcentaje -- otro campo -- al lado del gasto del mes anterior, que es la cifra que
            // nadie compara de memoria.
            (anterior.hayComparacion
                ? dato(I.texto('cara.contraElMesAnterior'),
                       cambioEnPlata(anterior.cambioEnElTotal),
                       apunte(frase(Number(anterior.cambioEnElTotal) <= 0 ? 'inicio.menosQueEn' : 'inicio.masQueEn',
                                    { mes: texto(mesCorto(anterior.mesAntes)) })))
                : '') +
            // LA FRASE ES `paraElAsistente`, NO `corte`. `Corte` es el mes por el que se partió el
            // historial, como `aaaa-mm`: es el eje de la comparación, no una oración. Mostrarlo
            // dejaba un «2026-01» suelto al pie de la tarjeta, que se lee como un dato filtrado y
            // no como una lectura. Visto en el teléfono el 2026-09-14.
            //
            // Y sin `hayConQueComparar` no se afirma nada: es la guarda del propio dominio.
            (anio.hayConQueComparar && anio.paraElAsistente
                ? '<p class="apunte" style="margin:12px 0 0">' + texto(anio.paraElAsistente) + '</p>'
                : '');
    }

    // ---------------------------------------------------------------- lo que se movió

    // LOS PRECIOS QUE CAMBIARON Y LOS RITMOS QUE SE ROMPIERON. Es la vista que no tiene
    // equivalente en una pantalla de supermercado: un super entrega facturas, no cambios de
    // precio ni rupturas de rutina.
    function pintarMovimientos(m) {
        var caja = cuerpo('movimientos');
        caja.setAttribute('aria-busy', 'false');

        var precios = (m && m.precios) || [], ritmos = (m && m.ritmos) || [];
        var r = (m && m.ritmo) || {};
        var html = '';

        // «Cada 7 días volvés al súper · Mirando tus últimas 8 compras»: las tres piezas son las
        // de la pantalla /ritmo de la app suelta, que esta cara no tiene y por eso resume aca.
        if (r.esObservado && r.diasEntreCompras)
            html += '<p class="apunte" style="margin:0 0 12px">' +
                    '<b>' + texto(I.con('ritmo.cada', { dias: dias(r.diasEntreCompras) })) + '</b> ' +
                    rotulo('ritmo.volves') + ' &middot; ' +
                    texto(I.con('ritmo.mirando', { compras: entero(r.comprasMiradas) })) + '</p>';

        if (precios.length) {
            html += '<p class="mk-titulo" style="margin:4px 0 6px">' +
                    rotulo('cara.preciosQueSeMovieron') + '</p>';
            precios.slice(0, 8).forEach(function (p) {
                html += dato(p.descripcion, colones(p.ahora),
                    apunte(frase('cara.antes', { valor: texto(colones(p.antes)) })));
            });
        }

        if (ritmos.length) {
            html += '<p class="mk-titulo" style="margin:14px 0 6px">' +
                    rotulo('cara.ritmosQueCambiaron') + '</p>';
            ritmos.slice(0, 8).forEach(function (x) {
                html += '<div class="dato"><span>' + texto(x.descripcion) + '</span><b>' +
                        texto(x.ahora) +
                        apunte(frase('cara.antes', { valor: texto(x.antes) })) + '</b></div>';
            });
        }

        if (!html) html = '<p class="apunte">' + rotulo('cara.nadaSeMovio') + '</p>';
        caja.innerHTML = html;
    }

    // ---------------------------------------------------------------- lo que viene

    // LOS QUE SE PINTAN: solo los ATRASADOS, y unos pocos. Es el corte de `Faltantes()` en
    // MisCompras.razor: la lista viene ordenada del mas atrasado al que menos falta, asi que se
    // corta en el primero que no toca y despues de ese no hay ninguno que si.
    var FALTANTES_QUE_SE_MUESTRAN = 2;

    function losQueTocan(lista) {
        var pocos = [];
        var productos = (lista && lista.productos) || [];
        for (var i = 0; i < productos.length; i++) {
            if (!productos[i].toca || pocos.length === FALTANTES_QUE_SE_MUESTRAN) break;
            pocos.push(productos[i]);
        }
        return pocos;
    }

    function pintarProxima(p) {
        var caja = cuerpo('proxima');
        caja.setAttribute('aria-busy', 'false');

        var lista = (p && p.lista) || {}, promos = (p && p.promociones) || [];
        var faltan = (p && p.abastecimientos) || [], garantias = (p && p.garantias) || [];
        var html = '';

        if (lista.existe && lista.cuantosTocan) {
            html += '<p class="mk-titulo" style="margin:0 0 6px">' +
                    rotulo('inicio.quizaTeToque') + '</p>';
            if (lista.paraElAsistente)
                html += '<p class="apunte" style="margin:0 0 10px">' + texto(lista.paraElAsistente) + '</p>';

            // CADA FILA CON SU RAZON, nunca la afirmacion de que falta: «suele volver 1 vez/semana»
            // y «hace 12 días». Aca se leia `x.cuando`, un campo que ProductoPorCaer NO tiene --
            // asi que la columna derecha salia vacia en todas las filas, siempre.
            losQueTocan(lista).forEach(function (x) {
                html += dato(x.descripcion,
                    texto(I.con('inicio.hace', { dias: dias(x.diasDesdeLaUltima) })),
                    apunte(frase('inicio.sueleVolver', { cadencia: texto(x.cadenciaCompacta) })));
            });

            if (lista.cuantosTocan > FALTANTES_QUE_SE_MUESTRAN)
                html += '<p class="apunte" style="margin:6px 0 0">' +
                        frase('inicio.yMasFaltantes', {
                            cuantos: texto(entero(lista.cuantosTocan - FALTANTES_QUE_SE_MUESTRAN))
                        }) + '</p>';
        }

        // LO QUE NO ESTA: una categoria que se repone cada tanto y hace demasiado que no entra.
        // La pantalla no puede renderizar una ausencia que nadie le paso -- se la pasa el
        // dominio.
        if (faltan.length) {
            html += '<p class="mk-titulo" style="margin:14px 0 6px">' +
                    rotulo('inicio.loQueDejoDeEntrar') + '</p>';
            faltan.slice(0, 8).forEach(function (a) {
                html += dato(a.titulo, texto(dias(a.diasDesdeLaUltima)),
                    a.esRitmoObservado && a.diasEsperados
                        ? apunte(frase('cara.solesCada', { dias: texto(dias(a.diasEsperados)) }))
                        : '');
            });
        }

        // LAS PROMOCIONES, COMO TEXTO Y SIN FOTO. El carrusel de la app suelta vive de imagenes
        // servidas por el super, y esta cara no sale a la red. Lo que queda es lo que el dominio
        // sabe: el gancho y hasta cuando vale.
        if (promos.length) {
            html += '<p class="mk-titulo" style="margin:14px 0 6px">' +
                    rotulo('hero.puedeInteresarte') + '</p>';
            promos.slice(0, 6).forEach(function (x) {
                html += '<div class="dato"><span>' + texto(x.gancho) +
                        (x.detalle ? '<br><span class="apunte">' + texto(x.detalle) + '</span>' : '') +
                        '</span><b class="apunte" style="font-weight:600">' +
                        texto(vigencia(x)) + '</b></div>';
            });
        }

        // LAS GARANTIAS: importa EL DIA QUE HAY QUE RECLAMAR, no el dia en que compro. Aca se
        // pintaba `fechaDeCompra`, que es el dato con el que la garantia se calcula y no el que la
        // persona necesita; la app suelta pone «te cubre hasta el 9 de mayo de 2026».
        if (garantias.length) {
            html += '<p class="mk-titulo" style="margin:14px 0 6px">' +
                    rotulo('inicio.tusGarantias') + '</p>';
            garantias.slice(0, 6).forEach(function (g) {
                html += dato(g.descripcion,
                    texto(I.con('inicio.cubreHasta', { fecha: fecha(g.venceEl) })),
                    g.numeroDeCompra ? '<span class="apunte mono" style="font-weight:400"> · ' +
                        texto(g.numeroDeCompra) + '</span>' : '');
            });
        }

        if (!html) html = '<p class="apunte">' + rotulo('cara.nadaPendiente') + '</p>';
        caja.innerHTML = html;
    }

    // ---------------------------------------------------------------- las compras

    function pintarCompras(r) {
        var caja = cuerpo('compras');
        caja.setAttribute('aria-busy', 'false');

        var filas = (r && (r.compras || r.lista)) || (Array.isArray(r) ? r : []);
        if (!filas.length) {
            P.apunte(caja, I.texto('comun.sinCompras'));
            return;
        }

        // De la mas reciente a la mas vieja: lo ultimo es lo que se mira.
        caja.innerHTML = filas.slice().reverse().slice(0, 20).map(function (c) {
            return '<div class="dato"><span>' + texto(fecha(c.fecha)) +
                   (c.sucursal ? '<br><span class="apunte">' + texto(c.sucursal) + '</span>' : '') +
                   '</span><b>' + colones(c.total) + '</b></div>';
        }).join('');
    }

    // ---------------------------------------------------------------- arranque

    function contar(t, malo) {
        var p = $('conexion');
        p.textContent = t;
        p.classList.toggle('malo', !!malo);
    }

    // LA ETIQUETA VA EN CADA PEDIDO QUE COMPONE FRASES. La superficie corre adentro de Gnothi,
    // sin navegador: si no se la mandan, contesta en el idioma del registro del supermercado.
    var EN_SU_IDIOMA = { idioma: I.etiqueta };

    function pintarTodo() {
        // LA PORTADA PRIMERO, y en una sola vuelta: el carrusel, la franja de cifras, el renglon
        // de estado, la rutina, el carrito y el habitual salen todos de la MISMA lectura. Partirlo
        // en seis pedidos serian seis saltos del puente para pintar una pantalla.
        P.pedir('portada', EN_SU_IDIOMA).then(pintarPortada)
            .catch(function (e) { $('cifras').textContent = P.porQue(e); });

        P.pedir('cabecera', EN_SU_IDIOMA).then(pintarFirma)
            .catch(function (e) { P.apunte(cuerpo('firma'), P.porQue(e), true); });

        P.pedir('canasta', EN_SU_IDIOMA).then(pintarMes)
            .catch(function (e) { P.apunte(cuerpo('mes'), P.porQue(e), true); });

        P.pedir('movimientos', EN_SU_IDIOMA).then(pintarMovimientos)
            .catch(function (e) { P.apunte(cuerpo('movimientos'), P.porQue(e), true); });

        P.pedir('proxima', EN_SU_IDIOMA).then(pintarProxima)
            .catch(function (e) { P.apunte(cuerpo('proxima'), P.porQue(e), true); });

        // SIN IDIOMA, y no es un olvido: `compras` devuelve fechas, sucursales y totales, que son
        // del registro del supermercado. No hay ninguna frase que componer.
        P.pedir('compras').then(pintarCompras)
            .catch(function (e) { P.apunte(cuerpo('compras'), P.porQue(e), true); });
    }

    function releer() {
        var b = $('releer');
        var chico = $('refrescar-chico');
        b.disabled = true;
        chico.disabled = true;
        b.setAttribute('aria-busy', 'true');
        b.textContent = I.texto('inicio.buscando');

        P.refrescar(contar).then(function (r) {
            if (r) pintarTodo();
            b.disabled = false;
            chico.disabled = false;
            b.setAttribute('aria-busy', 'false');
            b.textContent = I.texto('inicio.buscarNuevas');
        });
    }

    // LAS DOS PESTANAS SON SECCIONES. No hay router en una cara hibrida, asi que Inicio y Compras
    // viven las dos en el mismo documento y esto muestra una. `href="#"` con preventDefault: un
    // href a una ruta que no existe recargaria la pagina adentro del WebView.
    function mostrar(cual) {
        $('pestana-inicio').hidden = cual !== 'inicio';
        $('pestana-compras').hidden = cual !== 'compras';

        var pestanas = document.querySelectorAll('nav.pestanas .pestana');
        for (var i = 0; i < pestanas.length; i++)
            pestanas[i].classList.toggle('active', pestanas[i].getAttribute('data-pestana') === cual);

        window.scrollTo(0, 0);
    }

    function arrancar() {
        P.listo();
        $('releer').addEventListener('click', releer);
        $('refrescar-chico').addEventListener('click', releer);

        var saltos = document.querySelectorAll('[data-pestana]');
        for (var i = 0; i < saltos.length; i++)
            saltos[i].addEventListener('click', function (e) {
                e.preventDefault();
                mostrar(this.getAttribute('data-pestana'));
            });

        // PRIMERO SE PINTA LO QUE YA HAY, y despues se refresca. Al reves, abrir con el
        // publicador caido dejaria la pantalla en blanco hasta que el pedido falle -- y tener el
        // dato propio sirve justamente para que eso no pase.
        pintarTodo();
        P.refrescar(contar).then(function (r) { if (r) pintarTodo(); });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', arrancar);
    else arrancar();
})();
