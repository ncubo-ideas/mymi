const ENSAMBLADO = 'Municipalidad.Wasm';

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

export function hospedada() {
    return typeof window.gnothiPuente !== 'undefined' && window.gnothiPuente !== null;
}

export function pull(despuesDe) { return pedir('pull', String(despuesDe)); }

export function listar() { return pedir('listar').then(JSON.parse); }

export function leer(ruta) { return pedir('leer', ruta).then(JSON.parse); }

export function escribir(ruta, contenidoBase64) {
    return pedir('escribir', ruta, contenidoBase64).then(JSON.parse);
}

export function listo() {
    if (!window.gnothiPuente) return;

    if (typeof window.gnothiPuente.listo !== 'function') {
        console.error('el puente de Gnothi no expone listo(): el traspaso no va a llegar');
        return;
    }

    window.gnothiPuente.listo();
}

window.gnothi = window.gnothi || {};

window.gnothi.tools = function () {
    return DotNet.invokeMethodAsync(ENSAMBLADO, 'Tools');
};

window.gnothi.call = function (tool, argsJson) {
    return DotNet.invokeMethodAsync(ENSAMBLADO, 'Call', tool, argsJson || '{}');
};

window.gnothi.deliver = function (sobreJson) {
    return DotNet.invokeMethodAsync(ENSAMBLADO, 'DeliverAsync', sobreJson || '');
};

window.gnothi.respuesta = function (id, json, error) {
    const p = pendientes.get(String(id));
    if (!p) return;

    pendientes.delete(String(id));
    if (error) p.fallar(new Error(error));
    else p.cumplir(json);
};
