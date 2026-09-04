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
- El escenario de la institución levantado **en la misma máquina**, con
  `Gnothi.Mobile/escenario-webview.ps1`. Corre en Windows; si la Mac es otra máquina, hay que
  apuntar los puertos a su IP y **eso cambia P6**, porque `network_security_config` no existe
  en iOS pero App Transport Security sí.

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

Con el módulo abierto (P1), en el inspector de Safari (Develop → Simulator → el WebView):

```javascript
window.isSecureContext        // tiene que dar true
!!window.crypto.subtle        // tiene que dar true
!!navigator.storage           // tiene que dar true
```

| resultado | qué significa |
|---|---|
| **los tres `true`** | seguimos. El resto del guion vale tal cual |
| **alguno `false`** | **el diseño cambia.** Hay que servir el bundle desde `127.0.0.1` con un servidor local en proceso, y eso trae puerto, ciclo de vida, y que ya no se intercepta «antes de la red» |

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

Como en Android, **esta no se ejecuta sola**: el módulo hospedado nunca intenta salir a la red,
así que no hay nada que negar. Para medirla hay que agregarle una sonda temporal al módulo que
haga `fetch` contra el publicador. Cuando está puesta, la violación aparece en la consola del
módulo —que acá llega por el mismo canal del puente, porque iOS no tiene `WebChromeClient`—:

```
modulo:examenes: error: Refused to connect to 'http://127.0.0.1:8095/...' because it
violates the following Content Security Policy directive: "connect-src 'self'"
```

**Y una rendija que iOS puede cerrar mejor que Android.** Allá quedó abierta la navegación de
nivel superior, porque falta `ShouldOverrideUrlLoading`. Acá el punto natural es
`WKNavigationDelegate.DecidePolicy`, y **todavía no está puesto**. Es más fácil de no olvidar
que en Android; conviene ponerlo antes de que esto crezca.

---

## P6 · El alta desde el enlace de la institución

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

**El flujo del alta está duplicado.** Las cuatro funciones de
[`ViewController.cs`](ViewController.cs) —`DarDeAlta`, `AceptarAlta`, `DescartarAlta`,
`PushPropuesta`— son casi idénticas a las de `Gnothi.Mobile/MainActivity.cs`. Deberían vivir en
`Gnothi.Movil/Shell.cs`, que ya es dueño del catálogo, del push y del switch de `Handle`.

No se hizo todavía por una razón concreta: **`MainActivity.cs` viene sintácticamente roto del
merge `38dbe22`** —balance de llaves −2, perdió la declaración de `Start()`— así que Android no
compila y el cambio no se podría verificar de ese lado. Es el próximo movimiento.
