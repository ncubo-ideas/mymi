// LOS PUNTITOS DEL CARRUSEL DE INICIO se marcan mirando cuanto se desplazo el carrusel, no
// con una libreria: el proyecto no tenia ninguna cargada para esto (bootstrap solo trae su
// CSS, sin bootstrap.bundle.js), y sumar una dependencia pesada para tres puntos no valia
// la pena. Vive AFUERA de Blazor a proposito, igual que barra-compacta.js: es puro efecto
// visual sobre el DOM -que punto esta "activo"-, ninguna decision de datos ni de estado de
// la app.
//
// EL SCROLL SE ESCUCHA EN document CON CAPTURA (el 3er argumento `true`), no en el
// carrusel directo: Blazor vuelve a pintar el hero -y reemplaza el <div id="lgHomeCarrusel">
// entero- cada vez que Releer() corre (cambia la cantidad de banners con nuevos datos), asi
// que una referencia guardada al elemento dejaria de recibir eventos tarde o temprano. El
// evento "scroll" de un div no burbujea, pero SI se puede capturar en la fase de captura
// desde un ancestro -por eso `true`- y de ahi se filtra por id.
(function () {
    function actualizarPuntos(carrusel) {
        var envoltorio = carrusel.closest('.lg-home-carrusel-envoltorio');
        if (!envoltorio) return;
        var puntos = envoltorio.querySelectorAll('.lg-home-punto');
        if (!puntos.length || !carrusel.clientWidth) return;

        var indice = Math.round(carrusel.scrollLeft / carrusel.clientWidth);
        for (var i = 0; i < puntos.length; i++) {
            puntos[i].classList.toggle('activo', i === indice);
        }
    }

    function marcarActivoInicial() {
        var carrusel = document.getElementById('lgHomeCarrusel');
        if (carrusel) actualizarPuntos(carrusel);
    }

    document.addEventListener('scroll', function (evento) {
        var carrusel = evento.target;
        if (carrusel && carrusel.id === 'lgHomeCarrusel') actualizarPuntos(carrusel);
    }, true);

    window.addEventListener('load', marcarActivoInicial);
    document.addEventListener('DOMContentLoaded', marcarActivoInicial);
})();
