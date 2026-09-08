// Si la pantalla es angosta, para las decisiones que el CSS NO puede tomar solo.
//
// EL CSS ALCANZA PARA CASI TODO -la barra que se va al pie, la tabla que se apila- porque
// esas son formas distintas del MISMO contenido. Lo que no puede es cambiar QUE FILAS
// EXISTEN: en escritorio el detalle de una compra pagina de a quince y en telefono muestra
// quince con un boton que abre el resto, y esos son dos subconjuntos distintos. Elegir cual
// se renderiza es de C#, y C# no ve el ancho de la ventana.
//
// LA CONSULTA ES EL MISMO STRING QUE EL @media DEL CSS, a proposito. Si aca dijera 768 y
// alla 767.98 habria una franja de un pixel donde el CSS apila y el codigo pagina — un bug
// que nadie encuentra mirando ninguno de los dos archivos por separado. Copiado tal cual,
// desincronizarlos exige editar los dos.
const CONSULTA = '(max-width: 767.98px)';

// UNA SOLA MediaQueryList, GUARDADA, y esto no es una optimizacion: matchMedia() devuelve un
// OBJETO NUEVO en cada llamada -medido: dos llamadas seguidas con la misma consulta dan
// a !== b-. La primera version hacia
//
//     window.matchMedia(CONSULTA).addEventListener('change', ...)
//
// que crea la lista, le cuelga el listener y no se queda con la referencia. Nadie apunta a
// esa lista, asi que el recolector se la puede llevar y el listener DEJA DE EXISTIR sin un
// error, sin un warning y sin nada que mirar: la app simplemente no se entera nunca mas de
// que la ventana cambio de tamaño. Guardada en el modulo, vive lo que vive la pagina.
const CONSULTADA = window.matchMedia(CONSULTA);

export function esAngosta() {
    return CONSULTADA.matches;
}

// SE ESCUCHA matchMedia Y NO resize: 'change' dispara UNA vez, cuando se CRUZA el corte.
// Con resize habria que comparar el ancho anterior a mano en cada pixel que se arrastra.
export function observar(dotnet) {
    CONSULTADA.addEventListener('change', evento => {
        dotnet.invokeMethodAsync('CambioElAncho', evento.matches);
    });
}

// VOLVER A DONDE SE ESTABA, no a un destino fijo. history.back() conserva de DONDE vino
// la persona -recibos, la portada, infaltables-, que es lo que un boton con destino
// escrito no puede saber. El respaldo cubre la entrada EN FRIO: un enlace compartido o un
// bookmark abren la pagina sin historia, y ahi "volver" significa ir al inicio de la app.
export function volver(respaldo) {
    if (history.length > 1) history.back();
    else location.href = respaldo;
}
