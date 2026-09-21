// SINCRONIZAR EL MANIFIESTO DESDE EL MODULO. El arreglo de fondo del problema que el guardian
// solo ataja: el manifiesto de Modulos/<x>/manifest.json es una COPIA del modulo y se
// desincroniza a mano. Esto lo vuelve una PROYECCION: arranca el modulo, le pide su tools/list
// real, y reescribe el bloque `tools` del manifiesto con lo que el modulo expone de verdad.
//
//   node Herramientas/sincronizar-manifiesto.mjs            <- todos los modulos conocidos
//   node Herramientas/sincronizar-manifiesto.mjs examenes   <- uno
//   node Herramientas/sincronizar-manifiesto.mjs --check     <- NO escribe: falla si algo cambiaria
//   node Herramientas/sincronizar-manifiesto.mjs compras --dll=<ruta>  <- con un dll compilado aparte
//
// QUE SINCRONIZA (viene del modulo, es estructura, no se discute):
//   - el conjunto de tools: agrega las nuevas, quita los fantasmas
//   - por tool: el nombre
//   - por parametro: nombre, type y enum -que el inputSchema del modulo ya trae-
//
// QUE PRESERVA (es contenido curado, y el modulo no es su unica fuente legitima):
//   - todo el encabezado del manifiesto: module, title, institution, substrate, capabilities,
//     y los comentarios "//..."
//   - la description y el title de cada tool que YA existia en el manifiesto
//   - la description de cada parametro que ya existia
//
// Por que preservar la descripcion: puede estar curada en el manifiesto y no en el codigo, y el
// generador no sabe cual de las dos es la buena. Para una tool NUEVA no hay nada que preservar,
// asi que se toma la del modulo. UNA DESCRIPCION VACIA CUENTA COMO NUEVA: ese es el modo de
// volver a tomarla del codigo -- se borra en el manifiesto y se sincroniza --, y es lo que se hizo
// con examenes el 2026-09-18 para devolverlas al idioma del modulo.
//
// OJO CON EL IDIOMA: el titulo y la descripcion de una tool son VOZ DE LA INSTITUCION y siguen el
// idioma del modulo, no el de quien pregunta -- ver Metodologia/Guias/reglas-de-localizacion.md.
// Un manifiesto con los titulos en un idioma y las descripciones en otro es la mitad de la voz
// traducida por el Host, y cuando el manifiesto se firme, un texto traducido queda fuera de la
// firma. Preservar deja que eso pase sin avisar, asi que --check es el lugar donde se ve.
//
// El guardian de la suite sigue siendo la red: esto MANTIENE sincronizado, el guardian AVISA si
// alguien igual edito a mano y se desvio.

import { spawn } from "node:child_process";
import { readFileSync, writeFileSync, existsSync, readdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join, resolve, isAbsolute } from "node:path";

const repo = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const modulosRoot = join(repo, "Modulos");

// DONDE VIVE EL DLL DE CADA MODULO LO DICE SU installation.json, no una lista aca. Antes habia
// una copia de esas rutas en este archivo, y una copia se desincroniza igual que el manifiesto
// que este generador existe para no copiar. shell.arguments[0] es el dll; si no es absoluta va
// relativa a la carpeta del modulo, que es la misma regla que aplica el Host (Installation.Read).
// Solo cuentan los modulos que corren con dotnet: un webview no tiene tools/list que pedir.
// La carpeta de journal no hace falta: tools/list no lee el journal, solo enumera.
function descubrirModulos() {
  const modulos = {};
  for (const nombre of readdirSync(modulosRoot, { withFileTypes: true }).filter(d => d.isDirectory()).map(d => d.name)) {
    const carpeta = join(modulosRoot, nombre);
    const instalacion = join(carpeta, "installation.json");
    if (!existsSync(instalacion)) continue;
    const shell = (JSON.parse(readFileSync(instalacion, "utf8")).shell) || {};
    if (shell.command !== "dotnet" || !Array.isArray(shell.arguments) || !shell.arguments.length) continue;
    const primero = shell.arguments[0];
    modulos[nombre] = { dll: isAbsolute(primero) ? primero : resolve(carpeta, primero) };
  }
  return modulos;
}
const MODULOS = descubrirModulos();

// ── cliente MCP por stdio ────────────────────────────────────────────────────────────────────
function pedirToolsList(dll) {
  return new Promise((res, rej) => {
    const p = spawn("dotnet", [dll], { stdio: ["pipe", "pipe", "ignore"] });
    let buf = "";
    const pend = new Map();
    let id = 1;
    const enviar = (method, params) => {
      const n = id++;
      p.stdin.write(JSON.stringify({ jsonrpc: "2.0", id: n, method, params }) + "\n");
      return new Promise(r => pend.set(n, r));
    };
    const notificar = (method, params) =>
      p.stdin.write(JSON.stringify({ jsonrpc: "2.0", method, params }) + "\n");

    p.on("error", rej);
    const timeout = setTimeout(() => { p.kill(); rej(new Error("timeout hablando MCP con " + dll)); }, 40000);

    p.stdout.on("data", chunk => {
      buf += chunk;
      let i;
      while ((i = buf.indexOf("\n")) >= 0) {
        const linea = buf.slice(0, i).trim();
        buf = buf.slice(i + 1);
        if (!linea) continue;
        let msg;
        try { msg = JSON.parse(linea); } catch { continue; }
        if (msg.id && pend.has(msg.id)) { pend.get(msg.id)(msg); pend.delete(msg.id); }
      }
    });

    (async () => {
      await enviar("initialize", { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "sync-manifest", version: "1" } });
      notificar("notifications/initialized", {});
      const r = await enviar("tools/list", {});
      clearTimeout(timeout);
      p.kill();
      res((r.result && r.result.tools) || []);
    })().catch(e => { clearTimeout(timeout); p.kill(); rej(e); });
  });
}

// ── proyeccion: tools del modulo + contenido curado del manifiesto ───────────────────────────
function proyectarTools(toolsDelModulo, manifiestoActual) {
  const previas = new Map((manifiestoActual.tools || []).map(t => [t.name, t]));

  // EL ORDEN DEL MANIFIESTO SE PRESERVA, para que el diff muestre solo lo que de verdad cambio y
  // no un baraje de tools cada vez que se corre. Las que ya estaban salen en su orden anterior;
  // las nuevas, al final, en el orden en que el modulo las lista. Asi el generador es idempotente:
  // correrlo dos veces seguidas no produce un segundo diff.
  const ordenPrevio = [...previas.keys()];
  const ordenadas = [...toolsDelModulo].sort((a, b) => {
    const ia = ordenPrevio.indexOf(a.name), ib = ordenPrevio.indexOf(b.name);
    if (ia === -1 && ib === -1) return 0;          // dos nuevas: orden del modulo
    if (ia === -1) return 1;                        // a nueva -> despues
    if (ib === -1) return -1;                       // b nueva -> despues
    return ia - ib;                                 // ambas viejas: orden del manifiesto
  });

  return ordenadas.map(t => {
    const prev = previas.get(t.name);
    const props = (t.inputSchema && t.inputSchema.properties) || {};
    const requeridos = new Set((t.inputSchema && t.inputSchema.required) || Object.keys(props));

    // Los parametros salen en el orden de 'required' del modulo si esta, si no en el de props.
    const nombresParam = (t.inputSchema && t.inputSchema.required && t.inputSchema.required.length)
      ? t.inputSchema.required
      : Object.keys(props);

    const prevParams = new Map(((prev && prev.input) || []).map(p => [p.name, p]));

    const input = nombresParam.filter(n => props[n]).map(n => {
      const s = props[n];
      const pv = prevParams.get(n);
      const param = {
        name: n,
        type: s.type || "string",                                  // del modulo
        // description: preservar la curada; si no hay, la del modulo (schema del param)
        description: (pv && pv.description) || s.description || "",
      };
      if (Array.isArray(s.enum) && s.enum.length) param.enum = s.enum; // del modulo, gratis
      return param;
    });

    // Orden de claves CANONICO -name, title, description, readOnly, input- para que el generador
    // sea idempotente y los dos manifiestos queden con la misma forma. La description arriba
    // porque es lo que un humano lee primero.
    const tool = {
      name: t.name,                                                // del modulo
      title: (prev && prev.title) || "",                           // preservar
      // description: preservar la curada; para una tool NUEVA, la del modulo como punto de partida
      description: (prev && prev.description) || t.description || "",
      readOnly: prev ? prev.readOnly !== false : true,             // preservar, default true
      input,
    };
    return tool;
  });
}

function sincronizarUno(nombre, { escribir, dll }) {
  const cfg = dll ? { dll: resolve(dll) } : MODULOS[nombre];
  const manifestPath = join(modulosRoot, nombre, "manifest.json");
  if (!existsSync(cfg.dll))
    throw new Error(`no existe el DLL de ${nombre} (${cfg.dll}). Compilalo primero.`);
  if (!existsSync(manifestPath))
    throw new Error(`no existe ${manifestPath}. Este generador sincroniza el bloque tools de un manifiesto existente; el encabezado lo escribe el dueno.`);

  return pedirToolsList(cfg.dll).then(toolsModulo => {
    const actual = JSON.parse(readFileSync(manifestPath, "utf8"));
    const antes = JSON.stringify(actual.tools || []);
    const nuevo = { ...actual, tools: proyectarTools(toolsModulo, actual) };
    const despues = JSON.stringify(nuevo.tools);

    const cambio = antes !== despues;
    const nombresAntes = new Set((actual.tools || []).map(t => t.name));
    const nombresModulo = new Set(toolsModulo.map(t => t.name));
    const agregadas = [...nombresModulo].filter(n => !nombresAntes.has(n));
    const quitadas = [...nombresAntes].filter(n => !nombresModulo.has(n));

    if (cambio && escribir) writeFileSync(manifestPath, JSON.stringify(nuevo, null, 2) + "\n", "utf8");

    return { nombre, cambio, agregadas, quitadas, total: toolsModulo.length };
  });
}

// ── main ─────────────────────────────────────────────────────────────────────────────────────
const args = process.argv.slice(2);
const check = args.includes("--check");
// --dll=<ruta>: usar ESE dll en vez del que nombra installation.json. Para cuando el bin/Debug
// del modulo esta tomado por un proceso -los MCP de una sesion abierta- y se compilo a otra
// carpeta. Solo tiene sentido con un modulo nombrado.
const dllOverride = (args.find(a => a.startsWith("--dll=")) || "").slice("--dll=".length) || null;
const cuales = args.filter(a => !a.startsWith("--"));
const objetivo = cuales.length ? cuales : Object.keys(MODULOS);
if (dllOverride && cuales.length !== 1) { console.log("--dll= pide exactamente un modulo"); process.exit(1); }

let huboCambios = false;
for (const nombre of objetivo) {
  if (!MODULOS[nombre]) { console.log(`  ${nombre}: desconocido (no hay Modulos/${nombre}/installation.json con command dotnet)`); continue; }
  try {
    const r = await sincronizarUno(nombre, { escribir: !check, dll: dllOverride });
    const marca = r.cambio ? (check ? "CAMBIARIA" : "actualizado") : "al dia";
    let detalle = "";
    if (r.agregadas.length) detalle += ` +[${r.agregadas.join(", ")}]`;
    if (r.quitadas.length) detalle += ` -[${r.quitadas.join(", ")}]`;
    console.log(`  ${nombre}: ${marca} (${r.total} tools)${detalle}`);
    if (r.cambio) huboCambios = true;
  } catch (e) {
    console.log(`  ${nombre}: ERROR ${e.message}`);
    process.exitCode = 1;
  }
}

if (check && huboCambios) {
  console.log("\n--check: hay manifiestos desincronizados. Corre sin --check para actualizarlos.");
  process.exitCode = 1;
} else if (!check && huboCambios) {
  console.log("\nlisto. Revisa el diff: las tools NUEVAS traen la descripcion del codigo, que quizas quieras pulir.");
} else {
  console.log("\ntodos al dia.");
}
