// EL PUENTE CON GNOTHI, del lado del browser. Dos direcciones y dos mecanismos:
//
//   modulo -> Gnothi    window.gnothiPuente, que el nativo INYECTA en el WebView.
//   Gnothi -> modulo    window.gnothi, que se publica aca y llama a C# por DotNet.
//
// POR QUE CALLBACK Y NO RETORNO DIRECTO. En Android los metodos de un objeto inyectado con
// @JavascriptInterface son SINCRONOS: la pagina se queda dura hasta que vuelven. Para un
// pull -que hace HTTPS- eso congelaria la UI del modulo. Asi que el puente no devuelve
// nada: recibe un numero de pedido, contesta cuando puede llamando a
// window.gnothi.respuesta(id, json, error), y de este lado eso se envuelve en una promesa.
// La misma forma sirve en iOS, donde WKScriptMessageHandler tampoco puede devolver.

const pendientes = new Map();
let siguiente = 1;

function pedir(verbo, ...args) {
    return new Promise((cumplir, fallar) => {
        if (!window.gnothiPuente) { fallar(new Error('no hay puente: la app no está hospedada')); return; }

        const id = String(siguiente++);
        pendientes.set(id, { cumplir, fallar });
        try {
            window.gnothiPuente[verbo](id, ...args);
        } catch (e) {
            pendientes.delete(id);
            fallar(e);
        }
    });
}

// ---------------------------------------------------------------- modo

// SI HAY GNOTHI DEL OTRO LADO. Se pregunta por la presencia del puente y no por el origen:
// lo que la app necesita saber no es cómo se llama su origen sino si tiene con quién
// hablar. Un origen que diga "hospedado" sin puente no sirve para nada.

export function hospedada() {
    return typeof window.gnothiPuente !== 'undefined' && window.gnothiPuente !== null;
}

// ---------------------------------------------------------------- modulo -> Gnothi

// El cursor lo pone el actor; Gnothi releva y devuelve el sobre tal cual.
export function pull(despuesDe) { return pedir('pull', String(despuesDe)); }

export function listar()  { return pedir('listar').then(JSON.parse); }

export function leer(ruta) { return pedir('leer', ruta).then(JSON.parse); }

export function escribir(ruta, contenidoBase64) {
    return pedir('escribir', ruta, contenidoBase64).then(JSON.parse);
}

// ---------------------------------------------------------------- Gnothi -> modulo

// La superficie que el nativo invoca con evaluateJavascript. Los tres verbos entran a C#
// por DotNet.invokeMethodAsync, que es lo que Blazor expone para metodos [JSInvokable].
//
// Devuelven JSON como texto SIEMPRE, tambien los errores: quien llama es codigo nativo que
// no puede atrapar una excepcion de JavaScript.
const ASSEMBLY = 'Compras.Wasm';

window.gnothi = window.gnothi || {};

window.gnothi.tools = function () {
    return DotNet.invokeMethodAsync(ASSEMBLY, 'Tools');
};

window.gnothi.call = function (tool, argsJson) {
    return DotNet.invokeMethodAsync(ASSEMBLY, 'Call', tool, argsJson || '{}');
};

window.gnothi.deliver = function (sobreJson) {
    return DotNet.invokeMethodAsync(ASSEMBLY, 'Deliver', sobreJson || '');
};

// EL MODULO AVISA QUE YA ESTA. Gnothi no puede adivinarlo mirando la pantalla: el HTML se
// dibuja antes de que el actor termine de hidratarse, y ahi cualquier cosa que Gnothi le
// entregue rebota con "todavia no arranco". Es una notificacion, no un pedido: no espera
// respuesta.
export function listo() {
    if (!window.gnothiPuente) return;

    // Si el puente esta pero NO tiene este verbo, es un desajuste de versiones entre el modulo
    // y Gnothi, y tiene que doler: en silencio, el traspaso del handoff no llega nunca y no hay
    // ninguna pista de por que. Paso una vez.
    if (typeof window.gnothiPuente.listo !== 'function') {
        console.error('el puente de Gnothi no expone listo(): el traspaso del handoff no va a llegar');
        return;
    }

    window.gnothiPuente.listo();
}

window.gnothi.respuesta = function (id, json, error) {
    const p = pendientes.get(String(id));
    if (!p) return;

    pendientes.delete(String(id));
    if (error) p.fallar(new Error(error));
    else p.cumplir(json);
};
