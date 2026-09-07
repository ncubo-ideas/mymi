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
  xcrun simctl openurl booted "gnothi://alta?bundle=http://127.0.0.1:8096/examenes.zip"
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
cursor, el puente— no se toca, porque vive en `Gnothi.Movil` y no sabe de esto.

---

## P1 · El módulo arranca hospedado

Con un módulo ya instalado, tocá su tarjeta y después **«Abrir…»**.

**Qué tenés que ver:** la UI de la institución a pantalla completa, sin barra de direcciones, y
el aviso que solo aparece hospedado: *«Se está guardando en Gnothi…»*. Los números tienen que
ser los mismos que muestra su app suelta.

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
(`Gnothi.Movil/BundleServer.cs`), con el hash de cada script en línea computado al servir. Lo
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

**Y una rendija que en iOS ya está cerrada, y en Android sigue abierta.** Allá la navegación de
nivel superior quedó libre porque falta `ShouldOverrideUrlLoading`: si el módulo hace
`location = 'https://…'`, el WebView va. Acá está puesto —`OnlyItsOwnOrigin`, un
`WKNavigationDelegate` en [`IosWebModule.cs`](IosWebModule.cs)— y **sí se puede medir sin tocar
el módulo**, porque no depende de la CSP. En el inspector de Safari, con el módulo abierto:

```javascript
window.location = 'https://example.com'
```

La página **no** tiene que cambiar, y el log tiene que decir:

```
GNOTHI: examenes: navigation refused to https://example.com -- outside its own origin
```

Probá también `gnothi-mod://otro-modulo/` : también tiene que rebotar, porque la regla es el
origen completo (esquema **y** host), no sólo el esquema. Un módulo no puede navegar al de otro.

Esto es una diferencia real de plataforma a favor de iOS, y conviene decirlo en esos términos
cuando se compare: la contención de Android está incompleta en este punto.

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
> xcrun simctl openurl booted "gnothi://alta?bundle=http://127.0.0.1:8096/examenes.zip"
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
> bundle, qué va a poder contestar y dónde va a vivir el registro. Queda anotado en el
> `Info.plist` que antes de publicar hay que decidir si se queda; si no, es un `#if DEBUG`.
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
2. la hoja con lo que la institución propone: qué va a poder contestar, dónde va a vivir el
   registro, con quién va a hablar, y quién sirvió el bundle. **Todavía no hay nada instalado.**
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

---

## Deuda anotada, para que no parezca olvido

**El alta ya NO está duplicada, y el merge ya está arreglado.** Las dos cosas que esta sección
decía quedaron hechas: el flujo (`Enroll`, `AcceptEnrollment`, `DiscardEnrollment`,
`PushEnrollment`, `PushProposal`) vive en `Gnothi.Movil/Shell.cs`, que ya era dueño del
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
