# Capítulo 6 — El CLI y el acceso de la IA

Cómo se le da a una máquina una superficie sobre la app, cuánto de eso se puede generar solo, y qué
parte no se va a poder generar nunca.

> **Los dos están verificados.** El CLI sale de construir uno completo, con 44 pruebas. El MCP sale
> de construir un servidor y hablarle por stdio como lo hace un cliente de verdad — y esa corrida
> **desmintió la versión anterior de §6**, que proponía una tool genérica. Lo que quedó sin verificar
> se dice en §8.

---

## 1. Tres consumidores, un contrato

Sobre los mismos performs hay tres clientes distintos, y quieren cosas distintas:

| Quién | Qué quiere | Por dónde |
|---|---|---|
| Una persona | prosa, ejemplos, que le digan qué hacer | `app help` |
| Una IA operando la herramienta | el contrato completo, parseable, de una sola llamada | `app describe` |
| Una IA dentro de una conversación | una lista de tools que puede invocar | MCP |

El error a evitar es escribir tres cosas. **Es una declaración y tres renderizados.** Si el `help` y
el `describe` se escriben aparte, se separan en la segunda semana: alguien agrega un parámetro y
actualiza uno solo.

---

## 2. Qué se puede generar y qué no

Ésta es la respuesta honesta a *"que los CLI se generen automáticamente"*. Construir uno completo deja
la frontera bastante clara:

| | Se genera solo | Por qué |
|---|---|---|
| Parseo de argumentos, posicionales y `--opciones` | **sí** | Es mecánico dada la lista de parámetros |
| Validación de tipos, requeridos, valores legales | **sí** | La declaración ya dice tipo, obligatoriedad y conjunto |
| Rechazo de opciones desconocidas | **sí** | Es el complemento de la lista |
| El `help` en prosa | **sí** | Renderizado de la declaración |
| El `describe` máquina | **sí** | Idem, otro formato |
| Los códigos de salida y la higiene de stdout | **sí** | Es siempre lo mismo (§5) |
| El cuerpo DSL de cada comando | **casi** | Se deduce de la firma de la fachada (§3) |
| **Qué comandos existen** | **no** | Eso son los requerimientos |
| **Qué significa cada parámetro** | **no** | Prosa que alguien tiene que escribir |
| **Qué default tiene y por qué** | **no** | Es una decisión de diseño |
| **Las notas y advertencias** | **no** | Es lo que aprendiste construyéndolo |

La línea es nítida: **la mecánica se genera; el significado no.** Un generador que invente el resumen
de un comando produce documentación que miente, que es peor que no tenerla.

La meta realista no es *"escribí los requerimientos y salió el CLI"*. Es: **el desarrollador declara
la superficie y no escribe ni una línea de parseo, ayuda, validación ni formato.** Eso ya es la mayor
parte del trabajo, y es lo que hace que agregar un comando cueste minutos.

---

## 3. La fachada ya es media superficie

El capítulo 3 §3 pide que la fachada del dominio sea **primitiva de entrada** y **escalar o proyección
de salida**. Esa exigencia viene del substrato, pero tiene un regalo: **la firma de la fachada es
casi la declaración del comando.**

```csharp
internal int RecordIncome(string source, string description, string currency, decimal amount, DateTime on);
```

De ahí sale, por reflexión, sin que nadie lo escriba: el nombre, cinco parámetros, sus tipos, el orden
posicional, y que devuelve un entero.

Falta lo que la firma no puede decir: cuál es obligatorio y cuál tiene default, qué valores son
legales, qué significa cada uno, y ejemplos. **Ése es el archivo que el desarrollador escribe**, y es
corto.

Un generador razonable, entonces: recorre la fachada, emite el esqueleto de la declaración con los
parámetros ya puestos, y deja los huecos de prosa marcados para llenar. No inventa nada.

---

## 4. Los valores legales se leen del modelo vivo

Esto sí cambió la calidad de la herramienta, y es barato.

Las listas cerradas —categorías, fuentes, formas de pago— **no se escriben en el CLI**. Se le
preguntan al modelo al arrancar, y el contrato las reporta:

```
parameters:
  - command: "expense"
    name: "category"
    required: true
    choices: ["Housing", "Food", "Vehicle", …, "Apicultura"]
```

Esa última entrada es una categoría que la persona inventó. Aparece porque el contrato la leyó del
modelo, no de una lista escrita a mano.

Para una IA la diferencia es enorme: acierta a la primera en vez de descubrir la herramienta a fuerza
de errores. Para el desarrollador también: una categoría nueva no requiere tocar el CLI.

---

## 5. La higiene que no es opcional

Tres cosas que si no están, la herramienta no sirve para una máquina. Las tres se generan.

**Sin argumentos, el programa ramifica por audiencia.** No imprime ayuda: dice a quién le habla.

```
Si sos Claude, otro LLM o cualquier agente operando esta herramienta:
  -> corré `app describe` primero.
Si sos una persona:
  -> corré `app help`.
```

Es la convención del CLI del propio framework, y es un compromiso estructural: la herramienta es
AI-first, no AI-tolerada.

**stdout lleva sólo la respuesta.** Todo lo demás —el ruido del framework, avisos, fallas— va a
stderr. Esto no es cosmético: el framework escribe en stdout por su cuenta, incluidas marcas de
progreso **sin salto de línea** que se pegan al principio de la línea siguiente. Si no lo capturás y
lo redirigís, la salida deja de ser parseable. (regla 5 de [las reglas del substrato](reglas-del-substrato.md))

**Los códigos de salida distinguen de quién es la culpa:**

| | |
|---|---|
| 0 | hecho |
| 1 | la línea de comandos estaba mal — nunca llegó al modelo |
| 2 | el modelo lo rechazó — no se escribió nada |
| 3 | el journal no se pudo abrir |

Separar 1 de 2 importa: le dice a quien llama si tiene que corregir la invocación o aceptar una
respuesta del negocio.

**Forma plana para lo que va a parsear una máquina.** Dos colecciones —`commands` y `parameters`—
unidas por el nombre del comando, en vez de parámetros anidados adentro de cada comando. Sobrevive a
cualquier formatter y a cualquier parser.

---

## 6. CLI y MCP

Verificado construyendo los dos y hablándole al MCP por stdio como lo hace un cliente de verdad.

### 6.1 La tool genérica es la trampa, y este capítulo la tenía adentro

Una versión anterior de esta guía proponía la superficie MCP como **una sola tool genérica**:

```csharp
// NO. Se explica abajo por qué.
[McpServerTool]
public string Query(string puppet, string operation, string parameters)
    => Puppeteer.Find(puppet).PerformQuery(operation, parameters);
```

Es la forma que primero se le ocurre a cualquiera, porque parece la más económica: una tool y ya
tenés todo el dominio expuesto. **Y es exactamente al revés.** Tres razones, y ninguna es de estilo:

- **Convierte todo en strings.** `operation` y `parameters` son texto libre. Se pierde el
  `inputSchema`, que es lo único que impide que la IA mande algo que no existe.
- **Tira a la basura el descubrimiento.** Un cliente MCP lee los schemas para saber qué puede pedir.
  Con una tool genérica lee **una** firma que no le dice nada, y la semántica tiene que ir en la
  prosa de la descripción — que la IA tiene que leer bien, entender bien y copiar bien.
- **Expone los puppets.** Le da a la IA una **consola de DSL** en vez de un dominio. La unidad
  descubrible pasa a ser el motor, cuando debería ser **la aplicación**.

Las tools se escriben a mano, igual que un `[HttpGet]`, y **la app decide qué expone**.

### 6.2 La prueba que una tool tiene que pasar

> **Si la IA puede contestar algo que ya está en una pantalla, no probaste nada.**

Un total, un saldo, el último valor, una lista de movimientos: la app los muestra y el proveedor ya
los mostraba. Una tool que los repite no agrega una capacidad, agrega un camino.

Hay una señal que lo delata sola: **cuando la descripción de una tool necesita aclarar que "no es la
lista que la app ya muestra", ya se contestó.** En el negocio de exámenes eso pasó de verdad — una
tool recorría los analitos y devolvía la tendencia de cada uno, que es literalmente lo que la
pantalla renderiza. La reemplazó otra que contesta **una ausencia**: cuánto hace que no se mide cada
cosa, y si ya es demasiado. Eso una pantalla no lo puede mostrar, porque **una pantalla renderiza
datos que tiene y una ausencia no es un dato**.

Aplicado al sujeto de prueba: de sus cinco preguntas, **dos quedaron afuera a propósito** —el total
del mes y el rendimiento de un bien, que son dos pantallas— y las tres que quedaron contestan una
**ausencia**, una pregunta **contrafáctica** cuyo dato la persona trae en el momento de hablar, y una
**tasa sobre eventos** que alguien agrupó a mano.

### 6.3 Sin strings en la frontera semántica

**El argumento va como `enum`, no como `string`.** Con un string, la lista de valores válidos vive en
la **descripción** —prosa— y hay que validarla a mano. Con un enum vive en el **`inputSchema`**, que
es un contrato: el cliente no puede mandar un valor que no exista, y la rama de "vino algo raro" deja
de escribirse porque se volvió inalcanzable.

**El retorno tiene forma, no es un string.** Con `UseStructuredContent`, el SDK genera el
`outputSchema` desde el tipo y la IA recibe `structuredContent` con decimales de verdad. Sin eso, el
resultado viaja como un bloque de **texto** que la IA tiene que volver a parsear.

**Y el `[Description]` no es documentación: es la semántica que lee la IA.** Es lo único que tiene
para decidir si esa tool responde la pregunta de la persona, así que se redacta como **la pregunta
que la persona se hace**, no como el nombre del método. Los que dicen *"no inventes un promedio"* o
*"vacío significa que no hay nada que hacer"* están ahí porque son las formas conocidas de que una
respuesta correcta se convierta en un consejo equivocado.

### 6.4 Las `ServerInstructions` son el workflow, no el catálogo

El catálogo lo descubre el cliente solo, con `tools/list` y los schemas. Repetirlo ahí es ruido.

Lo que va en las instructions es **lo que la IA no puede deducir de la firma de un método**: de dónde
salen los datos, qué significa que falten, cómo se actualizan, qué **no** debe decir. Y lo que no
puede hacer — si la app trae datos nuevos sólo cuando la persona la abre, la IA tiene que saber que
no puede disparar eso y que le toca pedirlo.

### 6.5 Tres cosas que sólo aparecen al correrlo

**El `stdout` es el canal.** Con transporte stdio, cualquier cosa que el proceso escriba en `stdout`
se mete entre los mensajes JSON-RPC y el cliente no puede parsear. El motor imprime su progreso con
`Console.Write`, así que hay que desviar `stdout` a `stderr` **antes** de tocarlo, y mandar todo el
logging a `stderr` también. Ver la [regla 5 del substrato](reglas-del-substrato.md).

**Las fechas del `print` no parsean.** Salen en `MM/dd/yyyy` y `System.Text.Json` espera ISO 8601, así
que deserializar la salida de una consulta revienta con un error que **no menciona el formato** y
manda a revisar el DTO. Hace falta un conversor, y conviene que además **escriba ISO de salida**: lo
que tu superficie entrega no tiene por qué heredar la ambigüedad de lo que recibe.

**Y una del arnés, no del código:** al probar un servidor MCP con un pipe, **si stdin se cierra el
host se apaga y pierde lo que quedaba en el buffer de stdout**. Un servidor perfectamente sano parece
muerto. Hay que sostener stdin abierto después de mandar las peticiones.

### 6.6 El MCP es de sólo lectura, y son dos disciplinas

Nunca un `PerformCommand` — eso es lo obvio. Lo que no es obvio:

> **Nunca la semilla.** El `upgrade` es idempotente, pero el `PerformCommand` que lo envuelve
> **journaliza una entrada en cada arranque**. Si el servidor MCP usara la misma clase que la app,
> cada pregunta de la IA escribiría en el journal de la persona y rompería la invariante de un solo
> escritor.

La solución es un actor gemelo: **mismo nombre, mismo dominio, `OnHydrated` vacío**.

Y se rehidrata en **cada consulta**, no una vez. La app instalada le escribe a esa carpeta mientras
el proceso MCP vive, así que un actor cacheado contestaría con el estado de la primera pregunta: la
persona registraría algo y la IA seguiría viendo lo viejo.

### 6.7 Cuándo cada uno

| | CLI | MCP |
|---|---|---|
| Cuándo | desarrollo, scripts, la IA operando la máquina | la IA dentro de una conversación con la persona |
| Dónde corre | donde esté el journal | proceso local **junto al** journal |
| Cómo se entera | argumentos | rehidrata el journal en cada consulta |
| Qué expone | todo lo que la fachada tenga | **sólo lo que ninguna pantalla contesta** |

Un servidor MCP no le habla al browser ni el browser le habla a él: **los dos comparten la carpeta, y
el journal ya es el protocolo.** No hay que inventar uno.

---

## 7. Antes de dar la superficie por terminada

**El CLI**

- [ ] ¿Hay **una sola** declaración, y `help` y `describe` se renderizan de ella?
- [ ] ¿Los valores legales se leen del modelo vivo, no de una lista escrita a mano?
- [ ] Sin argumentos, ¿ramifica por audiencia en vez de imprimir ayuda?
- [ ] ¿stdout lleva sólo la respuesta, con el ruido del framework mandado a stderr?
- [ ] ¿Los códigos de salida distinguen "la invocación estaba mal" de "el modelo dijo que no"?
- [ ] ¿El contrato máquina es **plano**, unido por nombre?
- [ ] ¿Cada comando que escribe está detrás de un `Check`? ([regla 1 del substrato](reglas-del-substrato.md))
- [ ] ¿Una opción desconocida se rechaza nombrándola, en vez de ignorarse?

**El MCP**

- [ ] ¿**No** hay una tool genérica `Query(...)`? (§6.1)
- [ ] ¿**Cada** tool contesta algo que ninguna pantalla muestra? Y si una descripción necesita
      aclarar que "no es lo que la app ya muestra", ¿se borró esa tool? (§6.2)
- [ ] ¿Los argumentos de conjunto cerrado son `enum` y no `string`? (§6.3)
- [ ] ¿Los retornos tienen forma, con `UseStructuredContent`?
- [ ] ¿Los `[Description]` están escritos como **la pregunta de la persona**, y dicen qué **no**
      concluir cuando un campo viene vacío o en false?
- [ ] ¿Las `ServerInstructions` traen el **workflow** y no el catálogo? (§6.4)
- [ ] ¿`stdout` se desvía **antes** de tocar el motor, y todo el logging va a `stderr`? (§6.5)
- [ ] ¿Las fechas que vienen de un `print` pasan por un conversor? (§6.5)
- [ ] ¿El actor de sólo lectura tiene `OnHydrated` **vacío**, sin la semilla? (§6.6)
- [ ] ¿Se rehidrata en cada consulta, y no una sola vez al arrancar?

---

## 8. Lo que este capítulo no cubre

- **El generador en sí.** §2 y §3 dicen qué es generable y de dónde sale; **nadie lo ha escrito**. Y
  §6 le pone un límite que conviene tener presente: **qué tools existen no es generable**, porque
  depende de qué contesta una pantalla y qué no. Eso lo decide una persona.
- **Un MCP consumido por un cliente real.** El servidor se verificó con peticiones JSON-RPC por
  stdio; **no** se lo enchufó todavía a Claude Desktop ni a otro cliente, así que cómo se comporta
  una IA de verdad frente a estas descripciones sigue sin medirse.
- **MCP en móvil.** Ver capítulo 0 §4: en escritorio va por MCPB sobre stdio y está medido; en móvil
  es la *Puerta 2*, que es objetivo y no realidad.
- **Autenticación de un MCP sobre HTTP.** Mientras es un proceso local está protegido por serlo. Si
  deja de serlo, eso hay que resolverlo.
- **Settings de qué ve la IA.** Un contrato que reporta todas las operaciones y una persona que quiere
  ocultar algunas son dos requisitos que todavía no se cruzaron.
