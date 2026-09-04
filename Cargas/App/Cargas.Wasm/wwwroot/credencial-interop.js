// LA CREDENCIAL ANTE EL PROVEEDOR, fuera del journal.
//
// Device key, access token y refresh token son contexto de sesion de ESTA instalacion, no
// hechos de la vida de la persona: no van al journal -donde quedaban en claro y viajaban
// con cualquier copia o traspaso- sino al storage del browser del origen. localStorage
// alcanza: son tres strings, y es tan privado del origen como IndexedDB.
//
// Lo que esto NO es: durable. Si el browser desaloja el storage del origen o la persona
// borra los datos del sitio, la credencial se pierde y la app simplemente pide otra contra
// un device key nuevo -exactamente lo que ya hacia ante un 401-. El journal no se entera.
//
// Esta app todavia no corre hospedada dentro de Gnothi. Cuando lo haga, este archivo no se
// usara: la credencial la sostendra Gnothi y la PWA no vera ningun token.

const CLAVE = 'credencial-del-proveedor';

const VACIA = () => ({ deviceKey: '', token: '', refresh: '' });

export function leerCredencial() {
    try {
        const cruda = window.localStorage.getItem(CLAVE);
        if (!cruda) return VACIA();
        const c = JSON.parse(cruda);
        return { deviceKey: c.deviceKey || '', token: c.token || '', refresh: c.refresh || '' };
    } catch (e) {
        // Storage bloqueado (modo privado estricto) o JSON corrupto: se trata como "no hay".
        return VACIA();
    }
}

// Devuelve false si el browser no dejo guardar. La app sigue con la credencial en memoria
// durante esta sesion y lo dice en pantalla; en la proxima apertura pedira otra.
export function guardarCredencial(credencial) {
    try {
        window.localStorage.setItem(CLAVE, JSON.stringify({
            deviceKey: credencial.deviceKey || '',
            token: credencial.token || '',
            refresh: credencial.refresh || '',
        }));
        return true;
    } catch (e) {
        return false;
    }
}

export function borrarCredencial() {
    try {
        window.localStorage.removeItem(CLAVE);
        return true;
    } catch (e) {
        return false;
    }
}
