# 11 — Móvil: especificación del flujo (Android e iOS)

**Corte 2026-09-02, rev. 2.** Reemplaza a `11-android-especificacion.md` (app nativa + IPC — descartada: ver §9) y absorbe a `12-ios-especificacion.md`. Las diferencias de plataforma quedan en `12`. Escrita contra `master_gnothiHost` @ `aa19ef1`, el spike browser-wasm de Puppeteer (rama `wasm-replay`) y la investigación PWA/OPFS/tiendas.

**Vocabulario:** **Gnothi** = nuestra app. **La PWA** = la app web de la institución (Blazor WASM), que existe antes de Gnothi y que dentro de Gnothi **es el módulo**.

---

## 0. En una frase

> **En móvil, el módulo es la PWA de la institución corriendo dentro de un WebView que Gnothi controla. La PWA trae su sustrato; a cambio no tiene ningún derecho sobre el dispositivo de Sonia — ni siquiera red. Gnothi la alimenta. El módulo es el escritor único de su journal; Gnothi es el cartero.**

Por qué este flujo y no otro:

- **No hay `exec` en móvil** ⇒ nada de proceso hijo.
- **Las tiendas prohíben cargar código descargado en el proceso** (Play: dex/JAR/IL; Apple: todo salvo WebKit/JSC) ⇒ nada de IL in-process.
- **La única excepción que ambas nombran es JavaScript/wasm en un WebView** ⇒ ahí va el módulo.
- **iOS no tiene servicio entre apps de distintos desarrolladores** ⇒ nada de "la institución expone un servicio".
- **Y en un WebView, Puppeteer ya corre** — medido en el spike Tetris: motor completo, 29 ms de hidratación, dominio sin cambios.

Las cuatro restricciones apuntan al mismo lugar. No es una elección: es lo que queda.

---

## 1. Los actores

```
   asistente ──Puerta 3──► (Android: carátula 17 KB · iOS: App Intents estáticos) ──►┐
                                                                                      │
   asistente ──Puerta 2──► Gnothi.AgentLoop ─────────────────────────────────────────► Gnothi
                                                                                      │  Dispatcher
                                                                                      │  WebViewShell
                                                                                      ▼ evaluateJavaScript
                                              ┌── WebView "laboratorio" ─────────────────────┐
                                              │  origen: gnothi-mod://laboratorio            │
                                              │  dotnet.wasm + Puppeteer + dominio + UI      │
                                              │  actor  ← ESCRITOR ÚNICO                     │
                                              │  CSP: connect-src 'none'  ← sin red          │
                                              │  journal en MEMFS ──────────┐                │
                                              └─────────────────────────────┼────────────────┘
                                                                            │ flush (bridge JS→nativo)
                                              Gnothi nativo ────────────────┴──► <contenedor>/journals/laboratorio/
                                              módulo pide (after=N) ──► Gnothi nativo ──HTTPS──► publicador ──► deliver() ──► módulo asienta
```

Un proceso de app. N WebViews, uno por módulo, creados por demanda. **Cero IPC entre apps.**

---

## 2. La PWA tiene dos modos — y esto es lo primero que hay que construir

| | **standalone** ("app primero") | **hospedada** (dentro de Gnothi) |
|---|---|---|
| dónde corre | en el browser de Sonia, desde la URL de la institución | en un WebView de Gnothi, desde la caché de Gnothi |
| red | **sí** — hace su propio pull del publicador | **no** — CSP `connect-src 'none'`; recibe eventos por `gnothi.deliver()` |
| storage | el del browser: carpeta real (Chromium Android ≥132) u OPFS | MEMFS + flush al bridge → archivos de Gnothi |
| escritor | ella misma | ella misma |
| derechos sobre el dispositivo | los que el browser le dé | **ninguno** |

**El dominio y el actor son idénticos en los dos modos.** Lo que cambia son dos adaptadores inyectables: **la fuente de eventos** (pull propio vs `deliver`) y **el destino del flush** (storage del browser vs bridge). Es el mismo principio de siempre — mismo actor, cambia la carátula — aplicado dentro de la PWA.

⚠️ **No quitarle la red a la standalone.** "Sin derechos" es una propiedad **del modo hospedado**. La standalone es la etapa "app primero" y necesita su pull.

---

## 3. Cómo Gnothi carga el módulo: como un browser, con caché propia

1. Sonia agrega el módulo: la URL de la institución, o un enlace en su página.
2. Gnothi **descarga el bundle web una vez** y lo guarda en su contenedor.
3. Gnothi lo sirve al WebView por **un esquema propio por módulo**: `gnothi-mod://laboratorio/…` (Android: `WebViewAssetLoader` con host propio; iOS: `WKURLSchemeHandler`).

**El esquema propio es el aislamiento**, no una comodidad: cada módulo tiene un **origen** distinto ⇒ el motor web separa almacenamiento y contexto por la misma regla de same-origin que aísla dos sitios. Ningún módulo ve a otro, y no hay que escribir una línea para lograrlo.

⚠️ **Corregido el 2026-09-03 por el spike.** El esquema propio (`gnothi-mod://…`) tiene un riesgo que la rev. 2 no consideraba: **un esquema propio no es contexto seguro**, y sin contexto seguro se caen OPFS, `crypto.subtle` y todo lo que la PWA usa para guardar. El spike sirvió el módulo en un **host virtual sobre https** — `https://laboratorio.gnothi.local/` — interceptando con `ShouldInterceptRequest`, que **atiende antes de la red**: no hay DNS ni TLS, el host no existe fuera del aparato y los bytes salen de los archivos de Gnothi. Con eso **funcionó** (§12). El aislamiento no se pierde: **un host virtual por módulo sigue siendo un origen por módulo**, que es lo que este parágrafo pide. `WebViewAssetLoader` usa exactamente este patrón por la misma razón. En iOS el `WKURLSchemeHandler` sí registra un esquema propio: **queda por medir si ahí el origen es contexto seguro**.

---

## 4. Incapacidad, de verdad

⚠️ **Corregido el 2026-09-03 contra una medición.** La CSP de la rev. 2 —`default-src 'self'; connect-src 'none'; frame-src 'none'; worker-src 'none'`— **impide que el módulo arranque**. No lo deja sin red: no lo deja existir. Medido sirviendo el bundle publicado de Exámenes con esa cabecera exacta:

- `connect-src 'none'` bloquea el `fetch` de **`blazor.boot.json`**, con el que el propio runtime se carga. Consola: *"Connecting to …/blazor.boot.json violates … connect-src 'none'"* y después *"Failed to start platform"*. Pantalla en blanco.
- `default-src 'self'` sin `script-src` bloquea además el **script en línea** del `index.html` del bundle.

**La CSP que Gnothi inyecta al servir cada módulo es esta:**

```
Content-Security-Policy: default-src 'self';
                         script-src 'self' 'wasm-unsafe-eval' 'sha256-<hash del inline del index>';
                         style-src  'self' 'unsafe-inline';
                         connect-src 'self';
                         frame-src 'none'; worker-src 'none'
```

**Verificado con esa cabecera:** el bundle carga entero y la app se dibuja completa; lo único que queda bloqueado es el **service worker** (`worker-src 'none'`), que es justamente lo que se quiere bloquear.

**Por qué `'self'` no afloja el invariante.** El origen del módulo es `gnothi-mod://laboratorio`, y **lo sirve Gnothi desde archivos locales de su contenedor**. `connect-src 'self'` le permite pedir *lo que Gnothi ya le dio*, y nada más: el publicador es **otro origen** y le sigue estando prohibido. *"El módulo nunca sale a la red"* se sostiene igual, y ahora el módulo además arranca.


**EL HASH SE CALCULA AL SERVIR, no se escribe.** Un hash a mano se desincroniza en silencio la primera vez que la institución recompila su bundle, y el síntoma es una pantalla en blanco sin ninguna pista. Gnothi tiene el archivo en la mano cuando lo sirve: lee el documento, saca los `<script>` sin `src`, y arma `script-src` con un hash por cada uno. Implementado y corriendo en el spike (`Servidor.CspPara`). Es lo que hace la CSP viable de verdad: la alternativa —`'unsafe-inline'` en `script-src`— anularía la protección entera.

**CONFORMANCE, y está falsada (2026-09-03).** No alcanza con que el módulo *no intente* hablar con el publicador: la especificación dice que **no puede**. El spike inyecta una sonda que hace exactamente el `fetch` prohibido, al mismo publicador que Gnothi sí alcanza:

```
módulo   fetch('http://…/expediente/1/events?after=0')  →  "negado por el motor web: Failed to fetch"
Gnothi   GET   http://…/expediente/1/events?after=0     →  3 677 bytes
```

Los dos, en la misma corrida y contra el mismo endpoint. **La contención la impone el motor web, no nuestra disciplina** — que es toda la diferencia entre una promesa y una incapacidad. Si algún día esa línea dice "PUDO", el spike lo grita.

`worker-src 'none'` ⇒ sin service worker, sin push. **Es el motor web negándose, no una regla nuestra que el módulo pueda violar.** En escritorio esto es promesa; en móvil es incapacidad — y aplica igual en Android y en iOS.

**Dos detalles que el hash y el estilo imponen.** `'wasm-unsafe-eval'` es obligatorio: sin él el motor no puede compilar wasm. Y `style-src` necesita `'unsafe-inline'` porque los *atributos* `style` no se cubren con hash (haría falta `'unsafe-hashes'`); es un riesgo mucho menor que el de script, y la alternativa —que la plantilla de la institución no emita estilos en línea— es un requisito de conformance, no de la CSP. El hash del script en línea **lo calcula Gnothi al servir el bundle**, que es quien lo tiene en la mano.

---

## 5. El journal

### 5.1 Dónde vive
**En archivos del contenedor de Gnothi**, un directorio por módulo. **No en OPFS**, por dos razones: en OPFS sería opaco incluso para Gnothi, y **el cuarto pilar** —Gnothi responde lo que ningún módulo puede responder de sí mismo— exige poder hacer `stat` desde afuera.

### 5.2 Cómo escribe un actor síncrono sobre una API asíncrona
Es el patrón del briefing wasm (`notes/wasm-mvp-briefing.md` §5), sin cambios:

```
ARRANQUE    Gnothi nativo lee los archivos del journal → bytes → MEMFS del wasm
            → ConfigureStorage(FileSystem, path=/journal) → replay secuencial, síncrono ✅

OPERACIÓN   PerformCommand escribe en MEMFS y retorna → el módulo manda los bytes nuevos
            al bridge → Gnothi nativo los ANEXA al archivo, en el orden recibido
```

**Gnothi nativo es el brazo del módulo, no un segundo autor.** Nunca compone un registro; nunca escribe si el módulo no escribió. El seam natural es `Diary.OnRecordWritten` (`internal`; pide un shim público mínimo): entrega `(entryId, bytes)` exactamente como el motor los escribió.

**Trade-off a declarar en la UI:** entre el commit del actor y el flush hay una ventana en la que un cierre abrupto pierde entradas. **No presentarlo como durabilidad transaccional.** Flush por comando, no por timer.

### 5.3 Ajustes fijos
`compression`, `encryption`, `maxFileSize` se graban al crear el journal y **no se cambian después**. Son campos del manifiesto. `maxFileSize` default 4 MB queda debajo del tope de ~10 MB por archivo reportado para `WKWebView`.

---

## 6. Gnothi como cartero

Gnothi nativo habla con el publicador —**endpoint fijo por módulo**, registrado al instalar, no elegible por el módulo— **cuando el módulo se lo pide**, y le devuelve el payload:

```
módulo ──gnothi.pull(after=N)──► Gnothi nativo ──HTTPS GET …/events?after=N  (Bearer DE GNOTHI)──► publicador
módulo ◄──deliver(payloadJson)── Gnothi nativo ◄──────────── sobre {account, events, last} ─────────┘
   │
 check → asienta
```

- **El cursor es del actor, no de Gnothi.** Lo deriva del replay (`UltimoEventoProcesado()`), exactamente como hoy la standalone en `BucleDeRefresh.Ejecutar`. Gnothi valida que `after` sea un entero y **no guarda ningún cursor**: no hay dos verdades sobre "hasta dónde leí". *(La rev. 2 decía "cursores en almacenamiento de Gnothi"; corregido — `13` §8.)*
- **Tokens y reintentos:** almacenamiento de Gnothi (Keystore / Keychain), **fuera del journal** y **fuera del WebView**: el módulo nunca ve la credencial; con `connect-src 'none'` tampoco podría usarla. ⚠️ **Hoy la standalone journaliza device key, access y refresh token** (`Bitacora.cs`, `ExamenesAPI.cs`); sacarlos es prerrequisito de cualquier traspaso — `13` §2.
- Payload malformado o de emisor no autorizado: **cero asientos**. Un error de ejecución propio: **sí** queda auditable.
- **El pull ocurre cuando Sonia abre el módulo** (o cuando Gnothi pasa a primer plano, §12). `deliver` exige un WebView vivo; un pull en background solo puede **dejar los bytes en tránsito** (fuera del journal) para entregarlos al abrir — optimización, no día 1 (`13` §8). El módulo **no** despierta solo: sin worker, sin red, sin proceso propio.

---

## 7. La superficie del módulo hacia Gnothi

Las clases `[ModuleSurface]` / `[Query]` de `Gnothi.Contract`, expuestas al JS. **Son DOS direcciones con DOS mecanismos**, y la rev. 2 las mezclaba en un solo objeto (corregido 2026-09-03, ver §15):

**Gnothi → módulo** — lo publica el módulo, lo llama el nativo con `evaluateJavaScript`:

```
window.gnothi.tools()                → Promise<string>   // declarado vs real
window.gnothi.call(tool, argsJson)   → Promise<string>   // la consulta, contra el actor VIVO
window.gnothi.deliver(sobreJson)     → Promise<string>   // el cartero entrega; devuelve el resultado
```

**Módulo → Gnothi** — lo **inyecta Gnothi** en el WebView (`window.gnothiPuente`), no lo publica el módulo:

```
pull(after)                     el módulo pide eventos desde SU cursor; Gnothi releva (13 §8)
listar() · leer(ruta) · escribir(ruta, base64)    el journal, contra los archivos de Gnothi
```

⚠️ **El puente contesta por callback, no por retorno.** En Android los métodos de un objeto inyectado con `@JavascriptInterface` son **síncronos**: la página se queda dura hasta que vuelven, y un `pull` hace HTTPS. Así que el puente recibe un número de pedido y contesta después llamando a `window.gnothi.respuesta(id, json, error)`; del lado web eso se envuelve en una promesa. La misma forma sirve en iOS, donde `WKScriptMessageHandler` tampoco puede devolver.

**Detección de modo: por la presencia del puente, no por el origen.** Lo que la app necesita saber no es cómo se llama su origen sino **si tiene con quién hablar**; un origen que diga "hospedado" sin puente no sirve para nada.

`WebViewShell : IShell`:
```csharp
public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? args, CancellationToken ct)
    => webView.EvaluateJavaScript($"gnothi.call({Json(tool)}, {Json(args)})");   // opaco, se reenvía

public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
    => webView.EvaluateJavaScript("gnothi.tools()");                             // declarado vs real
```

**`Dispatcher.Serve` no cambia una línea.** Consentimiento, `Meter`, procedencia: igual que en escritorio.

---

## 8. La UI de la institución

**La PWA se renderiza a sí misma.** Sonia abre el módulo en Gnothi y ve el dashboard de la institución — su marca, su Blazor, sus gráficos. Eso cambia la forma de una regla, no su fondo:

- **Antes:** *"el módulo describe, el Host renderiza"* — para que el módulo no tuviera iniciativa.
- **Ahora:** el módulo se dibuja, pero **dentro de un WebView que solo existe cuando Sonia lo abre**, sin service worker, sin push, sin red. *"No despierta, no notifica, no interrumpe"* se sostiene **por contención** en vez de por declaratividad.

Consecuencia práctica: **el `view.json` y su renderer dejan de hacer falta en móvil.** La view es la PWA.


**IMPLEMENTADO Y CORRIENDO el 2026-09-03** (Xiaomi 14T Pro). Sonia abre el módulo desde su ficha en Gnothi y ve el tablero del laboratorio a pantalla completa: su marca, su Blazor, sus gráficos, y el aviso de que su expediente se está guardando en Gnothi. El botón atrás lo cierra y devuelve a Gnothi con su estado intacto.

**Es EL MISMO WebView que contesta las preguntas del asistente.** No hay dos. Por eso reabrirlo es instantáneo si el asistente ya le había preguntado algo: medido, la segunda apertura no vuelve a cargar el bundle.

**La regla que esto cambia, y es la única:** mientras el módulo está en pantalla **no se puede desalojar**, aunque cueste sus ~200 MB. En cuanto Sonia sale, vuelve a ser desalojable como cualquier otro. `AnfitrionDeModulosWeb.Desalojar` respeta lo visible.

**Lo que NO cambia:** la contención. Sigue sin red, sin service worker y sin proceso propio, y deja de existir cuando Gnothi lo desaloja. *"No despierta, no notifica, no interrumpe"* se sostiene por incapacidad, no porque el módulo se porte bien.

Lo que **no** cambia: el canal de atención que sigue abierto es **el nombre y la descripción de las tools** que Gnothi le entrega al asistente. Eso lo controla Gnothi, no la PWA.

---

## 9. Por qué se descartó "app nativa de la institución + IPC" (la rev. 1 de este documento)

Era válida en Android y no existía en iOS. Comparada con este flujo:

| | nativa + IPC | **PWA en Gnothi** |
|---|---|---|
| app por institución en tienda | un APK cada una | **ninguna** |
| IPC entre apps | sí, con verificación de firma | **ninguno** |
| "sin red" | regla (la institución podía saltarse a Gnothi) | **incapacidad** |
| escritor | la institución, fuera de Gnothi | **el módulo dentro de Gnothi** |
| medir desde afuera | ❌ | ✅ |
| iOS | **imposible** | ✅ |
| lo que el equipo ya tiene | había que construir la app nativa | **`Examenes.Wasm`, `Compras.Wasm`, `Facturas.Wasm` ya existen** |

Lo único que la nativa tenía mejor: el actor escribía directo, sin ventana de flush. Se acepta el trade-off.

---

## 10. El sustrato: instancia por módulo, bytes una sola vez *(optimización, no día 1)*

**La instancia** del runtime es por módulo — cada WebView instancia su `dotnet.wasm`, y Gnothi no puede meterle un runtime a un wasm ajeno. **Los bytes, en cambio, no tienen por qué viajar con la institución.**

El handler de Gnothi que sirve el bundle puede interceptar las rutas del runtime (`dotnet.wasm`, `Puppeteer.wasm`, `Choreography.wasm`, la BCL) y **servirlas desde el bundle de Gnothi**, iguales para todos los orígenes. La institución publica **solo dominio + superficie + UI** — decenas de KB.

⇒ **Una sola copia del motor que auditar, y el módulo chico vuelve a móvil.**

**[SUPUESTO]** Costos conocidos: acoplamiento de versión (mismo problema que `substrate.range` + journals dorados), y Blazor verifica hashes de integridad en `blazor.boot.json` — Gnothi tendría que servir un boot coherente. **Empezar con la PWA completa; medir; después esto.**

---

## 11. "App primero" y la migración

1. **Antes de Gnothi:** la PWA standalone, único escritor, con su pull y su storage del browser.
2. **Al instalar Gnothi:** descarga el bundle y crea el módulo. El alta entra por un enlace **al dominio de Gnothi** puesto en la página de la institución (`13` §7).
3. **Los hechos institucionales se re-piden al publicador:** el módulo nuevo arranca con cursor 0 y pide todo **una vez** (§6). **No hay que migrar el journal para eso.** Gnothi necesita **su propia** concesión ante el publicador (`13` §3); el traspaso **no lleva credenciales**.
4. **Las aserciones propias de Sonia** hechas en la standalone sí se migran, **una vez**, con un toque de ella ("Pasar mis datos a Gnothi"). **No es un export del journal** (la Ruta 3 está descartada): el journal no se mueve, solo cruza lo que Sonia afirmó. Hoy en Exámenes es **un dato** (`DeclararSexo`) y viaja como parámetro del enlace del handoff; el mecanismo queda **genérico** porque otras instituciones esperan más aserciones de Sonia: archivo `text/plain` por el share sheet (Web Share **rechaza** `.json`) o la carpeta compartida. Gnothi las entrega con `deliver`, el módulo las asienta con `autor=sonia`, y `check` las hace idempotentes: **repetir el traspaso no duplica** (`13` §5).
5. **La marca de que la migración ocurrió vive en el publicador** (`account.state = migrated` en el sobre de `/events`): nada del lado web sobrevive a "borrar datos". La standalone la lee en su próximo pull y **queda en solo lectura**: sigue mostrando lo que tenía, deja de sincronizar y ofrece abrir Gnothi (`13` §4, §6).
6. La standalone **pasa la mano** solo con un toque de Sonia — Android: `intent://…;package=<gnothi>;S.browser_fallback_url=…` desde un `onclick`; iOS: `<a href>` a un universal link de Gnothi (**no se dispara desde JS** y **no hay forma de detectar** si Gnothi está instalado). "Desaparecer" **no es desinstalar**: el icono lo quita Sonia.

**Es regla temporal, no incapacidad** — se dice así. Huecos cerrados, propuestas y lo que queda por medir: `13`.

---

## 12. Cold start y memoria — MEDIDO el 2026-09-03 en Android

**El spike que iba primero ya corrió.** La PWA **real** de Exámenes, el bundle publicado, dentro de un `WebView` de Android, servida desde los assets de la app con la CSP corregida de §4, arrancando en frío con la app cerrada. Instrumento: emulador Pixel 6 · Android 36 · x86_64, WebView `133.0.6943.137`. Código: `Spikes/Lab.WebViewWasm`.

| | |
|---|---|
| proceso → módulo dibujado | **3 364 ms** |
| navegación → módulo dibujado (el runtime wasm) | **2 650 ms** |
| documento servido y listo | 1 519 ms |
| PSS del proceso con el módulo vivo | **145 MB** |
| bundle dentro del apk, crudo | 11.2 MB en 76 archivos |

**Lo que contesta.** El runtime wasm **arranca dentro de un WebView**, el actor arranca con él —logcat: *"Starting Puppeteer.EventSourcing.ActorHandler's Actor"*—, el módulo crea su journal y la app queda **operativa**: dibuja su dashboard y responde. El camino móvil de este documento **existe**; dejó de ser teoría.

**Lo que confirma, y agrava.** Los segundos de arranque son reales: **~2.6 s solo el runtime**, y eso en un x86_64 con el CPU de una máquina de escritorio. La mitigación no cambia: crear el WebView del módulo cuando Gnothi pasa a primer plano, no cuando llega la pregunta. Y la memoria: **145 MB de PSS con UN módulo vivo** (incluye el runtime .NET del host y el WebView). **Quince módulos vivos no caben — y ahora está medido.** Instanciar por demanda y desalojar es obligatorio, no optimización, y no pierde nada porque el módulo no inicia nada.

- **Hidratación:** 29 ms medidos en browser-wasm con journal chico. Falta con años.
- **Payload: MEDIDO el 2026-09-03, y la duda se cierra del lado del peso.** El bundle publicado de Exámenes (`Examenes/PaginaDeDescarga/publicado`, 27-ago) **ya es Release con el linker activado** — `blazor.boot.json`: `debugLevel: 0`, `linkerEnabled: true`, `globalizationMode: sharded`. Pesa **3.6 MB en brotli** por la red y 11 MB sin comprimir (20 MB en disco contando las tres variantes que sirve el publish). Es decir: **pesa como 5, no como 35**; los ~35 MB eran el Debug. Lo que sigue sin medir es el **arranque** y la **memoria**, que son el costo dominante — y §10 puede bajarlo aún más para la institución.



### 12.1 · El modo hospedado, corriendo de verdad (2026-09-03)

El spike dejó de cargar la PWA "a secas": ahora Gnothi le inyecta su puente y el módulo corre **hospedado**, con la PWA republicada desde el repo. Instrumento: el mismo emulador; el publicador es el mock en la máquina anfitriona (`10.0.2.2:8095`). Código: `Spikes/Lab.WebViewWasm/PuenteGnothi.cs`.

| | primera apertura, sin journal | reapertura, con journal |
|---|---|---|
| proceso → módulo dibujado | 2 938 ms | 2 004 – 2 545 ms |
| runtime wasm | 2 273 ms | 1 593 – 1 880 ms |
| PSS | 160 MB | 157 – 159 MB |
| lo que pidió el módulo | `pull after=0` → 3 586 bytes | `pull after=218` → 24 bytes |

**Lo que queda demostrado, y no por lectura de código:**

1. **El módulo corre sin red propia.** Con `connect-src 'self'` no puede alcanzar al publicador; pidió por el puente y Gnothi relevó.
2. **Gnothi es otro cliente ante el publicador.** Consiguió **su** credencial contra **su** device key (`device-gnothi-spike`), y el módulo nunca la vio. (§13 §3.2.)
3. **El cursor es del actor.** Al reabrir pidió `after=218` — salido de su propio replay —, no desde 0. Gnothi lo validó y lo relevó **sin guardarlo**. (§13 §8.)
4. **El journal vive en los archivos de Gnothi, y se puede mirar desde afuera del motor web.** `run-as … ls -lR files/journals/laboratorio` muestra `examenes/meta.bin` y `examenes/journal/journal_000001.bin`, y `cat` muestra el DSL del dominio en claro. Es exactamente lo que §5.1 exige y lo que OPFS **no** permite.
5. **El aislamiento de rutas es de Gnothi, no confianza en el módulo:** el puente resuelve cada ruta relativa y rechaza cualquiera que se salga del directorio del módulo.

⚠️ **Un error que apareció al correrlo, y se corrigió.** En modo hospedado la pantalla decía *"No se está guardando nada"*: el cartel miraba `HayCarpeta`, que es vocabulario del modo suelto y ahí es false. Le mentía a la persona justo en el sentido contrario al que este código cuida. Ahora dice dónde está guardando.

⚠️ **Estos números son del emulador, no de un teléfono.** Un x86_64 sobre CPU de escritorio no compila wasm como un arm64 de gama media, y el desalojo de memoria tampoco se comporta igual. **Falta repetir la corrida en un teléfono físico.** El A54 de los reportes anteriores es de otro desarrollador y no está disponible para esto; el aparato a mano es un **Xiaomi**, que además es interesante por otra razón: es justo uno de los fabricantes cuyas políticas de ahorro de batería el `12` señala como riesgo para el cartero. Ese es el número que va a la especificación como definitivo.


### 12.2 · En un teléfono de verdad (2026-09-03)

Xiaomi 14T Pro · Android 16 (SDK 36) · arm64-v8a · WebView 151 · 10.9 GB de RAM. El publicador es el mock en la máquina de desarrollo, alcanzado con `adb reverse tcp:8095`.

| | teléfono | emulador |
|---|---|---|
| proceso → dibujado, en frío sin journal | **1 970 ms** | 2 938 ms |
| runtime wasm, en frío | **1 610 ms** | 2 273 ms |
| PSS, en frío | **216 MB** | 160 MB |
| proceso → dibujado, reapertura | **1 253 ms** | 2 004 – 2 545 ms |
| runtime wasm, reapertura | **1 067 ms** | 1 593 – 1 880 ms |
| PSS, reapertura | **248 MB** | 157 – 159 MB |

**El tiempo mejora y la memoria empeora, y lo segundo es lo que importa.** El arranque del runtime cae a ~1.6 s en frío y ~1.1 s al reabrir: entre un tercio y la mitad más rápido que el emulador. Pero el PSS **sube a 216–248 MB por módulo vivo**, contra los ~160 MB del emulador. La regla de §12 no solo se confirma, se endurece: **con 248 MB por módulo, dos o tres vivos ya son un problema en un teléfono de gama media.** Instanciar por demanda y desalojar deja de ser recomendación.

**Todo lo demás reproduce igual que en el emulador:**

- La contención se sostiene en hardware real: la sonda que intenta el `fetch` al publicador recibe *"negado por el motor web"*, mientras Gnothi alcanza ese mismo endpoint por el puente.
- El cartero releva: `pull after=0` → 3 677 bytes en frío; `pull after=218` → 115 bytes al reabrir, con el cursor salido del replay del actor.
- El handoff, con el enlace exacto que arma la instalación suelta: primera vez `aplicadas=1, omitidas=0`; segunda vez, mismo enlace, `aplicadas=0, omitidas=1`.
- El journal queda en `files/journals/laboratorio/examenes/journal/` y se lee desde afuera del motor web.


### 12.3 · Gnothi sirviendo un módulo web, en el teléfono (2026-09-03)

Ya no es el spike: es **Gnothi.Mobile** con un módulo instalado de verdad —`"type": "webview"` en su `installation.json`— respondiendo por el **mismo `Dispatcher`** que usa en escritorio. Xiaomi 14T Pro.

```
1 modules installed
examenes: el bundle declara su base en /app/
examenes: Gnothi consiguió su credencial ante el publicador
examenes: pull after=0 -> 3677 bytes
comprobación de examenes: {"matches":true,"declared":["lista_de_examenes","resumen"],"actual":["lista_de_examenes","resumen"]}
  resumen contestó 122 bytes: {"hayDatos":true,"examenes":19,"analitosFueraDeRango":0,"ultimoEventoLeido":218}
```

**Lo que eso dice.** El Host cargó un módulo cuya cáscara es un WebView, le preguntó qué sabe hacer —y coincide con lo que declaró su manifiesto—, y le hizo una consulta que contestó con datos reales sacados del journal que él mismo escribió en el contenedor de Gnothi. `Dispatcher`, `Meter` y el consentimiento no cambiaron una línea.

**Tres cosas que rompieron al pasar del spike al producto, y las tres valen como advertencia:**

1. **El bundle declara dónde vive.** Su `index.html` trae `<base href="/app/">` horneado al publicar. Servirlo en la raíz del origen daba 404 en todo y **una página en blanco sin ninguna pista**. Gnothi ahora lee esa base del propio archivo y sirve ahí. La base es parte del contrato del bundle, no una decisión del Host.
2. **Tráfico en claro.** Android lo bloquea desde API 28, y está bien: el cartero habla HTTPS. Pero un túnel de desarrollo aterriza en el loopback, y sin permitirlo el cartero fallaba con *"Connection failure"* y el módulo se abría vacío. Se resolvió con una `network_security_config` que permite claro **solo en 127.0.0.1 y localhost**. No es una puerta: nada que venga de la red entra por ahí.
3. **El cartero fallaba en silencio.** Registraba el éxito y no el fallo, así que el síntoma era un módulo vacío sin causa visible. Ahora dice qué pasó, con la misma disciplina que el flush del journal: una degradación muda es peor que un error.

⚠️ **Y una fricción de plataforma:** en este Xiaomi, lo que `adb push` escribe en la carpeta externa de la app **la app no lo ve**. Hubo que ir por `/data/local/tmp` y copiar con `run-as`. El `demo-emulador.ps1` del repo ya documentaba ese rodeo para el emulador; queda confirmado que también hace falta en un aparato físico con HyperOS.

⚠️ **Este teléfono es gama alta, así que estos tiempos son el mejor caso de Android, no el peor.** Un aparato de gama media va a tardar más; la memoria, en cambio, probablemente no baje mucho. Falta medir en uno modesto antes de dar los tiempos por buenos.

⚠️ **Y apareció fricción de sideload que el análisis no había previsto.** Instalar por USB requirió, además de las Opciones de desarrollador habituales, dos interruptores propios de Xiaomi: *depuración USB (ajustes de seguridad)* e *instalar vía USB*, ambos atados a la cuenta del fabricante. Es una capa de fricción **por encima** de Play Protect y de la verificación de desarrolladores. Cuenta para la ruta de distribución fuera de tienda.

**El mismo spike, ahora en iOS.** En Android ya corrió (arriba). Falta la corrida equivalente en `WKWebView`, que es donde no hay nada medido, y la repetición en un teléfono físico.

---

## 13. Lo que este flujo cede

| se pierde | por qué |
|---|---|
| **durabilidad transaccional del último comando** | ventana de flush MEMFS → archivo |
| **Puerta 3 dinámica en iOS** | App Intents estáticos (ver `12`) |
| **"app primero" como app aparte, después de Gnothi** | la standalone se retira; la institución vive dentro de Gnothi con su UI intacta |
| **sustrato compartido, hasta §10** | una instancia de runtime por módulo |

Lo que **gana**, y en escritorio no se tiene: **incapacidad de red real**, **aislamiento por origen gratis**, y **el modelo original completo** en la plataforma donde está el mercado.

---

## 14. Medido · supuesto · a medir

| | |
|---|---|
| ✅ medido | Puppeteer completo en browser-wasm; hidratación 29 ms; dominio sin cambios; replay secuencial en `wasm-replay` |
| ✅ medido | MEMFS síncrono + flush asíncrono (briefing wasm) |
| ✅ medido | payload del bundle publicado de Exámenes: **3.6 MB brotli / 11 MB sin comprimir**, Release con linker (2026-09-03, §12) |
| ✅ medido | la CSP con `connect-src 'none'` **impide arrancar** al módulo (bloquea `blazor.boot.json`); la corregida de §4 lo deja cargar entero y solo frena el service worker (2026-09-03) |
| ✅ **medido** | **la PWA real de Exámenes corre dentro de un `WebView` de Android**: arranca el runtime wasm, arranca el actor, crea su journal y la app queda operativa (emulador Android 36 x86_64, WebView 133, `Spikes/Lab.WebViewWasm`, 2026-09-03) |
| ✅ **medido** | **el modo hospedado corre entero** (§12.1): el módulo sin red propia pide por el puente, Gnothi consigue SU credencial y releva, el cursor sigue siendo del actor (`after=218` al reabrir) y el journal queda en los archivos de Gnothi, legible desde afuera del motor web |
| ✅ **medido en teléfono** | Xiaomi 14T Pro (§12.2): runtime wasm **1 610 ms** en frío / **1 067 ms** al reabrir, y **216–248 MB de PSS** por módulo vivo. Más rápido que el emulador y **con más memoria**: la regla de desalojar se endurece |
| ✅ **medido** | **Gnothi sirve un módulo web por su propio `Dispatcher`** en un teléfono (§12.3): conformance declarado=real y la tool `resumen` contesta 19 exámenes / cursor 218 desde el journal que el módulo escribió en el contenedor de Gnothi |
| ✅ **medido** | **el módulo se dibuja a sí mismo dentro de Gnothi** (§8): Sonia lo abre desde su ficha, ve el tablero del laboratorio a pantalla completa, y el botón atrás vuelve a Gnothi. Es el mismo WebView que contesta las preguntas, así que reabrir es instantáneo |
| ✅ **medido** | **la contención está falsada** (§4): en la misma corrida, el módulo intenta el `fetch` al publicador y el motor se lo niega, mientras Gnothi alcanza ese mismo endpoint por el puente. Es incapacidad, no disciplina |
| ✅ **medido** | el hash del script en línea **se calcula al servir** y el módulo arranca con un bundle recompilado, donde el hash escrito a mano ya no valía |
| ✅ **medido** | arranque **2 650 ms** (runtime wasm) / **3 364 ms** (proceso→dibujado) y **145 MB de PSS** con un módulo vivo — **en emulador**, falta un teléfono físico (§12) |
| ✅ verificado (texto) | Play y Apple permiten JS/wasm en WebView; prohíben IL descargado |
| ✅ documentado | esquemas propios por módulo en ambas plataformas; `IsBrowser()` es `true` en WebView ⇒ el guard actual sirve |
| 🔍 supuesto | que la revisión de tiendas acepte a Gnothi cargando web content de terceros — encuadre: **"browser para la capa de módulos"**; validar |
| 🔍 supuesto | tope ~10 MB/archivo en `WKWebView` (reportado) |
| ❓ a medir | repetir §12 en un teléfono de **gama media**: el 14T Pro es gama alta y sus tiempos son el mejor caso (§12.2) |
| ❓ a medir | cold start total con historia larga |
| ❓ a medir | §10 — servir el runtime desde Gnothi con `boot.json` coherente |

---

## 15. Cambios en el repo

**Pacifico:**
1. ⚠️ **Ya NO hay que mergear `wasm-replay`, y no es prerrequisito de nada** (validado 2026-09-03). Esa rama quedó **119 commits atrás** de master; su último commit es del 18-ago y master va por el 2-sep. Mergearla arrastraría una foto de agosto sobre tres semanas de trabajo. **El carve ya vive en master por otro camino:** el split a `Ncubo.Puppeteer.Adapters.Sockets` dejó a `Choreography` compilable para browser-wasm sin el shared framework de ASP.NET Core, y `ChoreographyCoreDependencyTests` lo fija. La prueba es empírica: el paquete que el repo consume, `Ncubo.Puppeteer 3.0.0-beta.10029`, **no declara dependencia de ASP.NET Core** y **corre de verdad en browser-wasm** — el actor arranca y el replay funciona, medido el mismo día en el navegador de escritorio y dentro del `WebView` de Android (§12). **Se trabaja sobre master.** Lo único pendiente del lado del motor es **correr el pipeline sobre master y subir `VersionDelMotor`**, para que el paquete corresponda a los commits actuales: la versión es el contador de build del pipeline (`-beta.$(Build.BuildId)`), no un hash.
2. Shim público mínimo sobre `Diary.OnRecordWritten` para el flush. **No bloquea:** el flush de hoy sincroniza los archivos de MEMFS y el puente nativo puede hacer lo mismo. Es una mejora —bytes exactos en vez de copiar archivos—, no un requisito de arranque.
**Gnothi:**
3. **`WebViewShell.cs`** en `Gnothi.Host` — tercera implementación de `IShell`. Compila solo en targets móviles; excluir del build de escritorio o condicionar por target.
4. **`Gnothi.Mobile`** (Android): un WebView por módulo, `WebViewAssetLoader` con host propio, bridge `@JavascriptInterface`, CSP inyectada. **`Gnothi.iOS`** (nuevo): lo mismo con `WKWebView`, `WKURLSchemeHandler`, `WKScriptMessageHandler`.
5. **Manifiesto**: `bundle.url` del módulo web; endpoint del publicador para el cartero; mecanismo de enrolamiento y scope (`13` §7).
6. **`ModuleCatalog`**: fuente = manifiestos de bundles web descargados.
7. **Conformance nuevo**: el bundle **no hace `fetch`** (servirlo con la CSP y verificar que nada falla por red); y **el flush no compone registros** (bytes idénticos a los que el motor emitió).

**Del lado de la PWA (plantilla de la institución):**
8. Los dos adaptadores inyectables: fuente de eventos (`pull` | `deliver`) y destino del flush (browser | bridge).
9. `window.gnothi.{tools, call, deliver, respuesta}` — con **[JSInvokable] + DotNet.invokeMethodAsync**, que es lo que Blazor expone; `[JSExport]` es el interop crudo de wasm y acá no hace falta (medido funcionando 2026-09-03). `pull` NO va acá: viaja por el puente que inyecta Gnothi (§7).
10. Detección de modo: **hospedada si Gnothi inyectó su puente** (`window.gnothiPuente`), no por el origen (§7).

Lo que **no** cambia: `Dispatcher`, `ToolIndex`, `Meter`, `ConsentWatch`, `AgentLoop`, `Gnothi.Contract`, y **el dominio**.
