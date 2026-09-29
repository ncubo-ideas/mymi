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

  // ── THE ROOM. A HIDDEN, PERMANENT iframe that is nobody's face.
  //
  // Inside it runs Gnothi's substrate -- the .NET runtime compiled to wasm -- and modules go in
  // there as bytes. It sits out of sight because THE ROOM HAS NO SCREEN: it exists to answer. The
  // institution's face is still its own frame, in its own origin, and is mounted only when the
  // person opens it.
  //
  // SAME DISCIPLINE AS THE ONE ABOVE, AND FOR THE SAME REASON: it is recognised by its WINDOW and
  // never by what the message says, and both directions are anchored to its own origin. A document
  // that navigated elsewhere stops hearing and stops being heard.
  var ROOM = 'room-gnothi';

  function roomFrame() { return document.getElementById(ROOM); }

  function openRoom(url) {
    var earlier = roomFrame();
    if (earlier) earlier.remove();

    var f = document.createElement('iframe');
    f.id = ROOM;
    f.src = url;
    f.style.display = 'none';
    f.setAttribute('aria-hidden', 'true');
    f.setAttribute('tabindex', '-1');
    document.body.appendChild(f);
  }

  // A REQUEST TO THE ROOM. It does not travel as javascript to evaluate -- which is what a module's
  // face needs, and why its CSP asks for 'unsafe-eval' -- but as DATA: the shim calls
  // `window.sala.pedido` with these five fields. The room needs no such concession.
  //
  // THE FIELD NAMES STAY IN SPANISH ON PURPOSE (`verbo`, `modulo`): they are the envelope the shim
  // reads, and the shim hands them straight to the substrate's own API, which is compiled apart.
  // Renaming the local variables around them is free; renaming the fields is a two-sided change --
  // and on 2026-09-28 a bulk rename here turned `verbo: verbo` into a reference to a variable that
  // no longer existed. Exactly the trap C10 records: a protocol field written as an identifier is
  // indistinguishable from a variable to any tool that does not know which is which.
  function toRoom(id, verb, module, a, b) {
    var f = roomFrame();
    var origin = f ? originOf(f) : null;
    if (f && f.contentWindow && origin)
      f.contentWindow.postMessage({ gnothi: 'sala', id: id, verbo: verb, modulo: module, a: a, b: b }, origin);
  }

  // THROW THE DOCUMENT AWAY, which is the only thing that recovers a room that does not answer.
  // Removing the iframe takes whatever was inside with it, and with it the border: what the module
  // had mounted lives in that document's memory, not on disk.
  function dropRoom() {
    var f = roomFrame();
    if (f) f.remove();
  }

  window.addEventListener('message', function (e) {
    if (!e.data || e.data.gnothi !== 'puente') return;

    // The room first: there is only one and it carries no module name, because which module it is
    // talking about comes from the frame of the call the Host opened, never from the message.
    var s = roomFrame();
    if (s && s.contentWindow === e.source && originOf(s) === e.origin) {
      DotNet.invokeMethodAsync('Gnothi.Desktop', 'FromRoom', e.data.m);
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

  return { toFrame: toFrame, evalIn: evalIn, openRoom: openRoom, toRoom: toRoom, dropRoom: dropRoom };
})();
