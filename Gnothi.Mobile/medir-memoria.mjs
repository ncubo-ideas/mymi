// MEDIR LA MEMORIA DE GNOTHI EN UN ANDROID, ABRIENDO MODULOS DE A UNO.
//
//   node medir-memoria.mjs medir <etiqueta>                 una muestra
//   node medir-memoria.mjs abrir <modulo>                   gn.abrir(modulo) por CDP y espera a que se estabilice
//   node medir-memoria.mjs atras                            BACK (con el teclado oculto) y espera
//   node medir-memoria.mjs secuencia <corrida> m1 m2 ...    abrir/medir/atras/medir por cada modulo
//   node medir-memoria.mjs procesos                         quien es quien
//   node medir-memoria.mjs puppeteer                        cuantas copias de Puppeteer.wasm hay cargadas, WebView por WebView
//   node medir-memoria.mjs reiniciar                        mata el proceso (run-as kill, NO force-stop) y lo relanza
//   node medir-memoria.mjs clonar <n> [base]                n copias de un modulo instalado (comparten su web/), para escalar
//   node medir-memoria.mjs limpiar                          borra los clones
//
// Cada muestra suma el proceso principal + los renderers sandboxed del WebView (hijos de
// webview_zygote; en el emulador Gnothi es la unica app con WebView). PSS es lo que Android
// cobra: memoria propia + la compartida repartida. RSS se anota para comparar. Una fila JSON por
// muestra en memoria.jsonl (o $MEMORIA_OUT), en el cwd.
//
// Pide el APK DEBUG (SetWebContentsDebuggingEnabled) para abrir modulos por CDP; con el Release hay
// que tocar las tarjetas (uiautomator) y medir con 'medir'. Con dos aparatos, ANDROID_SERIAL.
// Node 22 o mas (WebSocket y fetch globales). Medido el 2026-09-11: NotasGnothiHost/48.
//
// NUNCA `am force-stop` en el AVD Gnothi37k4: tumba system_server (miniatura de tarea). `run-as
// <paquete> kill -9 <pid>` lo mata sin tocar la tarea y system_server sobrevive (medido 2026-09-11).
import { execFileSync } from 'node:child_process';
import { appendFileSync, existsSync } from 'node:fs';

const ADB = process.env.ADB
	?? [process.env.ANDROID_HOME, process.env.ANDROID_SDK_ROOT, 'C:/af/sdk', 'C:/Program Files (x86)/Android/android-sdk']
		.filter(Boolean).map(sdk => `${sdk}/platform-tools/adb.exe`).find(existsSync)
	?? 'adb';
const SERIAL = process.env.ANDROID_SERIAL ?? 'emulator-5556';
const PKG = 'com.ncubo.gnothi.host';
const OUT = process.env.MEMORIA_OUT ?? 'memoria.jsonl';

const adb = (...args) => execFileSync(ADB, ['-s', SERIAL, ...args], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], maxBuffer: 64 << 20 }).trim();
const sh = cmd => adb('shell', cmd);
const shIn = (cmd, input) => execFileSync(ADB, ['-s', SERIAL, 'shell', cmd], { input, encoding: 'utf8' }).trim();
const sleep = ms => new Promise(r => setTimeout(r, ms));

function procesos() {
	const rows = sh('ps -A -o PID,PPID,UID,RSS,NAME').split('\n').slice(1)
		.map(l => l.trim().split(/\s+/))
		.map(([pid, ppid, uid, rss, ...name]) => ({ pid: +pid, ppid: +ppid, uid: +uid, rssKb: +rss, name: name.join(' ') }));
	const main = rows.find(r => r.name === PKG);
	const zygote = rows.find(r => r.name === 'webview_zygote');
	const renderers = rows.filter(r => zygote && r.ppid === zygote.pid);
	return { main, renderers, all: rows };
}

function pss(pid) {
	let out;
	try { out = sh(`dumpsys meminfo ${pid}`); } catch { return null; }
	const total = out.match(/TOTAL PSS:\s+(\d+)\s+TOTAL RSS:\s+(\d+)\s+TOTAL SWAP \(KB\):\s+(\d+)/);
	if (!total) return null;
	const n = re => { const m = out.match(re); return m ? +m[1] : null; };
	return { pssKb: +total[1], rssKb: +total[2], swapKb: +total[3], javaKb: n(/Java Heap:\s+(\d+)/), nativeKb: n(/Native Heap:\s+(\d+)/), codeKb: n(/Code:\s+(\d+)/), graphicsKb: n(/Graphics:\s+(\d+)/), webviews: n(/WebViews:\s+(\d+)/) };
}

function sistema() {
	const m = Object.fromEntries(sh('cat /proc/meminfo').split('\n').map(l => l.split(/:\s+/)).map(([k, v]) => [k, parseInt(v)]));
	let lvl = null;
	try { lvl = +(sh('dumpsys activity processes | grep mLastMemoryLevel').match(/mLastMemoryLevel=(\d+)/) || [])[1]; } catch { }
	return { memTotalKb: m.MemTotal, memAvailableKb: m.MemAvailable, swapUsedKb: m.SwapTotal - m.SwapFree, memoryLevel: Number.isNaN(lvl) ? null : lvl };
}

const mb = kb => +((kb ?? 0) / 1024).toFixed(1);

export function medir(etiqueta, extra = {}) {
	const p = procesos();
	if (!p.main) { console.log(`[${etiqueta}] Gnothi NO esta corriendo`); return null; }
	const main = pss(p.main.pid);
	const renderers = p.renderers.map(r => ({ pid: r.pid, name: r.name.replace(/^com\.google\.android\.webview:/, '').replace(/:org\.chromium.*$/, ''), ...pss(r.pid) }));
	const sys = sistema();
	const totalPss = (main?.pssKb ?? 0) + renderers.reduce((a, r) => a + (r.pssKb ?? 0), 0);
	const totalRss = (main?.rssKb ?? 0) + renderers.reduce((a, r) => a + (r.rssKb ?? 0), 0);
	const row = {
		t: new Date().toISOString(), etiqueta, ...extra,
		totalPssMb: mb(totalPss), totalRssMb: mb(totalRss),
		mainPssMb: mb(main?.pssKb), mainJavaMb: mb(main?.javaKb), mainNativeMb: mb(main?.nativeKb), mainGraphicsMb: mb(main?.graphicsKb),
		webviews: main?.webviews,
		renderers: renderers.map(r => ({ pid: r.pid, name: r.name, pssMb: mb(r.pssKb), graphicsMb: mb(r.graphicsKb) })),
		memAvailableMb: Math.round(sys.memAvailableKb / 1024), swapUsedMb: Math.round(sys.swapUsedKb / 1024), memoryLevel: sys.memoryLevel,
	};
	appendFileSync(OUT, JSON.stringify(row) + '\n');
	console.log(`[${etiqueta}] PSS total ${row.totalPssMb} MB (app ${row.mainPssMb} + renderers ${row.renderers.map(r => r.pssMb.toFixed(0)).join('+') || 0}) · RSS ${row.totalRssMb} MB · WebViews ${row.webviews} · disponible ${row.memAvailableMb} MB · nivel ${row.memoryLevel}`);
	return row;
}

// Muestrea hasta que dos lecturas seguidas difieran menos de `tol` MB (o se acabe el tiempo).
export async function estabilizar(etiqueta, { tol = 3, cada = 4000, max = 60000, extra = {} } = {}) {
	let prev = null, t0 = Date.now(), row;
	while (Date.now() - t0 < max) {
		await sleep(cada);
		row = medir(`${etiqueta} +${Math.round((Date.now() - t0) / 1000)}s`, extra);
		if (!row) return null;
		if (prev !== null && Math.abs(row.totalPssMb - prev) < tol) break;
		prev = row.totalPssMb;
	}
	return row;
}

// --- CDP sobre la cascara -----------------------------------------------------------------
// El socket lleva el PID de Gnothi; se elige ESE y no el primero de /proc/net/unix, donde
// quedan sockets viejos de otros procesos (trampa medida el 2026-09-10).
async function tunel(port) {
	const pid = sh(`pidof ${PKG}`);
	adb('forward', `tcp:${port}`, `localabstract:webview_devtools_remote_${pid}`);
	return await (await fetch(`http://127.0.0.1:${port}/json`)).json();
}

async function conectar(urlFilter = 'ui.html', port = 9223) {
	const targets = await tunel(port);
	const target = targets.find(t => (t.url ?? '').includes(urlFilter));
	if (!target) throw new Error(`no page matching '${urlFilter}'; open: ${targets.map(t => t.url).join(' | ')}`);
	const ws = new WebSocket(target.webSocketDebuggerUrl);
	await new Promise((ok, ko) => { ws.onopen = ok; ws.onerror = ko; });
	let seq = 0; const pending = new Map();
	ws.onmessage = m => { const r = JSON.parse(m.data); if (r.id && pending.has(r.id)) { pending.get(r.id)(r); pending.delete(r.id); } };
	const evaluate = expression => new Promise((ok, ko) => {
		const id = ++seq;
		pending.set(id, r => {
			if (r.error) return ko(new Error(JSON.stringify(r.error)));
			if (r.result.exceptionDetails) return ko(new Error(r.result.exceptionDetails.text));
			ok(r.result.result.value);
		});
		ws.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression, returnByValue: true, awaitPromise: true } }));
	});
	return { evaluate, close: () => ws.close() };
}

export async function targets(port = 9223) { return (await tunel(port)).map(t => t.url); }

export async function abrir(modulo) {
	const page = await conectar('ui.html');
	try { await page.evaluate(`gn.abrir('${modulo}')`); } finally { page.close(); }
	const t0 = Date.now();
	let abierto = false;
	while (Date.now() - t0 < 60000) {
		await sleep(1500);
		abierto = (await targets()).some(u => u.includes(`${modulo}.gnothi.local`));
		if (abierto) break;
	}
	console.log(`abrir ${modulo}: pagina ${abierto ? 'presente' : 'AUSENTE'} a los ${Math.round((Date.now() - t0) / 1000)}s`);
	return abierto;
}

// BACK llega a la Activity solo con el teclado oculto; si esta arriba, el primer BACK lo cierra.
// UN SOLO BACK, y se comprueba que el modulo se fue: si el teclado NO estaba arriba y se mandan
// dos, el segundo llega a la cascara, gn.back() dice false y MainActivity hace MoveTaskToBack --
// que en este AVD tumba system_server (miniatura de tarea). Costo una corrida el 2026-09-14.
export async function atras() {
	const enPantalla = () => (sh('dumpsys activity top').match(/android\.webkit\.WebView\{/g) || []).length;
	const n0 = enPantalla();
	sh('input keyevent 4');
	await sleep(2500);
	// NUNCA un segundo BACK automatico: el 2026-09-14 la vista tardo mas de 1,2 s en irse de la
	// jerarquia, el script mando otro, y ese fue el que saco a Gnothi al launcher. Se avisa y punto.
	if (enPantalla() >= n0 && n0 > 1) console.log('atras: el modulo sigue en pantalla (¿teclado o dialogo arriba?); no se manda otro BACK');
}

// --- cronometrar: cuanto tarda un modulo en estar LISTO (P-M17) --------------------------------
// El momento «listo» no se adivina mirando la pagina: Gnothi pone el WebView del modulo en
// pantalla (MainActivity.Mostrar) RECIEN cuando Open termino, y Open espera el listo() del puente.
// Asi que «listo» = aparece un WebView mas en la jerarquia de vistas de la Activity, que
// `dumpsys activity top` lista en ~80 ms. Los hitos intermedios salen del logcat con -v epoch:
// la pagina servida («the bundle declares its base») y, en Blazor, el actor arrancando.
const webviewsEnPantalla = () => (sh('dumpsys activity top').match(/android\.webkit\.WebView\{/g) || []).length;

export async function cronometrar(modulo, extra = {}) {
	const n0 = webviewsEnPantalla();
	const page = await conectar('ui.html');
	sh(`log -t GNOTHI-MEDIR "abrir ${modulo}"`);
	const t0 = Date.now();
	try { await page.evaluate(`gn.abrir('${modulo}')`); } finally { page.close(); }
	let listoMs = null;
	while (Date.now() - t0 < 90000) {
		if (webviewsEnPantalla() > n0) { listoMs = Date.now() - t0; break; }
		await sleep(50);
	}
	// Hitos del logcat, relativos al marcador.
	const lines = sh('logcat -d -v epoch -s GNOTHI-MEDIR:I gnothi:I modulo:I').split('\n');
	const marca = lines.map((l, i) => [l, i]).filter(([l]) => l.includes(`GNOTHI-MEDIR: abrir ${modulo}`)).at(-1);
	const epoch = l => parseFloat(l.trim().split(/\s+/)[0]) * 1000;
	let paginaMs = null, actorMs = null;
	if (marca) {
		const t = epoch(marca[0]);
		for (const l of lines.slice(marca[1] + 1)) {
			if (paginaMs === null && l.includes(`${modulo}: the bundle declares its base`)) paginaMs = Math.round(epoch(l) - t);
			if (actorMs === null && l.includes(`${modulo}: Starting`) && l.includes('Actor')) actorMs = Math.round(epoch(l) - t);
		}
	}
	const row = { t: new Date().toISOString(), etiqueta: `P-M17 · ${modulo}`, ...extra, modulo, paginaMs, actorMs, listoMs };
	appendFileSync(OUT, JSON.stringify(row) + '\n');
	console.log(`[${extra.modo ?? ''} ${modulo}] pagina ${paginaMs ?? '—'} ms · actor ${actorMs ?? '—'} ms · LISTO ${listoMs ?? 'NUNCA'} ms`);
	return row;
}

// --- reiniciar / clonar / limpiar -----------------------------------------------------------
export async function reiniciar() {
	let pid = ''; try { pid = sh(`pidof ${PKG}`); } catch { }
	// En Release run-as no esta permitido (package not debuggable): si adb corre como root, kill directo.
	if (pid) { try { sh(`run-as ${PKG} kill -9 ${pid}`); } catch { try { sh(`kill -9 ${pid}`); } catch { } } }
	await sleep(4000);
	// Si el gestor de paquetes esta reconstruyendose (tras un reinicio de system_server) el
	// resolve contesta "No activity found": se espera y, si sigue, se usa el nombre conocido.
	let act = '';
	for (let i = 0; i < 12 && !act.includes('/'); i++) {
		try { act = sh(`cmd package resolve-activity --brief ${PKG}`).split('\n').at(-1).trim(); } catch { act = ''; }
		if (!act.includes('/')) await sleep(5000);
	}
	if (!act.includes('/')) act = `${PKG}/crc64a35e2aa9284b3252.MainActivity`;
	console.log(sh(`am start -W -n ${act}`).split('\n').filter(l => /TotalTime|Error/.test(l)).join(' '));
	await sleep(6000);
}

// Copias de un modulo instalado que comparten su carpeta web/: solo manifest, installation,
// signature, brand y la marca de comprobado cambian de nombre. Sirven para medir, no para usar.
export function clonar(n, base = 'examenes') {
	const lineas = ['cd files/Modulos || exit 1'];
	for (let i = 1; i <= n; i++) {
		const c = `clon${i}`;
		lineas.push(
			`rm -rf ${c}; mkdir -p ${c}; cp ${base}/manifest.json ${base}/installation.json ${base}/signature.json ${base}/comprobado.json ${c}/ 2>/dev/null; cp -r ${base}/brand ${c}/ 2>/dev/null`,
			`sed -i 's/"module":  *"${base}"/"module": "${c}"/' ${c}/manifest.json`,
			`sed -i 's#/files/journals/${base}#/files/journals/${c}#' ${c}/installation.json`,
			`mkdir -p ../journals/${c} && cp -r ../journals/${base}/. ../journals/${c}/ 2>/dev/null`);
	}
	lineas.push('ls | grep -c clon');
	console.log(shIn(`run-as ${PKG} sh`, lineas.join('\n') + '\n') + ' clones');
}

export function limpiar() {
	console.log(shIn(`run-as ${PKG} sh`, 'cd files && rm -rf Modulos/clon* journals/clon* && ls Modulos\n'));
}

// --- puppeteer: cuantas copias del motor hay cargadas, WebView por WebView ------------------
// Cada modulo hospedado vive en su propio origen (https://<m>.gnothi.local), asi que cada WebView
// baja y compila SU Puppeteer.<hash>.wasm: no hay codigo compartido entre modulos aunque todos
// corran en el mismo renderer. Se lee por CDP en cada target, al runtime .NET (Blazor trae los
// ensamblados desde su Cache Storage, asi que la performance API NO los ve: medido 2026-09-11),
// y ademas en disco: cada modulo instalado tiene su web/_framework con su propio Puppeteer.
// SOLO expresiones sincronas: en un WebView oculto (document.hidden) las promesas de la pagina
// -caches.keys(), por ejemplo- no resuelven nunca y el evaluate se cuelga (medido 2026-09-11).
export async function puppeteer() {
	const urls = (await targets()).filter(u => u.includes('.gnothi.local'));
	const filas = [];
	for (const u of urls) {
		const modulo = u.match(/https:\/\/([^.]+)\.gnothi\.local/)[1];
		const page = await conectar(`${modulo}.gnothi.local`);
		try {
			const r = await Promise.race([
				page.evaluate(`(() => {
					const rt = globalThis.getDotnetRuntime && globalThis.getDotnetRuntime(0);
					if (!rt) return { runtime: false, hidden: document.hidden };
					const cfg = rt.getConfig(); const res = cfg.resources ?? {};
					const declarados = Object.keys(res.assembly ?? {}).concat(Object.keys(res.coreAssembly ?? {}));
					const cargados = (rt.INTERNAL.mono_wasm_get_loaded_files?.() ?? []).map(n => n.split('/').pop());
					const pupp = n => /^Puppeteer\\b/i.test(n);
					return { runtime: true, hidden: document.hidden, main: cfg.mainAssemblyName ?? null, declarados: declarados.length, cargados: cargados.length, puppDeclarado: declarados.filter(pupp), puppCargado: cargados.filter(pupp), otrosPuppet: cargados.filter(n => /Puppet/i.test(n) && !pupp(n)) };
				})()`),
				sleep(10000).then(() => { throw new Error('sin respuesta en 10 s'); }),
			]);
			filas.push({ modulo, ...r });
		} catch (e) { filas.push({ modulo, error: e.message }); }
		finally { page.close(); }
	}
	let disco = '';
	try { disco = shIn(`run-as ${PKG} sh`, 'cd files/Modulos && for m in *; do n=$(ls $m/web/_framework 2>/dev/null | grep -c "^Puppeteer\\."); echo "$m:$n"; done\n'); } catch (e) { disco = `error ${e.message}`; }
	const p = procesos();
	console.log(`En disco (files/Modulos/<m>/web/_framework): ${disco.split('\n').join(' · ')}`);
	console.log(`WebViews vivos: ${urls.length} · renderers: ${p.renderers.length} (${p.renderers.map(r => `pid ${r.pid}, ${Math.round(r.rssKb / 1024)} MB RSS`).join('; ')})`);
	for (const f of filas) {
		if (f.error) { console.log(`  ${f.modulo}: ERROR ${f.error}`); continue; }
		if (!f.runtime) { console.log(`  ${f.modulo}: sin runtime .NET (JS plano), Puppeteer NO`); continue; }
		console.log(`  ${f.modulo}${f.hidden ? ' (oculto)' : ' (en pantalla)'}: Puppeteer ${f.puppCargado.length ? 'CARGADO ' + f.puppCargado.join(', ') : 'NO cargado'} · main ${f.main}, ${f.cargados} archivos cargados de ${f.declarados} declarados${f.otrosPuppet.length ? ' · tambien ' + f.otrosPuppet.join(', ') : ''}`);
	}
	const copias = filas.filter(f => f.puppCargado?.length).length;
	console.log(`Copias de Puppeteer cargadas: ${copias} en ${urls.length} WebViews vivos, en ${p.renderers.length} proceso(s) renderer`);
	appendFileSync(OUT, JSON.stringify({ t: new Date().toISOString(), etiqueta: 'puppeteer', copias, webviews: urls.length, renderers: p.renderers.length, disco, detalle: filas }) + '\n');
	return filas;
}

// --- main ----------------------------------------------------------------------------------
const [, , cmd, ...args] = process.argv;
if (cmd === 'medir') medir(args[0] ?? 'muestra');
else if (cmd === 'procesos') { const p = procesos(); console.log(JSON.stringify({ main: p.main, renderers: p.renderers }, null, 1)); }
else if (cmd === 'targets') console.log((await targets()).join('\n'));
else if (cmd === 'abrir') { await abrir(args[0]); await estabilizar(`abierto ${args[0]}`); }
else if (cmd === 'atras') { await atras(); await estabilizar('atras'); }
else if (cmd === 'estabilizar') await estabilizar(args[0] ?? 'muestra');
else if (cmd === 'reiniciar') await reiniciar();
else if (cmd === 'cronometrar') {
	// cronometrar <modo> m1 m2 ...: abre cada modulo, mide hasta LISTO, vuelve atras. Con
	// `frio` reinicia la app antes, asi que cada primera apertura es en frio.
	const [modo, ...modulos] = args;
	if (modo.startsWith('frio')) await reiniciar();
	for (const m of modulos) {
		await cronometrar(m, { modo });
		await sleep(4000);
		medir(`P-M17 · tras abrir ${m}`, { modo, modulo: m });
		await atras();
	}
}
else if (cmd === 'puppeteer') await puppeteer();
else if (cmd === 'clonar') clonar(+args[0] || 4, args[1]);
else if (cmd === 'limpiar') limpiar();
else if (cmd === 'secuencia') {
	const [corrida, ...modulos] = args;
	medir(`${corrida} · base`, { corrida, paso: 0, vivos: 0 });
	let vivos = 0;
	for (const m of modulos) {
		if (await abrir(m)) vivos++;
		await estabilizar(`${corrida} · abierto ${m}`, { extra: { corrida, paso: vivos, modulo: m, estado: 'abierto', vivos } });
		await atras();
		await estabilizar(`${corrida} · atras de ${m}`, { max: 30000, extra: { corrida, paso: vivos, modulo: m, estado: 'atras', vivos } });
	}
	await sleep(20000);
	medir(`${corrida} · reposo 20s`, { corrida, paso: vivos, estado: 'reposo', vivos });
}
else console.log('uso: medir <etiqueta>|procesos|targets|puppeteer|abrir <m>|atras|estabilizar|reiniciar|clonar <n> [base]|limpiar|secuencia <corrida> m1 m2 ...');
