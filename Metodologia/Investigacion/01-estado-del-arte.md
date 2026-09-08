# ¿Ya está documentado el problema de construir dominios que agregan valor?

**Sí — y con más nombres de los que esperaba.** Pero con un matiz que cambia lo que se puede hacer con
eso: **está documentado en cinco literaturas distintas que casi no se citan entre sí**, y ninguna
resuelve el problema entero. Una nombra el anti-patrón, otra da el patrón del caso exacto del
laboratorio, otra da la teoría formal de cuándo una fila merece su propia clase, otra da el proceso de
taller, y otra da el vocabulario de negocio.

Y hay un hallazgo que conviene poner primero, porque cambia el tono de todo lo demás:

> ## El dominio de `Examenes` ya implementa la estructura que la literatura prescribe
>
> Sin haberla leído. Lo verifiqué contra el código. La correspondencia es casi exacta, y está en la
> **Parte 1**. Eso significa que esto **no es una investigación para descubrir qué hacer** — es una
> investigación para **poder nombrar lo que el equipo ya hace bien**, y así poder enseñarlo,
> auditarlo y defenderlo.

*Investigación hecha el 2026-09-02. Las fuentes consultadas están al final; lo que es síntesis mía y
no bibliografía está marcado como tal.*

---

## Parte 0 · Cómo se llama el problema

El problema —*"no quiero una clase que sea el reflejo del dato que me manda el laboratorio"*— tiene
**cuatro nombres publicados**, y cada uno ilumina una cara distinta.

| Nombre | Quién y cuándo | Qué aporta |
|---|---|---|
| **Anemic Domain Model** | Martin Fowler, 2003 | El nombre del **anti-patrón**: clases con datos y sin comportamiento. Fowler es explícito en que *"es contrario a la idea básica del diseño orientado a objetos, que es combinar datos y proceso"* |
| **Anti-Corruption Layer** | Eric Evans, *DDD*, 2003 | El nombre de **la defensa**. Una capa de traducción entre tu contexto y el sistema externo: traduce el modelo ajeno al tuyo al entrar, y al revés al salir. **Tu dominio nunca ve los conceptos ajenos** |
| **Conformist** | Evans, mismo capítulo | El nombre de **lo que pasa si no la ponés**: el contexto de abajo adopta el modelo de arriba tal cual. Parece lo más simple y produce acoplamiento y conceptos que no pertenecen |
| **Consumer-aligned data product** | Zhamak Dehghani, *Data Mesh*, 2022 | El vocabulario más cercano a *"clientecéntrico"*. Distingue **source-aligned** (1:1 con el sistema operacional de origen) de **consumer-aligned** (optimizado para el uso que extrae valor) |

**Lo que hay que retener de esta parte:** el problema no es nuevo ni exótico. Evans le puso nombre en
2003 y lo llamó *anti-corrupción* —no "adaptador", no "fachada"— justamente porque lo que previene es
que el modelo ajeno **corrompa** el propio.

Y el marco de Data Mesh da la frase exacta para explicarle a alguien de negocio qué se está haciendo:
**la app del laboratorio es un producto de datos alineado al consumidor, no al origen.**

---

## Parte 1 · El caso del laboratorio clínico está resuelto desde 1996

Éste es el hallazgo más fuerte de la investigación.

**Martin Fowler, *Analysis Patterns: Reusable Object Models* (1996), capítulo 3 — "Observations and
Measurements".** Los patrones de ese capítulo **se desarrollaron modelando el proceso de salud del
National Health Service británico**. No es una analogía: es literalmente el dominio de exámenes de
laboratorio.

El modelo de Fowler:

| Concepto | Qué es |
|---|---|
| **`PhenomenonType`** | *Lo que se puede medir* — altura, peso, glucosa, colesterol. Un catálogo |
| **`Quantity`** | Un número **con su unidad**. No un `decimal` suelto |
| **`Measurement`** | Asigna una `Quantity` a un `PhenomenonType` para un sujeto. *"John Smith mide 6 pies"* |
| **`CategoryObservation`** | La variante **no numérica**: el fenómeno cae en una categoría, no en una escala |
| **`Observation`** | La generalización de las dos: una cuantificación sobre un fenómeno dado |

Y el capítulo 2 del mismo libro aporta la pieza estructural que lo sostiene: la separación
**Knowledge Level / Operational Level** — el nivel donde vive *qué clase de cosas existen* separado
del nivel donde viven *los hechos concretos*.

### La correspondencia con `Examenes/Dominio`, verificada contra el código

| Fowler (1996) | `Examenes/Dominio` hoy |
|---|---|
| `PhenomenonType` (nivel de conocimiento) | **`Analito`**, con su catálogo estático: `Analito.ColesterolTotal`, `Analito.Hemoglobina`, `Analito.NitritosEnOrina`… |
| `Measurement` (cuantitativo) | **`AnalitoContinuo`** |
| `CategoryObservation` (categórico) | **`AnalitoNominal`** y **`AnalitoBinario`** |
| `Quantity` (número + unidad) | **`Magnitud`** + **`UnidadDeMedida`** — y `Magnitud` a su vez está partida en `MagnitudContinua` / `MagnitudBinaria` / `MagnitudNominal`, la misma partición que el catálogo |
| `Observation` (nivel operacional) | **`LecturaDeExamen`**, y las clases por analito (`Colesterol`, `Hemoglobina`, …) |
| Rango normal por sujeto | **`RangoDeReferencia`** + `RangoPara(paciente)` |

**El equipo llegó a la misma estructura de forma independiente, treinta años después.** Eso no es
casualidad: es que el problema tiene una forma, y quien lo modela con cuidado la encuentra.

> ### Y la contracara: FHIR es exactamente el modelo que ustedes NO quieren
>
> El estándar de la industria para un resultado de laboratorio es el recurso **`Observation` de HL7
> FHIR** con un código **LOINC** y un campo polimórfico **`value[x]`**: un solo recurso para todos los
> exámenes, donde lo que cambia es el código y el tipo del valor.
>
> Es la **misma forma que Fowler**, llevada al extremo del Type Object. Y FHIR dice de sí mismo que su
> intención es *"capturar mediciones y evaluaciones puntuales"* — o sea: **es un modelo de
> interoperabilidad, no un modelo de razonamiento.**
>
> **Ésa es la mejor manera de explicar el argumento clientecéntrico a alguien técnico:** FHIR es el
> producto de datos alineado al origen. Sirve para que dos sistemas se entiendan. No sirve para
> contestar *"¿mi colesterol viene bajando?"*, porque en FHIR **no hay dónde poner ese juicio**. Un
> `Observation` con `value[x]` puede transportar el 280; no puede saber que un 280 que viene de 320 es
> buena noticia.

---

### Pero el patrón de observación cubre la mitad, no el dominio entero

Verificado al extrapolarlo al gimnasio. Un dominio como ése necesita **tres familias de patrones**, y
la de Fowler cap. 3 es sólo una:

| Lo que el dominio hace | Familia | Dónde está |
|---|---|---|
| **Mide** algo del sujeto en el tiempo | Observación | Fowler **cap. 3** ✅ |
| **Prescribe** algo que después se cumple o no | Planificación | Fowler **cap. 8** — `Proposed and Implemented Action`, `Plan`, `Protocol`, `Completed and Abandoned Actions`, **`Suspension`** |
| El sujeto **declara hacia dónde quiere ir** | Intención | **No está en Fowler.** UFO-C (Guizzardi) y ODI (Ulwick) |

**El plan del gimnasio no es una observación: es el capítulo 8.** Y encaja pieza por pieza — el
ejercicio prescrito contra el que hizo es `Proposed and Implemented Action`, la rutina tipo es un
`Protocol`, la instancia de Sonia es un `Plan`. **Y `Suspension` (8.3) es el patrón del período
suspendido por atraso de pago**, que quedó abierto como hueco 9 del gimnasio y tiene nombre desde 1996.

**El objetivo es lo que el laboratorio no tiene.** El patrón de observación tiene **un** juicio: el
valor contra su rango. Un paciente no declara hacia dónde quiere que vaya su colesterol; alguien en un
gimnasio sí. **UFO-C** le pone nombre formal: los agentes portan *intentional moments*; *"el contenido
proposicional de una intención es un goal"*; y un compromiso se cumple cuando el agente ejecuta una
acción que es instancia de un **Plan** (un *action universal*). En esa teoría, entonces, **la meta y el
plan son dos cosas distintas y relacionadas por definición** — que es exactamente la relación entre
`Objetivo` y `Plan` en el gimnasio.

> **Y al laboratorio le pasó solo, lo cual cierra el argumento.**
> `Examenes/Dominio/MetaDeColesterol.cs`, agregado el 2026-08-26, dice en su comentario:
> *"antígeno → se lee contra su **línea base**; colesterol → se lee contra una **META**"*.
> **El mismo dominio terminó con los dos ejes**, y la distinción es por analito. El modelo de
> observación se le quedó corto justo donde apareció una intención declarada.

---

## Parte 2 · "¿Una clase por fila?" tiene una respuesta formal, y no es sí ni no

Ésta es la pregunta que planteás, y la literatura la tiene tratada con precisión. **Hay dos polos
documentados**, y el criterio de decisión entre ellos también.

### Polo A · Type Object — un tipo, muchas instancias-tipo

**Ralph Johnson y Bobby Woolf, *Type Object* (1997); Joe Yoder y Ralph Johnson, *Adaptive
Object-Model* (2002).**

> *"Los sistemas orientados a objetos generalmente usan una clase separada para cada clase de objeto,
> así que introducir una nueva clase de objeto requiere hacer una nueva clase, lo que requiere
> programar. Pero los desarrolladores de sistemas grandes suelen enfrentar el problema de tener una
> clase de la que deberían crear un número desconocido de subclases."*

La salida: dividir en **Entity** y **EntityType**. Las reglas de negocio se guardan **fuera del
programa** —en base de datos o archivos— y el código es un intérprete del modelo del usuario.

**Cuándo gana:** cuando las variantes se agregan en runtime, son muchas, o difieren **sólo en datos**.

### Polo B · Subtipo por variante

Gana cuando la variante tiene **comportamiento propio**. Es la regla que la guía de dominios del
equipo ya tiene escrita en su **E39**, con un criterio que resulta ser el mismo que el de la
literatura:

> **El test del Curador:** *quitá los nombres de las subclases y movés sus constantes a datos — ¿se
> pierde alguna regla o comportamiento?* Si **no**, los subtipos no están justificados.
>
> **El tell:** `if (kind==X) …lógica… else if (kind==Y) …otra lógica…` → comportamiento → subtipo. Un
> método compartido que lee **valores** distintos → datos → Definition.

### La formalización académica: Odell y Pirotte

- **Power Type** — James Odell, *Journal of Object-Oriented Programming*, 1994. La relación entre un
  tipo y el tipo de sus tipos.
- **Materialization** — Pirotte, Zimányi, Massart y Yakusheva, *"Materialization: a powerful and
  ubiquitous abstraction pattern"*, **VLDB 1994**. Formaliza la relación entre *una clase de
  categorías* (modelos de auto) y *una clase de objetos concretos* (autos individuales), en términos
  de `is-a`, `is-of` y la correspondencia clase/metaclase.

Es exactamente la relación `Analito.ColesterolTotal` ↔ `Colesterol`.

### La teoría más rigurosa: UFO / OntoUML (Guizzardi)

**Giancarlo Guizzardi**, *Ontological Foundations for Structural Conceptual Models* (2005) y trabajo
posterior. Es la respuesta académica seria a *"¿esto merece ser un subtipo?"*, y aporta dos cosas que
sirven directamente.

**Primero, una taxonomía de tipos de tipo.** Los *sortales* —tipos que dan un principio uniforme de
identidad e individuación— se subdividen en **kind, subkind, phase y role**. Un `Colesterol` no es lo
mismo que un `PacienteEnRiesgo`: el primero es un **subkind** (rígido: siempre lo fue y siempre lo
será), el segundo es un **phase** (dinámico: depende de un valor que cambia). Confundirlos es un
error catalogado.

**Segundo — y esto es lo que responde tu observación sobre las escalas — las *quality dimensions*.**

> Tu frase: *"el conteo de glóbulos rojos podría ser de millones, pero el colesterol podría estar en
> cantidades decimales muy pequeñas."*

En UFO eso tiene nombre formal: cada cualidad se proyecta en una **quality structure**, partida en
**quality dimensions**, y sus miembros son **quales**. Dos cualidades con **estructuras de calidad
distintas** son ontológicamente distintas **aunque las dos se representen con un número**. Ésa es la
justificación formal de que `AnalitoContinuo`, `AnalitoBinario` y `AnalitoNominal` sean tipos
distintos: **no difieren en el valor, difieren en el espacio de valores**.

**Y hay herramienta, no sólo teoría.** Existe un catálogo publicado de **anti-patrones
ontológicos** —incluyendo específicamente los de estructuras taxonómicas y de modelado de roles— y
validadores que los detectan (el plugin de OntoUML, `ontouml-js`). Es lo más cercano a *"un algoritmo
ya creado"* que encontré para esta parte del problema.

> **Síntesis mía, no bibliografía:** el modelo de Exámenes usa **los dos polos a la vez, en niveles
> distintos**, y ésa es la parte que ninguna fuente enuncia junta:
>
> - **En el nivel de conocimiento usa Type Object**: los diez analitos son *instancias* estáticas
>   (`Analito.ColesterolLdl`), no clases. Varían en datos → instancias.
> - **En el nivel operacional usa subtipos**: `Colesterol` y `Hemoglobina` son clases porque tienen
>   comportamiento propio. Y se puede comprobar en el código: `Colesterol` declara que su dirección
>   deseable es **bajar**, que su techo baja a los 50 años sin depender del sexo, y expone `Meta()`,
>   que devuelve una `MetaDeColesterol` — el escalón siguiente hacia el que la persona va;
>   `Hemoglobina` sabe calcular `GradoDeAnemia` y que su cadencia pasa de 12 a 6 meses si hay anemia.
>   **Ninguna otra clase sabe eso.** Pasan el test del Curador.
> - **Y la decisión se tomó fracción por fracción, con la razón escrita.** El LDL **sí** tiene clase
>   (`ColesterolDetallado`), porque *"es la fracción que decide la conducta clínica"* y forma su propia
>   serie. El HDL **no**: viaja como una `Medicion Hdl` adentro de esa clase, se muestra, y *"no es la
>   llave de la serie"*. Misma hoja, dos fracciones, dos decisiones distintas — y el comentario del
>   código dice por qué.
> - **La partición por espacio de valor aparece dos veces**, no una: en el catálogo
>   (`AnalitoContinuo` / `AnalitoBinario` / `AnalitoNominal`) y en el valor medido
>   (`MagnitudContinua` / `MagnitudBinaria` / `MagnitudNominal`). Es la *quality dimension* de UFO
>   llevada a los dos niveles de Fowler.
>
> No es "una clase por fila". Es **dos niveles, cada uno con la regla que le corresponde.**

---

## Parte 3 · Por qué NO existe un algoritmo — y la razón es de fondo

Buscando específicamente si se puede **derivar** el modelo desde el esquema del proveedor, la
respuesta de la literatura de *database reverse engineering* (DBRE) es un no con argumento medido:

> *"El código DDL, y por lo tanto el esquema físico, transmite **menos de la mitad** de la semántica
> del esquema lógico."*
> — Jean-Luc Hainaut, *Database Reverse Engineering* (Universidad de Namur), sobre la fase de
> conceptualización del esquema. Sus casos de estudio encuentran **más construcciones implícitas que
> explícitas** en los esquemas reales.

Y sobre el método en general: los métodos de DBRE *"no proveen un esquema abstracto de alto nivel
adecuado para razonar sobre el dominio de la aplicación"*, porque son **de abajo hacia arriba**.

### El argumento, dicho como corresponde

**No hay algoritmo, y no puede haberlo, por una razón de información:**

> **El valor agregado es exactamente aquello que NO está en el dato de origen.**
> Un algoritmo que sólo lee el dato de origen no puede producirlo. No es una limitación de las
> herramientas actuales: es que la información no está ahí.

El laboratorio manda `Colesterol: 280 mg/dL`. De ahí se puede derivar mecánicamente la clase
`Colesterol` con un campo `decimal`. **No se puede derivar:**

- que lo que preocupa es que **suba**, no que baje
- que 15 mg/dL es la variación mínima que se distingue del ruido
- que un cambio de 80 en un día es **error de medición** y en un año es una tendencia
- que un 280 que viene de 320 es **buena noticia**

Todo eso lo decidió una persona. **Y por eso toda metodología seria en este espacio es de
elicitación, no de derivación:** EventStorming, Domain Storytelling, Event Modeling, ODI — las cuatro
son **formatos de conversación**, no algoritmos.

> **Consecuencia práctica, y es la más importante de este documento:**
>
> Un generador puede producir **el sustrato** —los sustantivos que el artefacto trae, el cableado, los
> proyectos, el journal— y **no puede producir los juicios**. La frontera no es de esfuerzo ni de
> tecnología: es de información. Cualquier metodología que prometa generar el dominio completo desde
> el dato está prometiendo inventar los juicios, que es exactamente el fracaso a evitar.

---

## Parte 4 · Las metodologías que sí existen, y cuál se parece más a lo que pedís

Para la meta 2 —un proceso iterativo, visual, que produzca la columna vertebral— hay cuatro
candidatos publicados.

### 1 · EventStorming — Alberto Brandolini (2013)

Taller colaborativo para explorar dominios complejos. Notas adhesivas naranjas (eventos de dominio) en
una pared, luego comandos, agregados, contextos. **Fuerte para descubrir el proceso; débil para
producir un modelo de clases** — se detiene en agregados.

### 2 · Domain Storytelling — Stefan Hofer y Henning Schwentner (2015, libro 2021)

Lenguaje pictográfico simple: **actores, actividades y objetos de trabajo** en oraciones numeradas. Su
premisa es lingüística: *"escuchamos a los expertos de dominio mientras hablan de su trabajo y usamos
su lenguaje"*. Explícitamente **iterativo** y **visual**, y produce el vocabulario. Es el que más se
parece a lo que ya hace la entrevista de la Metodología.

### 3 · Event Modeling — Adam Dymitruk (junio de 2019) ← **el más cercano a lo que pedís**

Pone el ciclo completo de la información —intención del usuario, cambio de estado, modelos de
lectura derivados— en **una línea de tiempo legible llamada *blueprint***, dividida en ***slices***:
lazos pequeños y completos de comportamiento que se pueden construir y probar por separado.

Y trae un chequeo que **es exactamente el que la Metodología necesita**:

> **Information completeness.** *"Si una pantalla muestra un dato, el modelo tiene que probar de dónde
> viene, vía eventos. Cada atributo de un modelo de lectura tiene que ser trazable a uno o más eventos
> almacenados. Si un atributo no tiene evento de origen, descubriste **o un evento faltante, o un
> campo faltante, o una decisión faltante**."*

Ese *"o una decisión faltante"* es literalmente el hueco que la Metodología llama *juicio sin
declarar*. **Es el mismo chequeo, con veinte años de rodaje y otro nombre.**

### 4 · Outcome-Driven Innovation — Anthony Ulwick

Aporta la **forma canónica de un juicio**. Un *desired outcome statement* se compone de:

**dirección de mejora + medida + objeto + contexto**

y está diseñado para ser *"medible, controlable, accionable, libre de solución y estable en el
tiempo"*.

> Comparado con el bloque 3 de la entrevista actual —dirección, ruido, ritmo, alcance, urgencia—: **es
> la misma estructura**. La Metodología llegó a ella empíricamente; ODI la tiene formalizada y con
> criterios de calidad para cada enunciado.

### 5 · Generación asistida por LLM — el estado del arte 2025-2026

Hay trabajo académico reciente y directamente aplicable:

- ***Class Model Generation from Requirements using Large Language Models*** (2026) evalúa GPT-5,
  Claude Sonnet 4, Gemini 2.5 y Llama 3.1 generando diagramas de clases desde requerimientos en
  lenguaje natural. Propone un marco de **doble validación**: *LLM-as-a-Judge* más
  *human-in-the-loop*, evaluado en cinco dimensiones — **completitud, corrección, conformidad,
  comprensibilidad y alineación terminológica**.
- ***Model Generation from Requirements with LLMs: an Exploratory Study*** (2024) — genera en formato
  PlantUML.

Y el hallazgo metodológico coincide con tu intuición: *"hacen falta múltiples iteraciones de prompt
antes de que el LLM produzca diagramas en la forma y calidad deseadas"*, y **conviene empezar de
requerimientos tempranos y usar los diagramas producidos para refinar los requerimientos**, no al
revés.

> **O sea: el bucle que proponés —generar, mirar el diagrama, corregir, regenerar— es el que la
> literatura reciente encuentra que funciona.** Y las cinco dimensiones de evaluación son un punto de
> partida listo para el "visto bueno del equipo técnico".

---

## Parte 5 · Lo que NO encontré documentado

Ser honesto acá vale más que alargar la bibliografía. **Tres huecos reales**, y son justamente donde
está el aporte propio de la Metodología:

**1 · Nadie combina las dos entradas.** Todas las metodologías de taller parten de **la conversación**
(EventStorming, Domain Storytelling) o de **el proceso** (Event Modeling). La literatura de DBRE parte
del **esquema**. **No encontré una metodología publicada que tome sistemáticamente las dos —el
artefacto que la institución manda **y** la descripción funcional— y las cruce.** Y ése es exactamente
tu caso.

**2 · Nadie da un criterio de parada.** Todas dicen "es iterativo". Ninguna dice **cuándo dejar de
iterar**. Event Modeling se acerca con *information completeness*, pero eso es un criterio de
cobertura, no de suficiencia.

**3 · La decisión subtipo-vs-type-object está tratada, pero no operacionalizada por dominio.** Existe
el criterio (Fowler, E39, Guizzardi) y existen validadores para OntoUML — pero no encontré un
procedimiento que, dado el artefacto de una institución concreta, diga **fila por fila** cuál merece
clase. Se decide caso por caso, y el catálogo de anti-patrones sólo detecta errores **después**.

---

## Parte 6 · Qué adoptar, en orden de rendimiento

| | Qué | Por qué | Costo |
|---|---|---|---|
| **1** | El **vocabulario** de Data Mesh: *source-aligned* vs *consumer-aligned* | Es la frase que explica el proyecto entero a alguien de negocio en diez segundos, y está publicada | nulo |
| **2** | El ***information completeness*** de Event Modeling | Es el chequeo que ya se estaba inventando a mano. Adoptarlo con su nombre y su enunciado | bajo |
| **3** | La forma de **ODI** para escribir un juicio: dirección + medida + objeto + contexto | Le da estructura fija a la parte de la entrevista que hoy es prosa libre | bajo |
| **4** | El **par Knowledge Level / Operational Level** de Fowler como doctrina explícita | Ya está en el código; ponerle nombre lo vuelve enseñable y auditable | bajo |
| **5** | Las **cinco dimensiones de evaluación** del paper 2026 para el visto bueno del diagrama | Ya están validadas y son justo lo que hace falta para la revisión iterativa | bajo |
| **6** | El vocabulario de **UFO** —subkind vs phase, quality dimension— para la decisión de especializar | Es la teoría más rigurosa, y da el nombre formal de por qué las escalas distintas son tipos distintos | medio: hay que leerlo |
| **7** | Los **validadores de OntoUML** | Es lo único parecido a un algoritmo. Pero valida OntoUML, no C#: habría que traducir | alto |

**Lo que NO adoptaría:** convertir el modelo a OntoUML para validarlo. El costo de mantener dos
representaciones sincronizadas es exactamente el defecto que la Metodología ya persigue en otros
lados —una lista copiada siempre deriva—. El vocabulario sí; la herramienta, no todavía.

---

## Fuentes

**Libros y patrones**
- Martin Fowler, *Analysis Patterns: Reusable Object Models*, 1996 — cap. 2 (knowledge/operational level), cap. 3 (Observations and Measurements), **cap. 8 (Planning: Proposed and Implemented Action, Completed and Abandoned Actions, Suspension, Plan, Protocol, Resource Allocation)**. Diagramas UML: cap. 2 https://martinfowler.com/apsupp/apchap2.pdf · cap. 3 https://martinfowler.com/apsupp/apchap3.pdf · **cap. 8 https://martinfowler.com/apsupp/apchap8.pdf**
- Eric Evans, *Domain-Driven Design*, 2003 — Anti-Corruption Layer, Conformist, Published Language
- Martin Fowler, *AnemicDomainModel*, 2003
- Zhamak Dehghani, *Data Mesh*, O'Reilly, 2022 — https://www.oreilly.com/library/view/data-mesh/9781492092384/
- Stefan Hofer y Henning Schwentner, *Domain Storytelling*, 2021
- Alberto Brandolini, *EventStorming* — https://www.eventstorming.com/

**Patrones de tipo/instancia**
- Joseph Yoder, Federico Balaguer y Ralph Johnson, *Architecture and Design of Adaptive Object-Models*, ACM SIGPLAN Notices 36(12), noviembre 2001 — https://dl.acm.org/doi/10.1145/583960.583966 · Yoder y Johnson, *The Adaptive Object-Model Architectural Style*, WICSA 2002 — https://adaptiveobjectmodel.com/WICSA3/ArchitectureOfAOMsWICSA3.pdf
- Ralph Johnson y Bobby Woolf, *Type Object*, en *Pattern Languages of Program Design 3*, Addison-Wesley, 1997/1998, pp. 47–65 — https://www.cs.ox.ac.uk/people/jeremy.gibbons/dpa/typeobject.pdf
- Joseph Yoder, *From Analysis to Design of the Observation Pattern* — https://adaptiveobjectmodel.com/Observation/ObservationModel.pdf
- James Odell, *Power Types*, JOOP, 1994
- Pirotte, Zimányi, Massart, Yakusheva, *Materialization: a powerful and ubiquitous abstraction pattern*, VLDB 1994 — https://www.vldb.org/conf/1994/P630.PDF

**Ontología / modelado conceptual formal**
- Giancarlo Guizzardi, *Types and Taxonomic Structures in Conceptual Modeling: A Novel Ontological Theory and Engineering Support* — https://www.inf.ufes.br/~gguizzardi/Types_and_Taxonomic_Structures_in_Conceptual_Modeling:A%20Novel_Ontological_Theory_and_Engineering_Support.pdf
- Guizzardi et al., *Ontological Anti-Patterns in Taxonomic Structures* — https://www.inf.ufes.br/~gguizzardi/Ontological_Anti-Patterns_in_Taxonomic_S.pdf
- *Stability Patterns in Ontology-Driven Conceptual Modeling* — https://ceur-ws.org/Vol-2728/paper11.pdf
- OntoUML — especificación de *Quality*: https://ontouml.readthedocs.io/en/latest/classes/aspects/quality/index.html
- **UFO-C** — la capa social e intencional (agentes, *intentional moments*, intenciones, metas, compromisos): https://ontouml.readthedocs.io/en/latest/intro/ufo.html · https://dev.nemo.inf.ufes.br/seon/UFO.html
- Guizzardi et al., *A Commitment-based Reference Ontology for Services* (UFO-S) — https://inf.ufes.br/~gguizzardi/UFO-S.pdf

**La serie de papers de Puppeteer** — contrastados en [`03-contraste-con-los-papers.md`](03-contraste-con-los-papers.md)
- Rivera, A. (2026). *Anti-porous architecture: a unified design principle for CQRS + Actor + Event-Sourcing systems* (v0.2-draft). Zenodo. https://doi.org/10.5281/zenodo.20404863
- Rivera, A. (2026). *Program–value separability*. https://doi.org/10.5281/zenodo.20740697
- Rivera, A. (2026). *Reactions and the partition*. https://doi.org/10.5281/zenodo.20792156
- Rivera, A. (2026). *Most infrastructure layers are symptoms of the persistence model*. https://doi.org/10.5281/zenodo.20317450
- Rivera, A. (2026). *Inference without Authority: the three authorities that govern an output*. https://doi.org/10.5281/zenodo.21499637
- Papers 4, 5 y 7 de la misma serie: revisados, no tocan estos hallazgos.

**Estándares del dominio de salud**
- HL7 FHIR R5, recurso *Observation* — http://hl7.org/fhir/observation.html

**Metodología de proceso**
- Adam Dymitruk, *Event Modeling* — https://eventmodeling.org/
- Anthony Ulwick, *Outcome-Driven Innovation* — https://anthonyulwick.com/outcome-driven-innovation/ · *What is ODI* (PDF): https://innovationroundtable.com/summit/wp-content/uploads/2014/05/Strategyn_what_is_Outcome_Driven_Innovation.pdf

**Derivación automática y sus límites**
- Jean-Luc Hainaut, *Database Reverse Engineering* — https://staff.info.unamur.be/dbm/Documents/Publications-LIBD/DB-Encyclopedia/db-encyclopedia-DB-reverse-engineering-2007.pdf

**Generación asistida por LLM**
- *Class Model Generation from Requirements using Large Language Models*, 2026 — https://arxiv.org/html/2603.09100
- *Model Generation from Requirements with LLMs: an Exploratory Study*, 2024 — https://arxiv.org/html/2404.06371v1
