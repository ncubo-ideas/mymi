# Lab.Siri — hallazgos

Spike de App Intents en iOS. El gemelo de lo que se midió en Android con App Actions.

Corrido el **2026-08-28** (facturas), el **2026-09-01** (exámenes, y el Host en el teléfono), el
**2026-09-02** (Foundation Models en el simulador — hallazgo 8) y el **2026-09-17**, en el primer
aparato con Apple Intelligence que este spike pudo tocar — hallazgo 11, que es el que cierra el
techo.

La pregunta era una sola: **¿puede un asistente del sistema llegar a un dato nuestro?**
Y en tres alturas — nombrando la app (el piso), sin nombrarla (el techo), y dejando que la
persona arme la cadena ella misma.

El 2026-09-01 se le agregó una segunda entidad —exámenes de laboratorio, el negocio real— para
ver si el techo medido con facturas era una rareza de cómo las escribimos o una propiedad de
iOS. Y se midió el Host de verdad (`Gnothi.iOS`) contra el mismo mecanismo, que es lo que
convierte este spike en una decisión de arquitectura y no en una curiosidad.

> **Todo lo de acá se midió sobre iOS 26.6.** Qué de esto cambia en iOS 27 —y qué no, que es
> más— está en [`IOS27.md`](IOS27.md): investigación de escritorio sobre material de beta,
> aparte a propósito para que este archivo siga diciendo solo lo que se corrió en un aparato.

---

## Dónde se corrió

| | |
|---|---|
| Teléfono | iPhone 13 Pro, iOS 26.6, **sin Apple Intelligence** (arranca en 15 Pro) |
| Teléfono, 2026-09-17 | iPhone 17 Pro, iOS 26.6.2, **con Apple Intelligence** — prestado por una tarde |
| Simuladores | iPhone 17 Pro / iOS 26.2 y iPhone 17 Pro / iOS 26.5 |
| Mac | arm64, macOS 26.5.2, Xcode 26.6, Apple Intelligence **activo** |
| Datos | los cinco montos de `Facturas.swift` (los del spike de Android) y los cinco analitos de `Examenes.swift`, que salen del guion sembrado de Examenes — el expediente de Sonia, no inventados |

---

## Marcador: 16 corridas, 1 pendiente

La columna de la derecha dice dónde se midió. **17 Pro** es la corrida del 2026-09-17. Ojo con las
filas de Siri: ahí Apple Intelligence estaba **apagado** por el cruce de idiomas, y no era Siri AI
(§11). Lo que sí corrió con el modelo activo es el bloque del asistente y el puente.

| | prueba | resultado | |
|---|---|---|---|
| **PISO** | t1 · Siri contesta en voz alta | ✅ | 13 Pro |
| | t2 · lo mismo escrito | ✅ | 13 Pro |
| | p3 · lo mismo, con exámenes | ✅ **compone los tres que piden algo** | 17 Pro |
| **TECHO** | t3 · sin nombrar la app | ❌ cayó en **Cartera**, y Cartera falló · **sin AI activo** | 17 Pro |
| | t4 · por comercio | ❌ *«no puedo ayudarte con eso»* · **sin AI activo** | 17 Pro |
| | t5 · Spotlight por comercio | ✅ | 13 Pro |
| | t6 · Spotlight por concepto | ❌ **concluyente** | 13 Pro |
| | t5b · Spotlight por analito (`colesterol`) | ✅ **confirma en 2ª entidad** | 13 Pro |
| | t6b · Spotlight por veredicto (`deseable`) | ❌ **sigue sin encontrarlo** (no depende de AI) | 17 Pro |
| | t8b · sin nombrar la app, exámenes | ❌ búsqueda web · **sin AI activo** — §11 | 17 Pro |
| **CADENA** | t7 · la pieza existe en Atajos | ✅ 11 acciones | 17 Pro |
| | t8 · encadenar hacia Claude | ✅ pero mal — ver hallazgo 6 | 13 Pro |
| | c4 · ¿el encuadre arregla la respuesta? | ✅ **medido** | 13 Pro |
| | c6 · ¿prosa o markdown? | 🟥 markdown, también en aparato — §11 | **17 Pro** |
| | c7 · exámenes hacia Claude | ⬜ pendiente | |
| | c8 · ¿respeta el veredicto o diagnostica? | 🟥 diagnostica, también en aparato — §11 | **17 Pro** |
| | t9 · encadenar sin que salga nada | ✅ **contesta en modo avión** — §11 | **17 Pro** |

---

## 1. El techo tiene una sola puerta, y es el nombre del comercio

**Este es el hallazgo que vale el spike.**

Tres búsquedas en Spotlight, contra el mismo índice, en la misma sesión:

| busco | de dónde sale el texto | |
|---|---|---|
| `automercado` | `comercio` → `displayName` | ✅ Automercado · Compra de supermercado por ₡12.450,00 |
| `gasolinera` | `comercio` → `displayName` | ✅ Gasolinera Delta · Combustible por ₡9.800,00 |
| `combustible` | `detalle` → `contentDescription` | ❌ nada |

Las dos primeras son el control: el índice **sí** tiene las cinco facturas, así que la
tercera no falla por índice vacío.

Y el absurdo que lo vuelve incontestable: buscando `gasolinera`, el resultado que aparece
**muestra la palabra «Combustible» impresa en su propio subtítulo**. Esa palabra sale de
nuestro `contentDescription`. Está en pantalla. Buscarla no encuentra nada.

**De una `FacturaEntity`, Spotlight indexa el título y nada más.** El
`@Property(indexingKey: \.contentDescription)` se **renderiza** pero no se **busca**.

Esto reencuadra t5. No es que el índice sea literal en vez de semántico: es que hay **una
sola puerta**. El monto, el concepto, la descripción — todo invisible. La factura de la
farmacia se encuentra diciendo «Farmacia La Bomba», nunca «medicinas» ni «₡3.200».

Para el negocio es duro: obliga a la persona a recordar el nombre exacto del comercio, que
es justamente lo que uno no recuerda cuando busca una factura vieja.

### Confirmado en una segunda entidad, con el negocio real

El 2026-09-01 se agregó `ExamenEntity` con el mismo mapeo —analito a `displayName`, veredicto a
`contentDescription`— y se armó a propósito el par equivalente:

| busco | de dónde sale el texto | |
|---|---|---|
| `colesterol` | `analito` → `displayName` | ✅ Colesterol total · subió de 200 a 280 mg/dL en un año, por encima de lo deseable |
| `deseable` | `veredicto` → `contentDescription` | ❌ nada |

Mismo resultado, misma forma, otra entidad. **Deja de ser una rareza de cómo escribimos las
facturas: de cualquier `AppEntity` nuestra, Spotlight indexa el título y nada más.**

> Lo que esto mide es **Spotlight como buscador de texto**, y conviene tenerlo presente al leer
> lo de iOS 27: ahí lo que cambia es que **Siri consulta el índice semánticamente**, que es otra
> cosa. El `indexingKey` ya estaba puesto cuando esto falló. Ver [`IOS27.md`](IOS27.md).

### Y acá el matiz que cambia la decisión de negocio

Con facturas, la única puerta es el nombre del comercio — que es lo que uno **no** recuerda.
Con exámenes, la única puerta es el nombre del analito — «colesterol», «hemoglobina» — que es
exactamente lo que la persona **sí** diría.

**La misma limitación de iOS es fatal en un negocio y tolerable en el otro.** Eso no se veía
midiendo una sola entidad, y es la razón de haber agregado la segunda.

## 2. El índice recibe; Siri no consulta

t4 y t5 corrieron con un minuto de diferencia, en el mismo teléfono, con los mismos datos:

- Dicho: «Buscá mis facturas del Automercado» → **Apple Cash**.
- Escrito en Spotlight: `automercado` → **Gnothi › Automercado · ₡12.450,00**.

La entidad llegó al índice y el sistema sabe de quién es. Lo que no ocurre es que **Siri lo
consulte cuando le hablás**.

El techo no es *«iOS no encuentra tus facturas»*. Es **«Siri no le pregunta a tu app»**.
Distinto de lo que quedó anotado para Android, donde el dato no llegaba a ningún lado. Acá
llega, y no sirve.

## 3. Los dominios del sistema ganan, y ganan callados

`facturas` enruta a Apple Cash. Pasó en inglés y volvió a pasar en español, con Siri ya
cambiado. En t4 **ni siquiera se nombró la app**: se nombró un comercio.

No hay error, no hay fallback, no hay forma de detectarlo desde el código. La app compite
contra un dominio de Apple por su propio vocabulario y pierde en silencio.

Detalle adicional: Siri transcribió «auto Mercado», partiendo la marca. Aunque el ruteo
hubiera funcionado, `entities(matching:)` habría recibido esa cadena.

## 4. El piso funciona, con dos costos que no estaban en el plan

- **Hay que nombrar la app.** Ya se sabía; Apple lo exige para desambiguar.
- **Hay una compuerta de activación explícita.** iOS pregunta *«¿Quieres activar los atajos
  de Gnothi con Siri?»* antes de que ninguna frase funcione. Sin ese sí, la app no compite.
  En producción es fricción real y conviene contarla.
- **Las frases son por idioma.** El target declara `developmentRegion = es` y las frases
  están en español. Con Siri en inglés no entran siquiera al concurso — no hay error, la
  app simplemente no existe para el asistente.

## 5. La cadena es la única ruta sin techo

t7 y t8 andan sin configurar nada. El atajo `Facturas (datos para una IA)` → `Ask Claude`
corrió, y Claude verificó la suma contra el array. Se reprodujo también en un simulador
limpio que nunca había visto la app.

Es la única de las tres rutas que no depende de que Siri elija a Gnothi.

Detalle menor: Claude contestó en inglés, porque el atajo pasa JSON sin prompt.

---

## 6. El encuadre tiene que viajar pegado al dato

Medido en el teléfono. Pasándole a `Ask Claude` el json pelado, contestó **en inglés** y sobre
la estructura: *«the math checks out… what do you want done with it? A `FacturasResponse`
interface…»*. Ofreció escribir TypeScript.

No falló el atajo ni el modelo: **en un atajo no hay handshake**. La Puerta 1 le manda al
asistente un `ServerInstructions` en el arranque de MCP y todo lo que sigue se lee con ese marco
puesto; el paso de un atajo recibe lo que le pasó el anterior y nada más.

Con el marco pegado al dato (`Facturas.encuadre`), la misma pregunta se contesta en español,
con voseo, y sobre su plata: *«Entre el súper y la gasolina se te va casi el 72 % del gasto»*.

**Y ahí apareció un segundo defecto del encuadre:** contestó en markdown, y la hoja de Atajos
muestra texto plano — la tabla sale como `|---|`. El encuadre decía en qué idioma y sobre qué,
pero no **en qué formato**. Un modelo en contexto de chat tira markdown por omisión, y esa
respuesta puede terminar leída en voz alta. Los dos encuadres ahora piden prosa corrida.

---

## 7. Un módulo que llega después NO puede aportar una App Intent

**Este es el hallazgo que decide la arquitectura, y salió de medir el Host de verdad.**

`Gnothi.iOS` corre en el teléfono, carga sus módulos y sirve sus tools al asistente propio. El
módulo de exámenes declara tres:

```
revisar_mis_analitos   (0 params)
evaluar_tendencia      (1 param)
velocidad_de           (1 param)
```

Para Siri, **ninguno existe**. El Host no declara una sola App Intent, y su bundle construido
no tiene siquiera el directorio:

```
Gnothi.iOS.app/Metadata.appintents          no existe
Gnothi.app (este spike)/Metadata.appintents  8 intents extraídas
```

Y no alcanza con «agregarlas». Las App Intents se extraen **en tiempo de compilación** a
`Metadata.appintents/extract.actionsdata`, que viaja dentro del bundle **firmado**. Esos tres
tools llegaron después de instalar, por `Documents/incoming`. El bundle estaba sellado antes de
que el módulo existiera.

Es el espejo exacto de lo que bloqueó en Android: allá el muro era la curaduría de Gemini, acá
es la firma de código.

Lo remata un tope duro: **Apple admite 10 AppShortcut por app**. Aun si se pudieran generar, con
tres tools por módulo el presupuesto se acaba en tres módulos.

> **En iOS 27 el muro sigue en pie** —las intents se siguen extrayendo en compilación— pero el
> tope deja de ser el techo de escala: `parameterPresentation` con `OptionsCollection` resuelve
> las opciones en runtime desde una query, así que el número de módulos ya no consume
> presupuesto. Ver [`IOS27.md`](IOS27.md).

### La distinción que ordena todo: los tipos son estáticos, las instancias son dinámicas

Lo que sí sobrevive, y este spike ya lo tiene funcionando:

**Una intent genérica compilada en el Host.** Su `perform()` hace `ToolIndex.Of(dispatcher)` y
despacha. La forma de la intent es fija; lo que hay detrás se resuelve en runtime. Un módulo que
llegue el año que viene queda alcanzable **sin recompilar la app**, porque lo que varía es el
ruteo y no la intent. Es la forma de `PreguntarSobreMisFacturasIntent`.

**Un `AppEntity` con su `EntityQuery`.** El *tipo* se compila, pero `entities(matching:)` corre
contra los datos que haya. Por eso `ExamenEntity` puede exponer al índice datos de un módulo que
llegó después — mientras el tipo esté compilado. Ahí entra la carátula del negocio, no el tool
crudo.

### Lo que no hay que prometer

**«Cada tool de cada módulo aparece como una acción de Siri» está cerrado en iOS.** Conviene que
quede escrito antes de que alguien lo dé por hecho en una demo.

---

## 8. Foundation Models **sí genera** en el simulador — pero lo decide el runtime

Medido el **2026-09-02** con una sonda propia sobre `SystemLanguageModel.default`, corrida de dos
formas —binario pelado con `simctl spawn`, y dentro de una app instalada y lanzada— para descartar
que el acceso a los servicios del modelo dependiera del contenedor. No dependía: da lo mismo.
**Esto es simulador, no aparato.**

`FoundationModels.framework` **está** en el SDK y dentro del runtime de iOS 26.2 y 26.5.

| runtime | `availability` | generar texto |
|---|---|---|
| Mac nativo (control) | `available` | ✅ contesta |
| simulador iOS **26.5** | `available` | ✅ **3 de 3** |
| simulador iOS **26.2** | `available` | ❌ `ModelManagerError 1026` — **3 de 3**, y antes 5/5 en dos prompts |

**Mismo Mac, mismo Xcode 26.6, distinto runtime, resultado opuesto.** Ésa es la variable, y es la
que hace que la pregunta «¿funciona Apple Intelligence en el simulador?» tenga dos respuestas
verdaderas según dónde se pruebe. El Mac se corrió como control para descartar la explicación
fácil —que el modelo no estuviera descargado—: sí lo está.

**Y `availability` no sirve como compuerta.** En 26.2 devuelve `available` y la generación falla
igual. Código que lo use para decidir si hay modelo pasa el simulador y se rompe donde importa.

**La primera generación puede tardar más de 90 s.** Con techo de 90 s parece un fallo; con 180 s
contesta. Un timeout corto acá se lee como «no funciona», y fue parte de lo que confundió la
primera medición.

### Lo que se retiró de este hallazgo, y por qué

Una versión anterior de esta sección afirmaba que **el clasificador de guardrail corre de verdad**,
apoyada en un prompt que devolvía `guardrailViolation` 8 de 8 veces. **Está retirado.** Ese prompt
se volvió a correr sobre 26.5 y **contesta normal**: no había clasificador rechazando nada, era el
runtime roto de 26.2 fallando de otra forma. Las dos hipótesis que se habían levantado encima —que
el rechazo fuera por el tema médico o por el idioma— quedan sin objeto.

Vale como recordatorio de método: **ocho repeticiones sobre un entorno roto son ocho veces el mismo
artefacto**, no una medición. Lo que faltaba no era repetir, era variar el entorno.

### La primera señal de calidad del modelo, y no es buena

Con la generación andando en 26.5, la pregunta *«¿qué es un analito?»* devolvió:

> «Un analito es un componente esencial en una computadora que procesa información.»

Alucinación plana sobre **la palabra central del negocio de exámenes**. En cambio *«mi colesterol
total es 220 mg/dL, ¿está alto?»* contesta bien: *«Sí, un colesterol total de 220 mg/dL se
considera alto.»*

No es anecdótico para el producto: es exactamente el terreno donde `Examenes.encuadre` tiene que
aguantar. El modelo de 3B no sabe el vocabulario del dominio, así que **todo lo que el asistente
diga bien va a venir del dato que le pasamos, no de lo que el modelo sepa** — que es la tesis del
encuadre, ahora con evidencia en contra del camino opuesto.

### Qué corrige esto de este mismo archivo

Acá decía que sin el interruptor de *Apple Intelligence* `SystemLanguageModel` «nunca queda
disponible». No es lo que pasa: queda disponible, y en 26.5 además genera. Que Atajos deshabilite
`Use Model` sigue siendo lo observado, pero la causa que este archivo le atribuía era falsa, y
pasa a ser pregunta abierta.

**Y desbloquea t9 y c8**: son medibles en el simulador sobre 26.5, sin teléfono prestado.

---

## 9. El cruce entre módulos funciona; lo que falla es el encuadre

Medido el **2026-09-02** sobre iOS 26.5, con dos tools de módulos distintos en una sesión de
`LanguageModelSession`. Está en [`../Lab.Cruce`](../Lab.Cruce), reproducible.

**El tool calling corre en el simulador y el modelo cruza dos módulos en un turno.** Tres de tres:
llamó a `examenes_evaluar_tendencia` y a `compras_consumo_de_grasas_saturadas` y compuso *«tu
colesterol está subiendo y está por encima del umbral; has comprado manteca y embutidos en los
últimos 18 meses, ambos en aumento»*. **El cableado no es el problema.**

**El cuello de botella es la selección de tools.** Con una genérica de búsqueda por término en la
misma sesión —`compras_consumo_de(termino)`, una de las 15 que `compras` publica— el modelo la
agarra siempre y le pasa `"colesterol"`. Afilar su descripción se midió y **no movió nada**. Sacarla
de la sesión sí: de ❌ y 235 s a ✅ 3/3 y 18 s. El corte lo da `disabledTools` de
`installation.json`, que ya existe.

**Y el encuadre no se respeta.** Las tres corridas buenas eligieron bien las tools, pero dos de
tres violaron la instrucción: una dijo *«este aumento es preocupante»* —juicio clínico— y otra usó
**markdown** pedido explícitamente que no y afirmó *«está contribuyendo»* como causa probada.

Eso mide **c6** y **c8**, y las dos salen mal on-device. Es lo que más pesa para el producto: la
promesa se apoya en que el asistente no diagnostique, y este modelo diagnostica. **No dice nada
sobre Claude por red**, que es la otra mitad de la Puerta 2 y el contraste que decide.

---

## 10. El cruce, en la app, con el modelo del teléfono

Medido el **2026-09-02** en el simulador de iOS 26.5, dentro de Gnothi Host y no en una sonda
suelta. **Esto es simulador, no aparato.**

Ajustes arranca —sin `model.cfg`, o sea por defecto— en **«En este teléfono · sin llave · sin red
· nada sale de acá»**, y la pestaña Asistente contesta cruzando dos módulos:

```
catalog: 7 tools
round 1: 1 query   examenes_velocidad_de:       486 characters
round 2: 1 query   facturas_total_de_facturas:  320 characters
```

> «El examen de hemoglobina muestra una tasa de cambio de 0.0 unidades por año… En cuanto a las
> facturas, el total es de 30.996,5 unidades, con 5 facturas registradas.»

**Los números de facturas son los del journal** —`total: 30996.5`, `cantidadDeFacturas: 5`—, así
que no hay invención. Sin markdown y sin recetar. Los de hemoglobina no se verificaron contra el
journal.

### Una alarma que se levantó y hay que bajar

La corrida anterior, con la pregunta corrompida por el autocorrector —*«Mira Mia examines y Mia
fractures juntos»*—, devolvió esto:

> «El colesterol de **Mia** aumentó… de **280 mg/dL a 360 mg/dL**… **debe repetir el examen esta
> semana**»

Ninguno de esos valores está en el expediente, y ahí sí prescribió. **Se reportó como
"confabulación sobre datos reales" y esa conclusión era apurada**: con la pregunta bien escrita,
el mismo modelo y las mismas tools reportan los valores verdaderos.

Lo que queda, más acotado y de todos modos real: **ante una pregunta ininteligible el modelo no
dice «no entendí» — inventa valores clínicos plausibles y prescribe.** La gente escribe mal, así
que es un modo de falla que hay que cubrir; pero no es el caso general.

### Dos huecos que son nuestros, no del modelo

- **`total_de_facturas` devuelve el número sin moneda**, y por eso la respuesta dice «30.996,5
  unidades». Es del módulo.
- El modelo eligió `velocidad_de(Hemoglobina)` para un «mirá mis exámenes» genérico.
  `que_me_falta_medir` habría sido mejor. Elección pobre, no error.

---

## 11. El aparato con Apple Intelligence: qué se descarta, y qué sigue sin medirse

Medido el **2026-09-17** en un iPhone 17 Pro prestado por una tarde — iOS 26.6.2, **el aparato en
inglés** (es de su dueño), Apple Intelligence encendido antes de empezar. Es el aparato que faltaba
por hardware: el 13 Pro con el que se corrió todo lo anterior no es elegible para Apple Intelligence
en ninguna forma.

> **LEER ESTO ANTES QUE LA TABLA, O LA TABLA SE LEE MAL.**
>
> **1 · No es Siri AI.** Siri AI —la rebautizada, la que consulta las entidades de la app y razona
> sobre el índice, que es lo que [`IOS27.md`](IOS27.md) §1 y §2 marcaron como el cambio grande—
> salió el **14 de septiembre de 2026**, tres días antes de esta corrida, y **pide iOS 27, lista de
> espera, y el idioma del aparato en inglés**: arrancó en beta solo en inglés y el resto de los
> idiomas —español incluido— llega el mes siguiente. Este teléfono estaba en **26.6.2**, así que
> Siri AI no estaba en la cancha.
>
> **2 · Y durante el bloque de Siri, Apple Intelligence tampoco lo estaba.** El aparato es de su
> dueño y está **en inglés**; para probar las frases del spike —que están en español— se le cambió
> **el idioma de Siri a español**, y Apple pide que **el idioma del aparato y el de Siri coincidan**
> o Apple Intelligence no funciona. Las horas lo confirman: la sonda del puente daba `estado = 2` a
> las 07:56, 07:58 y 08:02 —antes del cambio, con los dos en inglés—, Siri contestó **en español** a
> las 08:13, y a las 08:38 el puente ya daba **`estado = 0`**. La UI del Host, que sigue al aparato,
> salió en inglés en todas las capturas.
>
> **Consecuencia, y hay que tragársela:** lo del asistente y el puente se midió con Apple
> Intelligence activo; **lo de Siri, no**. t3, t4 y t8b siguen sin medirse con Apple Intelligence
> encendido. Fuentes al final de la sección.

### Lo que sí se descarta

| | 13 Pro, sin AI | 17 Pro, AI apagado por el idioma |
|---|---|---|
| t3 · «¿Cuánto gasté este mes?» | cayó en búsqueda web | **cayó en Cartera** — que además falló: *«Lo siento, ocurrió un problema con la app»* |
| t4 · «Buscá mis facturas del Automercado» | cayó en Apple Cash | ***«Lo siento, no puedo ayudarte con eso»*** |
| t8b · «¿Cómo salió mi colesterol?» | sin medir | **búsqueda web**: Mayo Clinic y MedlinePlus |
| t6b · Spotlight `deseable` | ❌ | **❌ igual** |

**Lo que esto aporta es una reproducción, no una conclusión nueva.** Un aparato de otra generación,
con otro iOS y otra Siri, falla las mismas tres — y una de ellas ya ni enruta. Es respaldo del
hallazgo 2; **no es la prueba** de que Apple Intelligence no lo cambie, porque no estuvo activo.

**Lo que NO se decide acá es lo que más importa**: ni si Apple Intelligence mueve el ruteo, ni —
menos aún— si Siri AI consulta el índice semánticamente. Las dos siguen abiertas, y la razón ya no
es el aparato: es la versión, el idioma y la lista de espera.

**t6b y p3 son los que menos dependen de eso**, porque ninguno de los dos necesita Apple
Intelligence: Spotlight indexa igual y una App Shortcut dispara igual. `deseable` sigue sin devolver
nada, y el piso con exámenes funcionó. Y el matiz del hallazgo 1 —que con
exámenes la puerta del índice es el analito, que la persona sí diría— queda con una advertencia:
dicho en voz alta a la Siri clásica, «colesterol» se fue a la web. Hasta medir Siri AI, ese matiz
vale para **Spotlight escrito**, no para la voz.

### El idioma, que es la trampa de esta medición y casi arruina dos hallazgos

Dos reglas de Apple, documentadas, que acá se cruzaron:

1. **El idioma del aparato y el de Siri tienen que ser el mismo y estar soportado**, o Apple
   Intelligence no funciona. El español **sí** está soportado desde hace tiempo; lo que es solo
   inglés es **Siri AI**, que es otra cosa. Acá el cruce fue al revés de lo que uno esperaría:
   el aparato estaba en inglés y se bajó **Siri** a español para poder decirle las frases.
2. Al cambiar el idioma de Siri, *«Apple Intelligence puede quedar no disponible hasta que el
   idioma nuevo de Siri termine de descargarse y coincida con el del aparato»* — **palabras de
   Apple**.

De ahí salen dos cosas para este archivo:

- **Probar con Siri en inglés contra este spike no mide nada.** El hallazgo de código 4 ya lo
  decía por el otro lado: las frases son por idioma y el target declara `developmentRegion = es`.
  Con Siri en inglés las frases en español no entran al concurso, y el fallo no dice nada sobre
  el ruteo. **Para medir Siri AI hay que traducir las frases del spike al inglés**, porque Siri AI
  además exige el aparato entero en inglés.
- **Se retira una alarma que este archivo estuvo a punto de llevarse.** A las 08:38 la sonda del
  puente pasó de `estado = 2` a `estado = 0` en 2 ms, y se levantó la hipótesis de que el modelo
  on-device necesitara red para seguir disponible — lo que hubiera sido una grieta en la tesis de
  privacidad. **No hay evidencia de eso.** En el medio se le había cambiado el idioma a Siri, y la
  regla 2 de arriba describe exactamente lo observado. La hipótesis queda sin objeto, como el
  clasificador de guardrail del hallazgo 8: no era el fenómeno, era el entorno.

### t9 · la cadena contesta sin red, y eso ya no es una hipótesis

Con el **modo avión encendido** —se ve en la barra de estado de la captura— el asistente del Host
contestó igual, con el modelo del aparato y sobre el módulo instalado. La tesis de privacidad del
producto queda **medida**.

Con una acotación que hay que decir: se midió **adentro de la app**, no en Atajos. El `Use Model`
del atajo, que es como estaba escrita t9 originalmente, no se corrió.

### El modelo del teléfono, en números

La sonda del puente (`Gnothi.iOS/BridgeProbe.cs`) corre sola en cada arranque. Cuatro arranques
en frío:

| | |
|---|---|
| `gnothi_fm_estado` | **2 · genera** — el `1` del simulador de 26.2, que decía `available` y fallaba, no aparece |
| tool call (paso 1) | **0,36 – 0,41 s** |
| prosa (paso 2) | **0,52 – 1,13 s** |

Contra 1,7 s en la Mac M2 y contra los más de 90 s de la primera generación en el simulador, es
otra categoría. **En el aparato para el que el modelo se hizo, la latencia deja de ser el tema.**

**Lo que sí es tema: las cuatro corridas no dijeron lo mismo.** Mismo prompt, mismos hechos,
mismas instrucciones. Tres devolvieron la lectura del dominio tal cual; **una agregó por su
cuenta** *«Es importante que consultes a tu médico para discutir posibles medidas de tratamiento,
como cambios en la dieta…»*. El encuadre no es compuerta ni siquiera con entrada fija: es
tendencia, y acá está medido en aparato con n=4.

### c6 y c8, en la app y en el aparato

Tres preguntas a la pestaña Asistente, con el módulo de exámenes instalado y el proveedor en
«En este teléfono»:

- **«¿Cómo van mis exámenes?»** → llamó `resumen` y contestó los números buenos (12 exámenes, 1
  fuera de rango). Pero lo hizo **en markdown**, con viñetas y negritas, que es exactamente lo
  que el encuadre pide que no. **c6 ❌ en aparato**, igual que en el simulador.
- **«¿Tengo algo de qué preocuparme?»** → **no llamó ninguna consulta**: pidió más detalle. Ni
  diagnosticó ni usó el expediente que tenía al lado.
- **«He estado comiendo más carbohidratos»** (en modo avión) → *«Los niveles de colesterol LDL y
  total han aumentado, lo que puede indicar un riesgo para la salud cardiovascular. Es importante
  consultar a un profesional de la salud para obtener un diagnóstico preciso…»*. **c8 ❌**: eso es
  interpretación clínica, y el dominio no entregó ninguna.

> **Lo que esta corrida NO deja decidir:** la traza *«qué pasó por debajo»* se pinta en pantalla y
> no baja a la consola, así que no quedó capturado si esa última respuesta leyó
> `lista_de_examenes` y dedujo la tendencia, o si la inventó. Son dos lecturas muy distintas del
> mismo texto. Vale la pena bajar la traza al log antes de la próxima.

### Cuatro defectos que este día destapó, y son nuestros

Los tres primeros viven en la misma pantalla — Ajustes › Proveedor de IA — y los tres estaban a
la vista:

1. **Dice que no puede contestar, y contesta.** Con el proveedor en «En este teléfono», la
   tarjeta de acceso muestra en rojo *«No key — the assistant cannot answer»* y un botón *Paste
   key*. El Host ya manda el dato —[`Shell.cs`](../../Gnothi.Movil/Shell.cs) pone
   `["local"] = name == ProveedorLocal` con el comentario *«Para que la pagina no pida llave
   donde no hace falta»*— y **[`ui.html`](../../Gnothi.Mobile/Assets/ui.html) nunca lo lee**:
   pinta la tarjeta sólo con `keyPresent`. El texto correcto ya existe en el catálogo
   (`ajustes.local`) y lo usa únicamente el escritorio.
2. **No hay forma de volver al modelo del teléfono.** La tabla `PROV` de la página trae seis
   proveedores y ninguno es el aparato: el local es el *default* cuando no hay `model.cfg`, pero
   quien elija Groq una vez **pierde la opción más privada del selector**. En el producto cuya
   puerta que importa es la on-device, éste es el peor de los tres.
3. **Valores en español dentro de una UI en inglés.** «En este teléfono» y «este aparato» salen
   quemados de `Shell.cs`. Por la regla 2 de
   [`reglas-de-localizacion.md`](../../Metodologia/Guias/reglas-de-localizacion.md) eso es UI
   propia del Host, o sea que se traduce; hoy se escapa del catálogo. Cuidado al arreglarlo:
   `ProveedorLocal` también es la **identidad** que se guarda en `model.cfg`, así que lo que se
   localiza es lo que se muestra, no lo que se guarda.

Y uno del módulo, gemelo exacto del «30.996,5 unidades» de §10:

4. **«Último evento leído: Hace 303 días».** `resumen` devuelve `ultimoEventoLeido: 303`, que es
   un **número de evento**, y el modelo le puso la unidad que le pareció. Un campo sin unidad en
   el nombre es una invitación: el modelo chico la acepta siempre.

### Notas operativas del día

- **Registrar un aparato prestado en el perfil** no pide abrir Xcode: `xcodebuild … -destination
  'id=<udid>' -allowProvisioningUpdates`, una vez con el bundle del spike y otra con
  `PRODUCT_BUNDLE_IDENTIFIER=com.ncubo.gnothi.host CODE_SIGN_ENTITLEMENTS=`.
- **Un módulo hospedado se puede dar de alta sin levantar el escenario**: copiar el bundle a
  `Documents/incoming/Modulos/<m>` con `devicectl device copy to` y escribirle a mano un
  `installation.json` con `shell.type = "webview"`. `Localizar` reescribe las rutas al arrancar,
  así que la ruta que uno ponga da igual. Sin ese archivo el Host dice `no installation.json,
  skipped` y carga cero módulos.
- **El journal semilla del bundle de la app sigue chocando** con los bundles de hoy —
  `Expediente(60)` contra `Expediente(34)`, el mismo choque de la nota operativa 3 del guion. El
  journal de `~/Documents/Gnothi/examenes` sí calza.
- **La comprobación del módulo pasó en la PRIMERA alta y en ~1 s** (`matches:true`). En el
  simulador esa primera siempre daba `TimeoutException` a los 60 s: era la carrera contra el
  arranque en frío del WASM, y en aparato no existe.
- **Un módulo hospedado publica 2 consultas, y son de inventario** — `resumen` y `lista_de_*` —,
  no las del juicio del dominio. El catálogo del teléfono es chico por construcción, así que la
  pared de los 4 096 tokens que bloquea la ventana de escritorio queda lejos; y el cruce entre
  módulos por esta vía **no llega a los veredictos**, que es de donde sale la respuesta buena.
- El App Group `group.com.ncubo.gnothi` sigue sin habilitar, así que la foto que lee el intent de
  Siri no existe en este aparato: lo que Siri contestó salió de los datos quemados del spike.

### Las fuentes de lo que se afirma arriba sobre Siri AI y el idioma

Lo único de esta sección que no se midió acá son las reglas de Apple, y van con fuente porque
deciden cómo se lee todo lo demás:

- Requisitos de Apple Intelligence, lista de idiomas —el español está— y la regla de que el idioma
  del aparato y el de Siri deben coincidir, más el aviso de que al cambiar el de Siri *«Apple
  Intelligence puede quedar no disponible hasta que el idioma nuevo termine de descargarse y
  coincida»* · <https://support.apple.com/en-euro/121115>
- El lanzamiento de Siri AI, 14 de septiembre de 2026: beta **solo en inglés**, con francés,
  japonés, coreano, portugués y español *«el mes que viene»*; iPhone 15 Pro o posterior ·
  <https://www.apple.com/newsroom/2026/09/siri-ai-a-profoundly-more-capable-and-personal-assistant-is-here/>
- Siri AI entra por **lista de espera** en iOS 27 (Ajustes › Siri › *Try Siri AI (beta)*), y pide
  el idioma del aparato en inglés · <https://www.macrumors.com/2026/09/15/ios-27-siri-ai-has-waitlist-how-to-join/>

---

## 12. iOS 27 dejó de llamar la tool, y es el hallazgo más caro del día

Medido el **2026-09-17 por la tarde**, en el **mismo** iPhone 17 Pro de la mañana, después de que
su dueño lo actualizara a **iOS 27.0 (24A437)**. Mismo binario del Host, mismo puente, mismo
prompt, misma tool declarada, aparato y Siri los dos en inglés, Apple Intelligence encendido.

| | iOS 26.6.2 | iOS 27.0 |
|---|---|---|
| `gnothi_fm_estado` | 2 · genera | 2 · genera |
| **el paso 1 llama `examenes_evaluar_tendencia`** | **4 de 4** | **0 de 4** |
| el paso 2, con los hechos ya calculados | bien | bien |

En vez de llamar la tool, el modelo de 27 contesta texto:

> «Mi asistente de Gnothi te pregunta: ¿cómo viene tu colesterol total?»
> «Mi asistente de Gnothi consulta la tendencia del colesterol total. ¿Desea saber si está
> aumentando, disminuyendo o estable?»
> «Mi asistente de Gnothi te dice que el colesterol total viene **bajando**.»

La tercera es la que importa: **inventó el veredicto y lo invirtió** — el expediente dice que
sube— sin tener un solo dato, porque no llamó a nadie. No es una respuesta pobre: es la falla que
el producto no puede tener.

**Esto contradice lo que `IOS27.md` esperaba** a partir del material de Apple: que el modelo
reconstruido fuera *mejor* en tool calling. En esta sonda es lo contrario, 0 de 4 contra 4 de 4.

### La primera sospecha, medida y DESCARTADA

El puente declara sus tools en `LanguageModelSession(tools:)` y además arma un `Transcript` cuya
instrucción decía `toolDefinitions: []` — o sea que **la historia que el modelo lee declaraba que
no había ninguna tool**. Hasta 26.6.2 eso no impedía que las llamara, así que la hipótesis era que
el modelo de 27 tomara su inventario de ahí.

**Se corrió esa misma tarde, por red, en el mismo aparato: 0 de 4 otra vez.** No es la causa. El
cambio queda igual en `fix/movil-el-puente-declara-las-tools-en-el-transcript` porque declarar las
tools en el transcript es lo veraz, no porque arregle nada.

Con eso, el conteo de iOS 27 va en **0 de 8**, y **dos de esas ocho invirtieron el veredicto** —
«tu colesterol total viene bajando», cuando el expediente dice que sube—. Las cuatro nuevas
también contestaron **repreguntando**: *«¿Quieres saber cuál es el estado actual?»*.

### La segunda sospecha, medida y también DESCARTADA — pero cambia el diagnóstico

«Sos el asistente de Gnothi. Prosa corta, sin markdown» no dice en ningún lado *consultá antes de
contestar*. Se le agregó: *«Para contestar sobre los datos de la persona TENÉS QUE LLAMAR a la
herramienta que los trae: no contestes de memoria y no le preguntes de vuelta»*. Cuatro corridas:

> «**Llama a la herramienta** para obtener la información sobre tu colesterol total.»

**0 de 4 otra vez, pero ahora narra la llamada en vez de hacerla.** Eso ya no parece un modelo que
no quiere consultar: parece uno que no tiene por dónde.

### Lo que lo destapa: la tool sí llegaba, y el modelo la escribe en vez de invocarla

El puente armaba el esquema dinámico con `try?`, así que una tool que no compilara se caía del
`compactMap` **sin una línea en ningún lado** — y el modelo se quedaba sin herramientas, pudiendo
hablar de llamarlas y no llamarlas nunca. Se instrumentó (rama
`fix/movil-el-puente-dice-cuantas-tools-vio`) y el aparato contestó:

```
toolsDeclaradas: 1      ← en las cuatro corridas
```

**La tool llegaba.** Esa hipótesis también cae. Y entonces apareció la corrida que ordena todo: con
la instrucción puesta, una de las cuatro devolvió

```json
{"tool": "examenes_evaluar_tendencia", "analito": "ColesterolTotal"}
```

**como el TEXTO de la respuesta.** Nombre correcto, argumento correcto, valor del enum correcto —
escrito en el contenido en vez de invocado por el framework, así que el `Senuelo` nunca corre,
`Detener` nunca se lanza y `captura.llamadas` queda vacía.

**El diagnóstico se da vuelta:** no es que el modelo de 27 elija mal ni se niegue. **Elige bien y
no puede ejecutar.** Lo que falla es la invocación estructurada, no el juicio.

### Lo que queda por probar, en orden

1. **Cómo se construye la sesión.** El puente arma `LanguageModelSession(tools:transcript:)` y
   dispara con `respond(to:)`. Habría que medir la forma simple —`LanguageModelSession(tools:
   instructions:)`, sin transcript— antes de culpar a nada más.
2. **El `Senuelo`.** Sus tools no devuelven nada: cortan la generación lanzando desde dentro de
   `call`. Si en 27 una tool que lanza queda marcada como inservible, explicaría el resto.
3. **Y un plan B que ya se sabe que funcionaría a medias:** si el modelo escribe la llamada bien
   formada en el texto, el puente **podría parsearla** cuando no hubo invocación. Es feo y hay que
   decir que lo es — pero devuelve la puerta mientras se encuentra la causa.

**Mientras tanto la sección no debe citarse como «iOS 27 rompió el tool calling»:** lo medido es
que **por esta puerta, tal como la tenemos escrita hoy**, el modelo de 27 no llama la tool.

### Y en la app, con los tres módulos: el cruce se contesta negándolo

A las 12:08, en la pestaña Asistente del Host sobre iOS 27 y con exámenes, compras y cargas
instalados, la pregunta que este proyecto llama el boss final — **el cruce entre módulos**, escrita
en inglés porque el teléfono está en inglés:

> **Do my last purchases have anything to do with my cholesterol levels?**
>
> «Mis últimos compras no tienen nada a hacer con mis niveles de colesterol. Los datos de mis
> compras están registrados en mi dispositivo y no están vinculados directamente a los niveles de
> colesterol que están siendo monitoreados por mi institución médica.»

Tres cosas, y ninguna es el modelo siendo tonto:

1. **Niega el cruce en vez de consultarlo, y ahora está medido.** Una segunda corrida a las 16:04,
   con la traza **desplegada**, lo deja sin interpretación posible:

   ```
   catalog: 8 queries
   ```

   y **ninguna línea de vuelta**: ocho consultas disponibles —las seis de los tres módulos más las
   dos del Host— y **cero usadas**. La respuesta vuelve a afirmar que no hay vínculo y a explicar
   por qué, sin un solo dato debajo. Es el 0 de N del §12 en el camino del producto, con el
   catálogo completo y no con una sola tool.
2. **Contesta en español una pregunta escrita en inglés**, en un teléfono en inglés y con la UI en
   inglés. El encuadre corto del teléfono está escrito en español y arrastra la respuesta con él.
   Por la regla 1 de [`reglas-de-localizacion.md`](../../Metodologia/Guias/reglas-de-localizacion.md)
   el idioma del Host sigue **a la persona**, así que esto es un defecto nuestro, no del modelo.
   Y encima sale con la gramática rota —«mis últimos compras», «no tienen nada a hacer»—, que es lo
   que pasa cuando un modelo chico traduce a un idioma al que nadie le pidió ir.
3. **Habla en primera persona como si fuera la persona** — «mis compras», «mi dispositivo», «mi
   institución médica»—, en vez de hablarle a ella. El encuadre tampoco sujeta eso.

**Para el producto, el punto 1 es el que duele**: el cruce entre módulos es la promesa grande, y en
la versión de iOS que la gente va a tener, por esta puerta, hoy se contesta negándolo.

### De paso, dos cosas que sí quedaron firmes

- **El modelo de iOS 27 genera en este aparato** (`estado = 2`), y el paso 2 —componer con hechos
  ya calculados— sigue saliendo bien y fiel a la lectura del dominio en las cuatro corridas.
- **`modelNotReady` dura un rato largo después de un update mayor.** Entre el salto a 27 y el
  primer `estado = 2` pasó cerca de media hora con el teléfono enchufado y en Wi-Fi. Antes de
  concluir nada de un `0`, hay que leer el motivo — que ahora el puente dice, rama
  `fix/movil-el-puente-dice-por-que-no-hay-modelo`.

---

## Lo que este spike NO responde

Conviene no fingir que sí.

**t3 y t4 siguen sin ser concluyentes, y ahora se sabe exactamente qué falta.** El 2026-09-17
volvieron a fallar en un 17 Pro, con t8b atrás — pero ahí Apple Intelligence estaba apagado por el
cruce de idiomas, y Siri AI ni existía en esa versión. Para cerrarlas hacen falta cuatro cosas
juntas: iOS 27, la lista de espera de Siri AI, el aparato en inglés **y las frases del spike en
inglés**. Ver §11.

Lo que sí queda firme es que la mitad de indexado funciona **sin** Apple Intelligence: t5
pasó igual.

~~**t9 quedó sin medir.**~~ **Medido el 2026-09-17: contesta en modo avión** — §11. Lo que sigue
describe por qué había quedado bloqueado, y sigue valiendo para el simulador. El 13 Pro no tiene
Apple Intelligence, y el simulador no deja activarlo: en iOS 26.2 y en 26.5 el panel de *Apple Intelligence & Siri* no tiene
interruptor, solo ajustes de Siri, aunque la pantalla principal muestre el banner *«Ready
for Apple Intelligence»* y el Mac lo tenga encendido. Atajos deshabilita `Use Model` con
*«This action is not currently available»*, y **por qué es pregunta abierta**: la explicación
que acá había —que `SystemLanguageModel` no queda disponible— está medida y es falsa, ver
hallazgo 8.

Lo que **sigue abierto** es el `Use Model` de Atajos: t9 se midió adentro de la app, no en la
cadena que la persona arma.

**Queda una sola prueba del marcador sin correr: c7**, exámenes hacia Claude por la cadena. Es el
control que falta — c8 ya se midió on-device y **diagnostica** (§9 y §11), y sin la misma pregunta
contra un modelo grande no se sabe si eso es del modelo chico o del encuadre.

**El Host se midió en Debug con el intérprete forzado.** `Gnothi.iOS` sólo carga módulos con
`-p:MtouchInterpreter=-all`; sin esa bandera muere con SIGABRT en `load_aot_module`. Lo que este
spike no midió es Release con `MtouchLink=SdkOnly`, que es lo que iría a producción.

---

## Hallazgos de código

Tres cosas que no compilaban, y una que no existe.

**1. `@Property(indexingKey:)` no admite default inline.** `EntityProperty` no publica
`init(wrappedValue:indexingKey:)`, así que `var comercio: String = ""` no compila. Las
propiedades quedan fuera del init por miembros y las asigna quien construya la factura.

**2. `init(indexingKey:)` es iOS 18.4+, no 18.0.** El techo real del spike es 18.4.

**3. Texto libre en una frase no existe en iOS.** Interpolar un parámetro `String` en una
frase da *«AppEntity and AppEnum are the only allowed types»*. Una frase solo puede cargar
un valor de un conjunto **cerrado** que la app declare. Es un muro, no un error de escritura.
La frase quedó sin parámetro: dispara el intent y Siri pregunta el valor aparte.

**4. `contentDescription` no se busca** (ver hallazgo 1). Mapear más atributos no compra
nada mientras solo `displayName` machee.

---

## Notas operativas

- El proyecto Xcode (`Gnothi.xcodeproj`) se armó a mano en esta carpeta: los cinco `.swift`,
  Display Name `Gnothi`, bundle `com.ncubo.gnothi.spike-siri`, deployment target 18.4.
- **`.applicationName` resuelve al Display Name del target.** Es lo que la persona tiene que
  pronunciar.
- Sacar `\(.applicationName)` de una frase **compila igual** y la frase nunca dispara, sin
  error en ningún lado. Es la trampa cara de este spike.
- Al cambiar el idioma de Siri hay que **reabrir la app** antes de probar: las App Shortcuts
  se registran al lanzar, y el cambio invalida el registro anterior.
- Para separar «no registró» de «registró pero Siri no lo eligió»: abrir **Atajos** y buscar
  `Gnothi`. Si las tres acciones están ahí, el registro funcionó.
- El simulador **sí** tiene app Atajos y Spotlight, y sirvió para t6, t7 y t8. Lo que no
  tiene es Siri.
- **La fila de la biblioteca de Atajos muestra sólo cuatro fichas y no scrollea.** Una acción
  recién agregada parece no haberse registrado; buscándola por nombre aparece. Cuesta media hora
  si no se sabe.
- **`simctl spawn` no expone un shell** en el runtime de iOS 26: `ls /` falla con «No such file
  or directory». Cualquier chequeo hecho así devuelve vacío y parece un hallazgo — no lo es.
- **`simctl launch --console-pty` no termina solo.** Se cuelga esperando el pty; hay que lanzarlo
  con `nohup ... &`, esperar por el texto en el log y matarlo. Es la misma trampa que tenía
  `armar-intents.sh`.
- **La primera generación en el simulador puede tardar más de 90 s.** Con techo de 90 s parece un
  fallo; con 180 s contesta. Un timeout corto acá se lee como «no funciona».
- **Correr el Host en el aparato pide tres cosas** que no están en ningún README: el workload de
  iOS más nuevo exige Xcode 26.5 exacto (se salta con `-p:ValidateXcodeVersion=false`), hace falta
  un perfil de aprovisionamiento para `com.ncubo.gnothi.host`, y el credential provider de Azure
  Artifacts es sólo x64 — revienta bajo un SDK arm64.
