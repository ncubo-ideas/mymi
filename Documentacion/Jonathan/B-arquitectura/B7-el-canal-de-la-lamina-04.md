# B7 · El canal de la lámina 04 — el ícono pelea con el canal

**2026-09-11.** La lámina **04** de [`B6`](B6-los-cuatro-caminos-de-la-pwa.md) es la que planteó el
arquitecto del motor: **la app de la institución es solo la cara**, le pregunta a Gnothi *«¿qué
pinto?»* y Gnothi contesta *«esto»*; el motor y el expediente viven solo en Gnothi, que queda como
único dueño.

Este documento contesta **por qué canal viaja esa flecha**, y existe para poder **defender o
descartar la lámina 04 con fundamento** — no con lectura de documentación, sino con mediciones.

El cuadro completo, con `MEDIDO` / `PENDIENTE` / `CITADO` por fila:
**https://claude.ai/code/artifact/5fb9c662-f78e-46ba-9f64-ece584b72984**

---

## El filtro, y por qué hace falta enunciarlo

Esa flecha **no es un traspaso**: es **una API en el camino caliente de pintar una pantalla**. Eso
impone tres condiciones, y son el filtro con el que se juzga cada mecanismo:

1. **hay respuesta de vuelta** — no sirve *fire-and-forget*;
2. **sirve en cada pantalla** — no una sola vez;
3. **la arranca la página** — Gnothi no puede empujar (ver §4).

Sin ese filtro el barrido se desvía. Pasó: en una versión de la tabla apareció
`showDirectoryPicker`, que **no es candidato a nada acá** — es el canal de A2/A3, donde la PWA tiene
su propio journal en la carpeta de Sonia. En 04 la PWA **no tiene journal ni motor**, así que un
selector de carpetas no contesta *¿qué pinto?*.

---

## 1 · Lo que decide no es el canal: es de qué está hecha la cara

> **El ícono pelea con el canal.** Todo mecanismo que da pedido-y-respuesta de verdad exige **o un
> APK de la institución, o que Gnothi sea quien abra la página**. Una página con su propio ícono y
> sin APK **solo puede usar loopback**.

| variante | canal | ¿APK de la institución? | ícono propio | identidad del llamador | proceso extra | latencia | estado |
|---|---|---|---|---|---|---|---|
| **04a** · PWA suelta | `fetch` a loopback | **no** | **sí** | ninguna nativa; token por enlace | **foreground service + notificación permanente**, tipo `specialUse` | sin medir | pendiente |
| **04b** · carátula con WebView | Binder directo | sí (17 KB) | sí | **firma del APK** | ninguno — el `bind` levanta Gnothi | **153 ms** · 2 055 en frío | **medido, A54** |
| **04c** · TWA | `postMessage` (Chrome 115+) | **sí** | sí | firma del APK + `assetlinks.json` | ninguno | sin medir | pendiente |
| **04d** · la abre Gnothi | puente en proceso | no | **no** | por construcción | ninguno | — | **ya existe: es el camino 01** |

### 04c · qué es una TWA, y qué implica el APK

Una **Trusted Web Activity** es una app de Android cuyo contenido entero es un sitio web,
renderizado por Chrome sin barra de direcciones. **No lleva HTML, ni JS, ni motor.** Medido sobre la
WebAPK que corre en el emulador: **30 archivos** — 12 `.png`, 11 `.xml`, un `.dex`, un `.arsc` — y
**cero** `.html`.

O sea que *el contenido* no es nativo, y se actualiza republicando el sitio, igual que una PWA. Lo
único que cambia es **quién firma el APK y cómo llega al teléfono**:

| | PWA / WebAPK | TWA |
|---|---|---|
| quién firma el APK | **Chrome lo mintea al instalar** | **la institución** |
| cómo llega | desde el navegador, **sin tienda** | hay que distribuirlo |

**Si «la app de la institución no es nativa» significa *ningún APK suyo*, 04c queda afuera** — y con
ella el único canal barato con identidad fuerte. Queda en el cuadro porque la implicación es esa y
conviene que se vea, no porque esté descartada técnicamente.

### 04d ya está implementado, y es el camino 01

Si la cara pierde su ícono y se entra por Gnothi, **eso es lo que ya corre**: cuatro instituciones,
su misma app web sin recompilar, dentro de Gnothi.

Y ahí Gnothi **sirve la página**, que es lo que le permite imponer el CSP y dar la **única garantía
de red medida** —solo `lo`, sin DNS— (`B6`, cierre). La variante «la sirve la institución y Gnothi
solo la abre en una Custom Tab» cambia quién hospeda **a cambio de perder esa garantía**: es
estrictamente peor, y **no vale construirla**.

---

## 2 · Los mecanismos, y dónde choca cada uno

| mecanismo | dónde choca con 04 | estado |
|---|---|---|
| **HTTP a loopback** | en el canal, **en nada**. Choca en el **proceso** y en la **identidad** | pendiente |
| **WebSocket a loopback** | contenido mixto, y **sin escotilla** `targetAddressSpace`. Sin él **no hay empuje** | pendiente |
| **HTTPS a loopback** (patrón `plex.direct`) | nombre DNS público que resuelve a `127.0.0.1` con certificado válido: mata el contenido mixto **y** la instalación de CA | pendiente |
| **sockets crudos** | no existen para una PWA | **medido** |
| **AIDL / Binder desde la página** | la página **no tiene uid ni APK**: la llamada no llega | citado |
| **`content://`** | el esquema no es alcanzable desde contenido web | **medido** |
| **Intents / Web Share Target** | **cambio de app y sin valor de retorno** | pendiente |
| **cola en carpeta compartida** | exige **dos escritores**, y el grant de Gnothi es **solo lectura** | pendiente |
| **Shared Storage (W3C)** | **no devuelve datos a la página**; es Privacy Sandbox | **medido** |
| **Digital Credentials API** | la forma correcta, pero el payload es una **presentación de credencial** y quien contesta debe registrarse como *wallet* | citado |
| **WebRTC data channel** | necesita **señalización previa** —o sea otro canal— y paga el mismo costo de proceso | citado |

**`CITADO` no es pereza.** No se puede medir la ausencia de una API: no hay nada que llamar. Lo que
sí se mide, y por ausencia, es del otro lado — con `adb logcat -s GNOTHI` mientras la página intenta
escapadas hacia `IpcService`: si no aparece **ni un bind**, la llamada no llegó.

---

## 3 · La puerta que Gnothi ya tiene, y que la PWA no puede usar

`Gnothi.Mobile/IpcService.cs` — 212 líneas, `[Service(Exported = true)]`, **Messenger sobre
Binder**. Alcanza `Dispatcher.Serve` **entre procesos y con Gnothi cerrado**: el `bindService`
arranca el proceso sin ninguna Activity.

Y resuelve las dos cosas que el loopback no:

- **identidad fuerte** — `Message.SendingUid`, y sobre eso `Authorize` verifica el **SHA-256 del
  certificado de firma del APK** contra la carátula que el módulo declaró. Usa
  `GetSigningCertificates` a propósito y no `GetSignatures`, porque el viejo reporta el firmante
  original de una llave rotada. Y la firma **la lee Gnothi del uid que le dio el kernel**, nunca del
  mensaje;
- **sin notificación permanente** — el bind levanta el proceso bajo demanda.

Medido en `Documentacion/Movil/05-lo-medido.md`, Galaxy A54 físico: **2 055 ms en frío** (bind 22 →
conexión 931 → catálogo 278 → `Serve` 1 054) y **153 ms en caliente**.

**Es el canal que la lámina 04 necesita.** Lo único que se lo impide a una PWA es que una página no
tiene uid ni APK.

---

## 4 · Cuando se dice «loopback», es `http` — y eso ya está decidido en el código

|  | `http://127.0.0.1` | `https://127.0.0.1` |
|---|---|---|
| ¿la deja pasar una página `https`? | **sí** — `localhost` está exento de contenido mixto | sí |
| `ws://` / `wss://` | **no.** La excepción no alcanza al chequeo de WebSocket → **solo *polling*, sin empuje** | sí → **empuje desde Gnothi** |
| qué cuesta | nada | un certificado: **instalar una CA** (UX hostil) o el patrón `plex.direct` — **llave privada dentro de la app** |
| ¿va en claro? | sí, pero **no sale del aparato**. El riesgo no es la escucha: es que **cualquier app del teléfono puede tocar el puerto** | cifrado, y **el problema de identidad sigue igual** |
| Local Network Access | aplica a las dos: LNA mira **el espacio de direcciones, no el nombre**. El cruce `público → loopback` pide permiso desde **Chrome 142** | ídem |

**Y no es una elección abierta: ya está tomada.** `Gnothi.Mobile/Resources/xml/network_security_config.xml`
permite texto claro **solo** para `127.0.0.1` y `localhost`, con su razón escrita ahí:

> *«Desde API 28 Android bloquea el tráfico HTTP por omisión, y está bien… La ÚNICA excepción es el
> loopback… 127.0.0.1 es el propio aparato.»*

Así que **04a nace en `http` sobre loopback, y con ello sin empuje**: la página tiene que volver a
preguntar en cada pantalla.

---

## Lo medido y lo pendiente

Las mediciones, con su evidencia literal, están en [`F1`](../F-evidencia/F1-mediciones.md), sección
«Los canales de la lámina 04». El estado y los bloqueos, en
[`F2`](../F-evidencia/F2-estado-y-pendientes.md), `P16`. El spike y su instrumento, en
`Spikes/Lab.SondaWeb/`.
