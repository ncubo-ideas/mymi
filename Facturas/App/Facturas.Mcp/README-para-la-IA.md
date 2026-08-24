# APP1 · Facturas — la superficie que ve una IA

Este documento es para quien conecte una IA a esta app, y para la IA misma. Las mismas
ideas viajan por el protocolo en el campo `instructions` del `initialize`, así que una IA
que se conecte no necesita leer este archivo para funcionar.

## Cómo la IA descubre los servicios

Esta era la parte ambigua del enunciado, y la respuesta es que **con MCP no hay que
enseñarle nada a la IA**. El descubrimiento es del protocolo:

1. El servidor se registra en `.mcp.json` con el comando del ejecutable y la carpeta.
2. Al conectarse, el cliente manda `initialize`. El servidor responde con su
   `serverInfo` y con las `instructions` — el *workflow*, no el catálogo.
3. Después el cliente manda `tools/list`. El servidor responde, por tool: `name`,
   `description`, `inputSchema` y `outputSchema`, todos **generados desde la firma del
   método en C#** y de los `[Description]`.
4. La IA ve esa lista **antes** del primer prompt de la persona.

Es distinto de la experiencia con el CLI de Puppeteer, donde había que explicarle a la IA
cómo invocar el ejecutable para averiguar qué servicios existían. Acá no hay `--help` que
documentar.

Medido, con un cliente stdio de prueba:

```
initialize  → serverInfo + instructions
tools/list  → total_de_facturas, con inputSchema y outputSchema
tools/call  → structuredContent: {"hayDatos":true,"total":53393.0,
                                  "cantidadDeFacturas":4,"ultimoEventoLeido":104, ...}
```

## La superficie: una sola tool

`total_de_facturas` — cuánto gastó la persona en total, cuántas facturas son, y hasta qué
evento del proveedor leyó su app.

Es **intencional**: la escribe el autor de la app, igual que un `[HttpGet]`, y la app
decide qué expone. Deliberadamente **no** existe una tool genérica
`Query(puppet, operation, parameters)`: eso le daría a la IA una consola de DSL en vez de
un dominio, tiraría a la basura el descubrimiento por schema, y expondría los puppets —
desde afuera nadie tiene por qué saber si el total salió de uno o de cinco. La unidad
descubrible es la aplicación.

Agregar otra tool es un atributo y un método. La restricción no es técnica, es de diseño:
cada tool es una pregunta que la app decide contestar.

## Solo lectura, y por qué importa

El servidor **no escribe** en el journal. Dos disciplinas:

1. Nunca un `PerformCommand`; todas las consultas son `PerformQuery`.
2. **Nunca la semilla.** La app emite `upgrade('semilla')` en `OnHydrated`, y aunque el
   upgrade sea idempotente, el `PerformCommand` que lo envuelve journaliza una entrada en
   cada arranque — medido. Si este proceso reusara el host de la app, cada pregunta de la
   IA escribiría en el journal de la persona y rompería la invariante de un solo escritor.
   De ahí el host `SoloLectura`: mismo actor, mismo dominio, `OnHydrated` vacío.

Verificado: tres corridas del cliente (nueve llamadas JSON-RPC, tres de ellas
rehidratando el actor) dejaron el árbol del journal **byte por byte idéntico**, mismo md5.

## Lo que la IA no puede hacer

**No puede disparar un Refresh.** Traer facturas nuevas del proveedor es de la app, que lo
hace al abrir o cuando la persona aprieta el botón. Si la persona espera ver algo nuevo y
el total no cambió, la IA tiene que pedirle que abra su app y refresque. El campo
`ultimoEventoLeido` es la señal: si subió, entraron cosas nuevas.

Eso no es una limitación a resolver. La app es de la persona y escribe su propio registro;
un segundo escritor sobre el mismo journal rompería la invariante. Si alguna vez la IA
tuviera que provocar un cambio, la escritura debe pasar por la app.

## Por qué hace falta un proceso local

Una página en el browser **no puede hospedar un servidor MCP**: no puede escuchar
conexiones entrantes. Pero este proceso no necesita hablar con el browser. Los dos
comparten la carpeta del journal, y **el journal ya es el protocolo** — no hubo que
inventar ninguno.

## Cómo probarlo sin una IA

```bash
dotnet build src/Facturas.Mcp/Facturas.Mcp.csproj
```

Y después un cliente stdio que mande `initialize`, `notifications/initialized`,
`tools/list` y `tools/call`. Importa **no cerrar stdin** hasta haber leído cada respuesta:
si se le pipea todo de golpe, stdin llega a EOF, el host se apaga y las respuestas se
pierden sin vaciar el buffer. Pasó en la primera prueba.

## Una landmine que ya está resuelta

Con transporte stdio, **stdout es el canal JSON-RPC**. El replay de Puppeteer imprime su
progreso con `Console.Write` (`"Starting ...'s Actor"`, `"1%2%3%..."`). En una consola eso
es ruido; acá corrompería el protocolo. `Program.cs` desvía `Console.Out` a stderr antes de
tocar el motor, y todo el logging va a stderr también. El transporte escribe al stdout real
y no se ve afectado — verificado: el progreso del replay aparece en stderr y el stdout
queda con puro JSON.
