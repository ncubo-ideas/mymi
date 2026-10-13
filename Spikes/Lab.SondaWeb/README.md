# Lab.SondaWeb — ¿por qué canal puede una PWA hablarle a Gnothi?

**Medido el 2026-09-11**, emulador `GnothiWeb` (`emulator-5554`), **Chrome 133.0.6943.137**,
WebAPK `org.chromium.webapk.ac970f0088360685e_v2` — corriendo **dentro** de la PWA instalada
(`display-mode: standalone`, verificado en la propia corrida).

```bash
./armar.sh                                           # deja sonda.html donde el servidor sirve
adb forward tcp:9222 localabstract:chrome_devtools_remote
powershell -File cdp.ps1 -Ws "ws://127.0.0.1:9222/devtools/page/<id>" -JsFile sonda-inyectada.js
```

---

## Lo medido

| mecanismo | veredicto | la evidencia, literal |
|---|---|---|
| **sockets crudos** | **no existen** | `'TCPSocket' in self === false`, y lo mismo `UDPSocket` y `TCPServerSocket` |
| **`content://`** | **no fetchable** | `Fetch API cannot load content://…  URL scheme "content" is not supported` |
| **Shared Storage (W3C)** | **no es un canal** | existe con toda su superficie (`ctor=SharedStorage`, los 8 métodos), y aun así: `set` → `OperationError: sharedStorage is disabled`; `get` → `OperationError: Cannot call get() outside of a fenced frame`; y en consola `Attestation check for Shared Storage failed` |
| **`showDirectoryPicker`** | **existe** | `typeof === function`. Es el mecanismo que A2/A3 ya usan: **la carpeta es el canal** |
| **`getInstalledRelatedApps`** | **lista vacía** | `[]` — y **no es límite de plataforma**: falta el `asset_statement` del lado del APK, y el `related_applications` del manifest declara `platform: "play"`, que un APK cargado de costado no satisface. Lo dice el propio comentario del manifest |
| **`targetAddressSpace`** | **acepta `local`, `private`, `public`, `unknown`** | `'loopback'` → `The provided value 'loopback' is not a valid enum value of type IPAddressSpace`. La escotilla existe, con el vocabulario de *Private Network Access*, no con el de LNA |
| **HTTP a loopback** | **el navegador lo intentó** | `net::ERR_FAILED` a los 449 ms — tardó, o sea que salió a la red; no fue un bloqueo |
| **WebSocket a loopback** | **el navegador lo intentó** | `net::ERR_CONNECTION_REFUSED` — explícito, nadie escuchando |

## Las dos últimas filas NO valen como respuesta, y la razón importa

La WebAPK instalada fue minteada contra **`http://127.0.0.1:8096/app/`** — se lee en su propio
manifest (`org.chromium.webapk.shell_apk.startUrl`). Ese origen **es** contexto seguro por la
excepción de `localhost`, y por eso se eligió: ahí funcionan el service worker, la instalación y el
picker.

Pero para medir el canal lo invalida:

- un `fetch` de `http://127.0.0.1` a `http://127.0.0.1` es **loopback → loopback**, y eso **no es un
  cruce** en la taxonomía de Local Network Access;
- la página es **`http`**, así que **contenido mixto no aplica** — que es exactamente la regla que se
  quería medir.

O sea: pasan, y pasan por razones que en producción no se sostienen, donde la PWA la sirve la
institución desde `https://…`.

**Y hay un segundo techo del aparato:** Chrome **133**. El prompt de Local Network Access aterriza
en **142**. Este emulador no puede medirlo nunca — hace falta una imagen con Chrome ≥ 142.

Para que esas filas valgan hay que reinstalar la PWA desde un origen **https y no-loopback**. El
certificado de `PaginaDeDescarga/certs` **ya tiene el SAN necesario** (`DNS:localhost`,
`IP:127.0.0.1`, `IP:<ip-lan>`), así que no hay que regenerarlo: hace falta servir el `8453`
—nginx por docker, hoy apagado— e instalar la CA en el aparato. Eso mide el cruce
`local → loopback`; el `public → loopback` de producción necesitaría un dominio de verdad.

## Dos trampas que esta corrida encontró, y valen para cualquier spike futuro

**El service worker se come la sonda.** Su `fetch` handler contesta **toda** navegación de su scope
con el `index.html` cacheado, salvo que la URL esté en el manifiesto de assets. Abrir
`/app/sonda.html` dentro de la WebAPK devolvió **el 404 del router de Blazor** con esa misma URL en
la barra. Está advertido en `PaginaDeDescarga/nginx.conf`, y aun así pasó. De ahí que exista
`sonda-inyectada.js`: se evalúa en la página **que ya está abierta**, y el SW no interviene.

**Chrome de canal estable no manda la consola de la página a logcat.** `adb logcat -s chromium` dio
**cero** líneas de la sonda. Y el motivo de un bloqueo **solo vive en la consola**: desde JS, un
bloqueo por contenido mixto y un `connection refused` entregan el mismo `TypeError`. Por eso el
instrumento es `cdp.ps1` y no logcat.

La sonda es el lado **web** de la pregunta. El lado **nativo** ya existe y es
[`../Lab.Sonda`](../Lab.Sonda) — un APK (`com.ncubo.gnothi.sonda2`) que bindea a
`com.ncubo.gnothi.host` para medir la puerta Binder desde un llamador no autorizado.

## La pregunta, y por qué se miden también las que no sirven

La lámina **04** de los cuatro caminos —la que propuso el arquitecto del motor— dice que la app de
la institución es **solo la cara**: le pregunta a Gnothi *«¿qué pinto?»* y Gnothi contesta *«esto»*.
Gnothi queda como único dueño del expediente.

Esa flecha no es un traspaso: es **una API**, en el camino caliente de la UI. Y la pregunta es por
qué canal viaja cuando la cara es **contenido web dentro de un navegador**.

**Se miden todos los mecanismos, sirvan o no.** El entregable no es «cuál sirve» sino **dónde choca
cada uno**, y para eso una negativa medida vale mucho más que una leída: un `TypeError` literal con
número de versión de Chrome no se discute; una cita de documentación sí. Las filas que mueren se
miden igual — lo que cambia es qué se captura: en las vivas, latencia y costo; en las muertas, **el
error exacto, el instrumento y la versión**.

## Lo que mide, fila por fila

| mecanismo | dónde se espera el choque |
|---|---|
| **sockets crudos** (`TCPSocket`, `UDPSocket`, `TCPServerSocket`) | Direct Sockets es de *Isolated Web Apps*; se mide la ausencia en vez de citarla |
| **WebSocket a loopback** | contenido mixto. La excepción de `localhost` **no alcanza** al chequeo de WebSocket |
| **HTTP a loopback** | ninguno esperado. `localhost` está exento de contenido mixto |
| **`content://`** | el esquema no es fetchable desde contenido web |
| **Shared Storage (W3C)** | colisión de nombre: es Privacy Sandbox, no un canal entre apps |
| **`showDirectoryPicker`** | ninguno — es el mecanismo que A2/A3 ya usan: **la carpeta es el canal** |
| **`getInstalledRelatedApps`** | solo presencia. Sirve para entregar un token, no para pintar |
| **`intent://` y esquema propio** | *fire-and-forget*: cambio de app y **sin valor de retorno** |

## Los dos instrumentos, y por qué hacen falta los dos

**Desde JavaScript no se puede distinguir un bloqueo de un rechazo.** Un `fetch` a loopback sin
nadie escuchando, y un `fetch` bloqueado por seguridad, entregan **el mismo `TypeError`**. El evento
`error` de un WebSocket no trae motivo, por diseño. Así que:

```bash
adb logcat -c && adb logcat -s chromium    # el lado del navegador
adb logcat -s GNOTHI                        # el lado de Gnothi (IpcService loguea cada bind)
```

El de `GNOTHI` es el instrumento de la fila de AIDL/Binder, y funciona por ausencia: si la PWA
intenta cualquier escapada hacia `IpcService` y ahí **no aparece ni un bind**, eso es la medición —
la llamada no llega, y `Authorize` nunca corre.

## Un atajo que la sonda aprovecha

**La mitad del canal de loopback se mide sin escribir el servidor.** Si el `fetch` a
`http://127.0.0.1:<puerto>` falla con un error de **red** —nadie escuchando— y no con uno de
**seguridad**, entonces el navegador lo dejó pasar. Un *connection refused* es un **sí** del
navegador, y contesta la pregunta antes de construir el *foreground service*.

## Lo que esta sonda NO puede contestar

- **Si el proceso de Gnothi sobrevive** con el socket abierto: eso es un *foreground service* con
  notificación permanente, y hay que medirlo con `dumpsys deviceidle force-idle` y **en un aparato
  físico**, porque los gestores de batería de los fabricantes no existen en el emulador.
- **La latencia comparable** contra la puerta Binder. Esa ya está medida —2 055 ms en frío y
  **153 ms en caliente**, Galaxy A54, ver `Documentacion/Movil/05-lo-medido.md`— y para que la
  comparación se sostenga hay que medir el loopback **con el mismo cronómetro**.
- **Si un `DocumentsProvider` de Gnothi aparece en el picker de Chrome.** Está medido que el picker
  abre **DocumentsUI**, o sea el SAF (`F1:566`), y ese picker lista los proveedores de terceros. Es
  la única fila que podría abrir un camino nuevo, y exige código Android que todavía no existe.
