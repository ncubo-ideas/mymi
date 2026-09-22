// EL GUARDIAN DE LAS CARAS HIBRIDAS. Cada negocio tiene DOS pantallas del mismo hecho -- la app
// suelta en Blazor (<X>.Wasm, que es la fuente del diseño) y la cara hibrida (<X>.Cara, html y js
// planos que corren dentro de Mymi) -- y NADA las ata. Se escribieron a mano el 2026-09-14, las
// apps se localizaron el 16 y el 17, y el 18 solo una de las cinco hablaba ingles adentro de
// Mymi. Cuatro dias bastaron.
//
//   node Herramientas/revisar-las-caras.mjs              <- las cinco
//   node Herramientas/revisar-las-caras.mjs compras      <- una o varias
//   node Herramientas/revisar-las-caras.mjs --silencio   <- solo los hallazgos
//
// Sale con codigo 1 si encuentra algo, para que sirva de compuerta.
//
// QUE REVISA, y son las tres formas en que se desincronizaron de verdad:
//
//   1. LA HOJA. Que css/app.css de la cara sea IDENTICA a la de su app -- ahi es donde viven el
//      color, la tipografia y el layout de la institucion -- y que el html la enlace. Si son
//      distintas, alguien entresaco reglas a mano y eligio como se ve ella.
//
//   2. LAS CLASES SIN REGLA. Es el modo mudo de verse mal: la cara escribe una clase que ninguna
//      hoja define y el bloque sale sin caja, sin color o sin espacio, SIN un solo error en
//      consola. Paso dos veces: `inicio-atencion-detalle` en Delfos dejaba la caja de lectura
//      pelada, y `apunte` -- que escribe el puente del Host en las CINCO caras -- no estaba en
//      tres hojas, asi que un fallo se veia igual que una nota.
//
//   3. LOS CATALOGOS. Que los dos idiomas tengan las mismas claves, que ninguna clave que la cara
//      usa falte, y que los valores que comparte con la app digan EXACTAMENTE lo mismo. Una cara
//      con su propia redaccion es una segunda institucion.
//
// QUE NO PUEDE REVISAR, y conviene saberlo: si la cara lee un campo que la superficie no
// serializa. Eso solo se ve corriendo (`h.stays.stays`, `h.plate.plate`, `x.cuando` pintaban
// vacio para siempre). Al tocar una cara, leer el record de Vistas/, no lo que parece razonable.

import { readFileSync, readdirSync, existsSync, statSync } from "node:fs";
import { createHash } from "node:crypto";
import { relative, join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { createContext, runInContext } from "node:vm";

const raiz = join(dirname(fileURLToPath(import.meta.url)), "..");

// UN DIRECTORIO POR NEGOCIO, CINCO NODOS: la fila dice donde viven los dos lados y como nombra
// sus cosas esta cara. Concesionario esta en ingles entero -- es la convencion del repo para ese
// modulo -- asi que su catalogo, su atributo y su global cambian de nombre, no de forma.
const modulos = {
  compras: {
    marca: "La Guaria",
    cara: "Compras/App/Compras.Cara",
    app: "Compras/App/Compras.Wasm/wwwroot",
    textos: "texto", global: "TextoDeLaCara", atributo: "data-texto", llamada: "I", verbos: ["texto", "con"],
  },
  cargas: {
    marca: "Faro",
    cara: "Cargas/App/Cargas.Cara",
    app: "Cargas/App/Cargas.Wasm/wwwroot",
    textos: "texto", global: "TextoDeLaCara", atributo: "data-texto", llamada: "I", verbos: ["texto", "con"],
  },
  examenes: {
    marca: "Delfos",
    cara: "Examenes/App/Examenes.Cara",
    app: "Examenes/App/Examenes.Wasm/wwwroot",
    // Delfos se localizo el 2026-09-18 (PR 10107, Kenneth), asi que ya hay catalogo que
    // comparar. Nombra su carpeta y su global como los otros tres en espanol.
    textos: 'texto', global: 'TextoDeLaCara', atributo: 'data-texto', llamada: 'I', verbos: ['texto', 'con'],
  },
  municipalidad: {
    marca: "Monteverde",
    cara: "Municipalidad/App/Municipalidad.Cara",
    app: "Municipalidad/App/Municipalidad.Wasm/wwwroot",
    textos: "texto", global: "TextoDeLaCara", atributo: "data-texto", llamada: "I", verbos: ["texto", "con"],
  },
  dealership: {
    marca: "Sotavento",
    cara: "Concesionario/App/Concesionario.Cara",
    app: "Concesionario/App/Concesionario.Wasm/wwwroot",
    textos: "text", global: "FaceText", atributo: "data-text", llamada: "L", verbos: ["text", "fill"],
  },
};

// CLASES QUE NADIE DEFINE Y ESTA BIEN ASI. Cada una con su razon, porque una excepcion sin motivo
// es una regla apagada.
const perdonadas = {
  // La app suelta usa esta misma clase y tampoco la define: el svg trae su tamaño en atributos.
  // Mientras las dos se vean igual, no hay desincronizacion que reportar.
  "sotavento-mark": "la app suelta la escribe igual y tampoco la define",
};

const md5 = (p) => createHash("md5").update(readFileSync(p)).digest("hex");

function archivos(dir) {
  if (!existsSync(dir)) return [];
  return readdirSync(dir, { recursive: true, withFileTypes: true })
    .filter((e) => e.isFile())
    .map((e) => relative(dir, join(e.parentPath ?? e.path, e.name)).replaceAll("\\", "/"));
}

const aplanar = (o, prefijo = "") =>
  Object.entries(o).flatMap(([k, v]) =>
    k.startsWith("//") ? [] : (v && typeof v === "object" ? aplanar(v, prefijo + k + ".") : [[prefijo + k, v]]));

// ---------------------------------------------------------------------- las tres revisiones

function laHoja(m, decir, fallar) {
  const cara = join(raiz, m.cara), app = join(raiz, m.app);
  const suya = join(cara, "css/app.css"), deElla = join(app, "css/app.css");

  if (!existsSync(suya)) return fallar("la cara no trae css/app.css");
  if (!existsSync(deElla)) return decir("la app no tiene css/app.css: no hay contra que comparar");

  if (md5(suya) !== md5(deElla))
    fallar(`css/app.css NO es la de la app (${(statSync(suya).size / 1024) | 0} KB contra ` +
           `${(statSync(deElla).size / 1024) | 0} KB). Entresacar reglas a mano es decidir como se ve ella.`);
  else decir(`css/app.css identica a la de la app (${(statSync(suya).size / 1024) | 0} KB)`);

  const html = readFileSync(join(cara, "index.html"), "utf8");
  const enlazadas = [...html.matchAll(/<link[^>]+href="([^"]+\.css)"/g)].map((x) => x[1].replace(/^\.?\//, ""));
  const propias = archivos(cara).filter((f) => f.endsWith(".css"));
  const sueltas = propias.filter((f) => !enlazadas.some((e) => e.endsWith(f)));

  if (sueltas.length) fallar(`hojas que viajan y el html no enlaza: ${sueltas.join(", ")}`);
  else decir(`hojas enlazadas: ${enlazadas.join(", ")}`);
}

function lasClases(m, decir, fallar) {
  const cara = join(raiz, m.cara);
  const css = archivos(cara).filter((f) => f.endsWith(".css"))
    .map((f) => readFileSync(join(cara, f), "utf8")).join("\n");
  const definidas = new Set([...css.matchAll(/\.(-?[_a-zA-Z][\w-]*)/g)].map((x) => x[1]));

  const fuentes = ["index.html", "cara.js"].filter((f) => existsSync(join(cara, f)))
    .map((f) => readFileSync(join(cara, f), "utf8"));

  // Solo los nombres FIJOS se pueden juzgar. Lo que la cara arma en runtime -- «'sello--' + codigo»
  // -- se junta como prefijo y se comprueba que la hoja defina al menos una variante.
  //
  // EL CORTE ES POR ATRIBUTO ENTERO, no por palabra: en el js un `class="..."` casi siempre se
  // cierra con una comilla para concatenar (`'class="' + clase + '"'`), y partir eso en palabras
  // dejaba `clase` y `tono` -- nombres de VARIABLES -- como si fueran clases sin regla. Un
  // atributo con comilla, parentesis o suma adentro es dinamico entero: de ahi solo se sacan
  // prefijos, nunca nombres.
  // Un `class="..."` escrito en el js viene con la forma `literal' + expresion + 'literal`, asi
  // que se parte por la comilla simple y solo los tramos PARES son texto escrito; los impares son
  // codigo. Sin esto, `'class="' + clase + '"'` reportaba `clase` -- el nombre de una variable --
  // como una clase sin regla, que es ruido que apaga el guardian.
  const nombre = /^[a-zA-Z][\w-]*$/;
  const usadas = new Set(), prefijos = new Set();
  for (const fuente of fuentes)
    for (const x of fuente.matchAll(/class="([^"]*)"/g)) {
      const tramos = x[1].split("'");
      for (let i = 0; i < tramos.length; i++) {
        const esCodigo = i % 2 === 1;
        for (const c of tramos[i].split(/\s+/)) {
          if (!c) continue;
          const p = c.match(/^([a-zA-Z][\w-]*--)$/);
          if (p) { prefijos.add(p[1]); continue; }          // «sello--» pegado a lo que viene
          if (!esCodigo && nombre.test(c)) usadas.add(c);
        }
      }
    }

  // UN SUFIJO NO ES UNA CLASE. `'sello--' + (x ? 'nada-sin-comprobante' : 'sin-consulta')` deja
  // esos dos textos sueltos, y son la COLA de un modificador, no nombres por su cuenta: si
  // pegados al prefijo la hoja los define, estan bien. Se resuelve con la hoja, no adivinando.
  const esSufijo = (c) => [...prefijos].some((p) => definidas.has(p + c));

  const huerfanas = [...usadas]
    .filter((c) => !definidas.has(c) && !esSufijo(c) && !(c in perdonadas)).sort();
  const perdonadasVistas = [...usadas].filter((c) => c in perdonadas);

  if (huerfanas.length)
    fallar(`clases que la cara escribe y ninguna hoja define: ${huerfanas.join(", ")}. ` +
           `Se ven sin caja, sin color o sin espacio, y nadie avisa.`);
  else decir(`${usadas.size} clases con nombre fijo, todas con regla`);

  for (const c of perdonadasVistas) decir(`  (${c}: sin regla a proposito, ${perdonadas[c]})`);

  const sinVariante = [...prefijos].filter((p) => ![...definidas].some((d) => d.startsWith(p)));
  if (sinVariante.length)
    fallar(`modificadores que la cara arma en runtime y la hoja no define en ninguna variante: ${sinVariante.join(", ")}`);
  else if (prefijos.size) decir(`${prefijos.size} modificadores armados en runtime, con variantes en la hoja`);
}

function losCatalogos(m, decir, fallar) {
  if (!m.textos) return decir("sin catalogo: su app suelta tampoco esta localizada");

  const cara = join(raiz, m.cara), dir = join(cara, m.textos);
  const caja = {}; caja.window = caja; createContext(caja);
  for (const etiqueta of ["es-CR", "en"]) {
    const p = join(dir, etiqueta + ".js");
    if (!existsSync(p)) return fallar(`falta ${m.textos}/${etiqueta}.js: la cara no puede hablar ese idioma`);
    runInContext(readFileSync(p, "utf8"), caja);
  }
  const catalogo = caja[m.global];
  if (!catalogo) return fallar(`los catalogos no publicaron window.${m.global}`);

  // 1 · los dos idiomas dicen las mismas cosas
  const es = aplanar(catalogo["es-CR"] ?? {}).map(([k]) => k);
  const en = aplanar(catalogo["en"] ?? {}).map(([k]) => k);
  const soloEs = es.filter((k) => !en.includes(k)), soloEn = en.filter((k) => !es.includes(k));
  if (soloEs.length || soloEn.length)
    fallar(`catalogos asimetricos` +
      (soloEs.length ? `; solo en es-CR: ${soloEs.join(", ")}` : "") +
      (soloEn.length ? `; solo en en: ${soloEn.join(", ")}` : ""));
  else decir(`catalogos simetricos (${es.length} claves)`);

  // 2 · ninguna clave que la cara pide se quedo sin escribir
  const js = existsSync(join(cara, "cara.js")) ? readFileSync(join(cara, "cara.js"), "utf8") : "";
  const html = readFileSync(join(cara, "index.html"), "utf8");
  const verbos = m.verbos.join("|");
  const pedidas = new Set(), construidas = [];
  for (const x of js.matchAll(new RegExp(`\\b${m.llamada}\\.(?:${verbos})\\(\\s*'([^']+)'\\s*(\\+?)`, "g")))
    (x[2] ? construidas : pedidas).add?.(x[1]) ?? construidas.push(x[1]);
  for (const x of html.matchAll(new RegExp(`${m.atributo}(?:-[a-z]+)?="([^"]+)"`, "g"))) pedidas.add(x[1]);

  const faltantes = [...pedidas].filter((k) =>
    !es.includes(k) && !construidas.some((p) => k.startsWith(p)) && !k.endsWith("."));
  if (faltantes.length) fallar(`claves que la cara pide y el catalogo no tiene: ${faltantes.join(", ")}`);
  else decir(`${pedidas.size} claves pedidas, todas en el catalogo`);

  // 3 · lo que comparte con la app dice lo mismo, palabra por palabra
  for (const etiqueta of ["es-CR", "en"]) {
    const deElla = join(raiz, m.app, m.textos, etiqueta + ".json");
    if (!existsSync(deElla)) continue;
    const suyas = Object.fromEntries(aplanar(JSON.parse(readFileSync(deElla, "utf8"))));
    const distintas = aplanar(catalogo[etiqueta])
      .filter(([k, v]) => k in suyas && suyas[k] !== v)
      .map(([k, v]) => `${k} (app: ${JSON.stringify(suyas[k])} / cara: ${JSON.stringify(v)})`);
    const compartidas = aplanar(catalogo[etiqueta]).filter(([k]) => k in suyas).length;

    if (distintas.length)
      fallar(`${etiqueta}: la cara y la app dicen distinto en ${distintas.length}: ${distintas.join("; ")}`);
    else decir(`${etiqueta}: ${compartidas} claves compartidas con la app, todas iguales`);
  }
}

// ---------------------------------------------------------------------- correr

const args = process.argv.slice(2);
const silencio = args.includes("--silencio");
const cuales = args.filter((a) => !a.startsWith("--"));
const desconocidos = cuales.filter((c) => !(c in modulos));
if (desconocidos.length) {
  console.log(`no conozco: ${desconocidos.join(", ")}. Los que hay: ${Object.keys(modulos).join(", ")}`);
  process.exit(1);
}

let hallazgos = 0;
for (const nombre of (cuales.length ? cuales : Object.keys(modulos))) {
  const m = modulos[nombre];
  const lineas = [];
  const decir = (t) => lineas.push(`   · ${t}`);
  const fallar = (t) => { hallazgos++; lineas.push(`   ✗ ${t}`); };

  laHoja(m, decir, fallar);
  lasClases(m, decir, fallar);
  losCatalogos(m, decir, fallar);

  const suyos = lineas.filter((l) => l.startsWith("   ✗"));
  if (silencio && !suyos.length) continue;
  console.log(`\n${m.marca}  (${nombre})`);
  for (const l of (silencio ? suyos : lineas)) console.log(l);
}

console.log(hallazgos === 0
  ? `\nTODO OK  (las caras dicen lo que dicen sus apps)`
  : `\n${hallazgos} hallazgo(s). Una cara que se desvia de su app es una segunda institucion.`);

process.exit(hallazgos === 0 ? 0 : 1);
