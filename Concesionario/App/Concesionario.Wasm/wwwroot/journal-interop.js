const BASE_DE_DATOS = 'dealership-car';
const ALMACEN = 'handles';
const CLAVE_DE_CARPETA = 'carpeta-del-journal';
const CLAVE_DE_MODO = 'modo-de-persistencia';

let carpeta = null;
let modo = 'ninguno';

export function estaSoportado() {
    return typeof window.showDirectoryPicker === 'function';
}

export function opfsSoportado() {
    return !!(navigator.storage && typeof navigator.storage.getDirectory === 'function');
}

export function motivoSinAlmacenamiento() {
    if (opfsSoportado() || estaSoportado()) return '';
    return window.isSecureContext ? 'navegador' : 'inseguro';
}

export function estaInstalada() {
    if (navigator.standalone === true) return true;
    return window.matchMedia('(display-mode: standalone)').matches
        || window.matchMedia('(display-mode: window-controls-overlay)').matches;
}

export function modoActual() {
    return modo;
}

export function nombreDeLaCarpeta() {
    if (modo === 'opfs') return 'el almacenamiento de este navegador';
    return carpeta ? carpeta.name : '';
}

function abrirBase() {
    return new Promise((resolver, rechazar) => {
        const pedido = indexedDB.open(BASE_DE_DATOS, 1);
        pedido.onupgradeneeded = () => pedido.result.createObjectStore(ALMACEN);
        pedido.onsuccess = () => resolver(pedido.result);
        pedido.onerror = () => rechazar(pedido.error);
    });
}

function enTransaccion(modoDeTransaccion, accion) {
    return abrirBase().then(base => new Promise((resolver, rechazar) => {
        const transaccion = base.transaction(ALMACEN, modoDeTransaccion);
        const pedido = accion(transaccion.objectStore(ALMACEN));
        pedido.onsuccess = () => resolver(pedido.result);
        pedido.onerror = () => rechazar(pedido.error);
    }));
}

export async function elegirCarpeta() {
    if (!estaSoportado()) return { ok: false, motivo: 'este browser no tiene showDirectoryPicker' };

    try {
        carpeta = await window.showDirectoryPicker({ mode: 'readwrite', id: 'dealership-car-journal' });
        modo = 'carpeta';
        await enTransaccion('readwrite', almacen => almacen.put(carpeta, CLAVE_DE_CARPETA));
        await enTransaccion('readwrite', almacen => almacen.put(modo, CLAVE_DE_MODO));

        let permiso = await carpeta.queryPermission({ mode: 'readwrite' });
        if (permiso === 'prompt') permiso = await carpeta.requestPermission({ mode: 'readwrite' });

        return { ok: permiso === 'granted', nombre: carpeta.name, permiso };
    } catch (e) {
        return { ok: false, motivo: e.name === 'AbortError' ? 'la persona cancelo' : `${e.name}: ${e.message}` };
    }
}

export async function recuperarCarpeta() {
    if (!estaSoportado()) return { ok: false, motivo: 'este browser no tiene showDirectoryPicker' };

    try {
        const guardada = await enTransaccion('readonly', almacen => almacen.get(CLAVE_DE_CARPETA));
        if (!guardada) return { ok: false, motivo: 'no hay carpeta guardada todavia' };

        carpeta = guardada;
        modo = 'carpeta';
        const permiso = await carpeta.queryPermission({ mode: 'readwrite' });
        return { ok: permiso === 'granted', nombre: carpeta.name, permiso };
    } catch (e) {
        return { ok: false, motivo: `${e.name}: ${e.message}` };
    }
}

export async function pedirPermiso() {
    if (!carpeta) return { ok: false, motivo: 'no hay carpeta recuperada' };

    try {
        const permiso = await carpeta.requestPermission({ mode: 'readwrite' });
        return { ok: permiso === 'granted', nombre: carpeta.name, permiso };
    } catch (e) {
        return { ok: false, motivo: `${e.name}: ${e.message}` };
    }
}

export async function usarOpfs() {
    if (!opfsSoportado()) return { ok: false, motivo: 'este navegador no tiene almacenamiento persistente' };

    try {
        carpeta = await navigator.storage.getDirectory();
        modo = 'opfs';
        await enTransaccion('readwrite', almacen => almacen.put(modo, CLAVE_DE_MODO));
        return { ok: true, nombre: nombreDeLaCarpeta(), permiso: 'granted' };
    } catch (e) {
        return { ok: false, motivo: `${e.name}: ${e.message}` };
    }
}

export async function modoRecordado() {
    try {
        return (await enTransaccion('readonly', almacen => almacen.get(CLAVE_DE_MODO))) || 'ninguno';
    } catch (e) {
        return 'ninguno';
    }
}

export async function permisoActual() {
    if (modo === 'opfs') return 'granted';
    if (!carpeta) return 'sin-carpeta';
    try {
        return await carpeta.queryPermission({ mode: 'readwrite' });
    } catch (e) {
        return `error: ${e.name}`;
    }
}

export async function olvidarCarpeta() {
    carpeta = null;
    modo = 'ninguno';
    try {
        await enTransaccion('readwrite', almacen => almacen.delete(CLAVE_DE_MODO));
        await enTransaccion('readwrite', almacen => almacen.delete(CLAVE_DE_CARPETA));
        return { ok: true, motivo: '' };
    } catch (e) {
        return { ok: false, motivo: `${e.name}: ${e.message}` };
    }
}

function esBasuraDelBrowser(nombre) {
    return nombre.endsWith('.crswap');
}

async function recorrer(directorio, prefijo, salida) {
    for await (const [nombre, handle] of directorio.entries()) {
        const ruta = prefijo ? `${prefijo}/${nombre}` : nombre;
        if (handle.kind === 'directory') await recorrer(handle, ruta, salida);
        else if (!esBasuraDelBrowser(nombre)) salida.push(ruta);
    }
}

export async function listar() {
    if (!carpeta) return { ok: false, rutas: [], motivo: 'no hay carpeta' };

    try {
        const salida = [];
        await recorrer(carpeta, '', salida);
        salida.sort();
        return { ok: true, rutas: salida, motivo: '' };
    } catch (e) {
        const colgando = e.name === 'NotFoundError';
        return {
            ok: false,
            rutas: [],
            motivo: colgando
                ? 'la carpeta ya no existe: fue movida, renombrada o borrada'
                : `${e.name}: ${e.message}`,
        };
    }
}

async function directorioDe(ruta, crear) {
    const partes = ruta.split('/');
    partes.pop();
    let actual = carpeta;
    for (const parte of partes) actual = await actual.getDirectoryHandle(parte, { create: crear });
    return actual;
}

export async function leer(ruta) {
    if (!carpeta) return { ok: false, contenido: '', motivo: 'no hay carpeta' };
    try {
        const directorio = await directorioDe(ruta, false);
        const archivo = await (await directorio.getFileHandle(ruta.split('/').pop())).getFile();
        return { ok: true, contenido: aBase64(new Uint8Array(await archivo.arrayBuffer())), motivo: '' };
    } catch (e) {
        return { ok: false, contenido: '', motivo: `${e.name}: ${e.message}` };
    }
}

export async function escribir(ruta, base64) {
    if (!carpeta) return { ok: false, motivo: 'no hay carpeta' };

    let ultimo = null;
    for (let intento = 1; intento <= 2; intento++) {
        try {
            const directorio = await directorioDe(ruta, true);
            const handle = await directorio.getFileHandle(ruta.split('/').pop(), { create: true });
            const escritor = await handle.createWritable();
            await escritor.write(deBase64(base64));
            await escritor.close();
            return { ok: true, motivo: '' };
        } catch (e) {
            ultimo = e;
            if (e.name !== 'InvalidStateError') break;
        }
    }
    return {
        ok: false,
        motivo: ultimo.name === 'InvalidStateError'
            ? 'otro proceso escribio en la carpeta mientras esta app guardaba (tipicamente el '
              + 'servidor MCP, que reescribe meta.bin al leer). Se reintento una vez y volvio a pasar.'
            : `${ultimo.name}: ${ultimo.message}`,
    };
}

function aBase64(bytes) {
    let texto = '';
    const bloque = 0x8000;
    for (let i = 0; i < bytes.length; i += bloque)
        texto += String.fromCharCode.apply(null, bytes.subarray(i, i + bloque));
    return btoa(texto);
}

function deBase64(base64) {
    const texto = atob(base64);
    const bytes = new Uint8Array(texto.length);
    for (let i = 0; i < texto.length; i++) bytes[i] = texto.charCodeAt(i);
    return bytes;
}
