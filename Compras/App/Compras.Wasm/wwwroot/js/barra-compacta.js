// EL HEADER PASA A COMPACTO EN SCROLL, para "recuperar altura util de pantalla" (Ronda D).
// Vive AFUERA de Blazor a proposito: es puro efecto visual sobre el DOM -ninguna decision
// de datos ni de estado de la app-, y hacerlo en JS plano evita una ida y vuelta con C#
// solo para alternar una clase. Se registra UNA sola vez al cargar la pagina; la barra se
// agrega y se saca del DOM en cada navegacion (no se dibuja en Inicio), asi que la funcion
// busca ".barra" de nuevo en cada scroll en vez de guardar una referencia que dejaria de
// existir apenas la persona entra a Inicio.
(function () {
    var UMBRAL = 28; // px de scroll antes de compactar

    function actualizar() {
        var barra = document.querySelector('.barra');
        if (!barra) return;
        barra.classList.toggle('barra--compacta', window.scrollY > UMBRAL);
    }

    window.addEventListener('scroll', actualizar, { passive: true });
    window.addEventListener('load', actualizar);
    document.addEventListener('DOMContentLoaded', actualizar);
})();
