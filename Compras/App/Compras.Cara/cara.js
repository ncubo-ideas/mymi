// LA PORTADA DE COMPRAS, portada de Compras.Wasm/Pages/MisCompras.razor a JS plano.
//
// El protocolo NO esta aca: vive en puente.js, que lo pone el Host y es identico en todos los
// modulos. Este archivo es solo el dibujo.
//
// LO QUE PIDE SON [View], no tools. Las dos tools declaradas son lo que el asistente puede
// preguntar; esta pantalla necesita la canasta del mes con sus dos comparaciones, los precios
// que se movieron y el ritmo de cada producto.
(function () {
    'use strict';

    var P = window.Puente;
    var I = window.Idioma;
    var $ = P.$;

    // LO ESTATICO SE DICE YA, y no en arrancar(): el markup de arriba ya esta parseado cuando este
    // script corre, y esperar a DOMContentLoaded seria pintar en un idioma para repintar en el otro.
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

    // COLONES, SIN DECIMALES Y CON SEPARADOR DE MILES. Es la forma en que se lee un precio en el
    // super: «₡12 450», no «₡12450.00». El separador es un espacio fino, que es lo que la hoja
    // usa en toda cifra.
    function colones(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';
        return '₡' + String(Math.round(x)).replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
    }

    function entero(n) {
        var x = Number(n);
        if (!isFinite(x)) return '—';
        return String(Math.round(x)).replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
    }

    // LOS MESES SALEN DEL CATALOGO, una clave por mes, igual que en la app suelta. Eran dos
    // arreglos en espanol escritos aca: el nombre del mes es palabra de esta pantalla y no dato del
    // registro, asi que se traduce.
    function mesLargo(m) { return I.mesLargo(m || 1); }

    // «enero de 2025» / «January 2025» DESDE EL aaaa-mm QUE MANDA LA SUPERFICIE. El mes lo nombra
    // esta cara y no ella: la superficie decide QUE se dice y la cara COMO se ve, y un nombre de mes
    // es lo segundo. Antes lo componia LaPortadaDeLaCara con un arreglo en espanol, que era un
    // tercer juego de nombres de mes -- el de la app suelta, el de esta cara y ese -- y el unico que
    // no seguia al idioma.
    function mesDeAnio(aaaaMm) {
        var partes = String(aaaaMm || '').split('-');
        if (partes.length < 2) return String(aaaaMm || '');
        var m = Number(partes[1]);
        if (!(m >= 1 && m <= 12)) return String(aaaaMm);
        return I.con('comun.mesDeAnio', { mes: mesLargo(m), anio: partes[0] });
    }

    // Y el mes solo, para «cambió desde enero».
    function soloElMes(aaaaMm) {
        var partes = String(aaaaMm || '').split('-');
        if (partes.length < 2) return String(aaaaMm || '');
        var m = Number(partes[1]);
        return (m >= 1 && m <= 12) ? mesLargo(m) : String(aaaaMm);
    }

    // Y EL ORDEN DE LA FECHA TAMBIEN ES DEL IDIOMA -- «12 sep 2026» contra «Sep 12, 2026» --, asi
    // que la arma una frase con huecos nombrados y no una concatenacion.
    function fecha(iso) {
        if (!iso) return '';
        var d = new Date(iso);
        if (isNaN(d.getTime())) return String(iso).slice(0, 10);
        return I.con('comun.fechaCorta', {
            numero: d.getDate(), mes: I.mesCorto(d.getMonth() + 1), anio: d.getFullYear() });
    }

    function plural(n, una, varias) { return n === 1 ? una : varias; }

    // CUANTOS DIAS, dicho entero, para que «1 días» no aparezca nunca en ningun idioma.
    function dias(n) {
        return n + ' ' + texto(I.texto(plural(n, 'comun.dia', 'comun.dias')));
    }

    // LA FRASE DEL CATALOGO NO SE ESCAPA, Y ES DELIBERADO. Varias traen marcado propio -- el
    // <strong class="mk-carrito-cifra"> de la cifra del carrito, el <strong> del ahorro -- porque
    // estan copiadas enteras de la app suelta, que ya las lleva asi; escaparlas imprimiria las
    // etiquetas en vez de aplicarlas.
    //
    // ES SEGURO PORQUE EL CATALOGO ES NUESTRO: viaja en el bundle y nada del registro pasa por el.
    // LO QUE VIENE DEL REGISTRO LO ESCAPA QUIEN ARMA EL HUECO, con texto(), en el llamado. Esa es
    // la linea que hay que respetar al agregar una clave nueva.
    function frase(clave, huecos) {
        var dicho = I.texto(clave);
        for (var hueco in huecos)
            if (Object.prototype.hasOwnProperty.call(huecos, hueco))
                dicho = dicho.split('{' + hueco + '}').join(huecos[hueco]);
        return dicho;
    }

    // EL SIGNO DEL CAMBIO, y el color NO sigue al signo: sigue a lo que significa. En un
    // supermercado gastar MAS es la noticia mala aunque el numero suba, asi que subir va en rojo
    // y bajar en verde -- al reves que en un grafico financiero. Es la misma regla que la app.
    function delta(cambio) {
        var x = Number(cambio);
        if (!isFinite(x) || x === 0)
            return '<span class="delta--igual">' + texto(I.texto('delta.igual')) + '</span>';
        var clase = x > 0 ? 'delta--sube' : 'delta--baja';
        return '<span class="' + clase + '">' + (x > 0 ? '▲' : '▼') + ' ' +
               Math.abs(Math.round(x * 100) / 100) + '%</span>';
    }

    function dato(etiqueta, valor, extra) {
        return '<div class="dato"><span>' + texto(etiqueta) + '</span><b>' + valor +
               (extra ? ' ' + extra : '') + '</b></div>';
    }

    function porcentaje(p) {
        var x = Number(p);
        if (!isFinite(x)) return '—';
        return Math.round(x * 100) + '%';
    }

    // ---------------------------------------------------------------- la portada

    // EL CARRUSEL, LA FRANJA DE CIFRAS Y EL RENGLON DE ESTADO, que es con lo que abre la app.
    //
    // LOS BANNERS LLEGAN ELEGIDOS. Esta funcion no decide cual senal se muestra ni en que orden:
    // eso lo hace `LaPortadaDeLaCara` del lado de la superficie, porque son reglas del producto
    // (una novedad vieja no es noticia, una promo de algo que ella no compra no se fuerza). Aca
    // solo se dibuja lo que vino.
    function pintarPortada(c) {
        var cifras = $('cifras');
        var pista = $('lgHomeCarrusel');
        var puntos = $('carrusel-puntos');
        var envoltorio = $('carrusel-envoltorio');

        if (!c || c.hayDatos === false) {
            cifras.textContent = I.texto('portada.sinCompras');
            return;
        }

        var cuantas = I.con(plural(c.cantidad, 'portada.compraTuya', 'portada.comprasTuyas'),
                            { cuantas: entero(c.cantidad) });

        cifras.textContent = c.desdeCuando
            ? I.con('portada.cifrasDesde',
                    { compras: cuantas, desde: mesDeAnio(c.desdeCuando), total: colones(c.total) })
            : I.con('portada.cifras', { compras: cuantas, total: colones(c.total) });

        $('estado-cambio').textContent = c.cambios > 0
            ? I.con(plural(c.cambios, 'estado.cambioReciente', 'estado.cambiosRecientes'),
                    { cuantos: c.cambios })
            : I.texto('estado.sinCambios');
        $('estado-cambio').className = 'lg-home-estado-cambio' +
            (c.cambios > 0 ? '' : ' lg-home-estado-cambio--calmo');

        var banners = c.banners || [];
        if (banners.length === 0) { envoltorio.hidden = true; return; }

        envoltorio.hidden = false;
        pista.innerHTML = banners.map(function (b) {
            var precio = (b.precioAntes !== null && b.precioAntes !== undefined &&
                          b.precioAhora !== null && b.precioAhora !== undefined)
                ? '<span class="lg-home-banner-precio">' +
                      '<span class="lg-home-banner-precio-antes">' + colones(b.precioAntes) + '</span>' +
                      '<span aria-hidden="true">&rarr;</span>' +
                      '<span class="lg-home-banner-precio-ahora">' + colones(b.precioAhora) + '</span>' +
                  '</span>'
                : (b.secundario ? '<p class="lg-home-banner-secundario">' + texto(b.secundario) + '</p>' : '');

            // EL CTA NO ES UN ENLACE. En la app lleva a `producto/<nombre>`, que es una ruta; esta
            // cara no tiene router, asi que se dibuja igual -- la marca lo pide -- pero sin href.
            return '<div class="lg-home-banner lg-home-banner--' + texto(b.variante) +
                        (b.foto ? '' : ' lg-home-banner--sin-imagen') + '">' +
                   '<div class="lg-home-banner-texto">' +
                       (b.kicker ? '<span class="lg-home-banner-kicker">' + texto(b.kicker) + '</span>' : '') +
                       '<h1>' + texto(b.titulo) + '</h1>' +
                       precio +
                       (b.cta ? '<span class="lg-home-banner-cta">' + texto(b.cta) + ' &rarr;</span>' : '') +
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
            ? I.texto('carrito.unaCategoria')
            : I.con('carrito.variasCategorias', { cuantas: top.length });

        function grow(p) { return Math.max(1, Math.round(Number(p || 0) * 1000)); }

        caja.hidden = false;
        caja.querySelector('.card-body').innerHTML =
            '<div class="mk-carrito-cabeza">' +
                '<div class="mk-carrito-titulos">' +
                    '<h2 class="mk-carrito-titulo">' + texto(I.texto('carrito.titulo')) + '</h2>' +
                    '<p class="mk-carrito-periodo">' +
                        frase('carrito.periodo', { mes: texto(mesLargo(k.mes)), anio: texto(k.anio),
                                                   monto: colones(k.gastoTotal) }) + '</p>' +
                '</div>' +
                '<img class="mk-carrito-ilustracion" src="assets/hero-canasta-3d.png" alt="" aria-hidden="true" />' +
            '</div>' +
            '<p class="mk-carrito-destacado">' +
                frase('carrito.representan', { cuantas: cuantas, porcentaje: porcentaje(propTop) }) + '</p>' +
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
                          '<span class="mk-carrito-dato-nombre">' + texto(I.texto('carrito.otros')) + '</span>' +
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
                    '<span class="mk-oportunidad-kicker">' + texto(I.texto('habitual.kicker')) + '</span>' +
                    '<span class="mk-oportunidad-badge">' + texto(I.texto('habitual.badge')) + '</span>' +
                '</div>' +
                '<strong class="mk-oportunidad-nombre">' + texto(h.descripcion) + '</strong>' +
                '<div class="mk-oportunidad-precio"><span>' + texto(I.texto('habitual.ahora')) +
                    '</span><strong>' + colones(h.ahora) + '</strong></div>' +
                '<div class="mk-oportunidad-ahorro">' +
                    frase('habitual.ahorro', {
                        antes: '<span class="precio-antes">' + colones(h.antes) + '</span>',
                        ahorro: colones(h.ahorro) }) +
                '</div>' +
            '</div>';
    }

    // UN CAMBIO EN TU RUTINA: grid de dos columnas, foto fija y contenido. Si el producto no tiene
    // foto el <img> no se dibuja y la columna queda vacia -- el texto no se corre a ocuparla
    // porque su grid-column esta fijo en 2.
    function pintarRutina(r) {
        var caja = $('rutina');
        if (!r) { caja.hidden = true; return; }

        caja.hidden = false;
        caja.innerHTML =
            (r.foto ? '<img class="mk-cambio-foto" src="' + texto(r.foto) + '" alt="" />' : '') +
            '<span class="mk-cambio-que">' +
                '<span class="mk-cambio-nombre">' +
                    '<strong>' + texto(r.descripcion) + '</strong>' +
                    '<span class="mk-cambio-pill">' + texto(I.texto('rutina.pill')) + '</span>' +
                '</span>' +
                '<span class="mk-cambio-linea">' +
                    '<span>' + texto(r.antes) + '</span>' +
                    '<span aria-hidden="true">&rarr;</span>' +
                    '<span>' + texto(r.ahora) + '</span>' +
                '</span>' +
                (r.desde
                    ? '<span class="mk-cambio-desde">' +
                      frase('rutina.desde', { cuando: texto(soloElMes(r.desde)) }) + '</span>'
                    : '') +
            '</span>';
    }

    // ---------------------------------------------------------------- la firma

    // LA FIRMA DE LA SEMANA. Es lo que hace que la pantalla la reconozca a ella: cuantas compras
    // se miraron y cuantos productos vuelven siempre. La frase la escribe el dominio.
    function pintarFirma(c) {
        var caja = cuerpo('firma');
        caja.setAttribute('aria-busy', 'false');

        if (!c || c.hayDatos === false) {
            P.apunte(caja, I.con('firma.sinCompras', { buscar: I.texto('boton.buscar') }));
            return;
        }

        // LA CANTIDAD Y EL TOTAL YA NO VAN ACA: viven arriba, en la franja de «87 compras tuyas
        // desde enero de 2025 · ₡3 217 096», que es donde la app los pone. Repetirlos era lo que
        // hacia que esta tarjeta se leyera como un panel de cifras y no como «tus habituales».
        var f = c.firma || {}, r = c.rango || {};
        caja.innerHTML =
            (f.paraElAsistente ? '<p class="apunte" style="margin:0 0 12px">' + texto(f.paraElAsistente) + '</p>' : '') +
            (f.cuantosHabituales ? dato(I.texto('firma.habituales'), entero(f.cuantosHabituales)) : '') +
            (f.comprasMiradas ? dato(I.texto('firma.miradas'), entero(f.comprasMiradas)) : '') +
            (r.mesesConCompras ? dato(I.texto('firma.meses'), entero(r.mesesConCompras)) : '');
    }

    // ---------------------------------------------------------------- el mes

    function pintarMes(c) {
        var caja = cuerpo('mes');
        caja.setAttribute('aria-busy', 'false');

        if (!c || c.hayDatos === false) {
            P.apunte(caja, I.texto('mes.sinMes'));
            return;
        }

        var m = c.mes || {}, anterior = c.contraElMesAnterior || {}, anio = c.contraElAnioPasado || {};

        // EL MES LO ELIGE EL DOMINIO -- el ultimo con compras -- y no esta pantalla. Pedirle a la
        // persona que elija un mes seria pedirle que sepa cual tiene datos.
        caja.innerHTML =
            '<p class="mk-titulo" style="margin:0 0 10px">' +
                frase('comun.mesDeAnio', { mes: texto(mesLargo(m.mes)), anio: texto(m.anio) }) + '</p>' +
            dato(I.texto('mes.gasto'), colones(m.gastoTotal)) +
            dato(I.texto('mes.comida'), colones(m.gastoComestible)) +
            dato(I.texto('mes.resto'), colones(m.gastoNoComestible)) +
            dato(I.texto('mes.compras'), entero(m.cantidadDeCompras)) +
            (anterior.hayComparacion
                ? dato(I.texto('mes.contraAnterior'), delta(anterior.cambio),
                       '<span class="apunte" style="font-weight:400"> · ' + colones(anterior.gastoAntes) + '</span>')
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

        if (r.esObservado && r.diasEntreCompras)
            html += '<p class="apunte" style="margin:0 0 12px">' +
                    frase('movimientos.ritmo', {
                        cada: entero(r.diasEntreCompras),
                        dias: texto(I.texto(plural(r.diasEntreCompras, 'comun.dia', 'comun.dias'))),
                        miradas: entero(r.comprasMiradas) }) + '</p>';

        if (precios.length) {
            html += '<p class="mk-titulo" style="margin:4px 0 6px">' +
                    texto(I.texto('movimientos.precios')) + '</p>';
            precios.slice(0, 8).forEach(function (p) {
                html += dato(p.descripcion,
                    colones(p.ahora),
                    '<span class="apunte" style="font-weight:400">' +
                    frase('movimientos.antes', { antes: colones(p.antes) }) + '</span>');
            });
        }

        if (ritmos.length) {
            html += '<p class="mk-titulo" style="margin:14px 0 6px">' +
                    texto(I.texto('movimientos.ritmos')) + '</p>';
            ritmos.slice(0, 8).forEach(function (x) {
                html += '<div class="dato"><span>' + texto(x.descripcion) + '</span><b>' +
                        texto(x.ahora) + '<span class="apunte" style="font-weight:400">' +
                        frase('movimientos.antes', { antes: texto(x.antes) }) + '</span></b></div>';
            });
        }

        if (!html) html = '<p class="apunte">' + texto(I.texto('movimientos.nada')) + '</p>';
        caja.innerHTML = html;
    }

    // ---------------------------------------------------------------- lo que viene

    function pintarProxima(p) {
        var caja = cuerpo('proxima');
        caja.setAttribute('aria-busy', 'false');

        var lista = (p && p.lista) || {}, promos = (p && p.promociones) || [];
        var faltan = (p && p.abastecimientos) || [], garantias = (p && p.garantias) || [];
        var html = '';

        if (lista.existe && lista.cuantosTocan) {
            html += '<p class="mk-titulo" style="margin:0 0 6px">' +
                    texto(I.texto('proxima.lista')) + '</p>';
            if (lista.paraElAsistente)
                html += '<p class="apunte" style="margin:0 0 10px">' + texto(lista.paraElAsistente) + '</p>';
            (lista.productos || []).slice(0, 10).forEach(function (x) {
                html += '<div class="dato"><span>' + texto(x.descripcion || x.titulo) + '</span><b>' +
                        texto(x.cuando || '') + '</b></div>';
            });
        }

        // LO QUE NO ESTA: una categoria que se repone cada tanto y hace demasiado que no entra.
        // La pantalla no puede renderizar una ausencia que nadie le paso -- se la pasa el
        // dominio.
        if (faltan.length) {
            html += '<p class="mk-titulo" style="margin:14px 0 6px">' +
                    texto(I.texto('proxima.faltan')) + '</p>';
            faltan.slice(0, 8).forEach(function (a) {
                html += dato(a.titulo, dias(a.diasDesdeLaUltima),
                    a.esRitmoObservado && a.diasEsperados
                        ? '<span class="apunte" style="font-weight:400">' +
                          frase('proxima.soles', { cada: entero(a.diasEsperados) }) + '</span>'
                        : '');
            });
        }

        // LAS PROMOCIONES, COMO TEXTO Y SIN FOTO. El carrusel de la app suelta vive de imagenes
        // servidas por el super, y esta cara no sale a la red. Lo que queda es lo que el dominio
        // sabe: el gancho y hasta cuando vale.
        if (promos.length) {
            html += '<p class="mk-titulo" style="margin:14px 0 6px">' +
                    texto(I.texto('proxima.promos')) + '</p>';
            promos.slice(0, 6).forEach(function (x) {
                html += '<div class="dato"><span>' + texto(x.gancho) +
                        (x.detalle ? '<br><span class="apunte">' + texto(x.detalle) + '</span>' : '') +
                        '</span><b class="apunte" style="font-weight:600">' +
                        (x.validaHasta
                            ? frase('proxima.hasta', { cuando: texto(x.validaHasta) }) : '') +
                        '</b></div>';
            });
        }

        if (garantias.length) {
            html += '<p class="mk-titulo" style="margin:14px 0 6px">' +
                    texto(I.texto('proxima.garantias')) + '</p>';
            garantias.slice(0, 6).forEach(function (g) {
                html += dato(g.descripcion, fecha(g.fechaDeCompra),
                    g.numeroDeCompra ? '<span class="apunte mono" style="font-weight:400"> · ' +
                        texto(g.numeroDeCompra) + '</span>' : '');
            });
        }

        if (!html) html = '<p class="apunte">' + texto(I.texto('proxima.nada')) + '</p>';
        caja.innerHTML = html;
    }

    // ---------------------------------------------------------------- las compras

    function pintarCompras(r) {
        var caja = cuerpo('compras');
        caja.setAttribute('aria-busy', 'false');

        var filas = (r && (r.compras || r.lista)) || (Array.isArray(r) ? r : []);
        if (!filas.length) {
            P.apunte(caja, I.texto('compras.ninguna'));
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

    function pintarTodo() {
        // LA PORTADA PRIMERO, y en una sola vuelta: el carrusel, la franja de cifras, el renglon
        // de estado, la rutina, el carrito y el habitual salen todos de la MISMA lectura. Partirlo
        // en seis pedidos serian seis saltos del puente para pintar una pantalla.
        // EL IDIOMA VIAJA CON CADA PREGUNTA. La superficie no tiene navegador -- corre en proceso
        // adentro de Gnothi -- y esta cara si lo es, y ya negocio con el aparato; asi que se lo
        // dice. Sin esto la pantalla quedaba en ingles con las frases del dominio en espanol.
        var lengua = { idioma: I.etiqueta };

        P.pedir('portada', lengua).then(pintarPortada)
            .catch(function (e) { $('cifras').textContent = e.message; });

        P.pedir('cabecera', lengua).then(pintarFirma)
            .catch(function (e) { P.apunte(cuerpo('firma'), e.message, true); });

        P.pedir('canasta', lengua).then(pintarMes)
            .catch(function (e) { P.apunte(cuerpo('mes'), e.message, true); });

        P.pedir('movimientos', lengua).then(pintarMovimientos)
            .catch(function (e) { P.apunte(cuerpo('movimientos'), e.message, true); });

        P.pedir('proxima', lengua).then(pintarProxima)
            .catch(function (e) { P.apunte(cuerpo('proxima'), e.message, true); });

        P.pedir('compras', lengua).then(pintarCompras)
            .catch(function (e) { P.apunte(cuerpo('compras'), e.message, true); });
    }

    function releer() {
        var b = $('releer');
        var chico = $('refrescar-chico');
        b.disabled = true;
        chico.disabled = true;
        b.setAttribute('aria-busy', 'true');
        b.textContent = I.texto('boton.buscando');

        P.refrescar(contar).then(function (r) {
            if (r) pintarTodo();
            b.disabled = false;
            chico.disabled = false;
            b.setAttribute('aria-busy', 'false');
            b.textContent = I.texto('boton.buscar');
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
