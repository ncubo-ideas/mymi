# Guías para construir una app clientecéntrica

Un conjunto de capítulos, no un documento. Cada uno se puede leer solo; el orden es el que sigue un
desarrollador que llega nuevo.

---

## Los capítulos

| | Capítulo | Estado |
|---|---|---|
| **0** | [La arquitectura clientecéntrica](00-arquitectura-clientecentrica.md) — el Host, los módulos, URL1, URL2, el journal de la persona, el aislamiento | **aprobado** |
| 1 | Preparar el escritorio — qué bajar, estructura de directorios | **pendiente** — ya no bloqueado |
| 2 | Hello World — algo que el desarrollador pueda tocar y romper | **pendiente** — ya no bloqueado |
| **3** | [Construir la app](03-construir-la-app.md) — dominio, hosting, carátulas, y las reglas del substrato | **escrito y verificado**, con correcciones pendientes |
| **4** | [Requerimientos que agregan valor](04-requerimientos-que-agregan-valor.md) — qué entra en el modelo y qué no | **escrito**, confirmado por los casos del equipo |
| 5 | Traer los datos del proveedor — recetas de MySQL, SQL Server, un API de ejemplo | **pendiente** — sigue bloqueado |
| **6** | [El CLI y el acceso de la IA](06-cli-y-acceso-de-la-ia.md) — qué se genera solo y qué no | **escrito**, con **una corrección de fondo pendiente** |
| A | Compartir información entre apps | **pendiente**, pero ya tiene punto de partida (cap. 0 §6) |

---

## Por dónde empezar

**Si llegás nuevo al proyecto:** 0 → 4 → 3. El 0 te dice dónde corre todo y de quién es el journal;
el 4 te dice cómo decidir qué modelar; el 3 es cómo se escribe.

**Si venís a escribir código ya:** 3, y volvé al 0 cuando te toque decidir dónde va lo que escribiste.

**Si sos una IA operando una de estas apps:** 6 §5 tiene el contrato de la superficie; el `describe`
de la app tiene el resto.

---

## Qué está verificado y qué no

Importa, porque estas guías se van a leer como si todo tuviera el mismo respaldo, y no es así.

| | |
|---|---|
| **Capítulo 0** | Cruzado contra las infografías del equipo **y contra el repo Gnothi corriendo** (`Examenes`, `Compras`, `HolaMundo/Facturas`). Lo que es objetivo y no realidad está marcado como tal |
| **Capítulo 3** | Construido y corrido de punta a punta en un ejercicio de presupuesto. Las reglas de §7 salieron de chocar con ellas, y hay pruebas que las fijan |
| **Capítulo 4** | Doctrina. Ahora **confirmada** por los tres casos del equipo (laboratorio, carro, supermercado): los tres contestan una pregunta sobre la serie, ninguno lista datos |
| **Capítulo 6, CLI** | Construido y corrido. 44 pruebas |
| **Capítulo 6, MCP** | **Contradicho por el repo.** Ver abajo |

Cuando un capítulo dice algo que no verificó, lo dice en el lugar donde lo dice. Si encontrás una
afirmación sin esa marca y tampoco está respaldada, es un error del documento.

---

## Correcciones pendientes en capítulos ya escritos

**Capítulo 6 — la tool genérica está mal.** El capítulo describe una superficie MCP con un
`Query(puppet, operacion, parametros)` genérico. El equipo la rechaza explícitamente, y el repo hace
lo contrario: *"tres tools intencionales, una por pregunta — nunca un `Query(puppet, operacion, ...)`
genérico, que convertiría todo en strings y desperdiciaría el schema por tool"*. Hay más: el
argumento va como **enum, no como string**, para que la lista de valores válidos viva en el
`inputSchema` (un contrato) y no en la descripción (prosa que la IA tiene que leer bien). Y las
`ServerInstructions` **no son el catálogo** —eso el cliente lo descubre con `tools/list`— sino el
*workflow*: lo que la IA no puede deducir de la firma de un método.

**Capítulo 3 — la carátula HTTP.** El capítulo describe una carátula HTTP que la forma
clientecéntrica no usa. La forma real está en el capítulo 0 §7 (*una lógica y tres carátulas*: Wasm,
Mcp, Consola). Falta trasladarlo.

**Capítulo 3 — falta la convención de estructura del equipo.** Un directorio por negocio, cinco nodos
adentro, nombres en español. Está en el capítulo 0 §7 y debería mandar en el 3.

**Capítulo 3 §7 — una regla nueva del repo.** Un `upgrade` agregado en un segundo `PerformCommand`
**no queda disponible para otro proceso que rehidrate ese mismo journal**. Verificado por el equipo
en un journal real. Consecuencia: un `upgrade` nuevo hay que probarlo **abriendo el journal desde
cero desde el MCP**, no sólo con la app que lo sembró.

---

## Lo que bloquea a los capítulos pendientes

**1 y 2 (escritorio y Hello World). Desbloqueados.** El repo ya tiene tres negocios de ejemplo
corriendo —`HolaMundo/Facturas`, `Examenes`, `Compras`— con READMEs de "cómo se corre",
`docker-compose` por negocio, `.mcp.json` en la raíz y `NuGet.config`. Se pueden escribir mirando.
Ojo: los ejemplos **del Taller** son propios y chiquitos; los negocios del repo se citan, no se
adoptan.

**5 (datos del proveedor).** Sigue bloqueado: falta saber cómo es el lado del proveedor y no hay
ningún motor SQL corriendo contra el cual probar una receta.

**A (compartir entre apps).** Ya no arranca de cero. El capítulo 0 §6 fija el punto de partida: el
cruce lo hace **el asistente**, sobre journals que la persona autorizó, no un canal entre módulos —
los módulos están aislados por diseño y *"no hay comunicación entre módulos porque no hay dos
aplicaciones"*. Lo que falta es el caso en que un módulo necesita un dato de otro **sin** una IA en
el medio.

---

## Preguntas abiertas

Ninguna. Las tres que había quedaron resueltas:

1. ~~**¿Qué forma de app sirven las guías?**~~ La topología de servidor multi-puppet con cola Kafka no
   pertenece a este proyecto. Las guías sirven la forma clientecéntrica, y sólo esa.
2. ~~**¿Hay acceso al MVP?**~~ Sí: los tres negocios están en este mismo repo, en `master`.
3. ~~**¿Dónde viven estas guías?**~~ Acá, en `Taller/Guias/`.

Y la decisión de arquitectura sobre cómo alcanza la IA al servidor MCP **también está resuelta**: la
infografía *Frontera del Modelo* la contesta. Escritorio por MCPB sobre stdio (medido), móvil por la
*Puerta 2* (capa adaptable), híbrido por TLS con el costo conocido de recrear el intermediario. Está
en el capítulo 0 §4.
