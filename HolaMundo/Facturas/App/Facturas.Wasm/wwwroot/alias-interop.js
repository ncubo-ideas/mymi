// EL ALIAS PERSONAL DE UNA FACTURA (p.ej. "Birthday party" para TQ-0003), fuera del
// journal -mismo criterio que credencial-interop.js: esto no es un hecho de la vida
// de la persona emitido por el proveedor, es una etiqueta que la persona le pone a su
// propio historial ACA, en este dispositivo. localStorage alcanza: es un objeto chico
// {numero: alias}, tan privado del origen como IndexedDB.
//
// Lo que esto NO es: durable ni sincronizado. Si el browser desaloja el storage del
// origen o la persona borra los datos del sitio, los alias se pierden -el historial
// real (el journal) no se entera ni se ve afectado-.

const CLAVE = 'alias-de-facturas';

export function leerAlias() {
    try {
        const cruda = window.localStorage.getItem(CLAVE);
        return cruda ? JSON.parse(cruda) : {};
    } catch (e) {
        // Storage bloqueado (modo privado estricto) o JSON corrupto: se trata como "no hay".
        return {};
    }
}

// Devuelve false si el browser no dejo guardar. El alias sigue valiendo en memoria
// durante esta sesion; al recargar se perderia el que no se pudo respaldar.
export function guardarAlias(todos) {
    try {
        window.localStorage.setItem(CLAVE, JSON.stringify(todos || {}));
        return true;
    } catch (e) {
        return false;
    }
}
