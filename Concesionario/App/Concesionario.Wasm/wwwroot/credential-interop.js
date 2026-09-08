// LA CREDENCIAL ANTE EL CONCESIONARIO, fuera del journal.
//
// Device key, access token y refresh token son contexto de sesion de ESTA instalacion, no
// hechos de la vida de la persona: no van al journal -donde quedaban en claro y viajaban
// con cualquier copia del expediente- sino al storage del browser del origen. localStorage
// alcanza: son tres strings, y es tan privado del origen como IndexedDB.
//
// Lo que esto NO es: durable. Si el browser desaloja el storage del origen o la persona
// borra los datos del sitio, la credencial se pierde y la app simplemente pide otra contra
// un device key nuevo -exactamente lo que ya hacia ante un 401-. El journal no se entera.

const KEY = 'credencial-del-concesionario';

const EMPTY = () => ({ deviceKey: '', token: '', refresh: '' });

export function readCredential() {
    try {
        const raw = window.localStorage.getItem(KEY);
        if (!raw) return EMPTY();
        const c = JSON.parse(raw);
        return { deviceKey: c.deviceKey || '', token: c.token || '', refresh: c.refresh || '' };
    } catch (e) {
        // Storage bloqueado (modo privado estricto) o JSON corrupto: se trata como "no hay".
        return EMPTY();
    }
}

// Devuelve false si el browser no dejo guardar. La app sigue con la credencial en memoria
// durante esta sesion y lo dice en pantalla; en la proxima apertura pedira otra.
export function storeCredential(credential) {
    try {
        window.localStorage.setItem(KEY, JSON.stringify({
            deviceKey: credential.deviceKey || '',
            token: credential.token || '',
            refresh: credential.refresh || '',
        }));
        return true;
    } catch (e) {
        return false;
    }
}

export function clearCredential() {
    try {
        window.localStorage.removeItem(KEY);
        return true;
    } catch (e) {
        return false;
    }
}
