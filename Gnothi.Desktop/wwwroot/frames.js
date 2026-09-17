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

  window.addEventListener('message', function (e) {
    if (!e.data || e.data.gnothi !== 'puente') return;
    var frames = document.querySelectorAll('iframe[id^="cara-"]');
    for (var i = 0; i < frames.length; i++) {
      if (frames[i].contentWindow === e.source && originOf(frames[i]) === e.origin) {
        DotNet.invokeMethodAsync('Gnothi.Desktop', 'FromFrame', frames[i].id.substring(5), e.data.m);
        return;
      }
    }
  });

  return { toFrame: toFrame, evalIn: evalIn };
})();
