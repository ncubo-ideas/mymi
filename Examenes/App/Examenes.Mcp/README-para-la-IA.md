# Exámenes — la superficie que ve una IA

Este documento es para quien conecte una IA a esta app, y para la IA misma. Las mismas
ideas viajan por el protocolo en el campo `instructions` del `initialize`, así que una IA
que se conecte no necesita leer este archivo para funcionar.

## Cómo la IA descubre los servicios

Con MCP **no hay que enseñarle nada a la IA**. El descubrimiento es del protocolo:

1. El servidor se registra en `.mcp.json` con el comando del ejecutable y la carpeta.
2. Al conectarse, el cliente manda `initialize`. El servidor responde con su `serverInfo`
   y con las `instructions` — el *workflow*, no el catálogo.
3. Después el cliente manda `tools/list`. El servidor responde, por tool: `name`,
   `description`, `inputSchema` y `outputSchema`, todos **generados desde la firma del
   método en C#** y de los `[Description]`.
4. La IA ve esa lista **antes** del primer prompt de la persona.

Medido, con un cliente stdio de prueba:

```
initialize  → serverInfo {name: examenes-de-laboratorio} + instructions
tools/list  → evaluar_tendencia (inputSchema con `analito`, requerido), outputSchema ✓
               revisar_mis_analitos (sin argumentos),                    outputSchema ✓
tools/call  → structuredContent: {"hayDatos":true,"analito":"Antigeno prostatico",
                                  "tendencia":"en ascenso","mediciones":4,
                                  "valorInicial":3.0,"valorFinal":5.2,"unidad":"ng/mL",
                                  "diasEntreLaPrimeraYLaUltima":1096,"cambio":2.2,
                                  "exigeAtencion":true,"sugiereRepetirElExamen":false,…}
```

Los decimales llegan **como decimales** (`3.0`, `5.2`, `2.2`), no como texto: eso es
`UseStructuredContent` más un tipo de retorno con forma.

## La superficie: cuatro tools, y por qué pasan la prueba

> La prueba, textual: **si la IA puede responder algo que ya está en una pantalla, el demo
> no probó nada.**

Un total, un conteo o el último valor de un analito **no califican** — la app los muestra.
Por eso acá no hay una tool `resumen_de_examenes`.

| Tool | Qué contesta | Por qué ninguna pantalla lo trae |
|---|---|---|
| `evaluar_tendencia(analito)` | si ese analito viene subiendo, bajando o está estable | una hoja de laboratorio trae **un valor y una fecha**; esto es una conclusión sobre la serie |
| `velocidad_de(analito)` | a qué **ritmo** viene cambiando, sobre el tramo más reciente | la tendencia mira la serie entera y la velocidad mira el último tramo: con los datos del guion la serie completa da 0,73/año y no alarmaría, el último tramo da 2,20 contra un umbral de 0,75 |
| `presencia_de(analito)` | si un hallazgo binario apareció, **persiste** o no se confirmó | la hoja de aquel día dice «positivo» para siempre; que la prueba siguiente haya salido limpia sólo lo sabe la serie |
| `que_me_falta_medir()` | qué analitos están vencidos y cada cuánto conviene repetirlos | una pantalla sólo renderiza datos que tiene; esto es una **ausencia** — el expediente más el calendario más la periodicidad que cada analito define para sí |

Las dos primeras toman el enum `Analito` y la tercera toma `AnalitoBinario`, que es otro
enum. Son dos y no uno a propósito: las preguntas no son las mismas. Un nitrito no viene
subiendo ni tiene velocidad — aparece, persiste o cede — y con un enum único la IA podría
pedir `velocidad_de(SangreEnOrina)` y recibir una respuesta vacía que tendría que
interpretar. Separados, el schema lo impide.

La pregunta que valida el dominio es del tipo *"¿mi antígeno prostático viene subiendo o
está estable?"*. Exige la serie completa **y** una regla de interpretación que vive en el
dominio, no en los datos ni en la UI.

Son **intencionales**: las escribe el autor de la app, igual que un `[HttpGet]`.
Deliberadamente **no** existe una tool genérica `Query(puppet, operation, parameters)`: eso
le daría a la IA una consola de DSL en vez de un dominio, tiraría a la basura el
descubrimiento por schema, y expondría los puppets. La unidad descubrible es la
aplicación.

## Lo que la IA tiene que entender del dominio

Está en las `instructions`, y es lo que no se deduce de una firma:

- **Un mismo cambio significa cosas distintas según el tiempo.** Colesterol de 200 a 280
  en un año es un cambio real en la vida de la persona; el mismo salto en un día es un
  error de medición. El dominio ya hizo esa distinción: si `sugiereRepetirElExamen` viene
  en `true`, lo que corresponde es **sugerir repetir el examen, no alarmar**.
- **`valorInicial` / `valorFinal` cambian de sentido en un caso.** Cuando la tendencia es
  `salto inverosimil`, son los dos extremos **del salto sospechoso**, no de toda la serie:
  es el hecho accionable.
- **Decir los hechos, no consejos de plantilla.** *"Tu antígeno pasó de 3.0 a 5.2 ng/mL en
  tres años"*, no *"bajá las grasas"*. La IA no tiene de dónde sacar lo segundo, y es a
  propósito: el dominio devuelve hechos.
- **No diagnosticar.** Si algo exige atención, decirlo y recomendar que lo vea su médico.

## Solo lectura, y por qué importa

El servidor **no escribe** en el journal. Dos disciplinas:

1. Nunca un `PerformCommand`; todas las consultas son `PerformQuery`.
2. **Nunca la semilla.** `ExamenesApp` emite `upgrade('semilla')` en `OnHydrated`, y aunque
   el upgrade sea idempotente, el `PerformCommand` que lo envuelve journaliza una entrada
   en cada arranque. Si este proceso reusara el host de la app, cada pregunta de la IA
   escribiría en el journal de la persona y rompería la invariante de un solo escritor. De
   ahí el host `SoloLectura`: mismo actor, mismo dominio, `OnHydrated` vacío.

Verificado acá: tres corridas del cliente dejaron `journal_000001.bin`, `meta.bin` e
`index.bin` con el **mismo md5**, y sin cambio de `mtime`.

> **Matiz medido, que el README de `Facturas` no menciona.** La **primera** hidratación de
> un journal que todavía no tiene índice de lectura **sí** escribe: crea `index/index.bin`
> y reescribe `meta.bin`. Es una sola vez, es del motor, y **el journal nunca se toca**. De
> la segunda corrida en adelante no se escribe un byte. La invariante de un solo escritor
> se refiere al journal y se sostiene; conviene saberlo igual, porque si la app instalada y
> el proceso MCP corren a la vez sobre una carpeta **nueva**, los dos pueden querer crear
> ese índice.

## Lo que la IA no puede hacer

**No puede disparar un Refresh.** Traer exámenes nuevos del laboratorio es de la app, que
lo hace al abrir o cuando la persona aprieta el botón. Si la persona espera ver algo nuevo
y no aparece, la IA tiene que pedirle que abra su app y refresque. El campo
`ultimoEventoLeido` es la señal: si subió, entraron cosas nuevas.

Eso no es una limitación a resolver. La app es de la persona y escribe su propio registro;
un segundo escritor sobre el mismo journal rompería la invariante.

## Por qué hace falta un proceso local

Una página en el browser **no puede hospedar un servidor MCP**: no puede escuchar
conexiones entrantes. Pero este proceso no necesita hablar con el browser. Los dos
comparten la carpeta del journal, y **el journal ya es el protocolo**.

## Cómo probarlo sin una IA

```bash
dotnet build App/Examenes.Mcp/Examenes.Mcp.csproj
```

Y después un cliente stdio que mande `initialize`, `notifications/initialized`,
`tools/list` y `tools/call`. Importa **no cerrar stdin** hasta haber leído cada respuesta:
si se le pipea todo de golpe, stdin llega a EOF, el host se apaga y las respuestas se
pierden sin vaciar el buffer.

## Una landmine que ya está resuelta

Con transporte stdio, **stdout es el canal JSON-RPC**. El replay de Puppeteer imprime su
progreso con `Console.Write` (`"Starting ...'s Actor"`, `"1%2%3%…"`). En una consola eso es
ruido; acá corrompería el protocolo. `Program.cs` desvía `Console.Out` a stderr antes de
tocar el motor, y todo el logging va a stderr también.
