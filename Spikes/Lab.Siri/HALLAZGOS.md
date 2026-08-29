# Lab.Siri — hallazgos

Spike de App Intents en iOS. El gemelo de lo que se midió en Android con App Actions.

Corrido el **2026-08-28**.

La pregunta era una sola: **¿puede un asistente del sistema llegar a un dato nuestro?**
Y en tres alturas — nombrando la app (el piso), sin nombrarla (el techo), y dejando que la
persona arme la cadena ella misma.

---

## Dónde se corrió

| | |
|---|---|
| Teléfono | iPhone 13 Pro, iOS 26.6, **sin Apple Intelligence** (arranca en 15 Pro) |
| Simuladores | iPhone 17 Pro / iOS 26.2 y iPhone 17 Pro / iOS 26.5 |
| Mac | arm64, macOS 26.5.2, Xcode 26.6, Apple Intelligence **activo** |
| Datos | los cinco montos hardcodeados de `Facturas.swift`, los mismos del spike de Android |

---

## Marcador: 8 de 9

| | prueba | resultado |
|---|---|---|
| **PISO** | t1 · Siri contesta en voz alta | ✅ |
| | t2 · lo mismo escrito | ✅ |
| **TECHO** | t3 · sin nombrar la app | ❌ cayó en búsqueda web |
| | t4 · por comercio | ❌ cayó en Apple Cash |
| | t5 · Spotlight por comercio | ✅ |
| | t6 · Spotlight por concepto | ❌ **concluyente** |
| **CADENA** | t7 · la pieza existe en Atajos | ✅ |
| | t8 · encadenar hacia Claude | ✅ |
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
