// DRIVE THE ENROLLMENT ON THE DEVICE THE WAY A FINGER WOULD, and read back what the screen says.
//
// Real taps: every coordinate comes out of the page over CDP (getBoundingClientRect times
// devicePixelRatio) and goes in through `adb shell input tap`, so what runs is the touch path,
// not a call into the page. The screen is read as text (pantalla.mjs). It is P2 of
// PRUEBAS-WEBVIEW.md for the pasted link, without hands and without screenshots.
//
//   node Gnothi.Mobile/conducir-el-alta.mjs pegar [urlDelBundle]   card -> paste -> Enter -> prints the sheet
//   node Gnothi.Mobile/conducir-el-alta.mjs escanear               card -> "Escanear" -> the camera opens -> Cancelar
//   node Gnothi.Mobile/conducir-el-alta.mjs codigo <imagen>        shows the reader an image of a code (svg or png)
//   node Gnothi.Mobile/conducir-el-alta.mjs archivo                card -> "Elegir el archivo" -> the system picker -> BACK
//   node Gnothi.Mobile/conducir-el-alta.mjs instalar               "Instalar en mi teléfono" -> prints the result
//   node Gnothi.Mobile/conducir-el-alta.mjs descartar              "Ahora no"
//
// `codigo` exists because the emulator's camera (hw.camera.back=emulated) shows a test pattern
// and never a code: the image is decoded inside the page with the SAME reader the camera path
// uses (gn.scan.decode), and what it says goes down the same road (gn.scan.found). On a phone,
// `escanear` pointed at the institution's page is the real thing.
//
// THREE THINGS A FINGER KNOWS AND A SCRIPT HAD TO LEARN (measured 2026-09-08 on the G37 AVD):
//
//   - The window is ADJUST_PAN. With the keyboard up the page shifts on screen but
//     getBoundingClientRect does not, so a tap computed from the page lands ON THE KEYBOARD -- a
//     stray letter appeared at the end of the URL. The field is confirmed with Enter (keyevent
//     66), which reaches the focused input wherever the window put it.
//   - The proposal sheet is taller than the screen: "Instalar" sits below the fold. The card is
//     swiped until the button is on screen, and only then tapped.
//   - BACK with the keyboard already hidden reaches the Activity, and gn.back() on the home view
//     lets Android send the app away. So the keyboard is dismissed only when dumpsys says it is
//     showing.
import { readFileSync } from 'node:fs';
import { adb, adbArgs, conectar, sleep } from './pantalla.mjs';

const phase = process.argv[2] ?? 'pegar';
const bundle = process.argv[3] ?? 'http://127.0.0.1:8096/examenes.gmod';

const page = await conectar('ui.html');
const { evaluate } = page;

const [screenW, screenH] = adb('shell wm size').match(/(\d+)x(\d+)/).slice(1).map(Number);

const text = async selector =>
	(await evaluate(`(document.querySelector(${JSON.stringify(selector)}) || {}).innerText || ''`)).trim();

const center = selector => evaluate(`(() => {
	const e = document.querySelector(${JSON.stringify(selector)});
	if (!e) return null;
	const r = e.getBoundingClientRect(), d = devicePixelRatio;
	return [Math.round((r.left + r.width / 2) * d), Math.round((r.top + r.height / 2) * d)];
})()`);

// Tap an element, swiping first while its centre is off the screen.
async function tap(selector) {
	let c = await center(selector);
	if (!c) throw new Error('nothing on the page matches ' + selector);

	for (let i = 0; i < 6 && (c[1] > screenH - 60 || c[1] < 60); i++) {
		const up = c[1] > screenH - 60;
		const from = Math.round(screenH * (up ? 0.8 : 0.375)), to = Math.round(screenH * (up ? 0.375 : 0.8));
		adb(`shell input swipe ${Math.round(screenW / 2)} ${from} ${Math.round(screenW / 2)} ${to} 300`);
		await sleep(700);
		c = await center(selector);
	}

	adb(`shell input tap ${c[0]} ${c[1]}`);
	console.log(`tap ${selector} @ ${c[0]},${c[1]}`);
	await sleep(800);
}

const keyboardShowing = () => /mInputShown=true/.test(adb('shell dumpsys input_method'));

async function hideKeyboard() {
	if (keyboardShowing()) { adb('shell input keyevent 4'); await sleep(800); }
}

// `input text` on the device: quoted for ITS shell (a link carries ? & = %), spaces as %s, and
// handed to adb without a host shell, which would take the `&` apart first.
function type(value) {
	adbArgs('shell', `input text '${value.replace(/'/g, `'\\''`).replace(/ /g, '%s')}'`);
}

// Poll up to `seconds`, printing the enrollment strip every time it changes.
async function waitFor(done, seconds) {
	let last = '';
	for (let i = 0; i < seconds; i++) {
		await sleep(1000);
		if (await done()) return true;
		const now = (await text('#alta')).replace(/\n/g, ' ');
		if (now && now !== last) { console.log('  ' + now); last = now; }
	}
	return false;
}

// Idempotent: a sheet left open by an earlier run stays open -- tapping the card behind it would
// land on the backdrop and close it instead.
async function openTheCard() {
	const isOpen = async () => /\bon\b/.test(await evaluate("document.getElementById('sheet').className"));
	if (await isOpen()) return;
	await tap('.mod.add');
	if (!(await isOpen())) throw new Error('the sheet did not open');
}

async function printTheProposal() {
	const opened = await waitFor(() => evaluate("document.getElementById('propuesta').classList.contains('on')"), 30);
	if (!opened) { console.log('no proposal; the screen says:', (await text('#alta')).replace(/\n/g, ' ')); return; }

	console.log('--- the sheet ---');
	for (const selector of ['#pr-t', '#pr-i', '#pr-cuerpo']) console.log(await text(selector));
}

async function pegar() {
	console.log('modules before:', await evaluate('(gn.state && gn.state.modules || []).length'));
	await openTheCard();

	// A field that already has the focus is not tapped again: with the keyboard up the window
	// pans, and a tap computed from the page lands on a key (a stray letter, measured twice).
	if ((await evaluate("document.activeElement && document.activeElement.id")) !== 'alta-url') await tap('#alta-url');
	type(bundle);
	await sleep(600);
	console.log('typed:', await evaluate("document.getElementById('alta-url').value"));

	adb('shell input keyevent 66');
	await printTheProposal();
}

// The camera path up to the frames: the button, the permission, the stream. Then Cancelar.
async function escanear() {
	await openTheCard();
	console.log('reader offered:', await evaluate("document.getElementById('alta-scan').style.display !== 'none' && gn.scan.supported()"));
	await tap('#alta-scan');

	const open = await waitFor(() => evaluate("document.getElementById('scan').classList.contains('on')"), 10);
	console.log('reader open:', open, open ? '' : '| hint: ' + await text('#alta-hint'));
	if (!open) return;

	for (let i = 0; i < 12; i++) {
		await sleep(500);
		const state = await evaluate("(() => { const v = document.getElementById('cam'); return v.readyState + ':' + v.videoWidth + 'x' + v.videoHeight; })()");
		console.log('  video', state);
		if (/^[2-4]:[1-9]/.test(state)) break;
	}

	// Under the tap: the reader keeps the main thread busy decoding frames (four a second, on
	// software rendering in the emulator), so the click may take a moment to be handled.
	console.log('under Cancelar:', await evaluate("(() => { const b = document.querySelector('#scan button'), r = b.getBoundingClientRect(); const e = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2); return e ? e.tagName + (e.id ? '#' + e.id : '') + '.' + e.className : 'nothing'; })()"));
	await tap('#scan button');
	const closed = await waitFor(async () => !(await evaluate("document.getElementById('scan').classList.contains('on')")), 6);
	console.log('reader closed:', closed);
}

// "Elegir el archivo en mi teléfono": the tap, and whether the system's picker came up. The picker
// itself is native and not driven from here; BACK closes it, and the log says she chose nothing.
async function archivo() {
	await openTheCard();
	await tap('#alta-file');
	await sleep(1500);
	const top = adb('shell dumpsys activity activities').split('\n').find(l => /topResumedActivity|ResumedActivity/.test(l)) || '';
	const picker = /documentsui/i.test(top);
	console.log('picker on top:', picker, picker ? '' : '| top: ' + top.trim().slice(0, 120));
	if (picker) { adb('shell input keyevent 4'); await sleep(1200); console.log('back to Gnothi:', /gnothi/i.test(adb('shell dumpsys activity activities').split('\n').find(l => /topResumedActivity|ResumedActivity/.test(l)) || '')); }
}

// An image of a code, shown to the reader instead of the camera.
async function codigo() {
	const file = process.argv[3];
	if (!file) throw new Error('codigo needs the path of an image (svg or png)');
	const mime = file.toLowerCase().endsWith('.svg') ? 'image/svg+xml' : 'image/png';
	const data = readFileSync(file).toString('base64');

	// Through the page's own decoder, so what is tested is the reader the camera path uses.
	const said = await evaluate(`(async () => {
		const img = new Image(); img.src = 'data:${mime};base64,${data}'; await img.decode();
		return await gn.scan.decode(img, 600, 600); })()`);
	if (!said) { console.log('the reader saw no code in', file); return; }

	console.log('the code says:', said);
	await evaluate(`gn.scan.found(${JSON.stringify(said)})`);
	await printTheProposal();
}

async function instalar() {
	await hideKeyboard();
	await tap('#propuesta button.cta');
	await waitFor(async () => /Listo|No se pudo/.test(await text('#alta')), 25);
	console.log('screen says:', (await text('#alta')).replace(/\n/g, ' '));
	console.log('modules after:', await evaluate(
		"JSON.stringify((gn.state && gn.state.modules || []).map(m => m.name + ':' + (m.enabled ? 'on' : 'off')))"));
}

async function descartar() {
	await hideKeyboard();
	await tap('#propuesta button.ghost');
	await sleep(1000);
	console.log('proposal open:', await evaluate("document.getElementById('propuesta').classList.contains('on')"));
}

try {
	const run = { pegar, escanear, codigo, archivo, instalar, descartar }[phase];
	if (!run) { console.error('usage: node conducir-el-alta.mjs pegar [urlDelBundle] | escanear | codigo <imagen> | archivo | instalar | descartar'); process.exit(2); }
	console.log(`screen ${screenW}x${screenH} · page ${page.url}`);
	await run();
}
finally { page.close(); }
