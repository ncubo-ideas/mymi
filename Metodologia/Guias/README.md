# Guías para construir una app clientecéntrica

Un conjunto de capítulos, no un documento. Cada uno se puede leer solo.

---

## El orden de trabajo

Éste es el que manda. Es el orden en que pasan las cosas, y **nadie se salta el primer paso** — el
que sabe programar, menos que nadie.

| | Paso | Qué produce | Capítulo |
|---|---|---|---|
| **1** | **Punto de partida** — llegás con una descripción funcional en prosa y salís con las preguntas. **Vuelve después del paso 5** con el `HUECOS.md` en la mano: una sola pasada nunca alcanza | **`Preguntas.md`** | [Punto de partida](punto-de-partida.md) · [hoja](hoja-de-la-entrevista.md) |
| **2** | **Escribir bien ese archivo** — la capacidad, los juicios, lo que se descarta | `Preguntas.md` terminado | [4](04-requerimientos-que-agregan-valor.md) |
| **3** | **Preparar el escritorio** — y las tres fallas del feed privado, que se ven todas iguales | una máquina que compila | [1](01-preparar-el-escritorio.md) |
| **4** | **Hello World** — algo que tocar y romper antes de lo propio | entendimiento | [2 · Hola Mundo](../Ejemplos/HolaMundo/README.md) |
| **5** | **Construir** — el dominio, la lógica, y la carátula de **consola**, que es el arnés | `Dominio/` · `App/<N>.App` · `App/<N>.Consola` · **`HUECOS.md`** | [3](03-construir-la-app.md) |
| **6** | **Traer los datos del proveedor** | `EndPointDeEventos/` · `InyectorDeEventosDemo/` | [5](#) *pendiente* |
| **7** | **Exponerla** — las dos carátulas que miran **hacia afuera**: la que instala la persona y la que ve la IA | `App/<N>.Wasm` · `App/<N>.Mcp` | [6](06-cli-y-acceso-de-la-ia.md) · [7 · las puertas](07-las-puertas-de-la-ia.md) |
| **8** | **Auditar** contra `Preguntas.md` | el reporte de deriva | [Observables](observables.md) |
| **9** | **Publicar** — la página de descarga, el compose y el README de cómo se corre | `PaginaDeDescarga/` · `docker-compose.yml` · `README.md` | *pendiente* |
| **10** | **Declarar el módulo** — para que el Host lo catalogue, le conceda y lo verifique **sin arrancarlo** | `manifest.json` | [El manifiesto y la instalación](el-manifiesto-y-la-instalacion.md) |

> **Por qué las carátulas se parten entre el 5 y el 7.** El capítulo 3 las llama a las tres
> *"carátulas"* y podría parecer que van juntas. El corte no es arbitrario: la de **consola** es con la
> que **vos** trabajás —mide el dominio sin el sandbox del browser encima— y por eso se construye
> junto al dominio; la **Wasm** y la **Mcp** son con las que **te usan**, y no tienen sentido hasta que
> hay datos de verdad corriendo por URL2. Es el orden que los negocios del repo ya siguen: *"el WASM
> está bloqueado por el motor, así que se trabaja y se valida por Consola — es para lo que existe esa
> carátula"*.

> **Por qué el capítulo 2 no vive en esta carpeta.** Porque es el único que **es** un código, y un
> capítulo que explica archivos de otra carpeta se desincroniza — el mismo problema que ya medimos
> con los manifiestos. Vive al lado de lo que describe, en
> [`Ejemplos/HolaMundo/`](../Ejemplos/HolaMundo/README.md), donde no puede quedar viejo sin que se
> note.

Aparte del orden, y se lee cuando haga falta:

| Capítulo | Qué es |
|---|---|
| [0 · La arquitectura clientecéntrica](00-arquitectura-clientecentrica.md) | Dónde corre todo, de quién es el journal, el Host y los módulos. **Contexto, no requisito**: se puede hacer el paso 1 sin haberlo leído |
| [Las reglas del substrato](reglas-del-substrato.md) | Las 18 cosas que el motor hace y ninguna documentación dice. **Referencia, no tutorial**: si ya sabés lo que hacés, entrás por acá y no por el paso 5 |
| [Herramientas](../Herramientas/) | Los chequeos que se corren solos **sobre estas guías**. Hoy uno: que ningún enlace ni ancla apunte a la nada |
| El teléfono | **pendiente, y desbloqueado.** `Gnothi.Mobile` corre el núcleo del Host adentro de una app Android, medido en un Galaxy A54 físico. Por qué la cáscara **tiene** que ser en proceso, que no es MAUI, y qué significa que sea "el mismo código" |
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
| **Capítulo 2** | **Construido y corrido.** 12 comprobaciones del dominio puro, y el ciclo con journal dos veces sobre la misma carpeta para ver el replay. Las reglas 15 y 16 del substrato salieron de escribirlo |
| **Capítulo 3** | Construido y corrido de punta a punta en un ejercicio de presupuesto. Las [reglas del substrato](reglas-del-substrato.md) salieron de chocar con ellas, y hay pruebas que las fijan |
| **Capítulo 4** | Doctrina confirmada por los tres casos del equipo. Su **ejemplo trabajado** sigue una pregunta hasta el fondo en un dominio real, y el §6bis dice qué de este capítulo se puede auditar y qué **no** |
| **Punto de partida** | **Corrido tres veces**, y cada corrida lo cambió: presupuesto personal (2026-08-26, seis defectos en el guion), gimnasio con un peer del equipo (2026-08-28, de ahí el bloque 5 y la corrección del descarte del bloque 1), y con una Product Owner de fuera del equipo (2026-09-01, de ahí las correcciones del bloque 3). El detalle está en su §2 |
| **Capítulo 1** | **Escrito preparando una máquina y anotando dónde se rompió.** Versiones, mensajes de error y diagnósticos medidos. Lo que no se pudo verificar —Docker, `wasm-tools`— lo dice |
| **El manifiesto** | **Leído del Host que lo valida** (`Gnothi.Host/Manifest.cs`, rama `master_gnothiHostDesktop`), no propuesto. Pero el Host se diseña **en paralelo**: §8 dice qué sigue abierto |
| **Capítulo 7** | **Leído del código que corre** —`Gnothi.Assistant`, y el Host medido en un Galaxy A54—. La tercera puerta es un **spike de 8 de 9 pruebas** y está marcada como tal; de Android no se afirma nada porque su medición no está en el repo |
| **Capítulo 6, CLI** | Construido y corrido. 44 pruebas |
| **Capítulo 6, MCP** | **Corregido.** Se construyó un servidor y se le habló por stdio; esa corrida desmintió la versión anterior. Falta enchufarlo a un cliente real |

Cuando un capítulo dice algo que no verificó, lo dice en el lugar donde lo dice. Si encontrás una
afirmación sin esa marca y tampoco está respaldada, es un error del documento.

---

## Correcciones pendientes en capítulos ya escritos

Las cinco que había quedaron cerradas: la tool genérica del capítulo 6, las tres del capítulo 3 —la
carátula HTTP, la convención de estructura y la regla del `upgrade`— y el ejemplo trabajado del
capítulo 4.

Y apareció una nueva al mirar el Host:

**Capítulo 6 — sólo conoce la forma `stdio`.** El equipo está construyendo también la forma **cargo**:
dos DLL chicos, sin el SDK de MCP, con `[ModuleSurface]` y `[Query]` de `Gnothi.Contract` y el motor
prestado por el Host. Los tres negocios ya tienen su clase `*Surface`. Quien siga el capítulo 6 hoy
escribe un servidor MCP completo donde puede hacer falta **una clase con dos atributos**. El manifiesto
lo describe en su §6; el capítulo 6 debería tenerlo también.

---

## Lo que bloquea a los capítulos pendientes

**3 (escritorio). Escrito**, y con la parte cara medida: las tres fallas del feed privado que se ven
todas iguales desde el error, y la trampa de que el mismo comando funciona o falla según desde dónde
se corra.

**6 (datos del proveedor).** Sigue bloqueado: falta el lado del proveedor y no hay ningún motor SQL
corriendo contra el cual probar una receta.

**8 (auditar). Parcialmente desbloqueado.** Ya existe el instrumento: [la tabla de
observables](observables.md), con cinco chequeos corridos contra los tres negocios. De los trece,
faltan siete por correr y uno quedó declarado **no observable** a propósito. Lo que todavía bloquea la
auditoría completa es que **sólo `Compras` tiene su `Preguntas.md`** —lo escribió el equipo el
2026-08-27, y encontró tres defectos en un negocio con 461 comprobaciones en verde—. Sin la intención
declarada, un agente puede leer el código pero no sabe contra qué compararlo.

> **El 13 es nuevo y audita algo que ninguno de los doce anteriores audita: completitud.** Los doce
> preguntan si los juicios quedaron donde se dijo; el 13 pregunta si el dominio tiene todo lo que el
> archivo declaró que iba a saber. Corre contra la Parte 1 del `Preguntas.md`, y **necesita que esa
> tabla lleve la columna con el nombre de la clase** — hoy sólo la tiene `Pruebas/Gimnasio`, donde ya
> encontró un renglón sin clase.


**El teléfono. Desbloqueado, y con la parte cara medida.** `Gnothi.Mobile` compila el núcleo del Host
adentro de una app Android —**no es MAUI**— y corrió en un Galaxy A54 físico: 3 módulos, 8 consultas,
0 procesos hijos. El hallazgo que hace falta escribir: en un teléfono la cáscara **tiene** que ser en
proceso, porque una app con `targetSdk >= 29` no puede `exec()` archivos de su directorio de datos.
No es una preferencia de diseño, es el sistema operativo.

**9 (publicar). Sin capítulo, pero con material.** El paso existía en la práctica y no en este orden
de trabajo: se descubrió comparando las fases contra la estructura de los negocios del repo, y
`PaginaDeDescarga/` no tenía dónde caer. El capítulo 0 §9 ya trae la idea —*"dos deploys, y sólo uno
es a un servidor"*— y los README de `Examenes` y `Compras` traen el detalle que cuesta caro
redescubrir: el scope del service worker, el contexto seguro desde un teléfono, el workload de wasm.

**A (compartir entre apps).** El capítulo 0 §6 fija el punto de partida: el cruce lo hace **el
asistente**, sobre journals que la persona autorizó, no un canal entre módulos. Falta el caso en que
un módulo necesita un dato de otro **sin** una IA en el medio.

---

## Preguntas abiertas

Las tres originales quedaron resueltas: la forma de app que sirven las guías (la clientecéntrica, y
sólo esa), el acceso al MVP (los tres negocios están en este repo) y dónde viven las guías (acá).

Queda una nueva, y es de estructura.

**¿Cuál es la estructura de nodos que documentan estas guías?** La recomendación del equipo sobre
`internal` (2026-08-26) viene con un árbol que **no es el que tienen los negocios del repo**:

| En la recomendación | En `Examenes/` y `Compras/` hoy |
|---|---|
| `<N>.Dominio` | `Dominio/` |
| `<N>.Superficie` — endpoints y tools | `App/<N>.App` + `App/<N>.Mcp` |
| `<N>.Cli` — carátula de desarrollo | `App/<N>.Consola` |
| `<N>.Tests` — fixture y journals dorados | **no existe**: el fixture vive dentro de `.Consola` |
| `publicador-simulado` | `InyectorDeEventosDemo/` + `EndPointDeEventos/` |
| — | `App/<N>.Wasm` y `PaginaDeDescarga/`, que ahí no aparecen |

Aparece además `personas/`, un árbol de recursos de desarrollo con un journal por persona y por
institución, que ninguna guía menciona.

El capítulo 0 §7 documenta **la del repo**, porque es la que se puede verificar corriendo. La
recomendación dice de sí misma que *"no es definitiva: la estructura de proyectos puede cambiar
conforme avancemos"*, así que adivinar cuál gana sería inventar doctrina. **Lo que sí es
independiente del milestone —y ya está en el capítulo 3— son las dos razones de diseño por las que el
dominio va `internal`.**
