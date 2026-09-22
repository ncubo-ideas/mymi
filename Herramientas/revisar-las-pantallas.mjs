// EL GUARDIAN DE LAS PANTALLAS. Es el otro lado de `revisar-las-caras.mjs`: aquel mira la cara
// hibrida -- html y js planos que corren dentro de Mymi -- y este mira la app suelta en Blazor, que
// es donde viven los .razor y donde se decide el diseño.
//
//   node Herramientas/revisar-las-pantallas.mjs              <- las siete superficies
//   node Herramientas/revisar-las-pantallas.mjs compras      <- una o varias
//   node Herramientas/revisar-las-pantallas.mjs --silencio   <- solo los hallazgos
//   node Herramientas/revisar-las-pantallas.mjs --notas      <- ademas, lo perdonado y lo que no juzga
//
// Sale con codigo 1 si encuentra algo, para que sirva de compuerta.
//
// POR QUE EXISTE. `localizar-una-caratula.md` lo dice en su trampa 1, y costo 31 rotulos: la suite
// comprobaba que toda clave USADA estuviera en el catalogo y nunca lo contrario, que todo LITERAL
// estuviera cableado. Por ese hueco se descubrio un iPhone en ingles con Ajustes entero en espanol.
// Y su trampa 2: buscar tildes o palabras españolas no alcanza -- no vio «Proveedor», «Servidor» ni
// «Guardar». La regla tiene que ser ESTRUCTURAL: ningun nodo de texto del marcado trae letras, y
// todo lo visible entra por el catalogo o por un dato.
//
// QUE REVISA, y son las cuatro formas en que se rompio de verdad:
//
//   1. LOS DOS CATALOGOS DICEN LAS MISMAS COSAS. Una clave que existe en un idioma y no en el otro
//      sale entre llaves en media app y nadie avisa.
//
//   2. TODA CLAVE QUE EL CODIGO PIDE ESTA ESCRITA. Es la direccion que la suite ya cubria, y aca se
//      cubre sobre .razor Y .cs a la vez. Asi se encontro `tranquilo.mediciones`, que faltaba en los
//      dos idiomas y pintaba «{tranquilo.mediciones}» en cinco lugares de Delfos.
//
//   3. NINGUN LITERAL SE QUEDO SIN CABLEAR. La otra direccion, la de la trampa 1: un nodo de texto
//      del marcado, o un atributo que una persona lee -- title, alt, placeholder, aria-label --, con
//      letras adentro y sin pasar por el catalogo.
//
//   4. NINGUNA CLAVE DICE LO MISMO EN LOS DOS IDIOMAS sin decir por que. A veces es correcto -- «VIN»
//      es «VIN» -- y entonces se anota; sin anotar, es una traduccion que nadie hizo. El catalogo del
//      Host ya usa esa forma: `inicio.lema` lleva una llave "igual" que explica que el lema de la
//      marca no se traduce. Esto sale como nota, no como hallazgo.
//
// QUE NO PUEDE REVISAR, y conviene saberlo:
//
//   - UNA CLAVE ARMADA EN RUNTIME. `textos["fallo.paso." + paso]` se ve como el prefijo y nada mas:
//     que el catalogo tenga TODOS los valores que `paso` puede tomar hay que leerlo a mano. Ahi vivia
//     el otro defecto de Faro -- dos llamadas pasaban «abrir la app» con espacios contra una clave
//     con guiones -- y ninguna comprobacion estatica lo ve.
//   - SI EL VALOR TRADUCIDO DICE LO QUE DEBE. Que `en.json` tenga la clave no quiere decir que este
//     en ingles: `misFacturas.refresh.boton` decia «Refresh» en el catalogo ESPAÑOL.
//   - EL TEXTO QUE VIVE FUERA DEL .razor. El aviso de fallo de Blazor se escribe a mano en
//     index.html y se reemplaza por JS al arrancar; esto no lo alcanza.

import { readFileSync, existsSync, readdirSync } from "node:fs";
import { join, relative, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const raiz = join(dirname(fileURLToPath(import.meta.url)), "..");

// UNA FILA POR SUPERFICIE DE TEXTO. Los modulos traen su catalogo por HTTP desde wwwroot, uno por
// idioma; el Host lo trae embebido y con la forma al reves -- la clave primero y los dos idiomas
// adentro -- porque lo comparten el escritorio y los dos telefonos.
// SON DOS CATALOGOS POR NEGOCIO Y NO UNO, y esa es la forma adoptada: las palabras de la PANTALLA
// se piden por HTTP desde wwwroot, y las frases que compone el DOMINIO las arma la puerta en C#,
// antes de que exista una pantalla -- las necesita tambien el MCP, que no tiene navegador. Por eso
// `frases` es una fila aparte y no un directorio mas: es otro catalogo, con otro dueño y otro
// momento. Facturas no la tiene y no es un olvido: su dominio no compone ni una frase.
const superficies = {
  municipalidad: { marca: "Monteverde", dir: "Municipalidad/App/Municipalidad.Wasm", catalogo: "wwwroot/texto", idiomas: ["es-CR", "en"],
                   frases: { dir: "Municipalidad/App/Municipalidad.App/Frases", es: "CatalogoEnEspanol.cs", en: "CatalogoEnIngles.cs", pide: "Diga" } },
  cargas:        { marca: "Faro",       dir: "Cargas/App/Cargas.Wasm",               catalogo: "wwwroot/texto", idiomas: ["es-CR", "en"],
                   frases: { dir: "Cargas/App/Cargas.App/Frases", es: "CatalogoEnEspanol.cs", en: "CatalogoEnIngles.cs", pide: "Diga" } },
  compras:       { marca: "La Guaria",  dir: "Compras/App/Compras.Wasm",             catalogo: "wwwroot/texto", idiomas: ["es-CR", "en"],
                   frases: { dir: "Compras/App/Compras.App/Frases", es: "CatalogoEnEspanol.cs", en: "CatalogoEnIngles.cs", pide: "Diga" } },
  dealership:    { marca: "Sotavento", dir: "Concesionario/App/Concesionario.Wasm", catalogo: "wwwroot/text",  idiomas: ["es-CR", "en"],
                   frases: { dir: "Concesionario/App/Concesionario.App/Wording", es: "TheSpanishCatalogue.cs", en: "TheEnglishCatalogue.cs", pide: "Say" } },
  examenes:      { marca: "Delfos",     dir: "Examenes/App/Examenes.Wasm",           catalogo: "wwwroot/texto", idiomas: ["es-CR", "en"],
                   frases: { dir: "Examenes/App/Examenes.App/Frases", es: "CatalogoEnEspanol.cs", en: "CatalogoEnIngles.cs", pide: "Diga" } },
  facturas:      { marca: "Facturas",   dir: "HolaMundo/Facturas/App/Facturas.Wasm", catalogo: "wwwroot/texto", idiomas: ["es-CR", "en"] },
  escritorio:    { marca: "Mymi",       dir: "Gnothi.Desktop", archivo: "Gnothi.Host/textos.json", idiomas: ["es", "en"] },
};

// LITERALES QUE NO SE TRADUCEN Y ESTA BIEN ASI. Cada uno con su razon, porque una excepcion sin
// motivo es una regla apagada. La llave es «ruta|texto»: a proposito, para que perdonar «L» en una
// pantalla no perdone «L» en las demas.
const perdonadas = {
  // VOZ DE LA INSTITUCION (regla 4). El nombre y el lema los DECLARA ella en su manifiesto; el Host
  // -- y esta carátula -- no le ponen palabras en la boca.
  "Compras/App/Compras.Wasm/Layout/MainLayout.razor|Supermercado La Guaria": "nombre de la institucion",
  "Compras/App/Compras.Wasm/Layout/MainLayout.razor|[atributo aria-label] Supermercado La Guaria": "nombre de la institucion",
  "Compras/App/Compras.Wasm/Layout/MainLayout.razor|La Guaria": "nombre corto de la institucion",
  "Compras/App/Compras.Wasm/Layout/MainLayout.razor|Su barrio, su despensa, desde 1992": "el lema de la institucion",
  "Compras/App/Compras.Wasm/Pages/MisCompras.razor|La Guaria": "nombre corto de la institucion",
  "Concesionario/App/Concesionario.Wasm/Layout/SotaventoMark.razor|[atributo aria-label] Sotavento Motors": "nombre de la institucion",
  "Gnothi.Desktop/Components/Root.razor|Mymi": "el nombre del producto, como el lema: no se traduce",

  // NOMBRES PROPIOS DE TERCEROS.
  "Compras/App/Compras.Wasm/Layout/MainLayout.razor|WhatsApp": "nombre de un producto ajeno",
  "Concesionario/App/Concesionario.Wasm/Sections/Inicio.razor|WhatsApp": "nombre de un producto ajeno",

  // SIMBOLOS DE UNIDAD. Se escriben igual en los dos idiomas, y el valor que acompañan viaja en esa
  // unidad por contrato: en Faro los campos que se llaman Kilometros SON kilometros, y los que la
  // persona eligio se llaman ComoSeVen y traen su simbolo al lado.
  "Cargas/App/Cargas.Wasm/Pages/Tablero.razor|km": "simbolo de unidad, igual en los dos idiomas",
  "Cargas/App/Cargas.Wasm/Pages/Tablero.razor|~ km": "simbolo de unidad con su marca de estimado",
  "Cargas/App/Cargas.Wasm/Pages/Tablero.razor|L": "simbolo de unidad, igual en los dos idiomas",
  "Cargas/App/Cargas.Wasm/Pages/Ruta.razor|L": "simbolo de unidad, igual en los dos idiomas",
  "Cargas/App/Cargas.Wasm/Pages/Ruta.razor|/L": "simbolo de unidad, igual en los dos idiomas",

  // MARCAS DE UN DIBUJO. La trampa 5 de la guia: la decoracion no va en el catalogo. Son letras
  // pintadas sobre un grafico, no palabras que alguien lea como frase.
  "Cargas/App/Cargas.Wasm/Tablero/MedidorDeAutonomia.razor|E": "marca del medidor, como la de un tablero de carro",
  "Cargas/App/Cargas.Wasm/Tablero/MedidorDeAutonomia.razor|F": "marca del medidor, como la de un tablero de carro",
  "Examenes/App/Examenes.Wasm/Pages/MisExamenes.razor|i": "el icono de informacion, y va con aria-hidden",
  "Gnothi.Desktop/Components/Alta.razor|· v": "el prefijo de un numero de version, no una palabra",
  "Compras/App/Compras.Wasm/Layout/MainLayout.razor|demo": "sello de la app de muestra, igual en los dos idiomas",

  // FORMA DE UN DATO, no texto.
  "Gnothi.Desktop/Components/Ajustes.razor|[atributo placeholder] sk-…": "el prefijo con que empieza una llave, no una frase",
  "Municipalidad/App/Municipalidad.Wasm/Pages/ElArranque.razor|//events": "el tramo final de una URL que se muestra entera",
};

// CLAVES QUE DICEN LO MISMO EN LOS DOS IDIOMAS A PROPOSITO. La llave es «superficie|clave». El
// catalogo del Host no necesita fila aca: anota la coincidencia adentro, con una llave que no es un
// idioma -- `inicio.lema` lleva "igual": "el lema de la marca no se traduce" -- y esa es la mejor
// forma, porque viaja con el valor. Los catalogos de los modulos guardan un string pelado y no
// tienen donde anotarlo, asi que se declara aca.
const igualesAProposito = {
  "municipalidad|arranque.feed": "Feed es el termino tecnico, igual en los dos idiomas",
  "cargas|comun.no": "no se escribe igual en los dos idiomas",
  "compras|comun.am": "a.m. es la abreviatura latina, igual en los dos idiomas",
  "compras|comun.pm": "p.m. es la abreviatura latina, igual en los dos idiomas",
  "compras|inicio.versus": "vs es la abreviatura latina, igual en los dos idiomas",
  "examenes|detalle.tranquilo": "un hueco y un punto: no hay nada que traducir",
  "dealership|vehicle.rowVin": "VIN es la sigla internacional",
  "dealership|vehicle.tabManual": "Manual se escribe igual en los dos idiomas",
  "examenes|tranquilo.nota": "solo huecos y un separador: no hay nada que traducir",
  "examenes|detalle.conReferencia": "solo un hueco y un separador",
  "facturas|misFacturas.resumen.total": "total se escribe igual en los dos idiomas",
  "facturas|misFacturas.columna.total": "Total se escribe igual en los dos idiomas",
  "facturas|detalle.columna.total": "Total se escribe igual en los dos idiomas",
  "facturas|detalle.columna.sku": "SKU es la sigla, igual en los dos idiomas",
};

// LO MISMO, PARA EL CATALOGO DE FRASES DEL DOMINIO. Va en su propia tabla y no en la de arriba
// porque un negocio tiene DOS catalogos: la misma clave puede existir en los dos con dueños
// distintos, y mezclarlos haria que perdonarla en uno la perdonara en el otro.
const igualesEnLasFrases = {
  // Solo huecos y separadores: no hay palabra que traducir.
  "cargas|accion.marco": "solo dos huecos",
  "cargas|tramo.calculado": "huecos y simbolos de unidad",
  "cargas|kilometraje.conOrigen": "un hueco y su origen entre parentesis",
  "cargas|tanque.conOrigen": "un hueco y su origen entre parentesis",
  "compras|hallazgos.que.uno": "un hueco entre comillas",
  "compras|consumo.que.categoria": "solo un hueco",
  "compras|consumo.que.texto": "un hueco entre comillas",
  "compras|momento.fila": "dos huecos y un separador",
  "compras|presentacion.pieza": "un hueco y su simbolo de unidad",
  "examenes|lista.parte": "dos huecos",
  "examenes|lista.colesterol-detallado": "siglas internacionales y dos huecos",
  "examenes|ano.sin-comparacion": "dos huecos y un separador",
  "examenes|banda.expresada": "huecos, un guion y un separador",
  "examenes|control.cada.anos": "solo un hueco",
  "examenes|control.cada.meses": "solo un hueco",

  "dealership|day.short": "tres huecos",
  "dealership|quantity.distance": "un hueco y su simbolo de unidad",
  "dealership|clauses.two": "dos huecos y un separador",
  "dealership|countdown.both": "dos huecos y un separador",
  "dealership|reason.Ended": "un hueco y un punto",
  "dealership|envelope.reason.Settled": "un hueco y un punto",
  "dealership|envelope.clause.TheEnvelopeAlone": "solo un hueco",
  "dealership|envelope.remainder": "tres huecos",
  "dealership|sheet.bothHalves": "dos huecos",
  "dealership|sheet.subject.Fluid": "solo un hueco",
  "dealership|fuel.grade": "un hueco y otro entre parentesis",
  "dealership|coverage.notPublished": "dos huecos",
  "dealership|coverage.settled": "huecos y separadores",
  "dealership|coverage.clause": "dos huecos",
  "dealership|coverage.clause.withExclusions": "tres huecos",
  "dealership|coverage.clause.reasonless": "dos huecos",
  "dealership|money.Named": "un simbolo y un hueco",
  "dealership|money.NotNamed": "un hueco y el codigo de la moneda",
  "dealership|price.NotAsked": "solo un hueco",
  "dealership|completion.said": "dos huecos y un separador",
  "dealership|completion.reading.WithAReading": "solo un hueco",

  // Palabras que se escriben igual en los dos idiomas.
  "dealership|board.operation.short.SoftwareUpdate": "Software se escribe igual en los dos idiomas",
  "cargas|etiqueta.base": "base se escribe igual en los dos idiomas",
  "compras|categoria.Snacks.titulo": "snacks se escribe igual en los dos idiomas",
  "examenes|categoria.chocolate": "chocolate se escribe igual en los dos idiomas",
  "examenes|corto.color-de-la-orina": "color se escribe igual en los dos idiomas",
  "examenes|corto.color-de-las-heces": "color se escribe igual en los dos idiomas",
};

// ---------------------------------------------------------------------- utilidades

// Un tramo borrado conserva sus saltos de linea, asi los numeros de linea nunca se corren.
const blanco = (x) => x.replace(/[^\n]/g, " ");

const aplanar = (o, prefijo = "") =>
  Object.entries(o).flatMap(([k, v]) =>
    k.startsWith("//") ? [] : (v && typeof v === "object" ? aplanar(v, prefijo + k + ".") : [[prefijo + k, v]]));

// SE LEE SIN \r. El repo esta en CRLF y un `$` de fin de linea no cruza el retorno de carro: sin
// esto, todo filtro anclado al final de linea falla y devuelve C# como si fuera texto de la persona.
const leer = (p) => readFileSync(p, "utf8").replace(/\r\n/g, "\n");

function archivos(dir, extension) {
  if (!existsSync(dir)) return [];
  return readdirSync(dir, { recursive: true, withFileTypes: true })
    .filter((e) => e.isFile() && e.name.endsWith(extension))
    .map((e) => join(e.parentPath ?? e.path, e.name))
    .filter((p) => !/[\\/](obj|bin|publicado)[\\/]/.test(p));
}

const ruta = (f) => relative(raiz, f).replaceAll("\\", "/");

function saltarGrupo(s, k, abre, cierra) {
  let d = 0;
  for (; k < s.length; k++) {
    if (s[k] === abre) d++;
    else if (s[k] === cierra) { d--; if (d === 0) return k + 1; }
  }
  return s.length;
}

const borrar = (s, a, b) => s.slice(0, a) + blanco(s.slice(a, b)) + s.slice(b);

// ---------------------------------------------------------------------- el marcado

// BORRA TODO LO QUE ES CODIGO y deja solo marcado con sus `@expr`. Se hace ANTES de partir por
// etiquetas, y ese orden es el arreglo: un `@if (x < y)` o un `limite > piso2` traen `<` y `>` que
// el partidor lee como etiquetas, y entonces el C# llega en pedazos y sin su `@` delante.
function borrarElCodigo(s) {
  for (const palabra of ["@code", "@functions"]) {
    let i = 0;
    while ((i = s.indexOf(palabra, i)) >= 0) {
      const llave = s.indexOf("{", i);
      if (llave < 0) break;
      s = borrar(s, i, saltarGrupo(s, llave, "{", "}"));
      i += palabra.length;
    }
  }

  for (const [abre, cierra] of [["{", "}"], ["(", ")"]]) {
    let i = 0;
    while ((i = s.indexOf("@" + abre, i)) >= 0) s = borrar(s, i, saltarGrupo(s, i + 1, abre, cierra));
  }

  // La cabecera de un bloque de control con su condicion. Las llaves del CUERPO se quedan: ahi
  // adentro vive el marcado.
  const control = /(?:@)?(?<![\w.])(if|else\s+if|foreach|for|while|switch|lock|using|catch|do|else|try|finally)(?![\w])/g;
  let m;
  while ((m = control.exec(s)) !== null) {
    let k = m.index + m[0].length;
    while (k < s.length && /\s/.test(s[k])) k++;
    const fin = s[k] === "(" ? saltarGrupo(s, k, "(", ")") : m.index + m[0].length;
    s = borrar(s, m.index, fin);
    control.lastIndex = fin;
  }

  // SENTENCIAS SUELTAS A NIVEL DE MARCADO, que es donde Razor las admite adentro del cuerpo de un
  // control. Se consumen HASTA SU `;`, aunque cruce lineas: un ternario repartido en tres renglones
  // es una sola sentencia y cortarla por linea deja `: 0m` suelto pareciendo texto.
  const sentencia =
    /^[ \t]*(?:case\s+[^\n:]*:|default:|break;|continue;|(?:return|var|string|int|long|decimal|double|bool|object|dynamic|[A-Z][\w.<>?\[\],]*)\s[^\n;]*;|\w[\w.]*\s*(?:\+\+|--|=|\+=|-=|\()[^;]*;)[ \t]*$/gm;
  s = s.replace(sentencia, blanco);

  // La misma sentencia, cuando su `;` quedo en otra linea.
  const multilinea = /^[ \t]*(?:(?:return|var|string|int|long|decimal|double|bool|object|dynamic|[A-Z][\w.<>?\[\],]*)\s+\w+\s*=|\w[\w.]*\s*(?:\+\+|--|\+=|-=))[^;]*;/gm;
  s = s.replace(multilinea, blanco);

  // `continue;` y `break;` en cualquier lugar de la linea: `if (x) { continue; }` cabe entero en un
  // renglon y entonces no empieza donde los de arriba lo buscan. Ningun marcado dice `continue;`.
  s = s.replace(/\b(?:continue|break)\s*;/g, blanco);

  return s;
}

// LOS NODOS DE TEXTO del marcado, con la linea en que empieza cada uno.
function nodosDeTexto(fuente) {
  let s = fuente;
  s = s.replace(/@\*[\s\S]*?\*@/g, blanco);
  s = s.replace(/<script[\s\S]*?<\/script>/gi, blanco);
  s = s.replace(/<style[\s\S]*?<\/style>/gi, blanco);
  s = s.replace(/^\s*@(page|inject|using|inherits|implements|attribute|layout|namespace|typeparam|rendermode|preservewhitespace)\b.*$/gim, blanco);
  s = borrarElCodigo(s);

  const nodos = [];
  let linea = 1, i = 0, dentro = false, buf = "", lineaDelBuf = 1;
  while (i < s.length) {
    const c = s[i];
    if (c === "\n") linea++;
    if (!dentro && c === "<" && /[a-zA-Z/!]/.test(s[i + 1] ?? "")) {
      if (buf.trim()) nodos.push([lineaDelBuf, buf]);
      buf = ""; dentro = true; i++; continue;
    }
    if (dentro) {
      if (c === '"') {
        const j = s.indexOf('"', i + 1);
        if (j > 0) { for (const ch of s.slice(i, j)) if (ch === "\n") linea++; i = j + 1; continue; }
      }
      if (c === ">") { dentro = false; lineaDelBuf = linea; }
      i++; continue;
    }
    buf += c; i++;
  }
  if (buf.trim()) nodos.push([lineaDelBuf, buf]);
  return nodos;
}

// LO QUE QUEDA DE UN NODO cuando se le sacan las expresiones de Razor: eso es literal.
function loLiteralDe(t) {
  let s = t;
  s = s.replace(/@@/g, " ");
  s = s.replace(/&(#\d+|#x[0-9a-fA-F]+|[a-zA-Z]+);?/g, " ");

  for (const [abre, cierra] of [["(", ")"], ["{", "}"]]) {
    let out = "", i = 0;
    while (i < s.length) {
      const j = s.indexOf("@" + abre, i);
      if (j < 0) { out += s.slice(i); break; }
      out += s.slice(i, j);
      i = saltarGrupo(s, j + 1, abre, cierra);
    }
    s = out;
  }

  // `@expr.cadena["clave"].Llamada(...)`: se come la cadena de miembros con sus indices y argumentos.
  let out = "", i = 0;
  while (i < s.length) {
    const j = s.indexOf("@", i);
    if (j < 0) { out += s.slice(i); break; }
    out += s.slice(i, j);
    let k = j + 1;
    if (!/[a-zA-Z_]/.test(s[k] ?? "")) { out += s[j]; i = j + 1; continue; }
    while (k < s.length) {
      if (/[\w.]/.test(s[k])) { k++; continue; }
      if (s[k] === "(" || s[k] === "[") { k = saltarGrupo(s, k, s[k], s[k] === "(" ? ")" : "]"); continue; }
      break;
    }
    i = k;
  }
  return out.replace(/[{}()[\];]/g, " ");
}

// Lo que no es una palabra: cifras, signos y espacios.
const SIN_LETRAS = /^[\s\d.,:;·•—–\-−+/\\|%*#&@°"'`~^<>=_!?$₡€£¥()[\]{}…»«]*$/;
const TIENE_LETRA = /[a-zA-ZáéíóúüñÁÉÍÓÚÜÑ]/;

// ATRIBUTOS QUE UNA PERSONA LEE. `aria-label` entra: que solo lo diga un lector de pantalla no lo
// hace menos texto -- asi se escapo «Escribinos por WhatsApp» en espanol dentro de una app bilingue.
const ATRIBUTO_VISIBLE =
  /\b(title|alt|placeholder|aria-label|aria-description|aria-roledescription|aria-valuetext|label|Titulo|Title|Dicho|Texto|Text|Leyenda|Rotulo|Mensaje|Descripcion|Description|Nota|Encabezado|Caption)\s*=\s*"([^"]*)"/g;

// COMO SE PIDE UNA CLAVE. Cada negocio nombra su lector en su idioma -- `textos`, `texto`, `texts`,
// `T` -- y eso esta bien: lo que se comparte es la forma, no los nombres.
const COMO_SE_PIDE = [
  /\b(?:textos?|Texto|texts?|Texts|T)\s*\[\s*"([^"]+)"/g,
  /\.(?:Con|Fill|Diga|Say|T)\s*\(\s*"([^"]+)"/g,
  /\bTextos?\.T\s*\(\s*"([^"]+)"/g,
  /\bT\s*\(\s*"([^"]+)"/g,
];

// ---------------------------------------------------------------------- las cuatro revisiones

function losCatalogos(s, decir, fallar, notar) {
  const [a, b] = s.idiomas;
  const claves = {};

  if (s.archivo) {
    const j = JSON.parse(leer(join(raiz, s.archivo)));
    for (const t of s.idiomas) claves[t] = new Map();
    for (const [k, v] of Object.entries(j)) {
      if (k.startsWith("//") || !v || typeof v !== "object") continue;
      for (const t of s.idiomas) if (typeof v[t] === "string") claves[t].set(k, v[t]);
    }
    // Una llave que no es un idioma -- "igual" -- es la anotacion de que ese valor se repite a
    // proposito. Es la forma que ya usa el catalogo del Host en `inicio.lema`.
    claves.anotadas = new Set(Object.entries(j)
      .filter(([k, v]) => !k.startsWith("//") && v && typeof v === "object"
        && Object.keys(v).some((x) => !s.idiomas.includes(x) && !x.startsWith("//")))
      .map(([k]) => k));
  } else {
    for (const t of s.idiomas) {
      const p = join(raiz, s.dir, s.catalogo, t + ".json");
      claves[t] = existsSync(p) ? new Map(aplanar(JSON.parse(leer(p)))) : null;
    }
    claves.anotadas = new Set();
  }

  if (!claves[a] || !claves[b]) { fallar(`falta el catalogo de ${!claves[a] ? a : b}`); return null; }

  const soloA = [...claves[a].keys()].filter((k) => !claves[b].has(k));
  const soloB = [...claves[b].keys()].filter((k) => !claves[a].has(k));
  if (soloA.length || soloB.length)
    fallar(`catalogos asimetricos` +
      (soloA.length ? `; solo en ${a}: ${soloA.join(", ")}` : "") +
      (soloB.length ? `; solo en ${b}: ${soloB.join(", ")}` : ""));
  else decir(`catalogos simetricos (${claves[a].size} claves)`);

  // LO QUE SE DICE IGUAL EN LOS DOS IDIOMAS HAY QUE DECLARARLO. A veces es correcto -- «VIN» es
  // «VIN» -- y a veces es una traduccion que nadie hizo: `misFacturas.refresh.boton` decia «Refresh»
  // en el catalogo ESPAÑOL, al lado de un «Refrescando…» bien traducido. NO se puede distinguir por
  // la forma del valor: filtrar los que son puras letras tapa «VIN» y tapa «Refresh» igual. Asi que
  // no se adivina: o el catalogo lo anota, o esta fila lo declara con su razon.
  const iguales = [...claves[a].entries()].filter(([k, v]) =>
    claves[b].get(k) === v && typeof v === "string" && TIENE_LETRA.test(v)
    && !claves.anotadas.has(k) && !(`${s.nombre}|${k}` in igualesAProposito));
  if (iguales.length)
    fallar(`claves que dicen lo MISMO en los dos idiomas sin declarar por que:\n` +
           iguales.map(([k, v]) => `        ${k} = ${JSON.stringify(v)}`).join("\n"));
  else decir(`toda coincidencia entre los dos idiomas esta declarada`);

  return claves[a];
}

// EL SEGUNDO CATALOGO: las frases que compone el dominio, escritas en C# y no en JSON. Se revisa
// con las mismas tres preguntas que el de la pantalla, y por el mismo motivo: `Frases.Diga` devuelve
// la clave entre llaves cuando falta, igual que el indexador de `Textos`.
function lasFrases(s, decir, fallar, notar) {
  if (!s.frases) { decir(`sin frases del dominio: su dominio no compone ninguna`); return; }

  const dir = join(raiz, s.frases.dir);
  const claves = {};
  for (const [lengua, archivo] of [["es", s.frases.es], ["en", s.frases.en]]) {
    const p = join(dir, archivo);
    if (!existsSync(p)) { fallar(`falta ${s.frases.dir}/${archivo}`); return; }
    // ["clave"] = "una parte" + "y la que sigue",
    claves[lengua] = new Map([...leer(p).matchAll(/\["([^"]+)"\]\s*=\s*((?:\s*(?:@?"(?:[^"\\]|\\.)*"|\+))+)\s*,/g)]
      .map((x) => [x[1], [...x[2].matchAll(/"((?:[^"\\]|\\.)*)"/g)].map((c) => c[1]).join("")]));
  }

  const soloEs = [...claves.es.keys()].filter((k) => !claves.en.has(k));
  const soloEn = [...claves.en.keys()].filter((k) => !claves.es.has(k));
  if (soloEs.length || soloEn.length)
    fallar(`catalogos de frases asimetricos` +
      (soloEs.length ? `; solo en espanol: ${soloEs.join(", ")}` : "") +
      (soloEn.length ? `; solo en ingles: ${soloEn.join(", ")}` : ""));
  else decir(`frases del dominio simetricas (${claves.es.size} claves)`);

  const iguales = [...claves.es.entries()].filter(([k, v]) =>
    claves.en.get(k) === v && TIENE_LETRA.test(v) && !(`${s.nombre}|${k}` in igualesEnLasFrases));
  if (iguales.length)
    fallar(`frases que dicen lo MISMO en los dos idiomas sin declarar por que:\n` +
           iguales.map(([k, v]) => `        ${k} = ${JSON.stringify(v)}`).join("\n"));

  // QUIEN PIDE UNA FRASE es la puerta, no la pantalla, asi que se busca en el proyecto .App entero
  // y no en el shell. El directorio de las frases es hijo suyo.
  const app = dirname(dir);
  const pedidas = new Map(), armadas = new Set();
  const comoSePide = new RegExp(`\\.${s.frases.pide}\\s*\\(\\s*"([^"]+)"\\s*(\\+?)`, "g");
  for (const f of archivos(app, ".cs")) {
    const fuente = leer(f).replace(/^[ \t]*\/\/.*$/gm, blanco);
    for (const m of fuente.matchAll(comoSePide)) {
      const linea = fuente.slice(0, m.index).split("\n").length;
      if (m[2] || m[1].endsWith(".")) { armadas.add(`${m[1]}   (${ruta(f)}:${linea})`); continue; }
      if (!pedidas.has(m[1])) pedidas.set(m[1], []);
      pedidas.get(m[1]).push(`${ruta(f)}:${linea}`);
    }
  }

  const faltan = [...pedidas.keys()].filter((k) => !claves.es.has(k));
  if (faltan.length)
    fallar(`frases que la puerta pide y el catalogo NO tiene:\n` +
           faltan.map((k) => `        ${k}   (${pedidas.get(k).join(", ")})`).join("\n"));
  else decir(`${pedidas.size} frases pedidas, todas en el catalogo`);

  if (armadas.size)
    notar(`${armadas.size} frase(s) armadas en runtime: que el catalogo tenga TODOS sus valores se lee a mano\n` +
          [...armadas].map((x) => `        ${x}`).join("\n"));
}

function lasClavesQueSePiden(s, escritas, decir, fallar, notar) {
  const pedidas = new Map(), armadas = new Set();
  for (const f of [...archivos(join(raiz, s.dir), ".razor"), ...archivos(join(raiz, s.dir), ".cs")]) {
    // SIN COMENTARIOS. Un `@* T("clave") sin prefijo *@` que explica COMO se pide una clave no esta
    // pidiendo ninguna, y contarlo manda a buscar al catalogo una clave que se llama "clave".
    const fuente = leer(f).replace(/@\*[\s\S]*?\*@/g, blanco).replace(/^[ \t]*\/\/.*$/gm, blanco);
    for (const re of COMO_SE_PIDE)
      for (const m of fuente.matchAll(re)) {
        const linea = fuente.slice(0, m.index).split("\n").length;
        // Un prefijo que termina en punto es una clave que se arma en runtime; se anota y no se juzga.
        if (m[1].endsWith(".")) { armadas.add(`${m[1]}   (${ruta(f)}:${linea})`); continue; }
        if (!pedidas.has(m[1])) pedidas.set(m[1], []);
        pedidas.get(m[1]).push(`${ruta(f)}:${linea}`);
      }
  }

  const faltan = [...pedidas.keys()].filter((k) => !escritas.has(k));
  if (faltan.length)
    fallar(`claves que el codigo pide y el catalogo NO tiene:\n` +
           faltan.map((k) => `        ${k}   (${pedidas.get(k).join(", ")})`).join("\n"));
  else decir(`${pedidas.size} claves pedidas, todas en el catalogo`);

  if (armadas.size)
    notar(`${armadas.size} clave(s) armadas en runtime: que el catalogo tenga TODOS sus valores se lee a mano\n` +
          [...armadas].map((x) => `        ${x}`).join("\n"));
}

function losLiterales(s, decir, fallar, notar) {
  let sinCablear = 0, perdonadasVistas = [];

  for (const f of archivos(join(raiz, s.dir), ".razor")) {
    const fuente = leer(f), r = ruta(f), malos = [];

    const anotar = (linea, texto) => {
      const llave = `${r}|${texto}`;
      if (llave in perdonadas) { perdonadasVistas.push(`${texto}  (${r}:${linea}) — ${perdonadas[llave]}`); return; }
      malos.push([linea, texto]);
    };

    for (const [linea, nodo] of nodosDeTexto(fuente))
      for (const trozo of loLiteralDe(nodo).split("\n")) {
        const texto = trozo.trim();
        if (!texto || SIN_LETRAS.test(texto) || !TIENE_LETRA.test(texto)) continue;
        anotar(linea, texto);
      }

    // Los atributos se miran sobre la fuente sin comentarios ni @code, que es donde estan escritos.
    let marcado = fuente.replace(/@\*[\s\S]*?\*@/g, blanco);
    for (const palabra of ["@code", "@functions"]) {
      let i = 0;
      while ((i = marcado.indexOf(palabra, i)) >= 0) {
        const llave = marcado.indexOf("{", i);
        if (llave < 0) break;
        marcado = borrar(marcado, i, saltarGrupo(marcado, llave, "{", "}"));
        i += palabra.length;
      }
    }
    for (const m of marcado.matchAll(ATRIBUTO_VISIBLE)) {
      const v = m[2];
      if (!v || v.startsWith("@") || SIN_LETRAS.test(v) || !TIENE_LETRA.test(v)) continue;
      anotar(marcado.slice(0, m.index).split("\n").length, `[atributo ${m[1]}] ${v}`);
    }

    if (malos.length) {
      sinCablear += malos.length;
      fallar(`${r}\n` + malos.map(([l, t]) => `        :${l}  ${JSON.stringify(t)}`).join("\n"));
    }
  }

  if (!sinCablear) decir(`ningun literal sin cablear en el marcado`);
  if (perdonadasVistas.length)
    notar(`${perdonadasVistas.length} literal(es) que no se traducen a proposito:\n` +
          perdonadasVistas.map((x) => `        ${x}`).join("\n"));
}

// ---------------------------------------------------------------------- correr

const args = process.argv.slice(2);
const silencio = args.includes("--silencio");
const conNotas = args.includes("--notas");
const cuales = args.filter((a) => !a.startsWith("--"));
const desconocidas = cuales.filter((c) => !(c in superficies));
if (desconocidas.length) {
  console.log(`no conozco: ${desconocidas.join(", ")}. Las que hay: ${Object.keys(superficies).join(", ")}`);
  process.exit(2);
}

let hallazgos = 0;
for (const nombre of (cuales.length ? cuales : Object.keys(superficies))) {
  const s = { ...superficies[nombre], nombre };
  const lineas = [];
  const decir = (t) => lineas.push(`   · ${t}`);
  const notar = (t) => lineas.push(`   ? ${t}`);
  const fallar = (t) => { hallazgos++; lineas.push(`   ✗ ${t}`); };

  const escritas = losCatalogos(s, decir, fallar, notar);
  if (escritas) lasClavesQueSePiden(s, escritas, decir, fallar, notar);
  lasFrases(s, decir, fallar, notar);
  losLiterales(s, decir, fallar, notar);

  const suyos = lineas.filter((l) => l.startsWith("   ✗"));
  if (silencio && !suyos.length) continue;
  const cuantos = archivos(join(raiz, s.dir), ".razor").length;
  console.log(`\n${s.marca}  (${nombre}, ${cuantos} .razor)`);
  for (const l of (silencio ? suyos : lineas))
    if (conNotas || !l.startsWith("   ?")) console.log(l);
}

console.log(hallazgos === 0
  ? `\nTODO OK  (toda palabra de la pantalla entra por el catalogo o por un dato)`
  : `\n${hallazgos} hallazgo(s). Un literal sin cablear se queda en un idioma para siempre.`);

process.exit(hallazgos === 0 ? 0 : 1);
