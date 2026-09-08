# 03 · Qué funciona hoy, en qué aparato, y qué lo frena

**Corte: 2026-09-07.** Si sólo se lee un documento de esta carpeta, que sea éste.

---

## El cuadro maestro

| camino | quién corre el bucle | qué pide el aparato | ¿llave? | ¿red? | ¿anda hoy? |
|---|---|---|---|---|---|
| **Puerta 2 · BYOK** | nosotros | cualquier teléfono | **sí** | **sí** | ✅ **corre**, medido en A54 físico |
| **Puerta 2 · modelo del SO** («Puerta 4») | nosotros | iPhone 15 Pro+, iOS 26+ | no | no | ✅ corre — **medido en simulador**, no en aparato |
| **El Atajo** · Gnothi → la IA que ella elija | la persona | iPhone con iOS 18 | depende | sí | ✅ corre |
| **Puerta 3 · iOS** (Siri / App Intents) | Siri | iOS 18.4+ | no | no | ⚠️ **el piso sí, el techo no** |
| **Puerta 3 · Android** (AppFunctions) | Gemini | Android 16+ | no | no | ❌ **private preview**, EAP cerrado |
| **Android on-device** · LiteRT-LM + Gemma | nosotros | Android 12+, cualquier gama | no | no | ⚠️ **medido en el A54**, sin cablear al Host |
| **Android · Gemini Nano** vía AICore | nosotros | flagship (Pixel 8+, S24+) | no | no | ❌ **el A54 no tiene AICore** |
| **Puerta 1** · Claude/ChatGPT en el teléfono | el asistente | — | sí | sí | ❌ **ninguno llama a un servidor local** |

## Los módulos web hospedados, por plataforma

Es lo construido en las últimas dos semanas, y es donde más difiere el estado.

| | Android | iOS |
|---|---|---|
| el módulo abre hospedado | ✅ **[MEDIDO]** | ⚠️ **[NO MEDIDO]** — compila, sin aparato |
| el journal se escribe en la carpeta del Host | ✅ **[MEDIDO]** | **[NO MEDIDO]** |
| el alta con consentimiento | ✅ **[MEDIDO]** | **[NO MEDIDO]** |
| la CSP se aplica | ✅ **[MEDIDO]** — se ve denegar solo | **[NO MEDIDO]** |
| el formulario y la navegación, rechazados | ✅ **[MEDIDO 2026-09-07]**, antes y después | **[NO MEDIDO]** |
| el rechazo llega como hecho contable | ✅ **[MEDIDO]** | **[NO MEDIDO]** |
| el enlace de la institución abre la app | ✅ con `pm set-app-links` | ❌ **bloqueado**, ver abajo |

**Por qué iOS está sin medir:** no tenemos aparato. Está todo construido, compila con 0 errores,
y el guion `Gnothi.iOS/PRUEBAS-WEBVIEW-IOS.md` está escrito para que Renan lo corra.

## Las tres cosas que hay que saber de iOS antes de probarlo

**1 · Hay una pregunta que decide la forma de todo lo demás, y es la primera del guion.** El
módulo se sirve en un esquema propio (`gnothi-mod://`) porque **WebKit no deja manejar `http` ni
`https`** — `SetUrlSchemeHandler` tira para esquemas reservados. Si ese origen **no es contexto
seguro**, el módulo no arranca: se caen el almacenamiento de origen y `crypto.subtle`.

No es teoría: es exactamente lo que mordió a Android cuando su primera versión usaba un esquema
propio, y por eso hoy Android usa un host virtual sobre `https`. Está aislado a propósito en un
solo método (`OriginFor`), y si falla, la salida es servir desde `127.0.0.1`.

**La app se lo pregunta sola** y lo deja en el log antes de que corra una línea del bundle.

**2 · El enlace de la institución no se puede probar todavía.** `go.gnothi.app` **no existe**
—**[MEDIDO 2026-09-07]**, `nslookup` da *non-existent domain*—, y sin dominio no hay AASA, así
que iOS no le entrega el enlace a la app. En Android eso no se nota porque `pm set-app-links`
fuerza la aprobación local, que **es una muleta de laboratorio**. Para poder probar el alta igual
se agregó un esquema propio: `gnothi://alta?bundle=…`.

**3 · O App Intents, o módulos que llegan después. No las dos.** Declarar un App Intent desde C#
está cerrado, y no por un bug: **App Intents es un framework sólo-Swift** —property wrappers y
result builders sin representación en Objective-C, y un índice que genera el compilador de Swift
escaneando fuentes Swift—. Eso obliga a un target Swift aparte, y el trade-off que abre decide la
forma del producto en iOS. Está en `04`.

## Lo que Android puede y iOS no, y al revés

Van los dos, porque las comparaciones honestas tienen dos columnas.

**A favor de iOS:**
- **La persona puede dejar el paquete ella misma**, desde la app Archivos. En Android el paquete
  se empuja por `adb` porque no hay otra forma — otra muleta de laboratorio.
- **El modelo del propio teléfono, sin llave y sin red**, en iPhone 15 Pro o superior.

**A favor de Android:**
- **Todo está medido en un aparato de verdad.**
- El host virtual sobre `https` es contexto seguro sin discusión.
- `pm set-app-links` permite probar el enlace sin dominio. iOS **no tiene equivalente por línea
  de comandos**.
