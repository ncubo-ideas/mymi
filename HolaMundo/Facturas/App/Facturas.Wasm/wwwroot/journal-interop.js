// El puente entre el journal en MEMFS y una carpeta REAL del usuario.
//
// Reparto de trabajo, a proposito: este modulo solo habla con el browser (el picker,
// IndexedDB, la carpeta). NO toca MEMFS. Los archivos de MEMFS los lee y escribe C#
// con System.IO, que es la misma superficie que usa el motor, y los bytes cruzan en
// base64. Asi el puente no depende de APIs internas del runtime .NET, que cambian
// entre versiones.
//
// El mismatch de fondo (briefing 5.1): el storage de Puppeteer es SINCRONO y las APIs
// de archivos del browser son ASINCRONAS, y en wasm single-thread no se puede bloquear
// esperando una promesa. La salida es cargar sincrono / persistir asincrono: al
// arrancar se precarga MEMFS ANTES de tocar el motor, y despues de cada comando se
// baja a la carpeta con fire-and-forget.

const BASE_DE_DATOS = 'app1-facturas';
const ALMACEN = 'handles';
const CLAVE_DE_CARPETA = 'carpeta-del-journal';

let carpeta = null;   // FileSystemDirectoryHandle

// ---------------------------------------------------------------- soporte

export function estaSoportado() {
    return typeof window.showDirectoryPicker === 'function';
}

export function nombreDeLaCarpeta() {
    return carpeta ? carpeta.name : '';
}

// ---------------------------------------------------------------- IndexedDB

// El handle se guarda para no volver a preguntar la carpeta en cada arranque. Es un
// objeto estructurado-clonable, asi que IndexedDB lo persiste tal cual; localStorage
// no serviria porque solo guarda strings.
function abrirBase() {
    return new Promise((resolver, rechazar) => {
        const pedido = indexedDB.open(BASE_DE_DATOS, 1);
        pedido.onupgradeneeded = () => pedido.result.createObjectStore(ALMACEN);
        pedido.onsuccess = () => resolver(pedido.result);
        pedido.onerror = () => rechazar(pedido.error);
    });
}

function enTransaccion(modo, accion) {
    return abrirBase().then(base => new Promise((resolver, rechazar) => {
        const transaccion = base.transaction(ALMACEN, modo);
        const pedido = accion(transaccion.objectStore(ALMACEN));
        pedido.onsuccess = () => resolver(pedido.result);
        pedido.onerror = () => rechazar(pedido.error);
    }));
}

// ---------------------------------------------------------------- permiso

// EXIGE GESTO DE USUARIO. No se puede llamar al cargar la pagina: el browser lo
// rechaza si no viene de un click.
export async function elegirCarpeta() {
    if (!estaSoportado()) return { ok: false, motivo: 'este browser no tiene showDirectoryPicker' };

    try {
        carpeta = await window.showDirectoryPicker({ mode: 'readwrite', id: 'app1-journal' });
        await enTransaccion('readwrite', almacen => almacen.put(carpeta, CLAVE_DE_CARPETA));
        return { ok: true, nombre: carpeta.name, permiso: 'granted' };
    } catch (e) {
        // AbortError = el usuario cerro el dialogo. No es un error del programa.
        return { ok: false, motivo: e.name === 'AbortError' ? 'el usuario cancelo' : `${e.name}: ${e.message}` };
    }
}

// Tras recargar hay que REVALIDAR el permiso. queryPermission no pide gesto; si
// devuelve 'prompt' hay que llamar a pedirPermiso() desde un click.
export async function recuperarCarpeta() {
    if (!estaSoportado()) return { ok: false, motivo: 'este browser no tiene showDirectoryPicker' };

    try {
        const guardada = await enTransaccion('readonly', almacen => almacen.get(CLAVE_DE_CARPETA));
        if (!guardada) return { ok: false, motivo: 'no hay carpeta guardada todavia' };

        carpeta = guardada;
        const permiso = await carpeta.queryPermission({ mode: 'readwrite' });
        return { ok: permiso === 'granted', nombre: carpeta.name, permiso };
    } catch (e) {
        return { ok: false, motivo: `${e.name}: ${e.message}` };
    }
}

// EXIGE GESTO DE USUARIO, igual que el picker.
export async function pedirPermiso() {
    if (!carpeta) return { ok: false, motivo: 'no hay carpeta recuperada' };

    try {
        const permiso = await carpeta.requestPermission({ mode: 'readwrite' });
        return { ok: permiso === 'granted', nombre: carpeta.name, permiso };
    } catch (e) {
        return { ok: false, motivo: `${e.name}: ${e.message}` };
    }
}

// El permiso vigente, sin pedir nada. Devuelve 'granted', 'prompt', 'denied' o
// 'sin-carpeta'. El browser puede degradar un 'granted' a 'prompt' al reabrir la app,
// y ahi las escrituras empiezan a fallar aunque todo lo demas parezca normal.
export async function permisoActual() {
    if (!carpeta) return 'sin-carpeta';
    try {
        return await carpeta.queryPermission({ mode: 'readwrite' });
    } catch (e) {
        return `error: ${e.name}`;
    }
}

// Se llama justo cuando algo ya salio mal (handle colgando), asi que no puede tirar a su
// vez y tapar el error original.
export async function olvidarCarpeta() {
    carpeta = null;
    try {
        await enTransaccion('readwrite', almacen => almacen.delete(CLAVE_DE_CARPETA));
        return { ok: true, motivo: '' };
    } catch (e) {
        return { ok: false, motivo: `${e.name}: ${e.message}` };
    }
}

// ---------------------------------------------------------------- la carpeta

// Rutas relativas con '/' siempre, para que el lado C# las combine con el path de
// MEMFS sin traducir separadores.
async function recorrer(directorio, prefijo, salida) {
    for await (const [nombre, handle] of directorio.entries()) {
        const ruta = prefijo ? `${prefijo}/${nombre}` : nombre;
        if (handle.kind === 'directory') await recorrer(handle, ruta, salida);
        else salida.push(ruta);
    }
}

// Devuelve { ok, rutas, motivo }. NO tira.
//
// El handle guardado en IndexedDB puede quedar COLGANDO: si la persona movio, renombro o
// borro la carpeta, el handle sigue en la base pero ya no resuelve, y recorrerlo tira
// NotFoundError. Antes eso salia crudo como una JSException en la cara del usuario; es
// una situacion normal de la vida de una carpeta y la app tiene que saber decirla.
export async function listar() {
    if (!carpeta) return { ok: false, rutas: [], motivo: 'no hay carpeta' };

    try {
        const salida = [];
        await recorrer(carpeta, '', salida);
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
    partes.pop();                       // el ultimo es el archivo
    let actual = carpeta;
    for (const parte of partes) actual = await actual.getDirectoryHandle(parte, { create: crear });
    return actual;
}

// Devuelve { ok, contenido, motivo }. Antes devolvia null al fallar y el lado C# lo
// SALTABA en silencio: un archivo del journal que no se pudiera leer daba una
// rehidratacion incompleta, o sea un total equivocado sin ningun aviso. Un journal a
// medias es peor que no abrir.
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
    try {
        const directorio = await directorioDe(ruta, true);
        const handle = await directorio.getFileHandle(ruta.split('/').pop(), { create: true });
        const escritor = await handle.createWritable();
        await escritor.write(deBase64(base64));
        await escritor.close();
        return { ok: true };
    } catch (e) {
        return { ok: false, motivo: `${e.name}: ${e.message}` };
    }
}

// ---------------------------------------------------------------- base64

// Se hace por bloques: String.fromCharCode con un spread de decenas de miles de bytes
// desborda la pila de argumentos.
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
