// DRIVE THE ENROLLMENT ON THE DEVICE THE WAY A FINGER WOULD, and read back what the screen says.
//
// Real taps: every coordinate comes out of the page over CDP (getBoundingClientRect times
// devicePixelRatio) and goes in through `adb shell input tap`, so what runs is the touch path,
// not a call into the page. The screen is read as text (pantalla.mjs). It is P2 of
// PRUEBAS-WEBVIEW.md for the pasted link, without hands and without screenshots.
//
//   node Gnothi.Mobile/conducir-el-alta.mjs pegar [urlDelBundle]   card -> paste -> Enter -> prints the sheet
//   node Gnothi.Mobile/conducir-el-alta.mjs instalar               "Instalar en mi teléfono" -> prints the result
//   node Gnothi.Mobile/conducir-el-alta.mjs descartar              "Ahora no"
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
import { adb, conectar, sleep } from './pantalla.mjs';

const phase = process.argv[2] ?? 'pegar';
const bundle = process.argv[3] ?? 'http://127.0.0.1:8096/examenes.zip';

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

// `input text` on the device: quoted for its shell (a bundle link may carry ? & =), spaces as %s.
function type(value) {
	adb(`shell input text '${value.replace(/'/g, `'\\''`).replace(/ /g, '%s')}'`);
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

async function pegar() {
	console.log('modules before:', await evaluate('(gn.state && gn.state.modules || []).length'));

	await tap('.mod.add');
	if (!/\bon\b/.test(await evaluate("document.getElementById('sheet').className"))) throw new Error('the sheet did not open');

	await tap('#alta-url');
	type(bundle);
	await sleep(600);
	console.log('typed:', await evaluate("document.getElementById('alta-url').value"));

	adb('shell input keyevent 66');

	const opened = await waitFor(() => evaluate("document.getElementById('propuesta').classList.contains('on')"), 30);
	if (!opened) { console.log('no proposal; the screen says:', (await text('#alta')).replace(/\n/g, ' ')); return; }

	console.log('--- the sheet ---');
	for (const selector of ['#pr-t', '#pr-i', '#pr-cuerpo']) console.log(await text(selector));
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
	const run = { pegar, instalar, descartar }[phase];
	if (!run) { console.error('usage: node conducir-el-alta.mjs pegar [urlDelBundle] | instalar | descartar'); process.exit(2); }
	console.log(`screen ${screenW}x${screenH} · page ${page.url}`);
	await run();
}
finally { page.close(); }
