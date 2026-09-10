# iOS 27 — qué de este spike cambia, y qué no

Investigación de escritorio, **2026-09-02**, contra el Newsroom de Apple y las sesiones de
WWDC26 con transcripción. Las fuentes están al final.

**Esto no es una medición.** `HALLAZGOS.md` dice solo lo que se corrió en un aparato, y esa
disciplina es la razón por la que sirve; este archivo existe para no ensuciarla. Todo lo de
abajo salió de material de **beta** y hay que re-verificarlo en la RC.

---

## Primero el bloqueo, porque decide qué es medible

| Nivel | Requisito |
|---|---|
| iOS 27 | iPhone 11 y posteriores |
| Apple Intelligence / Siri AI | A17 Pro o superior → **iPhone 15 Pro en adelante** (piso real: 8 GB de RAM) |
| El modelo on-device más potente | iPhone Air, 17 Pro, 17 Pro Max; iPad M4 o Mac M3 con ≥12 GB |

**El 13 Pro con el que se corrió este spike no sirve para nada de esto.** Corre iOS 27 y no
recibe Siri AI en ninguna forma. Es la misma pared que dejó t3, t4 y t9 sin concluir, ahora
con más cosas detrás.

Calendario: iOS 27 se anunció el 8 de junio de 2026 (WWDC26), las betas siguen saliendo —la
última del 31 de agosto— y el lanzamiento público se espera a mediados de septiembre.

---

## Lo que se confirma, y por eso no hay que rehacer nada

**Las App Intents siguen siendo estáticas.** Ninguna sesión de WWDC26 introduce intents
dinámicas: la metadata se sigue extrayendo en compilación y viaja dentro del binario. El
**hallazgo 7 sigue en pie tal cual**, y con él la forma que ya está andando en
[`Gnothi.iOS/Intents/GnothiIntents.swift`](../../Gnothi.iOS/Intents/GnothiIntents.swift) — una
intent genérica compilada, ruteo en runtime, los tipos estáticos y las instancias dinámicas.

**MCP llegó a Xcode, no a iOS.** El press release lo menciona en un solo lugar: plug-ins de
Xcode 27. Ni iOS 27 ni macOS 27 en general. Hay blogs afirmando que Apple adoptó MCP a nivel de
sistema y que Siri AI invoca servidores MCP registrados; **eso no aparece en ningún material de
Apple** y conviene tratarlo como ruido.

O sea que **la Puerta 2 no se borra**. Un asistente ajeno llega a Gnothi por Siri y App
Intents, con la forma declarada en compilación. Lo que sí cambia es cuánto cuesta mantenerla
(ver abajo).

Y dos cosas que no se movieron: **el tope de 10 AppShortcut por app** y **los parámetros de
frase como conjuntos cerrados** (nada de texto libre, el hallazgo de código 3).

---

## Lo que cambia de verdad

### 1 · Siri sí consulta a la app — por dominios predefinidos

Las tres capacidades nuevas: acceder a las **entidades** de la app, **actuar** por sus intents,
y entender el **contexto en pantalla**. Pero la ejecución por voz pasa por **App Schemas**, que
se agrupan en dominios cerrados (mail, fotos, mensajes, documentos). Xcode falla el build si
adoptás un schema y no el relacionado que la conversación necesita.

Para «consulta genérica de los módulos de una persona» **probablemente ningún dominio calza**.
El hallazgo 2 —*el índice recibe, Siri no consulta*— se afloja para las apps que entran en un
dominio de Apple, y Gnothi no es una de ellas. **Puerta 1 por voz sigue siendo terreno de
Atajos**, que es donde este spike ya la dejó.

`SiriKit` quedó formalmente deprecado (ventana de ~2–3 años): App Intents es la única vía.

### 2 · El índice: OJO, la corrección importa

La investigación reporta que `indexingKey` marca qué propiedades son buscables y que Siri
ahora razona sobre el índice y **contesta preguntas** en vez de solo recuperar ítems.

**Pero `indexingKey` no es nuevo y este spike ya lo tenía puesto.** Las dos entidades lo
declaran sobre la propiedad de descripción —[`ExamenEntity.swift`](ExamenEntity.swift) línea 39,
[`FacturaEntity.swift`](FacturaEntity.swift) línea 47— y **con eso puesto** `deseable` y
`combustible` no macheaban. Las dos queries ya son `EntityStringQuery`, que es la otra
mitigación que la investigación propone.

Así que la lectura correcta es esta: lo que el hallazgo 1 midió es **Spotlight como buscador de
texto**, y eso puede seguir roto sin que importe. Lo nuevo es **Siri consultando el índice
semánticamente**. Son dos cosas distintas y solo la segunda está por medirse.

Y el matiz de negocio del hallazgo 1 sobrevive intacto: con exámenes la única puerta —el nombre
del analito— es lo que la persona **sí** diría.

Extra: Foundation Models trae un tool de búsqueda respaldado por Spotlight, o sea RAG local sin
salir del aparato.

### 3 · Foundation Models: acá está el cambio grande, y abre un hueco de diseño

El modelo on-device se reconstruyó: mejor en lógica y en **tool calling**, acepta imágenes,
sigue siendo local, gratis y sin API key. La red es **opt-in** (PCC es un modelo aparte con su
propio entitlement). Eso contesta t9 —**¿la cadena contesta sin red?**— por diseño y no por
suerte, que es la tesis de privacidad del producto.

Y las **tools de schema dinámico están soportadas oficialmente**: se implementa
`var parameters: GenerationSchema` construido con `DynamicGenerationSchema` y se recibe
`GeneratedContent` en `call`. Eso es **`ToolIndex` conectado a un modelo local sin recompilar
nada** — exactamente lo que la Puerta 2 hace hoy a mano. Hay gente usándolo para volver
FoundationModels un host MCP.

**El hueco: FoundationModels es Swift y el Host es `net9.0-ios`.** Dos caminos, ninguno gratis:

| Camino | Lo que cuesta |
|---|---|
| **El bucle en Swift** | Hoy la costura Host→Swift es unidireccional: .NET escribe la foto, Swift la lee. Un tool call de verdad necesita ida y vuelta cruzando proceso, o sea **reintroducir transporte** justo en el eslabón donde el diseño pide llamada a función. Y con dos procesos sobre el journal, el escritor único hay que resolverlo explícito |
| **El bucle en .NET** | Bindings a FoundationModels y `net10.0-ios27.0`, que hoy es **preview** (`XCODE_27_0_PREVIEW`, Xcode 27 beta 4, y solo .NET 10 — no hay .NET 11). Arrastra migrar el Host de net9 a net10 y **volver a correr la sonda**, porque el intérprete se re-mide en cada movimiento de runtime |

No hay que elegir hoy. Hay que saber que la elección existe y que la barata en apariencia —Swift—
es la que toca una invariante.

### 4 · Piezas nuevas que sí encajan con módulos que llegan después

- **`RelevantEntities`** · sugerirle al sistema entidades con contexto de cuándo son relevantes,
  vía `updateEntities`. Es registro en runtime de **contenido**, no de tools — pero es
  precisamente la forma de un módulo que se instaló el mes pasado.
- **`ExecutionTargets`** · decidir explícitamente qué proceso ejecuta la intent (`.main`,
  extensión de App Intents, de WidgetKit). Para la invariante de **un solo escritor**, esto es
  el `.main` explícito en vez de la heurística del sistema.
- **`LongRunningIntent`** · pasa los 30 s, muestra progreso como Live Activity, y admite GPU en
  background con entitlement. Pensado para inferencia on-device.
- **`parameterPresentation` + `OptionsCollection(EntityQuery())`** · **saca el número de módulos
  del presupuesto de 10 AppShortcut.** Un solo shortcut con opciones resueltas en runtime desde
  una query. El tope no se movió, pero deja de ser el techo de escala.
- **`EntityCollection`** · pasa identificadores a `perform()` sin resolver las entidades
  completas, si una consulta devuelve muchos ítems.
- **Model abstraction layer** · protocolo `LanguageModel`, con implementaciones open source
  (`CoreAILanguageModel`, `MLXLanguageModel`) y paquetes Swift de Anthropic y Google.
- **Core AI** · sucesor de Core ML para correr modelo propio, y la salida para aparatos sin
  Apple Intelligence. **No está en el SDK del simulador: pide aparato real.**

### 5 · Lo que no apareció

**Aislamiento por módulo: nada.** No hay primitiva de sandbox por-módulo. Lo más cercano es
`ExecutionTargets`, que es frontera de **proceso**, no de **datos**. El verbo `Aísla` del Host
sigue sin existir y sigue sin poder prometerse.

Tampoco cambió App Groups como capability del programa pago, ni hay señal de ruptura en firma,
entitlements o en la cláusula 2.5.2 — la ruta del intérprete de Mono queda como está.

---

## Qué re-medir, en orden

1. **Conseguir un aparato con A17 Pro o superior.** Sin eso, la mitad de esto no es medible y
   no tiene sentido empezar por otro lado.
2. **La pregunta semántica de Siri sobre las dos entidades** que ya tienen `indexingKey`. No es
   un cambio de código: es correr t6/t6b otra vez con Siri AI encendido.
3. **Un tool dinámico con `GenerationSchema` armado desde `ToolIndex`**, para ver si la Puerta 2
   se vuelve trivial en vez de desaparecer. Es la medición que más decide.
4. **`ExecutionTargets = .main`** sobre la intent genérica, para fijar el escritor único.
5. **`parameterPresentation` + `OptionsCollection`** sobre la intent genérica, para sacar el
   conteo de módulos del presupuesto de 10.

---

## Resumen de una línea

| Pregunta | Respuesta |
|---|---|
| Registrar tools en runtime | **No** · el muro sigue; `RelevantEntities` cubre contenido, no tools |
| Un asistente consumiendo MCP local | **No** · MCP es de Xcode 27; la vía es Siri → App Intents |
| Spotlight indexa más que el título | **Como texto, no se sabe; lo nuevo es que Siri consulta el índice** |
| Siri consulta a la app por voz | **Sí, pero por App Schemas de dominios en los que Gnothi no entra** |
| Foundation Models on-device sin red, con tools | **Sí, y con tools de schema dinámico** |
| Tope de 10 / texto libre en frases | **Sin cambios** · mitigable con `OptionsCollection` y `EntityStringQuery` |
| Sandbox por módulo | **Nada** |
| Firma / entitlements / AOT | **Sin cambios detectados**; .NET 10 con Xcode 27 en preview |

---

## Fuentes

Primarias, de Apple:

- Newsroom, 8 jun 2026 · <https://www.apple.com/newsroom/2026/06/apple-aids-app-development-with-new-intelligence-frameworks-and-advanced-tools/>
- WWDC26 240, *Build intelligent Siri experiences with App Schemas* · <https://developer.apple.com/videos/play/wwdc2026/240/>
- WWDC26 345, *Discover new capabilities in the App Intents framework* · <https://developer.apple.com/videos/play/wwdc2026/345/>
- WWDC26 241, *What's new in the Foundation Models framework* · <https://developer.apple.com/videos/play/wwdc2026/241/>
- WWDC26 339, *Bring an LLM provider to the Foundation Models framework* · <https://developer.apple.com/videos/play/wwdc2026/339/>
- Foros, tools con parámetros dinámicos · <https://developer.apple.com/forums/thread/793471>
- Foros, `parameterPresentation` y el límite de AppShortcuts · <https://developer.apple.com/forums/thread/795383>

Secundarias:

- Alcance real de MCP, Xcode y no iOS · <https://andrew.ooo/answers/apple-mcp-xcode-27-vs-anthropic-mcp-vs-microsoft-mcp-june-2026/>
- Requisitos de Apple Intelligence / Siri AI · <https://www.macworld.com/article/3160007/siri-ai-and-apple-intelligence-do-you-need-to-buy-a-new-iphone-ipad-or-mac.html>
- El nivel de 12 GB para el modelo más potente · <https://www.macrumors.com/2026/06/08/most-powerful-on-device-ai-now-requires-iphone-17-pro-or-air/>
- Core AI, rango de modelos y posición en el stack · <https://blakecrosley.com/blog/core-ai-run-models-apple-silicon>
- Core AI ausente del SDK del simulador · <https://github.com/john-rocky/coreai-kit>
- Tools dinámicos → host MCP con FoundationModels · <https://levelup.gitconnected.com/swift-foundation-models-dynamic-tools-with-dynamic-argument-structure-dynamic-calls-11e89ae96068>
- Soporte preview de Xcode 27 en .NET · <https://github.com/dotnet/macios/releases>
- Fecha de lanzamiento · <https://9to5mac.com/2026/08/25/ios-27-release-date-when-next-major-iphone-update-is-coming/>
