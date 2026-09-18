# El puente: ¿`ModelFoundation : IModel` sale limpio?

**Medido el 2026-09-02, simulador de iOS 26.5.** Reproducible con `./armar.sh`.

La Puerta 2 de la app corre contra un modelo por red. Para que corra contra el modelo del
teléfono —sin clave, sin red, sin que nada salga— hace falta que Foundation Models entre por
`IModel`. Y ahí hay una fricción de forma:

> `IModel.Next()` espera que el modelo **devuelva** las tool calls para que `AgentLoop` las
> ejecute contra el `Dispatcher`. Foundation Models funciona al revés: le entregás objetos
> `Tool` de Swift y **él las llama**.

Si esa fricción no se puede resolver, el puente obliga a **invertir el bucle** —Swift maneja la
sesión y llama de vuelta al C#—, y entonces habría dos bucles distintos en el producto: justo la
deriva que la suite de conformance existe para atrapar.

## Resultado: se resuelve, y sale limpio · 3 de 3

    paso 1  intercepto -> ["ColesterolTotal"]
    paso 2  OK · el senuelo se volvio a llamar = false
            "Tu colesterol total viene subiendo. De 182 a 228 mg/dL en 29 meses,
             y paso el umbral de 200."

**Interceptar sin ejecutar.** Se le pasan tools *señuelo* cuyo `call()` captura los argumentos y
lanza. El modelo dice qué quiere llamar y con qué, y el trabajo no se hace. Eso es exactamente
`Said(text: null, calls: [...])`.

**Reanudar con un resultado ajeno.** El transcript se **arma desde cero** con
`Transcript(entries:)`, metiendo un `.toolOutput` fabricado con los hechos que calculó el C#. El
modelo los consume y redacta, **sin volver a llamar al señuelo**. Eso es el paso siguiente de
`Next()`, con el `Returned` del C# adentro.

### El detalle que casi lo hunde

Tras el aborto, **el `transcript` de la sesión queda con una sola entrada** (`.instructions`): ni
el prompt ni la tool call quedan registrados. Leerlo no sirve.

No hace falta: **el C# ya es dueño de la conversación**. `Exchange`/`Asked`/`Said`/`Returned` se
traducen a entradas de `Transcript` y se arma una sesión nueva en cada paso. Todos los
constructores que eso pide son públicos:

    Transcript(entries:)
    Transcript.Prompt(segments:)
    Transcript.ToolCalls(_:)  ·  Transcript.ToolCall(id:toolName:arguments:)
    Transcript.ToolOutput(id:toolName:segments:)
    Transcript.TextSegment(content:)

Y los tipos calzan casi uno a uno con los de `IModel`:

| `IModel` | `Transcript` |
|---|---|
| `ToolCall(Id, PublicName, Arguments)` | `ToolCall(id, toolName, arguments)` |
| `Returned(CallId, Json, Failed)` | `ToolOutput(id, toolName, segments)` |
| `Asked` / `Said` | `.prompt` / `.response` |

## Qué habilita

`ModelFoundation : IModel` **sin tocar `AgentLoop`**, un solo bucle, sin deriva entre puertas. En
Ajustes sería un proveedor más — y el único **sin URL y sin clave**.

La línea que hay que cambiar es una, y es la misma que hoy rompe con Anthropic
([`Shell.cs:428`](../../Gnothi.MobileShared/Shell.cs)): construye siempre `ModelOpenAiCompatible` sin
mirar qué proveedor eligió la persona.

## Lo que NO resuelve

El puente es **necesario y no suficiente**. Todo lo que se midió del modelo de 3B sigue en pie:
con el catálogo completo elige mal, y el encuadre se le escapa. La tercera corrida de esta misma
sonda terminó con *«¡Es importante que lo mantengas bajo control!»* — consejo que la instrucción
prohibía. Ver [`../Lab.Cruce`](../Lab.Cruce) y `../Lab.Siri/HALLAZGOS.md` §8.

Tampoco se midió acá el cruce C#↔Swift en sí: la sonda es Swift puro. Falta el `@_cdecl` +
`DllImport`, que es mecánica conocida y no tiene incógnita de diseño.
