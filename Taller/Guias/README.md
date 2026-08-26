# Guías para construir una app clientecéntrica

Un conjunto de capítulos, no un documento. Cada uno se puede leer solo.

---

## El orden de trabajo

Éste es el que manda. Es el orden en que pasan las cosas, y **nadie se salta el primer paso** — el
que sabe programar, menos que nadie.

| | Paso | Qué produce | Capítulo |
|---|---|---|---|
| **1** | **Punto de partida** — llegás con una descripción funcional en prosa y salís con las preguntas | **`Preguntas.md`** | [Punto de partida](punto-de-partida.md) |
| **2** | **Escribir bien ese archivo** — la capacidad, los juicios, lo que se descarta | `Preguntas.md` terminado | [4](04-requerimientos-que-agregan-valor.md) |
| **3** | **Preparar el escritorio** | una máquina que compila | [1](#) *pendiente* |
| **4** | **Hello World** — algo que tocar y romper antes de lo propio | entendimiento | [2](#) *pendiente* |
| **5** | **Construir** — dominio, hosting, carátulas | la app | [3](03-construir-la-app.md) |
| **6** | **Traer los datos del proveedor** | URL2 alimentando el journal | [5](#) *pendiente* |
| **7** | **Exponerla al CLI y a la IA** | la superficie | [6](06-cli-y-acceso-de-la-ia.md) |
| **8** | **Auditar** contra `Preguntas.md` | el reporte de deriva | [Observables](observables.md) |

Aparte del orden, y se lee cuando haga falta:

| Capítulo | Qué es |
|---|---|
| [0 · La arquitectura clientecéntrica](00-arquitectura-clientecentrica.md) | Dónde corre todo, de quién es el journal, el Host y los módulos. **Contexto, no requisito**: se puede hacer el paso 1 sin haberlo leído |
| A · Compartir información entre apps | **pendiente**, con punto de partida en el cap. 0 §6 |

> **Los números son identificadores, no orden.** Vienen de cuando las guías se escribieron en otra
> secuencia, y ya no coinciden con el orden de trabajo. Renumerarlas es un cambio aparte y limpio;
> mientras tanto, la tabla de arriba es la que hay que seguir.

---

## Qué está verificado y qué no

Importa, porque estas guías se van a leer como si todo tuviera el mismo respaldo, y no es así.

| | |
|---|---|
| **Capítulo 0** | Cruzado contra las infografías del equipo **y contra el repo corriendo** (`Examenes`, `Compras`, `HolaMundo/Facturas`). Lo que es objetivo y no realidad está marcado como tal |
| **Capítulo 3** | Construido y corrido de punta a punta en un ejercicio de presupuesto. Las reglas de §7 salieron de chocar con ellas, y hay pruebas que las fijan |
| **Capítulo 4** | Doctrina, **confirmada** por los tres casos del equipo: los tres contestan una pregunta sobre la serie, ninguno lista datos |
| **Punto de partida** | **Escrito, nunca corrido.** Nadie hizo todavía esa entrevista con una persona de verdad. Es lo primero que hay que probar |
| **Capítulo 6, CLI** | Construido y corrido. 44 pruebas |
| **Capítulo 6, MCP** | **Contradicho por el repo.** Ver abajo |

Cuando un capítulo dice algo que no verificó, lo dice en el lugar donde lo dice. Si encontrás una
afirmación sin esa marca y tampoco está respaldada, es un error del documento.

---

## Correcciones pendientes en capítulos ya escritos

**Capítulo 6 — la tool genérica está mal.** El capítulo describe una superficie MCP con un
`Query(puppet, operacion, parametros)` genérico. El repo hace lo contrario y lo argumenta: *"tres
tools intencionales, una por pregunta — nunca un `Query(puppet, operacion, ...)` genérico, que
convertiría todo en strings y desperdiciaría el schema por tool"*. Hay más: el argumento va como
**enum, no como string**, para que la lista de valores válidos viva en el `inputSchema` (un contrato)
y no en la descripción (prosa que la IA tiene que leer bien). Y las `ServerInstructions` **no son el
catálogo** —eso el cliente lo descubre con `tools/list`— sino el *workflow*.

**Capítulo 4 — le falta el ejemplo trabajado.** Está la doctrina y falta la demostración: una
pregunta, las clases que fuerza, la decisión de diseño que fuerza, y lo que quedó afuera. Y hay que
reescribirlo con **observables**, para que el paso 8 sea posible.

**Capítulo 3 — la carátula HTTP.** Describe una carátula HTTP que la forma clientecéntrica no usa.
La forma real está en el capítulo 0 §7 (*una lógica y tres carátulas*: Wasm, Mcp, Consola).

**Capítulo 3 — falta la convención de estructura del equipo.** Un directorio por negocio, cinco
nodos, nombres en español. Está en el capítulo 0 §7 y debería mandar en el 3.

**Capítulo 3 §7 — una regla nueva del repo.** Un `upgrade` agregado en un segundo `PerformCommand`
**no queda disponible para otro proceso que rehidrate ese mismo journal**. Un `upgrade` nuevo hay que
probarlo **abriendo el journal desde cero desde el MCP**, no sólo con la app que lo sembró.

---

## Lo que bloquea a los capítulos pendientes

**3 y 4 del orden de trabajo (escritorio y Hello World). Desbloqueados.** El repo ya tiene tres
negocios corriendo con sus READMEs de "cómo se corre", `docker-compose` por negocio, `.mcp.json` y
`NuGet.config`. Se pueden escribir mirando. Ojo: los ejemplos **del Taller** son propios y chiquitos;
los negocios del repo se citan, no se adoptan.

**6 (datos del proveedor).** Sigue bloqueado: falta el lado del proveedor y no hay ningún motor SQL
corriendo contra el cual probar una receta.

**8 (auditar). Parcialmente desbloqueado.** Ya existe el instrumento: [la tabla de
observables](observables.md), con cinco chequeos corridos contra los tres negocios. Faltan cuatro por
correr, y uno quedó declarado **no observable** a propósito. Lo que todavía bloquea la auditoría
completa es que **ningún negocio tiene su `Preguntas.md`**: sin la intención declarada, un agente
puede leer el código pero no sabe contra qué compararlo.

**A (compartir entre apps).** El capítulo 0 §6 fija el punto de partida: el cruce lo hace **el
asistente**, sobre journals que la persona autorizó, no un canal entre módulos. Falta el caso en que
un módulo necesita un dato de otro **sin** una IA en el medio.

---

## Preguntas abiertas

Ninguna. Las tres que había quedaron resueltas: la forma de app que sirven las guías (la
clientecéntrica, y sólo esa), el acceso al MVP (los tres negocios están en este repo) y dónde viven
las guías (acá).
