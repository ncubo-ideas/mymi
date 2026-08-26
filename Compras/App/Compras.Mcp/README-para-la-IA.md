# Compras · Supermercado — la superficie que ve una IA

Este documento es para quien conecte una IA a esta app, y para la IA misma. Las mismas
ideas viajan por el protocolo en el campo `instructions` del `initialize`, así que una IA
que se conecte no necesita leer este archivo para funcionar.

## Cómo la IA descubre los servicios

Con MCP no hay que enseñarle nada a la IA. El descubrimiento es del protocolo:

1. El servidor se registra en `.mcp.json` con el comando del ejecutable y la carpeta.
2. Al conectarse, el cliente manda `initialize`. El servidor responde con su
   `serverInfo` y con las `instructions` — el *workflow*, no el catálogo.
3. Después el cliente manda `tools/list`. El servidor responde, por tool: `name`,
   `description`, `inputSchema` y `outputSchema`, todos **generados desde la firma del
   método en C#** y de los `[Description]`.
4. La IA ve esa lista **antes** del primer prompt de la persona.

## La superficie: cuatro tools

- **`total_de_compras`** — cuánto gastó la persona en total, cuántas compras son, y
  hasta qué evento del supermercado leyó su app.
- **`canasta_del_mes`** — la composición de UN mes: gasto recomendado / neutro /
  ocasional (sobre lo comestible) y lo no comestible aparte.
- **`comparar_canastas`** — cómo CAMBIÓ la canasta entre dos meses. Es la pregunta que
  ningún ticket suelto contesta, y la mitad "compras" del cruce con el laboratorio.
- **`garantia_de_producto`** — cuándo se compró un producto y hasta cuándo tiene
  garantía, con su respaldo completo (compra, fecha, cobertura, vigencia).

Son **intencionales**: las escribe el autor de la app, igual que un `[HttpGet]`, y la
app decide qué expone. Deliberadamente **no** existe una tool genérica
`Query(puppet, operation, parameters)`: eso le daría a la IA una consola de DSL en vez
de un dominio, tiraría a la basura el descubrimiento por schema, y expondría los
puppets. La unidad descubrible es la aplicación.

Agregar otra tool es un atributo y un método. La restricción no es técnica, es de
diseño: cada tool es una pregunta que la app decide contestar.

## Los datos se prestan para el análisis — y ese es el punto

Cada línea de compra lleva su **categoría**, y la categoría lleva su **perfil de
consumo** (recomendado / neutro / ocasional). El dominio devuelve **hechos** — montos y
proporciones — nunca prosa: la canasta de agosto 2025 del guion es 9% ocasional, la de
agosto 2026 es 97%. El cruce con el colesterol del laboratorio lo hace el **asistente**,
que es el único que ve los dos negocios; los módulos no se hablan entre sí.

## Solo lectura, y por qué importa

El servidor **no escribe** en el journal. Dos disciplinas:

1. Nunca un `PerformCommand`; todas las consultas son `PerformQuery`.
2. **Nunca la semilla.** La app emite `upgrade('semilla')` en `OnHydrated`, y aunque el
   upgrade sea idempotente, el `PerformCommand` que lo envuelve journaliza una entrada
   en cada arranque — medido en Examenes. De ahí el host `SoloLectura`: mismo actor,
   mismo dominio, `OnHydrated` vacío.

## Lo que la IA no puede hacer

**No puede disparar un Refresh.** Traer compras nuevas del supermercado es de la app,
que lo hace al abrir o cuando la persona aprieta el botón. El campo `ultimoEventoLeido`
es la señal: si subió, entraron cosas nuevas.

## Una landmine que ya está resuelta

Con transporte stdio, **stdout es el canal JSON-RPC**. El replay de Puppeteer imprime su
progreso con `Console.Write`. `Program.cs` desvía `Console.Out` a stderr antes de tocar
el motor, y todo el logging va a stderr también.
