# Guía para crear un módulo de Gnothi — lo que una institución tiene que empaquetar (teléfono)

**2026-09-11.** Escrita para el equipo técnico de una institución que quiere que su registro
aparezca en el Gnothi de sus clientes, en el teléfono. Sale de lo que el código exige hoy
(`Gnothi.Host/Manifest.cs`, `PackageInspector.cs`, `Gnothi.Movil/WebModule.cs`,
`ModuleEnrollment.cs`, `BundleServer.cs`, `GnothiCourier.cs`) y de dos módulos que ya pasaron el
alta: los cinco del prototipo en Blazor y `holajs`, uno en HTML+JS plano medido el 2026-09-10.
Cuando algo es una decisión que el equipo todavía no tomó, se dice.

**Alcance.** Esta guía cubre el módulo **hospedado**, que es el que corre en el teléfono
(Android e iOS).

**Cómo leerla.** La parte 1 es el paquete y el manifiesto. La parte 2 es lo que la app tiene que
cumplir adentro de Gnothi. La parte 3 es lo que la institución publica en su sitio. La parte 4
separa lo que es contrato de lo que es demo. La 5 es la lista de verificación, la 6 lo que está
abierto, y el **apéndice A es la referencia campo por campo**: qué dato va en cada uno y qué regla
cumple.

---

## 0 · La idea en cuatro frases

Gnothi es una app que la persona instala en su teléfono. Cada institución le entrega **un
módulo**: un paquete con el registro que esa institución lleva de ella y una lista de preguntas
que ese registro sabe contestar. Gnothi junta los módulos y se los muestra a la IA que la persona
use, como una sola lista de consultas; **ella decide con un interruptor qué se ve**. El módulo
**no tiene red, no despierta solo y no elige dónde escribe**: Gnothi le trae los eventos, le da
una carpeta y lo apaga cuando nadie lo usa.

Lo que la institución escribe es **una declaración** (el manifiesto) más **su app web**, la misma
que ya publica. Lo que Gnothi escribe es la instalación: con qué cáscara corre, qué carpeta le
concedió y si está encendido. Esa separación es la regla que ordena todo lo demás: **un paquete
que trae lo que le toca al Host se rechaza**.

---

## 1 · El paquete y el manifiesto

### 1.1 El paquete: un `.gmod`

Un `.gmod` es un zip con otro nombre. Adentro van dos cosas: el manifiesto y la app.

```
<modulo>.gmod
├── manifest.json                  ← obligatorio, en la raíz
├── web/                           ← la app publicada, tal cual la sirve la institución
│   ├── index.html                 ← obligatorio; con <base href>
│   ├── manifest.webmanifest       ← recomendado: de ahí sale la marca si no se declara
│   └── …                          ← js, css, wasm, imágenes, fuentes
├── brand/logo.png                 ← opcional (ver A.2)
├── view.json                      ← opcional (ver 1.4)
└── signature.json                 ← opcional; lo escribe la herramienta de firma (ver 3.3)
```

**Lo que hace que un paquete se rechace** (`PackageInspector.cs`), y el rechazo llega con su
motivo legible:

| rechazo | por qué |
|---|---|
| no trae `web/index.html` | la app no puede arrancar |
| trae `installation.json` | ese archivo es del Host: cáscara, carpeta y consentimiento son actos de Gnothi |
| trae un `.exe` | un módulo es carga, no un programa |
| trae un journal de muestra (`meta.bin`, una carpeta `journal/`) | el registro se construye en el aparato de la persona; no viaja en el paquete |
| una ruta con `..`, con `:` o que empieza con `/` | zip-slip: escribiría fuera de su carpeta |
| `module` no es un identificador | letras, dígitos, `.`, `_`, `-`; hasta 64; empieza con letra o dígito |
| un `.dll` fuera de `web/` | un módulo hospedado no carga nada fuera de su app |
| firmado y la firma no cierra | alguien lo tocó después de firmarlo |

**Lo que no rechaza pero sí dice:** el peso, cuántos archivos, si trae motor .NET adentro (se
cuenta y se le muestra a la persona, porque un navegador no corre C# sin él) y qué capacidades
pide de más. Con capacidades de más el módulo **se instala apagado** y la persona lo enciende si
quiere, después de leer por qué.

**Techo:** 64 MB mientras baja, medido contra los bytes reales y no contra `Content-Length`.

### 1.2 El manifiesto: `manifest.json`

Lo escribe la institución. Declara **hechos verificables sobre ella y sobre lo que ofrece**.
Nunca instrucciones para el Host ni para el modelo: por eso no existen, y no van a existir,
`assistantHint`, `tone` ni `persona`. Un texto que le diga a la IA cómo comportarse sería
inyección de prompt con formato de contrato.

**Las claves van en inglés** (`module`, `title`, `tools`, `input`, `readOnly`…), con una
excepción heredada que sigue en español: `publicador`. Los **valores** van en el idioma de la
persona. Un manifiesto con claves en español (`modulo`, `titulo`, `entrada`) no se lee.

El ejemplo completo y comentado está en `Herramientas/manifiesto-de-ejemplo.json`, con su
`manifiesto-de-ejemplo.view.json` al lado. **Es copiable tal cual.** Los comentarios van como
claves que empiezan con `//`, no como `//` de línea: el lector de C# acepta las dos formas, pero
`JSON.parse` no acepta comentarios de línea y hay herramientas que lo usan.

**Lo mínimo que instala y funciona:**

```json
{
  "manifest": "1.1",
  "module": "delfos",
  "title": "Tus exámenes, al día",
  "institution": "Laboratorio Clínico Delfos",
  "substrate": { "range": ">=3.0.0 <4.0.0" },
  "capabilities": { "network": false, "folder": "own", "speaker": false, "microphone": false },
  "tools": [ … ]
}
```

**Todo lo demás es opcional**, y cada grupo que se agrega muestra una cosa más en pantalla. El
resumen por grupo está acá; **el detalle campo por campo, con qué dato va y qué regla cumple,
está en el apéndice A**.

| grupo | qué declara | para qué sirve |
|---|---|---|
| `origin` | desde qué origen publica la institución | se contrasta con quien sirvió el paquete y con su `.well-known` (ver 3.2) |
| `brand` | nombre corto, eslogan, logo, color, banner, tipografía | la tarjeta y el detalle del módulo. **Archivos dentro del paquete, nunca URL**; la institución declara, el Host compone |
| `publisher` | dominio, razón social, cédula, versión, fecha | la franja de procedencia; `version` es lo que permite decir «hay una versión nueva» |
| `about` | rubro, descripción, lo que no hace, idioma, moneda, unidades | el texto que lee la persona; `locale` se lo dice al modelo para que busque en el idioma del registro |
| `journal` | modo del registro y lo que se promete sobre él | la pantalla del alta: cuánto pesa, cada cuánto se actualiza, si se puede reconstruir |
| `publicador` | dónde viven los eventos | Gnothi lo **fija** al instalar; el módulo no lo usa ni lo cambia (ver 3.4) |
| `view` | el nombre de un json dentro del paquete | ver 1.4 |

Un tope que se pasa **no tumba el módulo**: se instala y Gnothi lo anota como aviso. Dejar a la
persona sin su laboratorio por un renglón largo sería peor que el renglón.

**Si `brand` no está**, la marca sale del `manifest.webmanifest` que la app web ya trae
(`icons`, `theme_color`). Un módulo 1.0 se pinta con su marca sin tocarlo.

### 1.3 Las tools: lo que la IA va a poder preguntar

```json
"tools": [
  {
    "name": "evaluar_tendencia",
    "title": "Cómo viene un analito",
    "description": "Si un analito de la persona VIENE SUBIENDO, viene bajando o está estable, mirando toda su historia y no el último resultado. Solo sirve para analitos que traen un NÚMERO.",
    "readOnly": true,
    "input": [
      { "name": "analito", "type": "string", "description": "Cuál analito evaluar.",
        "enum": ["ColesterolTotal", "ColesterolLdl", "Hemoglobina"] }
    ]
  }
]
```

| campo | regla |
|---|---|
| `name` | **el nombre real sobre el cable**: tiene que coincidir exactamente con lo que el módulo expone. Gnothi lo comprueba al instalar (`gnothi_verify_module`: declaradas contra reales) y una tool declarada que no existe, o una real que no se declaró, marca `matches: false`. La IA lo ve como `<modulo>_<name>` |
| `title` | lo que lee la persona en su lista de interruptores |
| `description` | **viaja tal cual al modelo**. No es documentación: es lo único que la IA tiene para decidir si esta consulta contesta la pregunta de la persona. Se escribe como la pregunta que una persona se hace sobre sus propios datos, con lo que la respuesta **no** dice. Gnothi no la reescribe |
| `readOnly` | hoy siempre `true`: un módulo contesta, no actúa |
| `input` | lista plana de parámetros: `name`, `type` (`string`, `number`, `boolean`…), `description`, `enum` opcional. El `enum` va como dato, no enterrado en la prosa: es una señal mucho más fuerte para el modelo |

**Cada tool tiene su interruptor.** La persona puede apagar una y dejar las otras; el módulo no
se entera. Por eso conviene una tool por pregunta, no una tool genérica con un `switch` adentro.

**Lo que devuelve una tool es JSON**, y conviene que traiga siempre un `hayDatos` (o equivalente)
que diga si el registro está vacío, y hasta qué evento leyó (`ultimoEventoLeido`): es lo que le
permite a la IA decir «tu app todavía no trajo eso» en vez de inventar.

**No existe ninguna tool para pedirle algo a la persona.** Una consulta llamada
`completar_encuesta` es un canal hacia su atención, y la asimetría «el módulo responde, no
pregunta» es de las que Gnothi defiende.

### 1.4 La vista declarativa: `view.json`

Opcional. Describe la mitad de arriba del detalle del módulo con seis bloques cerrados (`texto`,
`datos`, `aviso` con `hasta` obligatorio, `contacto`, `salida`, `enlace`); el Host la pinta como
texto, nunca como markup, y ningún valor puede ser un dato de la persona porque el paquete es el
mismo para todos los clientes. **El formato está definido y el renderer todavía no existe** (fase
4 del plan de marca): hoy el archivo viaja y no se pinta. El ejemplo comentado está en
`Herramientas/manifiesto-de-ejemplo.view.json`; los bloques, en el apéndice A.6.

### 1.5 Las cuatro categorías de dato

Confundirlas es el error más común al escribir un módulo:

| categoría | dueño | dónde va |
|---|---|---|
| hechos institucionales (un examen, una compra, una carga) | la persona | **el registro**, append-only, emitidos por la institución |
| repertorio (rangos de referencia, horarios, reglas) | la institución | **dentro del módulo**, versionado; cambiarlo es publicar una versión nueva |
| aserciones de la persona («soy vegetariana», una corrección) | la persona | el registro, con su procedencia marcada |
| contexto de sesión (tokens, cursores, reintentos) | el Host | **no va al registro** |

**Criterio falsable:** cambiar un rango de referencia produce un módulo nuevo y no toca ningún
registro existente. Si para actualizar un rango hay que escribir en el registro de alguien, el
repertorio se filtró a la historia.

---

## 2 · La app adentro de Gnothi

Es la app web que la institución **ya publica**, empaquetada en `web/`. Gnothi la sirve desde
adentro en un WebView, en su propio origen, con una política de seguridad que él escribe, y le
inyecta un puente. **Es agnóstica de tecnología**: los cinco módulos del prototipo son Blazor
WebAssembly; `holajs` es HTML + CSS + JS plano de 7 KB, y pasó el alta completa (inspección,
`matches: true` con tres tools, respondió, guardó su registro, y la CSP le negó un `fetch` al
publicador y lo anotó). Lo que Gnothi exige es **el contrato de JavaScript**, no un framework.

### 2.1 Lo que tiene que haber en `web/`, y la contención

- **`index.html`** con `<base href="/lo-que-sea/">`. Gnothi lee ese `base` una vez y sirve la app
  **en ese lugar**: una app publicada para `/app/` servida en la raíz da 404 en todo y pantalla
  en blanco sin una sola pista. Medido, costó una corrida entera.
- **`manifest.webmanifest`** (el de la PWA). No es obligatorio, pero de ahí salen el icono y el
  color cuando `brand` no los declara, y sus iconos pasan por la misma aduana que un logo
  declarado (los bytes dicen el tipo, no la extensión).
- **Todo lo que la app pide**: js, css, wasm, imágenes, fuentes. Gnothi sirve los tipos MIME
  usuales, incluido `application/wasm` (sin él, el motor no compila en streaming).

La política que Gnothi le escribe al documento (`BundleServer.cs`) es, con lo que eso permite y
lo que no:

| directiva | lo que significa para la app |
|---|---|
| `default-src 'self'` | todo sale del propio paquete: imágenes, fuentes, wasm, json |
| `connect-src 'self'` | **sin red propia.** Un `fetch` o `XMLHttpRequest` a otro origen se rechaza y Gnothi lo registra (`REFUSED by connect-src`). Los eventos los trae Gnothi por el puente |
| `script-src` con el **hash de cada script inline** | los `<script>` inline del `index.html` **pasan**: Gnothi calcula su hash al servir. Los **atributos** `onclick="…"` y similares **no pasan**: engancharlos con `addEventListener` |
| `style-src 'self' 'unsafe-inline'` | los estilos inline **pasan** |
| `frame-src 'none'` | **sin iframes** |
| `worker-src 'none'` | **sin worker ni service worker**: el módulo no despierta solo |
| `form-action 'self'` · `base-uri 'self'` | un `<form>` no puede postear afuera; el `<base>` no se cambia en marcha |
| navegación fuera del origen | la cierra cada plataforma, no la CSP: `window.location = 'https://otro'` no sale |

### 2.2 El contrato de JavaScript

Dos direcciones y dos mecanismos. **Los nombres del cable van en español y no se traducen**:
`gnothiPuente`, `contesta`, `listo`, `listar`, `leer`, `escribir`, `pull`, y las claves con que el
puente contesta (`ok`, `rutas`, `contenido`, `motivo`). Ya corren en módulos publicados; son
contrato.

**Gnothi → módulo.** Lo publica el módulo en `window.gnothi`; Gnothi lo llama con
`evaluateJavaScript`. **Devuelven siempre JSON como texto**, también los errores, porque quien
llama es código nativo que no puede atrapar una excepción de JavaScript. Pueden devolver una
promesa.

```
window.gnothi.tools()                    → string  JSON: ["resumen", "lista_de_examenes", …]
window.gnothi.call(tool, argsJson)       → string  JSON con la respuesta de la consulta
window.gnothi.deliver(sobreJson)         → string  el cartero entrega un sobre; devuelve { ok, asentados, ultimo }
window.gnothi.respuesta(id, json, error)   ← Gnothi contesta acá los pedidos que el módulo le hizo
window.gnothi.traspasar(asercionesJson)  → string  opcional: recibe las aserciones de la app suelta al mudarse
```

**Módulo → Gnothi.** Lo **inyecta Gnothi** en `window.gnothiPuente`, con **seis verbos**. El
puente **no devuelve nada**: recibe un número de pedido y contesta después por
`window.gnothi.respuesta(id, json, error)`. En Android los métodos inyectados son síncronos y un
`pull` hace HTTPS; la misma forma sirve en iOS. Del lado web eso se envuelve en una promesa.

```
gnothiPuente.listo()                          el módulo avisa que ya puede contestar (notificación, sin id)
gnothiPuente.contesta(id, json, error)        lo usa GNOTHI para recoger lo que tools/call/deliver devolvieron;
                                              el módulo no lo llama, y no debe redefinir el objeto
gnothiPuente.listar(id)                       → { ok, rutas: ["eventos.jsonl", …], motivo }
gnothiPuente.leer(id, ruta)                   → { ok, contenido: <base64>, motivo }
gnothiPuente.escribir(id, ruta, base64)       → { ok, motivo }
gnothiPuente.pull(id, after)                  → el sobre del publicador, tal cual (ver 3.4)
```

**Tres reglas que, si no se cumplen, el módulo no abre:**

1. **Llamar a `gnothiPuente.listo()`** cuando el módulo ya cargó su registro y puede contestar.
   Sin eso Gnothi espera **60 segundos** y falla al abrir. Es una notificación, no un pedido.
2. **Cada consulta contesta en menos de 30 segundos**, o Gnothi la da por fallida.
3. **Detectar el modo por la presencia del puente**, no por el origen:
   `typeof window.gnothiPuente !== 'undefined'`. La misma app corre suelta en el navegador de la
   institución (pidiendo eventos ella misma) y hospedada (pidiéndoselos a Gnothi).

**El registro vive en archivos que Gnothi guarda por fuera del WebView** (`listar`/`leer`/
`escribir`), en la carpeta que le concedió al módulo. Ni `localStorage` ni OPFS: el cuarto pilar
de Gnothi es contestar lo que ningún módulo puede contestar de sí mismo (cuánto pesa su registro,
cuándo cambió), y para eso tiene que poder mirarlo desde afuera del motor web. El formato del
archivo lo elige la institución: `holajs` escribe una línea JSON por evento; los módulos Blazor
escriben el journal del motor. Gnothi no compone nada: escribe los bytes que el módulo le dio. Una
ruta que intente salir de la carpeta (`../`) se rechaza.

**El cursor es del módulo.** El `after` del `pull` lo saca el módulo de su propio registro;
Gnothi lo valida como entero y lo releva, **no lo guarda**. No hay dos verdades sobre hasta dónde
se leyó. Si el módulo no tiene publicador registrado, `pull` devuelve un sobre vacío
(`{ events: [], last: after }`) y no un error: el módulo abre igual con lo que ya tiene.

### 2.3 El esqueleto mínimo, en JS plano

Es lo que `holajs` hace, reducido. Un `app.js` cargado desde `index.html`:

```js
(function () {
  const pendientes = new Map(); let siguiente = 1;
  const hospedada = () => typeof window.gnothiPuente !== 'undefined' && window.gnothiPuente !== null;

  // módulo → Gnothi, envuelto en promesa
  function pedir(verbo, ...args) {
    return new Promise((ok, fallo) => {
      if (!hospedada()) return fallo(new Error('no hay puente'));
      const id = String(siguiente++);
      pendientes.set(id, { ok, fallo });
      try { window.gnothiPuente[verbo](id, ...args); } catch (e) { pendientes.delete(id); fallo(e); }
    });
  }
  window.gnothi = window.gnothi || {};
  window.gnothi.respuesta = (id, json, error) => {
    const p = pendientes.get(String(id)); if (!p) return;
    pendientes.delete(String(id)); error ? p.fallo(new Error(error)) : p.ok(json);
  };

  let eventos = [], cursor = 0;                      // el registro, en memoria
  function aplicar(sobre) {                          // idempotente: id > cursor
    const nuevos = (sobre.events || []).filter(e => e.id > cursor).sort((a, b) => a.id - b.id);
    for (const e of nuevos) { eventos.push(e); cursor = e.id; }
    return nuevos.length;
  }
  async function guardar() {                         // bytes → base64 → carpeta concedida
    const texto = eventos.map(e => JSON.stringify(e)).join('\n') + '\n';
    await pedir('escribir', 'eventos.jsonl', btoa(unescape(encodeURIComponent(texto))));
  }

  // Gnothi → módulo: siempre JSON como texto
  window.gnothi.tools = () => JSON.stringify(['resumen']);
  window.gnothi.call = (tool, argsJson) => {
    try {
      const args = argsJson ? JSON.parse(argsJson) : {};
      if (tool === 'resumen') return JSON.stringify({ hayDatos: eventos.length > 0, eventos: eventos.length, ultimoEventoLeido: cursor });
      return JSON.stringify({ ok: false, motivo: 'tool desconocida: ' + tool });
    } catch (e) { return JSON.stringify({ ok: false, motivo: String(e.message || e) }); }
  };
  window.gnothi.deliver = async (sobreJson) => {
    const n = aplicar(JSON.parse(sobreJson || '{}')); if (n) await guardar();
    return JSON.stringify({ ok: true, asentados: n, ultimo: cursor });
  };

  async function arrancar() {
    if (hospedada()) {
      const l = JSON.parse(await pedir('listar'));
      if (l.ok && l.rutas.includes('eventos.jsonl')) { /* leer, decodificar base64, poblar eventos/cursor */ }
      const sobre = JSON.parse(await pedir('pull', String(cursor)));
      if (aplicar(sobre)) await guardar();
      window.gnothiPuente.listo();                   // sin esto, 60 s y falla
    }
  }
  document.readyState === 'loading' ? document.addEventListener('DOMContentLoaded', arrancar) : arrancar();
})();
```

Para una app Blazor el puente es el mismo; lo que cambia es que `tools`/`call`/`deliver` entran a
C# por `DotNet.invokeMethodAsync` (ver `Cargas/App/Cargas.Wasm/wwwroot/gnothi-interop.js`, que
tiene el nombre del ensamblado quemado y hay que cambiarlo).

### 2.4 Cómo se instala en el teléfono

La persona no busca archivos. **Toda alta empieza afuera**, en un enlace de la institución:

```
https://go.gnothi.app/alta?bundle=https://delfos.cr/delfos.gmod&h=<sello>
```

Hoy `go.gnothi.app` **no existe**, así que los canales que corren son: pegar el enlace dentro de
Gnothi, leer un QR con la cámara desde Gnothi, o abrir/compartir el `.gmod` como archivo. Los tres
desembocan en el mismo caño: Gnothi baja el paquete, lo inspecciona, le muestra a la persona una
hoja con quién es, qué contesta, qué revisó, dónde va a vivir el registro y a qué única dirección
va a poder ir por sus eventos, y **ella acepta o descarta**. Después de aceptar Gnothi compara
`tools()` con el manifiesto (**la verificación**: si difieren, el acta dice `matches: false`) y
llama la primera tool.

El registro queda en `files/journals/<modulo>/` del contenedor de Gnothi. Un módulo con runtime
.NET adentro pesa en memoria ~150–250 MB; Gnothi lo levanta al pedirle algo y lo desaloja después,
salvo que la persona lo esté mirando.

**Estado:** medido en emulador y en aparatos Android reales (Xiaomi 14T Pro, Galaxy A54). En iOS
las ocho pruebas del guion están medidas (`Gnothi.iOS/HALLAZGOS-CORRIENDO-EL-GUION.md`): el alta por
enlace pegado y por archivo en un simulador iPhone 17 Pro, y la memoria en un iPhone 13 Pro real;
falta la cámara real (QR) en un iPhone.

---

## 3 · Lo que la institución publica en su sitio

### 3.1 El paquete

`https://<origen>/<modulo>.gmod`, servido con `Content-Type: application/vnd.gnothi.module`. El
MIME importa: en Android los selectores y el share entregan `content://` y ahí manda el tipo, no
la extensión; sin él el archivo llega como `application/zip` y hay que adivinar.

### 3.2 El `.well-known`

`https://<origen>/.well-known/gnothi/modules.json`:

```json
{
  "gnothi": 1,
  "institution": "Laboratorio Clínico Delfos",
  "keys":    [ { "id": "delfos-2026-09", "alg": "ES256", "public": "<SubjectPublicKeyInfo base64url>" } ],
  "revoked": [ { "id": "delfos-2026-01", "public": "…", "since": "2026-09-01T00:00:00Z", "reason": "…" } ],
  "modules": [ { "module": "delfos", "bundle": "https://delfos.cr/delfos.gmod", "seal": "<sello>", "version": "2.4.0" } ]
}
```

Gnothi le pregunta al origen si el paquete es suyo. Tres cosas tienen que coincidir: el origen
que sirvió el paquete, el `origin` que el manifiesto declara, y lo que ese origen publica acá.
**Política decidida (el punto medio):** un origen que publica la lista y **no** nombra el módulo,
o que nombra otro paquete, **se rechaza**; un origen que no publica nada **avisa** en la hoja
(«llegó sin respaldo verificable») y deja instalar. Además, con `version` acá, Gnothi le muestra a
la persona «hay una versión nueva» al arrancar y al volver al frente; nunca auto-instala.

El **sello** (`seal`, y el `&h=` del enlace) son los primeros 16 bytes del SHA-256 del
`manifest.json` exactamente como está en el zip, en base64url sin relleno. Da integridad, no
autenticidad: si el paquete cambió después de imprimir el QR, el alta falla.

### 3.3 La firma

`signature.json` dentro del paquete: el SHA-256 de **cada archivo** y una firma ES256 (ECDSA
P-256) sobre esa lista, con la llave pública adentro. Gnothi **recuerda la llave con la que aceptó
el módulo la primera vez** (el pin) y dice en voz alta cuando un paquete posterior viene firmado
por otra. Una llave que el `.well-known` lista como `revoked` es rechazo duro. **Una firma prueba
continuidad, no virtud**: «el mismo autor que la vez pasada», no «un autor confiable». La hoja
nunca dice «verificado por Gnothi».

Hoy la firma es **opcional**: un paquete sin firmar instala y la hoja lo dice. La custodia de la
llave es de la institución. Se firma con la herramienta del Host, que crea la llave la primera
vez y escribe el `signature.json`; no hay que escribirlo a mano:

```bash
Gnothi.Host --sign <paquete.gmod> <llave.json> [<key-id>]      # firma; crea la llave si no existe
Gnothi.Host --revoke <llave.json> <revoked.json> [motivo]      # retira una llave y crea otra
```

### 3.4 El publicador de eventos

Es la única salida a la red de todo el sistema, y **la hace Gnothi**, con su propia credencial,
contra la dirección que el manifiesto declaró y Gnothi fijó. El módulo nunca ve la credencial.

```
POST {eventos}/device/token           body { "deviceKey": "…", "recurso": "expediente", "id": "1" }
                                       → { "accessToken": "…", "refreshToken": "…" }
GET  {eventos}/{recurso}/{id}/events?after=N      Authorization: Bearer <accessToken>
                                       → { "account": { "state": "active" | "migrated", "migratedAt", "host", "graceUntil" },
                                           "events":  [ { "id": 235, … }, … ],
                                           "last":    987 }
```

| regla | detalle |
|---|---|
| `id` de cada evento | entero, **estrictamente creciente** por recurso; es el cursor |
| `after=N` | devuelve solo los eventos con `id > N`; `after=0` es «desde el principio» |
| `last` | el mayor `id` que el publicador tiene; le dice a la app si está al día |
| `account.state` | viaja **siempre**, aunque no haya eventos: es como la app suelta se entera de que la cuenta se mudó a Gnothi. Campo aditivo |
| 401 / 403 | Gnothi pide **una** credencial nueva y reintenta una vez; a la segunda falla y lo registra |
| el contenido de cada evento | **es de la institución**: `tipo`, `examen`, `fecha`, `valor`… Gnothi no lo abre ni lo reordena |

El alcance de la credencial es **leer los eventos de ese recurso**, y nada más: ni perfil ni otros
clientes. Si la institución tiene servidor de autorización propio (OAuth con PKCE), esa es la
única función que cambia del lado de Gnothi; hoy ninguna lo tiene y el mecanismo es el de
`device/token`.

---

## 4 · Qué es contrato y qué es demo

En el repo las dos cosas conviven, y conviene dárselas explícitas.

| | es **contrato** (no cambia por institución) | es **demo** (la institución lo reemplaza por lo suyo) |
|---|---|---|
| paquete | el `.gmod`, `manifest.json`, `web/`, las reglas de rechazo | los cinco módulos del prototipo (exámenes, compras, cargas, concesionario, municipalidad) |
| manifiesto | los campos y topes del apéndice A, la regla «hechos, no instrucciones» | los valores de `armar-bundle.ps1`: nombres, colores, eslóganes, dominios `.cr` inventados |
| puente JS | los seis verbos de `gnothiPuente`, los cuatro de `window.gnothi`, el callback `respuesta`, `listo()`, las claves `ok`/`rutas`/`contenido`/`motivo` | `gnothi-interop.js` de cada módulo (tiene el nombre del ensamblado quemado) |
| publicador | la forma del sobre `{account, events, last}`, `device/token`, `events?after=N`, `id` creciente | `*/EndPointDeEventos` (un mock en memoria), `*/InyectorDeEventosDemo`, los eventos `ExamenAgregado`… con sus campos |
| sitio | el `.well-known`, el MIME, el enlace `alta?bundle=&h=`, `signature.json` | `PaginaDeLaInstitucion` (una página de prueba que sirve un solo paquete) |
| registro | que vive en la carpeta concedida y Gnothi puede mirarlo desde afuera | el formato: JSON lines en `holajs`, el journal del motor en los Blazor |

**Sobre los eventos en particular:** lo que hoy hay son eventos de prueba que el equipo inventó
para poder generar historia (`InyectorDeEventosDemo` mete exámenes o compras a mano). **Lo que la
institución tiene que respetar es el sobre y el cursor**; lo que hay adentro de cada evento es su
dominio, y el módulo que ella escribe es el único que lo interpreta.

---

## 5 · Lista de verificación antes de entregar

**El paquete y el manifiesto**

- [ ] `manifest.json` en la raíz del zip, claves en inglés, `module` identificador y `manifest: "1.1"`.
- [ ] Cada tool declarada existe con ese nombre exacto; ninguna real quedó sin declarar
      (`gnothi_verify_module` → `matches: true`).
- [ ] Descripciones escritas para el modelo, con lo que **no** dice; `about.locale` puesto.
- [ ] `capabilities` en `false`/`own`. Si pide algo más, saber que entra apagado.
- [ ] Sin `installation.json`, sin `.exe`, sin journal de muestra, sin rutas con `..`.
- [ ] Logo/banner/fuente como archivos dentro del paquete, dentro de los topes.
- [ ] `publisher.version` puesto; sube en cada publicación.

**La app**

- [ ] `web/index.html` con `<base href>` correcto; `manifest.webmanifest` con `icons` y `theme_color`.
- [ ] `window.gnothi.tools/call/deliver/respuesta` publicados; devuelven JSON como texto siempre.
- [ ] `gnothiPuente.listo()` se llama al terminar de cargar; ninguna consulta pasa de 30 s.
- [ ] Cero `fetch` a otros orígenes; cero `onclick=` inline; sin iframes, sin worker ni service worker.
- [ ] El registro se guarda por `escribir`, no en `localStorage`.
- [ ] La misma app corre suelta (sin puente) y hospedada (con puente).
- [ ] Probado en el emulador Android (`Gnothi.Mobile/PRUEBAS-WEBVIEW.md`).

**El sitio**

- [ ] `origin` declarado y `.well-known/gnothi/modules.json` publicado en ese origen, con el módulo listado y su `version`.
- [ ] Firmado con `--sign`; la llave pública en `keys` del `.well-known`.
- [ ] El `.gmod` se sirve con `application/vnd.gnothi.module`.
- [ ] El publicador contesta `device/token` y `{recurso}/{id}/events?after=N` con el sobre completo.

---

## 6 · Lo que está abierto, y quién lo decide

Para no prometer lo que no hay:

| abierto | estado | dueño |
|---|---|---|
| `go.gnothi.app` | no existe; pegar el enlace, QR y archivo funcionan sin él | equipo |
| renderer de `view.json` | formato definido, no se pinta | equipo (fase 4 de marca) |
| `substrate.range` | se declara, **no se valida** | equipo |
| respaldo de origen: ¿requisito o aviso? | hoy el punto medio (rechaza lo listado-y-distinto, avisa el silencio); `OriginIsRequired` lo sube | Edgar con Álvaro |
| firma obligatoria y custodia de llave | apuntado, no decidido | equipo |
| iOS: QR con cámara real | el alta por enlace y por archivo pasa en simulador; el QR pide un iPhone de verdad | equipo (Mac) |
| un `gnothi-bridge.js` neutro y un inspector local del paquete | `holajs` muestra la forma; `--pack` ya inspecciona | equipo |
| directorio de instituciones (descubrimiento) | fase 5 del plan del alta | Álvaro |
| la mudanza de la cuenta (la app suelta deja de actualizarse) | la hoja no lo dice; cinco opciones en `13 §10.5` | Álvaro |

---

## Apéndice A · Referencia campo por campo

Qué dato va en cada campo, si es obligatorio, y qué regla le aplica Gnothi. **O** = obligatorio,
**R** = recomendado, **–** = opcional. Los topes son caracteres salvo que se diga otra cosa.
Ningún campo lleva rutas absolutas, URLs (salvo `origin` y `publicador.eventos`) ni CSS.

### A.1 Raíz

| campo | | tipo | qué poner | regla |
|---|---|---|---|---|
| `manifest` | O | texto | `"1.1"` | el formato del manifiesto, no la versión del módulo |
| `module` | O | texto | el identificador del módulo: `delfos`, `laguaria` | letras, dígitos, `.` `_` `-`; hasta 64; empieza con letra o dígito. Es el nombre de la carpeta, el prefijo de las tools (`delfos_resumen`) y la identidad del módulo: **no cambia entre versiones** |
| `title` | O | texto | el nombre que la institución le da al módulo: «Tus exámenes, al día» | 32. Se ve como segunda línea de la tarjeta y en el botón «Abrir …» |
| `institution` | O | texto | quién entrega el módulo: «Laboratorio Clínico Delfos» | se usa para identificar cuando no hay `brand.shortName`; sin siglas internas |
| `origin` | R | texto | `https://delfos.cr` | `scheme://host[:port]`, sin ruta. Tiene que ser el origen que sirve el `.gmod` y publica el `.well-known` |
| `substrate.range` | O | texto | `">=3.0.0 <4.0.0"` | un **rango** del motor, nunca una versión exacta. Hoy se declara y no se valida |
| `capabilities` | O | objeto | ver A.5 | lo que pida de más apaga el módulo al instalar |
| `tools` | O | lista | ver A.7 | al menos una |
| `view` | – | texto | `"view.json"` | nombre de archivo dentro del paquete (ver A.6) |

### A.2 `brand` — la marca

| campo | | qué poner | regla |
|---|---|---|---|
| `shortName` | R | el nombre con que la gente la conoce: «Delfos», no la razón social | 28. Es **la línea grande** de la tarjeta, el detalle, el alta y el journal |
| `slogan` | – | una línea corta en la voz de la institución | 40. Va debajo del nombre en la tarjeta, en lugar del `title` |
| `logo` | R | ruta a un archivo del paquete: `brand/logo.png` o `web/icon-512.png` | png, jpg, webp o svg; **64 KB**; **512 px** de lado (svg sin tope de px). Cuadrado se ve mejor. **Nunca una URL** |
| `color` | R | `#0E7C86` | exactamente `#rrggbb`. Es el tinte del icono; el Host garantiza el contraste. No se acepta CSS |
| `banner` | – | ruta a una foto ancha: `brand/banner.jpg` | png, jpg o webp; **200 KB**; **1600 px**. Sin svg. Va detrás de la tarjeta con el nombre y el eslogan encima |
| `font.family` | – | «Nunito Sans» | 40. Solo para el **nombre de marca**; el resto va en la letra de Gnothi |
| `font.file` | – con `family` | ruta a `brand/NunitoSans-800.woff2` | woff2, woff, ttf u otf; **160 KB**; el peso en que se escribe el nombre |
| `font.bold` | – | un segundo archivo para la negrita | mismas reglas |

Si el grupo falta, Gnothi toma `icons` y `theme_color` del `manifest.webmanifest` de `web/`.

### A.3 `publisher` — quién publica y qué versión es

| campo | | qué poner | regla |
|---|---|---|---|
| `domain` | R | `delfos.cr` | un dominio, **sin** `https://` ni ruta. Es el único campo del grupo que Gnothi **contrasta**: acepta `eventos.delfos.cr` como suyo y rechaza `delfos.cr.malo.com` |
| `legal` | – | «Laboratorio Clínico Delfos S.A.» | 64. Se muestra en la franja de procedencia, junto al dominio |
| `taxId` | – | la cédula jurídica: `3-101-123456` | texto libre |
| `jurisdiction` | – | `CR` | texto |
| `version` | R | `2.4.0` | semver del **módulo** (no del formato). Sube en cada publicación; con `version` en el `.well-known`, Gnothi anuncia «hay una versión nueva» |
| `released` | – | `2026-08-12` | `aaaa-mm-dd` |
| `license` | – | `propietaria` | texto |

### A.4 `about` — qué es esto, para la persona

| campo | | qué poner | regla |
|---|---|---|---|
| `category` | R | una de: `laboratorio`, `salud`, `supermercado`, `comercio`, `banca`, `seguros`, `combustible`, `taller`, `telecom`, `educacion`, `gobierno`, `servicios` | lista cerrada; de ahí sale el icono cuando no hay logo. Otra cae al icono genérico con un aviso |
| `description` | R | dos líneas en segunda persona: «Tus resultados de laboratorio, con su historia y qué te toca repetir.» | 180. Es el primer texto escrito para la persona, no para el modelo |
| `doesNotDo` | R | el anti-claim: «No interpreta resultados ni reemplaza a tu médico.» | 120. Lo lee la persona en el alta |
| `locale` | **R** | `es-CR`, `en-US` | BCP 47. Es el idioma **del registro**: productos, analitos, frases. Gnothi se lo dice al modelo en cada parámetro de texto libre. Sin él, «milk» contra «leche» encuentra cero |
| `currency` | – | `CRC`, `USD`; vacío si no maneja plata | sin esto, los montos llegan sin unidad y el asistente no sabe de qué son |
| `units` | – | `mg/dL, g/dL, ng/mL` | texto; no se ve, lo usa el asistente |

### A.5 `capabilities` — lo que el módulo necesita

| campo | | valor esperado | qué pasa si no |
|---|---|---|---|
| `network` | O | `false` | `true` cuenta como exceso: el módulo entra **apagado**. Los eventos los trae Gnothi |
| `folder` | O | `"own"` | otro valor cuenta como exceso |
| `speaker` | O | `false` | `true` = exceso |
| `microphone` | O | `false` | `true` = exceso |

Un módulo de datos no necesita ninguna. Que sea contrastable es lo que vale: un módulo de
facturas que pide micrófono no necesita análisis, necesita un aviso.

### A.6 `journal` y `view.json`

**`journal`** — lo que la institución promete sobre el registro:

| campo | | qué poner | regla |
|---|---|---|---|
| `compression` | – | `None` o `GZip` | **se fija al crear el registro y no se puede cambiar**; pedir otro modo después lanza excepción |
| `encryption` | – | `None` o `Aes256Gcm` | igual: decidirlo **antes** del primer asiento. Activarlo después es empezar un registro nuevo |
| `maxFileSize` | – | `4194304` | bytes; rotación de archivo. Igual: fijo al crear |
| `retention` | R | «5 años» | texto para la hoja del alta |
| `expectedSize` | R | «~2 MB al año» | texto para la hoja del alta |
| `updates` | R | «al abrir la app» | texto para la hoja del alta |
| `reconstruible` | R | `true` si todo se puede volver a traer del publicador; `false` si lleva aportes de la persona que no están en otro lado | ausente no es `true`: Gnothi no vacía un registro que no sabe reconstruible |

**`view.json`** — seis bloques, en el orden que la institución quiera; un tipo desconocido se
ignora y el resto se pinta. Renderer pendiente.

| bloque | campos | tope |
|---|---|---|
| `texto` | `cuerpo` | 240; hasta 2 bloques |
| `datos` | `filas: [{ etiqueta, valor }]` | hasta 6 filas; etiqueta 24, valor 48. Hechos de la institución, iguales para todos |
| `aviso` | `cuerpo`, `hasta` (**obligatorio**, `aaaa-mm-dd`, máximo 90 días) | 140; uno solo. Sin fecha no se pinta: un aviso sin vencimiento es publicidad |
| `contacto` | `telefono`, `sitio`, `ayuda` | uno solo; son datos, no botones |
| `salida` | `cuerpo`: cómo darse de baja **del servicio** (distinto de apagar el módulo) | 160; uno solo |
| `enlace` | `texto`, `url` | hasta 2; se abre en el navegador de la persona, afuera de Gnothi |

Nada de html, css, script ni imágenes. Ningún valor puede ser un dato de la persona.

### A.7 `publicador` y `tools`

**`publicador`** — dónde viven los eventos:

| campo | | qué poner | regla |
|---|---|---|---|
| `eventos` | – | `https://eventos.delfos.cr/` | la URL base del servicio de eventos (ver 3.4). Gnothi la **fija** al instalar. Sin este grupo el módulo vive de lo que ya tiene |
| `recurso` | – | `expediente`, `cliente`, `vehicle` | el recurso de la persona; por omisión `expediente` |
| `id` | – | `1`, un VIN, una cédula | cuál instancia; por omisión `1`. **Ojo:** el paquete es el mismo para todos los clientes, así que hoy este `id` identifica al recurso ante el publicador y la credencial de Gnothi acota a quién puede leerlo |

**`tools[]`** — cada consulta:

| campo | | qué poner | regla |
|---|---|---|---|
| `name` | O | `evaluar_tendencia` | el nombre real sobre el cable; `snake_case`; sin el prefijo del módulo. Se comprueba contra lo que `tools()` devuelve |
| `title` | R | «Cómo viene un analito» | lo que lee la persona en el interruptor |
| `description` | O | la pregunta que contesta, y lo que **no** dice | viaja tal cual al modelo |
| `readOnly` | – | `true` | por omisión `true` |
| `input[]` | O (puede ser `[]`) | por parámetro: `name`, `type`, `description`, `enum` | `type` en `string`, `number`, `integer`, `boolean`; `enum` como lista de textos. Tienen que coincidir con lo que `call` acepta |

### A.8 Lo que responde el módulo

| qué | forma | regla |
|---|---|---|
| respuesta de una tool (`call`) | JSON como texto | la forma la elige la institución. Recomendado: `hayDatos`, `ultimoEventoLeido`, y para un «no» un `motivo` legible. Nunca una excepción sin capturar: devolver `{ ok: false, motivo }` |
| `tools()` | JSON array de textos | los mismos `name` del manifiesto, ni uno más ni uno menos |
| `deliver(sobre)` | `{ ok, asentados, ultimo }` | idempotente: un evento ya asentado no se vuelve a asentar |
| respuesta a `listar`/`leer`/`escribir` (las escribe Gnothi) | `{ ok, rutas }` · `{ ok, contenido }` · `{ ok }`, siempre con `motivo` | claves en español; contrato |

### A.9 Lo que NO escribe la institución

`installation.json` lo escribe Gnothi al instalar y el paquete no debe traerlo. Para saber qué
decide ahí: `shell` (cómo corre el módulo), `grantedFolder` (la única carpeta), `enabled` y
`disabledTools` (los interruptores de la persona), `provenance` (quién sirvió el paquete y
cuándo), `signer` (el pin de la llave). Ninguno de esos es declarable por el manifiesto: eso es lo
que hace que la institución no pueda elegir cómo se la ejecuta ni encenderse sola.

---

## Fuentes en el repo

`Gnothi.Host/Manifest.cs` · `PackageInspector.cs` · `PackageSignature.cs` · `HostTools.cs` ·
`Gnothi.Movil/WebModule.cs` · `BundleServer.cs` · `ModuleEnrollment.cs` · `GnothiCourier.cs` ·
`Herramientas/README.md` y los dos ejemplos · `Gnothi.Mobile/armar-bundle.ps1` ·
`PaginaDeLaInstitucion/Program.cs` · `Examenes/EndPointDeEventos/Program.cs` ·
`Documentacion/Especificacion/11` §6–7 y `13` §7 · `Documentacion/Jonathan/B6` ·
`NotasGnothiHost` 42, 43, 44 (fuera del repo). El módulo `holajs` vive en el scratchpad de la sesión
del 2026-09-10, no en el repo.
