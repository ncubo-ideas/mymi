// Segundo sondeo, sobre dos cosas que la primera corrida dejo a medio camino.
(async () => {
  const out = [];
  const err = e => `${e && e.name ? e.name : typeof e}: ${e && e.message ? e.message : String(e)}`;

  // 1 · QUE VALORES ACEPTA targetAddressSpace en ESTA version. La primera corrida midio que
  // 'loopback' no es valido -- o sea que la escotilla existe con OTRO vocabulario. Cual.
  for (const v of ['loopback', 'local', 'private', 'public', 'unknown']) {
    const t0 = performance.now();
    try { await fetch('http://127.0.0.1:47623/x', { cache: 'no-store', targetAddressSpace: v });
      out.push(`targetAddressSpace='${v}' -> ACEPTADO, y el fetch contesto`); }
    catch (e) {
      const m = err(e), ms = Math.round(performance.now() - t0);
      out.push(`targetAddressSpace='${v}' -> ${/not a valid enum value/.test(m) ? 'ENUM INVALIDO' : 'ACEPTADO (fallo de red, ' + ms + ' ms)'} :: ${m}`);
    }
  }

  // 2 · SI sharedStorage SE PUEDE LEER DE VUELTA desde la pagina. Existe con toda su superficie,
  // pero si `get` no devuelve nada a la pagina, no es un canal: es un buzon de una sola via.
  try {
    await window.sharedStorage.set('gnothi-sonda', 'hola');
    out.push('sharedStorage.set -> OK (escribio)');
  } catch (e) { out.push('sharedStorage.set -> ' + err(e)); }
  try {
    const v = await window.sharedStorage.get('gnothi-sonda');
    out.push(`sharedStorage.get -> DEVOLVIO ${JSON.stringify(v)}`);
  } catch (e) { out.push('sharedStorage.get -> ' + err(e)); }

  return out.join('\n');
})()
