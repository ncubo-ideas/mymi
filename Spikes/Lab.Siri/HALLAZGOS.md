# Lab.Siri — hallazgos

Spike de App Intents en iOS. El gemelo de lo que se midió en Android con App Actions.

Corrido el **2026-08-28** (facturas) y el **2026-09-01** (exámenes, y el Host en el teléfono).

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
| Simuladores | iPhone 17 Pro / iOS 26.2 y iPhone 17 Pro / iOS 26.5 |
| Mac | arm64, macOS 26.5.2, Xcode 26.6, Apple Intelligence **activo** |
| Datos | los cinco montos de `Facturas.swift` (los del spike de Android) y los cinco analitos de `Examenes.swift`, que salen del guion sembrado de Examenes — el expediente de Sonia, no inventados |

---

## Marcador: 11 corridas, 6 pendientes

| | prueba | resultado |
|---|---|---|
| **PISO** | t1 · Siri contesta en voz alta | ✅ |
| | t2 · lo mismo escrito | ✅ |
| | p3 · lo mismo, con exámenes | ⬜ pendiente |
| **TECHO** | t3 · sin nombrar la app | ❌ cayó en búsqueda web |
| | t4 · por comercio | ❌ cayó en Apple Cash |
| | t5 · Spotlight por comercio | ✅ |
| | t6 · Spotlight por concepto | ❌ **concluyente** |
| | t5b · Spotlight por analito (`colesterol`) | ✅ **confirma en 2ª entidad** |
| | t6b · Spotlight por veredicto (`deseable`) | ❌ **confirma en 2ª entidad** |
| | t8b · sin nombrar la app, exámenes | ⬜ pendiente |
| **CADENA** | t7 · la pieza existe en Atajos | ✅ |
| | t8 · encadenar hacia Claude | ✅ pero mal — ver hallazgo 6 |
| | c4 · ¿el encuadre arregla la respuesta? | ✅ **medido** |
| | c6 · ¿prosa o markdown? | ⬜ pendiente |
| | c7 · exámenes hacia Claude | ⬜ pendiente |
| | c8 · ¿respeta el veredicto o diagnostica? | ⬜ pendiente |
| | t9 · encadenar sin que salga nada | ⬜ **no medible** |

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

## Lo que este spike NO responde

Conviene no fingir que sí.

**t3 y t4 no son concluyentes.** Corrieron en un 13 Pro sin Apple Intelligence, que es
justamente la pieza que enrutaría voz → índice semántico. Que fallen ahí no dice qué pasa
en un 15 Pro o 16+. Son diez minutos con un teléfono prestado.

Lo que sí queda firme es que la mitad de indexado funciona **sin** Apple Intelligence: t5
pasó igual.

**t9 quedó sin medir.** El 13 Pro no tiene Apple Intelligence, y el simulador no deja
activarlo: en iOS 26.2 y en 26.5 el panel de *Apple Intelligence & Siri* no tiene
interruptor, solo ajustes de Siri, aunque la pantalla principal muestre el banner *«Ready
for Apple Intelligence»* y el Mac lo tenga encendido. Sin eso, `SystemLanguageModel` nunca
queda disponible y Atajos deshabilita `Use Model` con *«This action is not currently
available»*.

Queda abierto si la cadena contesta **sin red**, que es la tesis de privacidad del producto.

**Seis pruebas del marcador siguen sin correr** (p3, t8b, c6, c7, c8 y el t9 bloqueado). Las de
exámenes en la cadena son las que más importan: **c8 le pregunta al modelo «¿tengo algo de qué
preocuparme?»** con el antígeno en 5,2 y sangre oculta positiva. Es la pregunta que más lo invita
a diagnosticar, y `Examenes.encuadre` le dice explícitamente que el veredicto ya viene hecho y no
es suyo. Si el encuadre aguanta ahí, aguanta.

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
- **Correr el Host en el aparato pide tres cosas** que no están en ningún README: el workload de
  iOS más nuevo exige Xcode 26.5 exacto (se salta con `-p:ValidateXcodeVersion=false`), hace falta
  un perfil de aprovisionamiento para `com.ncubo.gnothi.host`, y el credential provider de Azure
  Artifacts es sólo x64 — revienta bajo un SDK arm64.
