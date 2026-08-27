// El puente entre el journal en MEMFS y una carpeta REAL de la persona.
//
// Reparto de trabajo, a proposito: este modulo solo habla con el browser (el picker,
// IndexedDB, la carpeta). NO toca MEMFS. Los archivos de MEMFS los lee y escribe C# con
// System.IO, que es la misma superficie que usa el motor, y los bytes cruzan en base64.
// Asi el puente no depende de APIs internas del runtime .NET, que cambian entre versiones.
//
// El mismatch de fondo (ARCHITECTURE §4.1): el storage de Puppeteer es SINCRONO y las APIs
// de archivos del browser son ASINCRONAS, y en wasm single-thread no se puede bloquear
// esperando una promesa. La salida es cargar sincrono / persistir asincrono: al arrancar se
// precarga MEMFS ANTES de tocar el motor, y despues de CADA comando se baja a la carpeta.

const BASE_DE_DATOS = 'dealership-car';
const ALMACEN = 'handles';
const CLAVE_DE_CARPETA = 'carpeta-del-journal';
const CLAVE_DE_MODO = 'modo-de-persistencia';

// DOS DESTINOS, y no son equivalentes:
//
//   'carpeta'  la carpeta que la persona eligio. El caso que justifica la app: es SUYA, y el
//              servidor MCP de T9 la lee desde AFUERA del browser. El journal es el
//              protocolo entre los dos; no hubo que inventar ninguno.
//   'opfs'     almacenamiento privado del origen. Persiste igual, pero vive dentro del
//              sandbox del browser y NADIE lo ve desde afuera, el MCP tampoco. Es el modo
//              degradado -- showDirectoryPicker es hoy desktop Chromium y nada mas -- y la
//              pantalla tiene que decir que la mitad "tu IA lo lee" no funciona ahi.
//   'ninguno'  solo memoria. Se pierde al cerrar.
//
// LO QUE HACE ESTO BARATO: OPFS devuelve un FileSystemDirectoryHandle, el MISMO tipo que el
// picker. Recorrer / listar / leer / escribir no distinguen uno de otro, y el orden
// precarga-MEMFS -> actor -> flush-por-comando queda intacto: lo unico que cambia es a que
// raiz apunta `carpeta`.
let carpeta = null;   // FileSystemDirectoryHandle (del picker o de OPFS)
let modo = 'ninguno';

// ---------------------------------------------------------------- soporte

export function estaSoportado() {
    return typeof window.showDirectoryPicker === 'function';
}

export function opfsSoportado() {
    return !!(navigator.storage && typeof navigator.storage.getDirectory === 'function');
}

// POR QUE no hay almacenamiento, que no es lo mismo que QUE no lo haya.
//
// Las dos APIs -el picker y OPFS- exigen CONTEXTO SEGURO: https, o el literal localhost.
// Fuera de eso el browser ni siquiera las define, asi que detectar por "typeof === function"
// da el MISMO false para dos causas que se arreglan distinto:
//
//   'navegador'  este browser no las tiene. La salida es abrir la app en otro.
//   'inseguro'   la pagina no es contexto seguro. La salida es entrar por https, y el
//                browser NO tiene la culpa.
export function motivoSinAlmacenamiento() {
    if (opfsSoportado() || estaSoportado()) return '';
    return window.isSecureContext ? 'navegador' : 'inseguro';
}

// Si corre como app instalada y no como pestana. No cambia nada del mecanismo: cambia como
// se le habla a la persona. Dos formas porque iOS no implementa display-mode.
export function estaInstalada() {
    if (navigator.standalone === true) return true;
    return window.matchMedia('(display-mode: standalone)').matches
        || window.matchMedia('(display-mode: window-controls-overlay)').matches;
}

export function modoActual() {
    return modo;
}

// EN INGLES, porque esto NO es un identificador: es el nombre que la pantalla imprime, y la
// pantalla es inglesa (decision 6 de T7). El token de modo de arriba si es identificador y se
// queda en espanol -- ademas esta persistido en IndexedDB y renombrarlo romperia el reengache.
export function nombreDeLaCarpeta() {
    if (modo === 'opfs') return "this browser's storage";
    return carpeta ? carpeta.name : '';
}

// ---------------------------------------------------------------- IndexedDB

// El handle se guarda para no volver a preguntar la carpeta en cada arranque. Es un objeto
// estructurado-clonable, asi que IndexedDB lo persiste tal cual; localStorage no serviria
// porque solo guarda strings.
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

// ---------------------------------------------------------------- permiso

// EXIGE GESTO DE USUARIO. No se puede llamar al cargar la pagina: el browser lo rechaza si
// no viene de un click.
export async function elegirCarpeta() {
    if (!estaSoportado()) return { ok: false, motivo: 'este browser no tiene showDirectoryPicker' };

    try {
        carpeta = await window.showDirectoryPicker({ mode: 'readwrite', id: 'dealership-car-journal' });
        modo = 'carpeta';
        await enTransaccion('readwrite', almacen => almacen.put(carpeta, CLAVE_DE_CARPETA));
        await enTransaccion('readwrite', almacen => almacen.put(modo, CLAVE_DE_MODO));

        // ELEGIR LA CARPETA NO ES TENER PERMISO DE ESCRITURA SOBRE ELLA, y devolver 'granted' de
        // taquito era un bug con consecuencias: el dialogo de Chrome ofrece "Ver archivos" y
        // "Editar archivos", y pedir mode:'readwrite' NO garantiza la segunda -- la persona puede
        // dar solo lectura, o descartar el segundo prompt. MEDIDO en una carpeta real: el picker
        // resolvio, esta funcion contesto 'granted', y queryPermission decia 'denied'. La app se
        // creia con destino y descartaba en silencio cada comando.
        //
        // Se pregunta, y si quedo en 'prompt' se pide -- que se puede, porque venimos de un click.
        let permiso = await carpeta.queryPermission({ mode: 'readwrite' });
        if (permiso === 'prompt') permiso = await carpeta.requestPermission({ mode: 'readwrite' });

        return { ok: permiso === 'granted', nombre: carpeta.name, permiso };
    } catch (e) {
        // AbortError = la persona cerro el dialogo. No es un error del programa.
        return { ok: false, motivo: e.name === 'AbortError' ? 'la persona cancelo' : `${e.name}: ${e.message}` };
    }
}

// Tras recargar hay que REVALIDAR el permiso. queryPermission no pide gesto; si devuelve
// 'prompt' hace falta pedirPermiso() desde un click.
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

// El modo degradado. NO exige gesto ni permiso: el origen ya es dueno de su propio
// almacenamiento, asi que se puede activar al arrancar.
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

// Que destino quedo elegido la vez pasada. Se consulta al arrancar para no volver a
// preguntar: si la persona ya opto por OPFS, se reengancha sola y sin gesto.
export async function modoRecordado() {
    try {
        return (await enTransaccion('readonly', almacen => almacen.get(CLAVE_DE_MODO))) || 'ninguno';
    } catch (e) {
        return 'ninguno';
    }
}

// El permiso vigente, sin pedir nada. Devuelve 'granted', 'prompt', 'denied' o 'sin-carpeta'.
// El browser puede degradar un 'granted' a 'prompt' al reabrir la app, y ahi las escrituras
// empiezan a fallar aunque todo lo demas parezca normal.
export async function permisoActual() {
    // OPFS no tiene permiso que revalidar: es del origen y no caduca al reabrir.
    if (modo === 'opfs') return 'granted';
    if (!carpeta) return 'sin-carpeta';
    try {
        return await carpeta.queryPermission({ mode: 'readwrite' });
    } catch (e) {
        return `error: ${e.name}`;
    }
}

// Se llama justo cuando algo ya salio mal (handle colgando), asi que no puede tirar a su vez
// y tapar el error original.
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

// ---------------------------------------------------------------- la carpeta

// Rutas relativas con '/' siempre, para que el lado C# las combine con el path de MEMFS sin
// traducir separadores.
// EL .crswap NO ES PARTE DEL JOURNAL Y NO SE PRECARGA. El File System Access API escribe a
// traves de un archivo de intercambio -- <nombre>.crswap -- y lo renombra al cerrar; si la
// escritura se interrumpe, el intercambio queda huerfano en la carpeta de la persona. MEDIDO:
// una colision con el servidor MCP dejo un meta.bin.crswap, y sin este filtro la precarga lo
// copiaba a MEMFS como si fuera un archivo del actor y lo contaba en el total.
//
// Se filtra al LISTAR y no al leer, porque listar es lo que define "que es el journal": la
// precarga, el conteo y el flush salen todos de aca.
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

// Devuelve { ok, rutas, motivo }. NO tira.
//
// El handle guardado en IndexedDB puede quedar COLGANDO: si la persona movio, renombro o
// borro la carpeta, el handle sigue en la base pero ya no resuelve, y recorrerlo tira
// NotFoundError. Es una situacion normal de la vida de una carpeta y la app tiene que saber
// decirla en vez de mostrar una JSException cruda.
export async function listar() {
    if (!carpeta) return { ok: false, rutas: [], motivo: 'no hay carpeta' };

    try {
        const salida = [];
        await recorrer(carpeta, '', salida);
        // ORDENADAS, y no por gusto: el orden de FileSystemDirectoryHandle.entries() no esta
        // especificado, y la precarga tiene que dejar MEMFS igual corrida a corrida para que
        // dos aperturas del mismo journal sean comparables. T9 midio que NTFS ya devuelve
        // ordenado y que esta defensa es justamente para un filesystem que no ordena; el
        // browser es ese filesystem.
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
    partes.pop();                       // el ultimo es el archivo
    let actual = carpeta;
    for (const parte of partes) actual = await actual.getDirectoryHandle(parte, { create: crear });
    return actual;
}

// Devuelve { ok, contenido, motivo }. Nunca null: un archivo del journal que no se pueda
// leer NO se puede saltar en silencio -- la rehidratacion quedaria incompleta y la agenda
// saldria mal sin ningun aviso. Quien decide que hacer con el fallo es el lado C#.
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

// DOS INTENTOS, y el segundo existe por una razon medida: EL LECTOR DE LA IA LE MUEVE LOS
// ARCHIVOS ABAJO DE LOS PIES A ESTE BROWSER.
//
// El servidor MCP de T9 es read-only en cuanto a HECHOS, pero no escribe cero bytes: su Start()
// crea index/index.bin y reescribe meta.bin en cada consulta (CONTRADICTIONS C30). Para el
// File System Access API eso alcanza -- createWritable() sobre un handle cuyo archivo cambio en
// disco desde que se leyo tira
//
//   InvalidStateError: An operation that depends on state cached in an interface object was
//   made but the state had changed since it was read from disk.
//
// MEDIDO sobre la carpeta real: el MCP toco meta.bin e index.bin a las 11:08:41, el flush del
// browser reventó, y quedo un meta.bin.crswap huerfano. T9 habia medido que un lector y un
// escritor conviven -- pero con la CONSOLA, que usa System.IO y no cachea nada; el browser es
// mas estricto y esa medicion no se transfiere.
//
// Reintentar re-adquiere el handle y con eso el estado fresco. No es una carrera que se pueda
// cerrar del lado del browser: los dos procesos son duenos legitimos de esa carpeta.
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
