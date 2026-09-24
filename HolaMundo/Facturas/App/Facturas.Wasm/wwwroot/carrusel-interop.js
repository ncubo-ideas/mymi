// El carrusel de promociones. El swipe es scroll-snap nativo del navegador -CSS puro,
// cero dependencias nuevas-; este modulo hace dos cosas: 1) avisarle a Blazor que punto
// resaltar mirando el scroll (nunca al reves: la pista no se mueve sola por eso, salvo
// el propio autoplay), y 2) avanzar solo cada tantos milisegundos.
//
// EL INDICE SE CALCULA POR offsetLeft, no por "scrollLeft / ancho": con gap entre
// diapositivas esa cuenta se corre de a poco y el punto activo se desincroniza del
// slide real -era justo el bug reportado ("el indicador se queda pegado en el primer
// punto")-. Comparar contra el offsetLeft real de cada hijo es exacto pase lo que pase
// con gaps o anchos desiguales.
//
// UN SOLO INDICE (`estado.activo`), UN SOLO TEMPORIZADOR: el estado vive UNA vez, guardado
// en el propio elemento de la pista. Swipe manual, click en un punto y autoplay son tres
// maneras de LLEGAR al mismo cambio de indice, no tres estados distintos -y los tres
// reinician el mismo reloj, asi que nunca hay dos setInterval vivos ni el autoplay
// "peleando" con un gesto recien hecho.

export function inicializar(pista, referenciaNet, autoplayMs) {
    const prefiereMenosMovimiento =
        window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    const estado = {
        activo: indiceMasCercano(pista),
        temporizador: null,
        pendienteDeScroll: false,
    };
    pista._triunfoCarrusel = estado;

    function avisar(indice) {
        if (indice === estado.activo) return;
        estado.activo = indice;
        referenciaNet.invokeMethodAsync('ActualizarDiapositivaActiva', indice);
    }

    function reiniciarAutoplay() {
        if (estado.temporizador) clearInterval(estado.temporizador);
        const total = pista.children.length;
        if (total <= 1 || prefiereMenosMovimiento) return;

        estado.temporizador = setInterval(() => {
            const siguiente = (estado.activo + 1) % total;
            moverA(pista, siguiente);
            avisar(siguiente);
        }, autoplayMs || 4500);
    }

    pista.addEventListener('scroll', () => {
        if (estado.pendienteDeScroll) return;
        estado.pendienteDeScroll = true;
        requestAnimationFrame(() => {
            avisar(indiceMasCercano(pista));
            estado.pendienteDeScroll = false;
        });
    }, { passive: true });

    // Cualquier gesto que arranca en la pista (swipe, o el mousedown de un drag en
    // desktop) reinicia el reloj: el proximo avance automatico es una espera completa
    // desde la ULTIMA interaccion de la persona, no desde que se abrio la pagina.
    pista.addEventListener('pointerdown', reiniciarAutoplay, { passive: true });

    estado.avisar = avisar;
    estado.reiniciarAutoplay = reiniciarAutoplay;
    reiniciarAutoplay();
}

export function destruir(pista) {
    const estado = pista && pista._triunfoCarrusel;
    if (!estado) return;
    if (estado.temporizador) clearInterval(estado.temporizador);
    delete pista._triunfoCarrusel;
}

// Llamado desde afuera (click en un punto) Y desde adentro (autoplay). Mueve la pista y,
// si hay estado inicializado, avisa al instante (no espera a que el scroll "se asiente")
// y reinicia el reloj -tocar un punto cuenta como interaccion manual, igual que un swipe.
export function irA(pista, indice) {
    moverA(pista, indice);
    const estado = pista && pista._triunfoCarrusel;
    if (estado) {
        estado.avisar(indice);
        estado.reiniciarAutoplay();
    }
}

function moverA(pista, indice) {
    const objetivo = pista.children[indice];
    if (!objetivo) return;
    pista.scrollTo({ left: objetivo.offsetLeft, behavior: 'smooth' });
}

function indiceMasCercano(pista) {
    let mejor = 0;
    let mejorDistancia = Infinity;
    for (let i = 0; i < pista.children.length; i++) {
        const distancia = Math.abs(pista.children[i].offsetLeft - pista.scrollLeft);
        if (distancia < mejorDistancia) {
            mejorDistancia = distancia;
            mejor = i;
        }
    }
    return mejor;
}
