// GNOTHI'S SIDE OF THE FRAME BRIDGE. A module's face is an <iframe id="cara-<modulo>"> on its own
// origin; what it says arrives here as a message and goes into C# by Blazor's interop, and what
// Gnothi wants run in it goes back as a message the module's shim evaluates. The frame is told
// apart by its window, never by what the message claims; and both directions are pinned to the
// frame's OWN origin (the loopback port FrameWebModule chose), so a frame that navigated itself
// somewhere else neither hears Gnothi nor is heard by it.
window.gnothiFrames = (function () {
  function frameOf(module) { return document.getElementById('cara-' + module); }

  function originOf(frame) {
    try { return new URL(frame.src).origin; } catch (x) { return null; }
  }

  function post(module, data) {
    var f = frameOf(module);
    var origin = f ? originOf(f) : null;
    if (f && f.contentWindow && origin) f.contentWindow.postMessage(data, origin);
  }

  function toFrame(module, js) { post(module, { gnothi: 'js', js: js }); }

  function evalIn(module, id, js) { post(module, { gnothi: 'eval', id: id, js: js }); }

  // ── LA SALA. Un iframe OCULTO Y PERMANENTE, que no es la cara de nadie.
  //
  // Adentro corre el sustrato de Gnothi -- el runtime .NET compilado a wasm -- y los modulos entran
  // ahi como bytes. Va fuera de la vista porque LA SALA NO TIENE PANTALLA: existe para contestar. La
  // cara de la institucion sigue siendo su propio marco, en su propio origen, y se monta solo cuando
  // la persona la abre.
  //
  // MISMA DISCIPLINA QUE LA DE ARRIBA, Y POR EL MISMO MOTIVO: se reconoce por su WINDOW y nunca por
  // lo que el mensaje diga, y las dos direcciones van ancladas a su propio origen. Un documento que
  // navego a otro lado deja de oir y de ser oido.
  var SALA = 'sala-gnothi';

  function salaFrame() { return document.getElementById(SALA); }

  function abrirSala(url) {
    var previo = salaFrame();
    if (previo) previo.remove();

    var f = document.createElement('iframe');
    f.id = SALA;
    f.src = url;
    f.style.display = 'none';
    f.setAttribute('aria-hidden', 'true');
    f.setAttribute('tabindex', '-1');
    document.body.appendChild(f);
  }

  // UN PEDIDO A LA SALA. No viaja como javascript para evaluar -- que es lo que la cara de un modulo
  // necesita y por lo que su CSP pide 'unsafe-eval' -- sino como DATO: el shim llama a
  // `window.sala.pedido` con estos cinco campos. La sala no necesita esa concesion.
  function aLaSala(id, verbo, modulo, a, b) {
    var f = salaFrame();
    var origin = f ? originOf(f) : null;
    if (f && f.contentWindow && origin)
      f.contentWindow.postMessage({ gnothi: 'sala', id: id, verbo: verbo, modulo: modulo, a: a, b: b }, origin);
  }

  // TIRAR EL DOCUMENTO, que es lo unico que recupera una sala que no contesta. Sacar el iframe se
  // lleva puesto lo que hubiera adentro, y con ello la frontera: lo que el modulo tenia montado vive
  // en la memoria de ese documento, no en el disco.
  function tirarSala() {
    var f = salaFrame();
    if (f) f.remove();
  }

  window.addEventListener('message', function (e) {
    if (!e.data || e.data.gnothi !== 'puente') return;

    // La sala primero: es uno solo y no lleva nombre de modulo, porque el modulo del que habla sale
    // del marco de la llamada que el Host abrio y nunca del mensaje.
    var s = salaFrame();
    if (s && s.contentWindow === e.source && originOf(s) === e.origin) {
      DotNet.invokeMethodAsync('Gnothi.Desktop', 'FromSala', e.data.m);
      return;
    }

    var frames = document.querySelectorAll('iframe[id^="cara-"]');
    for (var i = 0; i < frames.length; i++) {
      if (frames[i].contentWindow === e.source && originOf(frames[i]) === e.origin) {
        DotNet.invokeMethodAsync('Gnothi.Desktop', 'FromFrame', frames[i].id.substring(5), e.data.m);
        return;
      }
    }
  });

  return { toFrame: toFrame, evalIn: evalIn, abrirSala: abrirSala, aLaSala: aLaSala, tirarSala: tirarSala };
})();
