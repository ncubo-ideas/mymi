# B5 · El browser: por qué se descartó, y qué sí sirvió de él

El arquitecto lo cerró explícitamente:

> *«Ya se hizo un hola mundo que va al browser, pero **ese no es nuestro escenario**. Pero no está
> mal chequearlo, o sea que no se nos vaya a caer la aplicación porque alguien tomó esa ruta.»*

Este documento explica por qué la decisión es correcta —hay dos capas independientes de razones— y qué se
aprendió del andamio que vale conservar.

---

## Razón 1 · Una página no puede ser servidor MCP

Por las dos vías, y cualquiera alcanza:

**No puede escuchar.** Un servidor MCP sobre HTTP/SSE tiene que ser alcanzable en una URL. Una página del
browser **no tiene socket de servidor**: puede conectarse hacia afuera, nunca aceptar hacia adentro. No es
un permiso que falte — es que el modelo de seguridad del browser está construido sobre esa asimetría. Ni
el service worker sirve: intercepta fetches de *las páginas que controla*, no conexiones de procesos
ajenos.

**No puede ser spawneada.** Un servidor MCP sobre stdio requiere que el cliente **cree el proceso** y se
quede con sus tuberías. Una página no es un proceso que otro programa pueda lanzar así.

Está escrito como decisión en el POC: *«una página en el browser no puede hospedar un servidor MCP (no
puede escuchar conexiones entrantes), así que este proceso es inevitable.»*

**La inversión sí funciona:** un PWA puede ser *cliente* MCP, conectándose hacia afuera. O sea que podría
ser **la superficie del asistente**, no el servidor. Lo que no puede ser es el que expone datos.

### El arco se cierra: el PWA volvió, pero como vitrina

Y acá se cierra el arco. El browser **no murió del todo**: reapareció como **la superficie del comercio.**
El PWA wasm del comercio se hospeda **localmente** dentro de un shell WebView2 (`Comercio.Shell`, ver
`C7`), **separado del dato** — el módulo de compras es cargo cargado dentro de Gnothi, no vive en la
página. Eso confirma exactamente la Razón 1: **un PWA sirve de cliente y de vitrina, nunca de servidor.**
La vitrina quedó local; el dato quedó en el módulo.

Montar ese PWA localmente tiene sus propias trampas —el `base href` que hay que pasar de `/app/` a `/`,
sacar el service worker, reubicar el user-data de WebView2—; van en `E1`.

## Razón 2 · Y es la que cierra la discusión

**El journal adentro del browser no lo puede leer ningún otro proceso.**

Medido: OPFS e IndexedDB persisten pero **no dejan bytes que otro programa pueda abrir**. La File System
Access API sí deja archivos reales —y por eso es la que se usó— pero exige gesto de usuario, el permiso
caduca al reabrir, y el handle guardado queda colgando si la persona mueve la carpeta.

Así que el browser no puede ser el módulo **por dos capas distintas**: ni puede exponer la interfaz, ni
puede dejar el dato accesible.

> **Aclaración de hecho:** el prototipo **no usa OPFS**. Grep sobre todo el código: cero ocurrencias de
> `opfs`, `navigator.storage`, `storage.getDirectory`. Usa `showDirectoryPicker` + `getFileHandle` +
> `createWritable` (File System Access API) e IndexedDB **solo** para recordar el handle del directorio.
> La distinción no es cosmética: usar OPFS habría matado el caso de uso, porque el lector externo no
> podría abrir nada.

## Razón 3 · Y el pull tampoco va ahí

En el diseño, el que trae los eventos del publicador es el **Host**: trae los bytes y se los pasa al
módulo como parámetro, y el módulo asienta.

> **El Host es cartero, no escriba: no puede abrir el sobre ni dictar la carta.**

En el andamio lo hace la app del browser, que es exactamente al revés.

---

## Qué sí sirvió, y vale conservar

**Poblar el journal a mano.** Es la forma más rápida de tener un registro realista con una persona
apretando un botón, y de ver el ciclo completo — el proveedor publica, la persona refresca, el número se
mueve.

**Demostrar que el dominio cruza sin cambios.** El proyecto de dominio no tiene ni una dependencia y no se
le tocó una línea para correr en wasm. Eso es evidencia de portabilidad y de limpieza del dominio, y es
real.

**Y wasm funcionó como detector de porosidad falsable.** El patrón que se repitió:

> **Cada cosa que wasm rompió fue una fuga de infraestructura del motor** —primero un framework de
> servidor web, después hilos bloqueantes— **nunca del dominio.**

El compilador y el runtime trazan juntos el borde. Si el dominio de alguien necesita cambios para correr
confinado, **eso es el hallazgo**.

---

## La disciplina que hay que mantener: tres targets, no dos

Este es el reclamo legítimo que hizo el equipo, y la respuesta precisa.

Conviven artefactos de targets distintos en el repo, y es justo exigir saber cuál pesa antes de triagear
un bug. Pero son **tres**, no dos:

| pieza | qué es | hoy | mañana |
|---|---|---|---|
| el Host | lo que se construye | .NET escritorio | .NET nativo por plataforma |
| **el módulo** | lo que migra | **dos DLLs cargo, cargadas en proceso** | **WASI** |
| `Gnothi.Desktop` | WebView2 como **Hybrid** (BlazorWebView), queries al Dispatcher nativo | una cara del Host | sigue |
| `Comercio.Shell` | WebView2 como **host de un PWA** local (`SetVirtualHostNameToFolderMapping`) | vitrina del comercio | sigue |
| la app del browser | **andamio** | browser-wasm | se muere |

Las dos filas del medio existen para cortar una confusión: **no todo lo que tiene un WebView2 es el andamio
que se muere.** `Gnothi.Desktop` usa WebView2 como **Hybrid** —BlazorWebView, y las queries corren nativas
contra el Dispatcher, sin wasm ni puerto—; `Comercio.Shell` usa WebView2 como **cáscara de un PWA** servido
de disco. Ninguno de los dos es el andamio browser-wasm: ese sigue siendo el único que se muere. Ver `C7`.

Y el dato que corrige una confusión frecuente: **el POC del Host no es browser-wasm.** Sus tres proyectos
son `Microsoft.NET.Sdk`, `net9.0`, sin RID — consolas de escritorio hablando MCP por stdio. El único
artefacto de browser-wasm es la app andamio.

### Regla de triage

| síntoma | dónde vive |
|---|---|
| service worker, picker, MEMFS, estado de handle inválido | **andamio** — no toca al Host |
| `Cannot wait on monitors`, el guard de hilos | **el motor** — y pesa para WASI aunque el síntoma haya salido en el browser |
| manifiesto, catálogo, consentimiento, listado de consultas | **el Host** — escritorio puro |
| totales mal calculados, replay | **dominio / app** — común a los dos |

### Y un caso donde la cautela se justificó

El `Cannot wait on monitors` apareció **en el browser**, y la reacción natural es archivarlo como bug del
andamio. **No lo es:** el guard del motor tiene forma de plataforma, así que bajo WASI vuelve — en el
target que sí importa.

O sea que la advertencia es correcta en un sentido: **un bug del browser puede ser un bug de WASI
disfrazado.** Pero la inversa también: un error del picker no le va a llegar nunca a un módulo WASI.
