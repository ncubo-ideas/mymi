# B11 · Lo que dicen las tiendas, con el texto en la mano

**Consultado el 2026-09-16.** Los tres textos citados literales, con su fecha. Antes de esto el set
repetía una afirmación —*«Play y Apple nombran JavaScript en un webview como la excepción»*— que
está en tres comentarios de código (`Module.cs:47`, `HybridShell.cs:45`, `WebViewShellStub.cs:5`) y
en `C8`, `C9` y `B7`, **y que salió de una versión del acuerdo de Apple que ya no existe.**

Esto no es una opinión sobre si nos van a aceptar. Es **el texto**, y qué dice de cada forma.

---

## 1 · Apple — el acuerdo de developer

**Apple Developer Program License Agreement**, con fecha **18 de agosto de 2026**, §3.3.1(B)
*Executable Code*:

> Except as set forth in the next paragraph, an Application may not download or install executable
> code. Interpreted code may be downloaded to an Application but only so long as such code: (a) does
> not change the primary purpose of the Application by providing features or functionality that are
> inconsistent with the intended and advertised purpose of the Application (b) does not bypass
> signing, sandbox, or other security features of the OS; and (c) for Applications distributed on
> the App Store, does not create a store or storefront for other Applications.

**LA EXCEPCIÓN DE WebKit/JavaScriptCore YA NO ESTÁ.** La redacción vieja —la que citábamos, del
antiguo §3.3.2— decía que *«las únicas excepciones son scripts y código descargados y ejecutados
por el WebKit o el JavaScriptCore de Apple»*. Hoy dice **«interpreted code»**, sin nombrar motor.

Es **más ancho** de lo que creíamos: nada en esta cláusula obliga a que el intérprete sea el del
navegador. Pero trae tres condiciones, y la tercera no tiene nada que ver con ejecutar código:

| condición | Gnothi |
|---|---|
| (a) no cambia el propósito principal ni lo anunciado | **a favor si se declara honesto**: hospedar módulos **es** el propósito anunciado |
| (b) no evade firma, sandbox ni otras defensas del SO | **es el nudo** — ver abajo |
| (c) **no crea una tienda o escaparate para otras aplicaciones** | **el riesgo más grande, y no lo habíamos visto** |

**Sobre (c):** Gnothi es un catálogo donde instituciones publican módulos que la persona instala por
un enlace, con hoja de consentimiento, versión, firma y actualización. **Eso se parece mucho a un
escaparate.** Es un riesgo de *forma del producto*, no de forma del módulo: le pega igual a las
cuatro. Nunca lo habíamos nombrado.

## 2 · Apple — las guías de revisión

**App Review Guidelines**, §2.5.2 (documento vivo, sin fecha publicada):

> Apps should be self-contained in their bundles, and may not read or write data outside the
> designated container area, nor may they download, install, or execute code which introduces or
> changes features or functionality of the app, including other apps.

**Esto es más estricto que el acuerdo, y es lo que aplica el revisor.** Leído llano, *«código que
introduce o cambia funcionalidad»* incluye **también la forma hospedada**: un módulo le agrega a
Gnothi consultas que antes no tenía. La tensión entre 2.5.2 y 3.3.1(B) es real y no la resuelve
ninguno de los dos textos.

### Y la puerta que nadie había mirado: §4.7

> **4.7** Apps may offer certain software that is not embedded in the binary, specifically HTML5 and
> JavaScript mini apps and mini games, streaming games, chatbots, and plug-ins.

**Éste es el régimen bajo el que Gnothi sería revisado**, no el 2.5.2 a secas. Y trae cinco
sub-reglas. Tres nos tocan directo:

> **4.7.2** Your app may not extend or expose native platform APIs or technologies to the software
> without prior permission from Apple.

**Esto le pega a la forma hospedada, que dábamos por segura.** El puente de Gnothi
—`window.gnothiPuente` con `listar`, `leer`, `escribir`, `pull`— es literalmente exponerle sistema
de archivos y red a software que no viene en el binario. **Requiere permiso previo de Apple.** Es el
hallazgo más importante de esta búsqueda: *ninguna de las cuatro formas está limpia en iOS*.

> **4.7.3** Your app may not share data or privacy permissions to any individual software offered in
> your app without explicit user consent in each instance.

**Ésta la cumplimos, y es nuestra mejor carta.** La hoja de consentimiento por módulo, los
interruptores por consulta y la carpeta concedida por módulo son exactamente esto. No es suerte: es
la tesis del producto.

> **4.7.4** You must provide an index of software and metadata available in your app. It must
> include universal links that lead to all of the software offered in your app.

A medias: existe `/.well-known/gnothi/modules.json` del publicador y el enlace de alta
`go.gnothi.app/alta?bundle=…`, pero **el índice es del publicador, no de Gnothi**. Habría que ver si
eso cuenta.

> **4.7.5** Your app must provide a way for users to identify software that exceeds the app's age
> rating, and use an age restriction mechanism based on verified or declared age.

**No está construido.**

## 3 · Google Play — y es el texto más claro de los tres

Política de **Device and Network Abuse**:

> An app distributed via Google Play may not modify, replace, or update itself using any method
> other than Google Play's update mechanism. Likewise, an app may not download executable code (such
> as dex, JAR, .so files) from a source other than Google Play. This restriction does not apply to
> code that runs in a virtual machine or an interpreter where either provides **indirect** access to
> Android APIs (such as JavaScript in a webview or browser).

La palabra que decide es **«indirect»**, y parte nuestras formas por la mitad sin ambigüedad:

| forma | qué ejecuta el código de la institución | acceso a las APIs de Android | contra el texto |
|---|---|---|---|
| **2 · hospedada** | el WebView | **indirecto** — sólo lo que el puente le pase | **pasa**, es el ejemplo que la política da |
| **1 · cargo**, **3 · híbrida**, **4 · Blazor** | Mono, en el proceso de Gnothi | **directo** — el DLL puede llamar lo que Gnothi pueda llamar | **no pasa** |
| *(la caja de wasm, `F5`)* | wasmtime, con los imports que Gnothi concede | **indirecto**, por construcción | **pasa** — encaja palabra por palabra |

**Es el «no» más limpio de toda la búsqueda**, y también el «sí» más limpio: la caja de wasm de `F5`
dejó de ser una curiosidad de contención. Es la única arquitectura que cumple el texto de Google
**y** resuelve la objeción de fondo de las formas 1, 3 y 4 —que el DLL de la institución corre con
los permisos de Gnothi—, con una sola decisión.

---

## El mapa, corregido

| | Android / Play | iPhone / App Store |
|---|---|---|
| **1 · cargo** | no pasa (acceso directo) | 2.5.2 llano dice que no; 3.3.1(B) es discutible |
| **2 · hospedada** | **pasa** | **4.7.2: el puente necesita permiso previo de Apple** |
| **3 · híbrida** | no pasa | igual que 1, más el 4.7.2 por la cara |
| **4 · Blazor en el Host** | no pasa | igual que 1 |
| **wasm confinado (`F5`)** | **pasa por el texto** | 4.7.2 sigue aplicando al puente |
| **y a las cuatro** | — | **3.3.1(B)(c): ¿es Gnothi un escaparate?** |

## Lo que cambia respecto de lo que teníamos escrito

1. **La cita era de una versión muerta del acuerdo.** La excepción de WebKit/JavaScriptCore ya no
   existe; hoy es «interpreted code» con tres condiciones. Hay que corregir `Module.cs:47`,
   `HybridShell.cs:45`, `WebViewShellStub.cs:5`, `C8` y `C9`, que la repiten como hecho.
2. **La forma hospedada NO estaba a salvo.** `4.7.2` le pega al puente, que es justo lo que la hace
   útil. Decíamos *«la única publicable sin discusión»*; lo correcto es **«la única que pasa el
   texto de Google»**.
3. **Apareció un riesgo que no era de código**: el escaparate (3.3.1(B)(c)). Le pega al producto, no
   a la forma, y es el único que podría hundir las cuatro a la vez.
4. **La caja de wasm subió de categoría.** Pasó de experimento de contención a **la arquitectura que
   el texto de Google describe**.
5. **`4.7.3` lo cumplimos de sobra**, y conviene saberlo: es lo que se pone en las notas de revisión.

## Lo que sigue sin saberse, y hay que decirlo

- **Si wasm cuenta como «interpreted code» para Apple.** El texto de Apple no nombra motores, así que
  la lectura amplia lo cubre; el 4.7, que sí enumera, dice *«HTML5 and JavaScript»* y no dice wasm.
- **Si un `.dll` de IL ejecutado por el intérprete de Mono cuenta como «interpreted code».** En iOS
  no hay JIT: ese DLL **se interpreta**, literalmente. Contra la condición (b) —no evadir el
  sandbox— es defendible, porque corre dentro del sandbox de Gnothi. Nadie lo ha probado.
- **Si el catálogo de módulos es un escaparate.** No hay precedente que conozcamos.
- **Y nada de esto es un veredicto.** Son textos leídos, no una revisión pasada. Lo único que los
  convierte en hecho es una sonda — ver el plan de las dos apps mínimas, que sigue vigente y ahora
  tiene una tercera pregunta que hacerle a Apple: el 4.7.2.

---

**Fuentes, con fecha de consulta 2026-09-16:**
[App Review Guidelines](https://developer.apple.com/app-store/review/guidelines/) ·
[Apple Developer Program License Agreement (PDF, 18 ago 2026)](https://developer.apple.com/support/downloads/terms/apple-developer-program/Apple-Developer-Program-License-Agreement-English.pdf) ·
[Google Play — Device and Network Abuse](https://support.google.com/googleplay/android-developer/answer/9888379)
