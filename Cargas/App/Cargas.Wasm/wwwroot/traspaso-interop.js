// EL TRASPASO DE LAS ANOTACIONES A GNOTHI, del lado del browser (especificacion 13 §5.2).
//
// El sobre lo arma C# (Cargas.App.Traspaso); aca solo se decide COMO VIAJA. En Cargas no
// cabe en el enlace del handoff: medido con el guion de 28 cargas, el sobre pesa ~9.7 KB y
// escapado para una URL ~21 KB. Asi que va por la HOJA DE COMPARTIR como archivo, que es la
// segunda puerta que Gnothi atiende (ACTION_SEND en Android; Share Extension en iOS,
// todavia por medir).
//
// TEXT/PLAIN Y NO JSON, y no es una eleccion: la lista de tipos que Web Share permite es
// cerrada y no incluye application/json ni .json. El nombre .txt es envoltorio; lo que
// importa es el contenido, que Gnothi lee entero y le entrega al modulo.
//
// EXIGE GESTO: navigator.share solo funciona dentro de la activacion transitoria de un toque.
// Llamarlo desde el click de la tecla, nunca al inicializar.
//
// DONDE NO HAY HOJA DE COMPARTIR CON ARCHIVOS -desktop, o un browser viejo- se degrada a la
// descarga del mismo archivo: la persona se lo lleva y lo abre desde Gnothi. Se dice cual de
// las dos paso, porque la pantalla tiene que poder contarlo.

export function esAndroid() {
    return /Android/i.test(navigator.userAgent);
}

function archivoDe(nombre, contenido) {
    return new File([contenido], nombre, { type: 'text/plain' });
}

export function puedeCompartirArchivos() {
    try {
        if (typeof navigator.share !== 'function' || typeof navigator.canShare !== 'function') return false;
        return navigator.canShare({ files: [archivoDe('prueba.txt', 'prueba')] });
    } catch (e) {
        return false;
    }
}

// Devuelve { ok, como: 'compartido' | 'descargado' | '', motivo }. NO tira: el sobre es de
// la persona y un fallo del share sheet tiene que decirse, no reventar la pantalla.
export async function compartir(nombre, contenido, titulo) {
    const archivo = archivoDe(nombre, contenido);

    if (puedeCompartirArchivos()) {
        try {
            await navigator.share({ files: [archivo], title: titulo });
            return { ok: true, como: 'compartido', motivo: '' };
        } catch (e) {
            // Cancelar la hoja es una decision de la persona, no un error.
            if (e.name === 'AbortError') return { ok: false, como: '', motivo: 'cancelado' };
            return { ok: false, como: '', motivo: `${e.name}: ${e.message}` };
        }
    }

    try {
        const url = URL.createObjectURL(archivo);
        const ancla = document.createElement('a');
        ancla.href = url;
        ancla.download = nombre;
        document.body.appendChild(ancla);
        ancla.click();
        ancla.remove();
        URL.revokeObjectURL(url);
        return { ok: true, como: 'descargado', motivo: '' };
    } catch (e) {
        return { ok: false, como: '', motivo: `${e.name}: ${e.message}` };
    }
}
