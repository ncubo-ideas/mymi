// La misma sonda que sonda.html, como expresion inyectable por CDP.
//
// POR QUE EXISTEN LAS DOS FORMAS:
//   sonda.html        -- para que una persona la corra en un telefono sin adb
//   sonda-inyectada.js -- para medir DENTRO de la WebAPK, porque el service worker de la PWA
//                         contesta toda navegacion de su scope con su index.html cacheado y
//                         sonda.html nunca llega a cargarse ahi (medido: la pagina decia
//                         "No encontrado", el 404 del router de Blazor, con url sonda.html).
//
// No toca el DOM a proposito: corre en cualquier pagina del origen correcto.
(async () => {
  const filas = [];
  const err = e => `${e && e.name ? e.name : typeof e}: ${e && e.message ? e.message : String(e)}`;
  const fila = (mecanismo, intento, veredicto, detalle) => filas.push({ mecanismo, intento, veredicto, detalle });

  // El tiempo es el discriminador: un bloqueo por politica se decide antes de tocar la red y
  // es inmediato; un intento real que nadie contesta paga los reintentos de TCP.
  const leer = ms => ms < 100
    ? `${ms} ms - INMEDIATO: huele a bloqueo por politica, antes de tocar la red`
    : `${ms} ms - TARDO: salio a la red de verdad y nadie contesto`;

  const puerto = 47623;

  // --- entorno. Sin esto ninguna fila de abajo se puede citar.
  const modo = ['standalone', 'minimal-ui', 'fullscreen', 'browser'].find(m => matchMedia(`(display-mode: ${m})`).matches) || '?';
  fila('entorno', 'display-mode / origen / Chrome', modo === 'standalone' ? 'DENTRO DE LA PWA' : 'PESTANA (no cuenta)',
    `display-mode=${modo} | origin=${location.origin} | isSecureContext=${isSecureContext} | `
    + (navigator.userAgent.match(/Chrome\/[\d.]+/) || ['?'])[0]);

  // --- 1 sockets crudos
  for (const n of ['TCPSocket', 'UDPSocket', 'TCPServerSocket']) {
    const hay = n in self;
    let d = `'${n}' in self === ${hay}`;
    if (hay) { try { new self[n]('127.0.0.1', 1); d += ' | construyo sin tirar'; } catch (e) { d += ' | ' + err(e); } }
    fila('sockets crudos', `'${n}' in self`, hay ? 'EXISTE' : 'NO EXISTE', d);
  }

  // --- 2 Shared Storage del W3C (colision de nombre con el "shared storage" de Android)
  {
    const hay = 'sharedStorage' in window, x = hay ? window.sharedStorage : null;
    fila('Shared Storage (W3C)', `'sharedStorage' in window`, hay ? 'EXISTE' : 'NO EXISTE',
      `in window === ${hay}` + (hay ? ` | typeof=${typeof x} | ctor=${x && x.constructor ? x.constructor.name : '?'} | `
        + ['set', 'append', 'delete', 'clear', 'get', 'run', 'selectURL', 'worklet'].map(m => `${m}=${typeof x[m] !== 'undefined'}`).join(' ') : ''));
  }

  // --- 3 lo que si hay hoy: el picker (mecanismo de A2/A3) y la deteccion del Host
  fila('File System Access', 'typeof showDirectoryPicker', typeof window.showDirectoryPicker === 'function' ? 'EXISTE' : 'NO EXISTE',
    `typeof === ${typeof window.showDirectoryPicker} | es el mecanismo de A2/A3: la carpeta ES el canal`);

  if (!navigator.getInstalledRelatedApps) fila('getInstalledRelatedApps', 'navigator.getInstalledRelatedApps', 'NO EXISTE', 'undefined');
  else try {
    const apps = await navigator.getInstalledRelatedApps();
    fila('getInstalledRelatedApps', 'await getInstalledRelatedApps()', apps.length ? 'VE EL HOST' : 'LISTA VACIA',
      JSON.stringify(apps) + ' | solo presencia: sirve para entregar un token, no para pintar');
  } catch (e) { fila('getInstalledRelatedApps', 'await getInstalledRelatedApps()', 'TIRA', err(e)); }

  // --- 4 content:// desde contenido web
  try { const r = await fetch('content://com.android.externalstorage.documents/tree/primary%3A'); fila('content:// (fetch)', `fetch('content://...')`, 'CONTESTA', 'HTTP ' + r.status); }
  catch (e) { fila('content:// (fetch)', `fetch('content://...')`, 'NO FETCHABLE', err(e)); }

  // --- 5 HTTP a loopback, con y sin la escotilla de Local Network Access
  for (const [etq, init] of [['fetch normal', { cache: 'no-store' }],
                             ['targetAddressSpace:loopback', { cache: 'no-store', targetAddressSpace: 'loopback' }]]) {
    const t0 = performance.now();
    try { const r = await fetch(`http://127.0.0.1:${puerto}/gnothi/ping`, init);
      fila('HTTP a loopback', etq, 'CONTESTA', `HTTP ${r.status} en ${Math.round(performance.now() - t0)} ms`); }
    catch (e) { fila('HTTP a loopback', etq, 'FALLA', `${err(e)} | ${leer(Math.round(performance.now() - t0))}`); }
  }

  // --- 6 WebSocket a loopback. No tiene targetAddressSpace: no hay escotilla que probar.
  await new Promise(resolve => {
    const url = `ws://127.0.0.1:${puerto}/`;
    let s; const t0 = performance.now();
    const cerrar = (que, v, extra) => { fila('WebSocket a loopback', `new WebSocket('${url}')`, v,
      `${que} | ${leer(Math.round(performance.now() - t0))} | readyState=${s ? s.readyState : '?'}${extra ? ' | ' + extra : ''}`);
      try { s && s.close(); } catch (_) {} resolve(); };
    try { s = new WebSocket(url); }
    catch (e) { fila('WebSocket a loopback', `new WebSocket('${url}')`, 'BLOQUEADO', 'tira en el constructor: ' + err(e)); return resolve(); }
    s.onopen = () => cerrar('open', 'ABRE', 'habia alguien escuchando y el navegador lo permitio');
    s.onerror = () => cerrar('error', 'FALLA', 'el evento error no trae motivo: el texto esta en la consola, no en JS');
    setTimeout(() => s.readyState <= 1 && cerrar('timeout', 'SIN RESPUESTA', '3 s sin open ni error'), 3000);
  });

  const l = ['| mecanismo | intento | veredicto | detalle |', '|---|---|---|---|'];
  for (const f of filas) l.push(`| ${f.mecanismo} | \`${f.intento}\` | **${f.veredicto}** | ${f.detalle} |`);
  return l.join('\n');
})()
