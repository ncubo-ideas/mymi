// READ AND DRIVE GNOTHI'S SCREEN ON A PHONE, WITHOUT SCREENSHOTS.
//
// On the G37 emulator `screencap` returns an empty PNG -- the same software bitmap path that
// crashes system_server on task thumbnails -- so the screen is read as TEXT instead.
// AndroidWebModule turns on SetWebContentsDebuggingEnabled, which opens a Chrome DevTools socket
// per WebView; this tunnels it over adb and evaluates JavaScript inside the shell's page. What
// comes back is verifiable text -- a sheet's innerText, a button's position -- which is better
// evidence than a picture, and it is the only kind this AVD gives.
//
//   node Gnothi.Mobile/pantalla.mjs "<expression>" [urlFilter]
//
//   node Gnothi.Mobile/pantalla.mjs "gn.state.modules.length"
//   node Gnothi.Mobile/pantalla.mjs "document.getElementById('alta').innerText"
//
// THE TARGET MATTERS. /json lists the shell (file:///android_asset/ui.html, the default filter)
// AND one page per hosted module (https://<modulo>.gnothi.local/app/). Taking the first blindly
// grabs whichever module is open and it looks like the shell shows something else -- which, in
// passing, is the containment showing: one origin per module.
//
// The socket carries the app's PID: every restart of Gnothi needs a new tunnel, and this makes
// one on every call. adb is $ADB, or platform-tools under $ANDROID_HOME / $ANDROID_SDK_ROOT, or
// the SDKs the PowerShell scripts use; with two devices attached, set $ANDROID_SERIAL as adb
// itself expects. Node 22 or later (global WebSocket and fetch).
import { execSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

export const ADB = process.env.ADB
	?? [process.env.ANDROID_HOME, process.env.ANDROID_SDK_ROOT, 'C:/af/sdk', 'C:/Program Files (x86)/Android/android-sdk']
		.filter(Boolean)
		.map(sdk => `${sdk}/platform-tools/adb.exe`)
		.find(existsSync)
	?? 'adb';

// One adb call, output trimmed. Throws on a non-zero exit, with adb's own message.
export const adb = args => execSync(`"${ADB}" ${args}`, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();

export const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

// Tunnel the shell's DevTools socket and open a session on the page whose url contains urlFilter.
export async function conectar(urlFilter = 'ui.html', port = 9222) {
	const socket = adb('shell cat /proc/net/unix').match(/webview_devtools_remote_\d+/)?.[0];
	if (!socket) throw new Error('no WebView devtools socket on the device: is Gnothi running?');
	adb(`forward tcp:${port} localabstract:${socket}`);

	const targets = await (await fetch(`http://127.0.0.1:${port}/json`)).json();
	const target = targets.find(t => (t.url ?? '').includes(urlFilter));
	if (!target) throw new Error(`no page matching '${urlFilter}'; open: ${targets.map(t => t.url).join(' | ')}`);

	const ws = new WebSocket(target.webSocketDebuggerUrl);
	await new Promise((ok, ko) => { ws.onopen = ok; ws.onerror = ko; });

	let seq = 0;
	const pending = new Map();
	ws.onmessage = m => {
		const reply = JSON.parse(m.data);
		if (reply.id && pending.has(reply.id)) { pending.get(reply.id)(reply); pending.delete(reply.id); }
	};

	// Runtime.evaluate with the value brought back by value; a throw in the page becomes a throw here.
	const evaluate = expression => new Promise((ok, ko) => {
		const id = ++seq;
		pending.set(id, reply => {
			if (reply.error) return ko(new Error('CDP: ' + JSON.stringify(reply.error)));
			const thrown = reply.result.exceptionDetails;
			if (thrown) return ko(new Error('the page threw: ' + (thrown.exception?.description ?? thrown.text)));
			ok(reply.result.result.value);
		});
		ws.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression, returnByValue: true, awaitPromise: true } }));
	});

	return { url: target.url, evaluate, close: () => ws.close() };
}

// Run directly: evaluate one expression and print it.
if (import.meta.url === pathToFileURL(process.argv[1]).href) {
	const [, , expression, filter] = process.argv;
	if (!expression) { console.error('usage: node pantalla.mjs "<expression>" [urlFilter]'); process.exit(2); }

	const page = await conectar(filter);
	try {
		const value = await page.evaluate(expression);
		console.log(typeof value === 'string' ? value : JSON.stringify(value));
	}
	finally { page.close(); }
}
