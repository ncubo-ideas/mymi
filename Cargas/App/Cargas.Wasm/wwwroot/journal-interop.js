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

const BASE_DE_DATOS = 'cargas';
const ALMACEN = 'handles';
const CLAVE_DE_CARPETA = 'carpeta-del-journal';
const CLAVE_DE_MODO = 'modo-de-persistencia';

// LOS TRES MODOS DEL BRIEFING (§6.2). "Carpeta real elegida por el usuario" es hoy
// desktop Chromium y nada mas, asi que la app NO puede fallar donde no hay picker:
// degrada a OPFS y ofrece exportar.
//
//   'carpeta'  la carpeta que la persona eligio. El caso estrella: es SUYA y el
//              servidor MCP la lee desde afuera del browser.
//   'opfs'     almacenamiento privado del origen. Persiste igual, pero lo administra
//              el browser: no es una carpeta elegida y NADIE la ve desde afuera,
//              tampoco el MCP. Es el modo degradado, no un reemplazo.
//   'ninguno'  solo memoria. Se pierde al cerrar.
//
// LA PIEZA QUE HACE ESTO BARATO: OPFS devuelve un FileSystemDirectoryHandle, el MISMO
// tipo que el picker. Por eso recorrer/listar/leer/escribir de abajo funcionan sin
// cambios y el orden precarga-MEMFS -> actor -> flush queda intacto: lo unico que
// cambia es a que raiz apunta `carpeta`.
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
// Las dos APIs de storage -el picker y OPFS- exigen CONTEXTO SEGURO: https, o el literal
// localhost. Fuera de eso el browser ni siquiera las define, asi que la deteccion por
// "typeof === function" da el MISMO false para dos causas que se arreglan distinto:
//
//   'navegador'  este browser no las tiene. La salida es abrir la app en otro.
//   'inseguro'   la pagina no es contexto seguro. La salida es entrar por https, y el
//                browser NO tiene la culpa.
//
// MEDIDO: en un iPhone entrando por la IP de la maquina -http://192.168.x.x:8110- OPFS
// figuraba como no soportado. Desde la misma Safari en la Mac, por localhost, si estaba.
// Decir "tu navegador no puede" en ese caso es culpar al equipo equivocado y mandar a la
// persona a buscar otro browser que va a fallar igual.
export function motivoSinAlmacenamiento() {
    if (opfsSoportado() || estaSoportado()) return '';
    return window.isSecureContext ? 'navegador' : 'inseguro';
}

export function esContextoSeguro() {
    return window.isSecureContext === true;
}

// INSTALADA, la app no es "un navegador" para quien la usa: es una ventana con su
// nombre y su icono. Decirle "en este navegador" ahi suena a que esta hablando de otra
// cosa. El almacenamiento es el mismo; lo que cambia es como se llama delante de la
// persona.
//
// Dos formas porque iOS no implementa display-mode: navigator.standalone es lo unico
// que lo reporta ahi.
export function estaInstalada() {
    if (navigator.standalone === true) return true;
    return window.matchMedia('(display-mode: standalone)').matches
        || window.matchMedia('(display-mode: window-controls-overlay)').matches;
}

export function modoActual() {
    return modo;
}

export function nombreDeLaCarpeta() {
    if (modo === 'opfs') return 'almacenamiento de este navegador';
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
        modo = 'carpeta';
        await enTransaccion('readwrite', almacen => almacen.put(carpeta, CLAVE_DE_CARPETA));
        await enTransaccion('readwrite', almacen => almacen.put(modo, CLAVE_DE_MODO));
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

// ---------------------------------------------------------------- OPFS

// El modo degradado. NO exige gesto de usuario ni permiso: el origen ya es dueno de su
// propio almacenamiento, asi que se puede activar al arrancar.
//
// Lo que se gana: el expediente sobrevive al cierre. Lo que NO se gana, y hay que
// decirlo en la UI: no es una carpeta de la persona, y el servidor MCP no la puede leer
// -vive dentro del sandbox del browser-. La mitad "tu IA lo lee" del producto NO
// funciona en este modo.
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

// Que modo quedo elegido la vez pasada. Se consulta al arrancar para no volver a
// preguntar: si la persona ya opto por OPFS, se reengancha sola y sin gesto.
export async function modoRecordado() {
    try {
        return (await enTransaccion('readonly', almacen => almacen.get(CLAVE_DE_MODO))) || 'ninguno';
    } catch (e) {
        return 'ninguno';
    }
}

// ------------------------------------------------------------- preferencias de pantalla

// LAS PREFERENCIAS QUE NO SON HECHOS. Que pestaña quedo abierta y que carro se estaba
// mirando son de ESTE browser, no de la persona: no son hechos de la estacion ni aportes
// sobre el carro, asi que no tienen nada que hacer en el journal. Si estuvieran ahi, el
// servidor MCP las leeria y no significarian nada para el.
//
// Distinto es la UNIDAD en que ella mira su rendimiento: esa SI va al journal, porque
// cambia lo que la IA *dice* y no solo lo que se *ve*. La linea esta ahi.
//
// Va en el mismo store que el handle de la carpeta, con clave propia. El precedente es
// CLAVE_DE_MODO, que ya guarda un string de preferencia ahi mismo desde el primer dia.
const PREFIJO_DE_PREFERENCIA = 'preferencia:';

// Devuelve '' cuando no hay nada guardado, y TAMBIEN cuando IndexedDB falla. Una
// preferencia que no se pudo leer no puede tumbar el arranque: la pantalla cae a su
// default y la persona ni se entera.
export async function preferencia(nombre) {
    try {
        return (await enTransaccion('readonly',
            almacen => almacen.get(PREFIJO_DE_PREFERENCIA + nombre))) || '';
    } catch (e) {
        return '';
    }
}

// Guardar tampoco puede tumbar nada: si no se pudo, la proxima vez se abre en el default.
export async function recordarPreferencia(nombre, valor) {
    try {
        await enTransaccion('readwrite',
            almacen => almacen.put(valor, PREFIJO_DE_PREFERENCIA + nombre));
        return true;
    } catch (e) {
        return false;
    }
}

// El permiso vigente, sin pedir nada. Devuelve 'granted', 'prompt', 'denied' o
// 'sin-carpeta'. El browser puede degradar un 'granted' a 'prompt' al reabrir la app,
// y ahi las escrituras empiezan a fallar aunque todo lo demas parezca normal.
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

// Se llama justo cuando algo ya salio mal (handle colgando), asi que no puede tirar a su
// vez y tapar el error original.
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

// ---------------------------------------------------------------- exportar / importar

// EL TERCER MODO, y el unico que funciona en todos lados: un archivo que la persona
// se lleva. No depende de ninguna API de almacenamiento — es un download y un input
// type=file, que existen desde siempre.
//
// Es la salida cuando el navegador no da carpeta: el expediente igual puede salir del
// browser, ir a otra maquina, o entrar al MCP a mano.
//
// El formato es deliberadamente simple: un JSON con las rutas y sus bytes en base64.
// No es un zip porque un zip sin libreria es trabajo, y el journal son unos pocos
// archivos. Que sea legible ayuda a diagnosticar.
const FORMATO = 'cargas.journal.v1';

export async function exportarJournal() {
    const listado = await listar();
    if (!listado.ok) return { ok: false, motivo: listado.motivo };
    if (listado.rutas.length === 0) return { ok: false, motivo: 'todavia no hay nada que exportar' };

    try {
        const archivos = [];
        for (const ruta of listado.rutas) {
            const leido = await leer(ruta);
            // Un export a medias es peor que ninguno: si un archivo no se puede leer,
            // se aborta en vez de entregar un expediente incompleto que parezca sano.
            if (!leido.ok) return { ok: false, motivo: `no se pudo leer ${ruta}: ${leido.motivo}` };
            archivos.push({ ruta, contenido: leido.contenido });
        }

        const sobre = JSON.stringify({ formato: FORMATO, exportado: new Date().toISOString(), archivos });
        const url = URL.createObjectURL(new Blob([sobre], { type: 'application/json' }));
        const ancla = document.createElement('a');
        ancla.href = url;
        const nombre = `mis-cargas-${new Date().toISOString().slice(0, 10)}.json`;
        ancla.download = nombre;
        document.body.appendChild(ancla);
        ancla.click();
        ancla.remove();
        URL.revokeObjectURL(url);

        return { ok: true, motivo: '', archivos: archivos.length, nombre };
    } catch (e) {
        return { ok: false, motivo: `${e.name}: ${e.message}` };
    }
}

// Abre el selector de archivos y devuelve el contenido del sobre, SIN escribir nada.
// Quien decide que hacer con el es el lado C#: escribirlo exige tener un destino
// activo, y ademas el actor tiene que rearrancar despues.
export async function abrirJournalExportado() {
    return new Promise(resolver => {
        const entrada = document.createElement('input');
        entrada.type = 'file';
        entrada.accept = 'application/json,.json';

        entrada.onchange = async () => {
            const archivo = entrada.files && entrada.files[0];
            if (!archivo) return resolver({ ok: false, motivo: 'no se eligio ningun archivo', archivos: [] });

            try {
                const sobre = JSON.parse(await archivo.text());
                if (sobre.formato !== FORMATO)
                    return resolver({ ok: false, motivo: 'ese archivo no es un expediente exportado por esta app', archivos: [] });

                resolver({ ok: true, motivo: '', archivos: sobre.archivos || [] });
            } catch (e) {
                resolver({ ok: false, motivo: `no se pudo leer el archivo: ${e.name}`, archivos: [] });
            }
        };

        // Cancelar el dialogo no dispara onchange en todos los browsers, asi que la
        // promesa quedaria colgada para siempre. 'cancel' lo cubre donde existe.
        entrada.oncancel = () => resolver({ ok: false, motivo: 'el usuario cancelo', archivos: [] });

        entrada.click();
    });
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
