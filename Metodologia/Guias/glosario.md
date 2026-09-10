# Glosario — nuestros nombres y los publicados

Cada término que la Metodología acuñó, al lado del nombre con que la literatura ya lo conoce, la
fuente, y **un estado**. El estado es lo que hace útil este archivo: *"no estoy seguro de usar
Knowledge Source"* es una frase que abre un debate sólo si hay dónde anotar que está en debate.

**Por qué existe.** Las guías hablan en lenguaje llano porque las lee gente que no es técnica, y eso
no cambia. Pero cada cosa que nombramos tiene, casi siempre, un nombre publicado — y usarlo nos da
poder de decisión: podemos decir *"esto es Replace Subclass with Fields, y Fowler dice cuándo no
aplica"* en vez de defender una intuición. Un lector que conoce esas literaturas entiende y adopta lo
nuestro en la mitad del tiempo.

**Cómo se usa.** Las guías siguen diciendo *la columna de contraste*; donde convenga, enlazan acá. Los
documentos de investigación usan el término publicado en la primera mención, con el nuestro entre
paréntesis. **Este archivo es el único lugar donde viven las dos formas juntas** — una lista copiada
siempre deriva, y ésta no se copia.

| Estado | Qué significa |
|---|---|
| **adoptado** | Se usa con ese nombre en las guías o en la investigación; la correspondencia está verificada contra la fuente |
| **en debate** | La correspondencia está verificada, pero **no se decidió** si adoptamos el término, sólo lo citamos, o lo dejamos. Cualquiera del equipo puede abrir la discusión con el número de la fila |
| **propio** | No se encontró nombre publicado. Es aporte de la Metodología, y hay que decirlo así, no dejarlo ambiguo |

*Versión 0 — 2026-09-07. Los términos verificados en la investigación de septiembre de 2026 (documentos
01 a 05, fuera del repo por ahora). Las fuentes completas, con enlaces, están en el 01.*

---

## Adoptados

| # | Nuestro nombre | Término publicado | Fuente | Por qué es el mismo |
|---|---|---|---|---|
| 1 | **espacio de valor** — *"el conteo de glóbulos rojos está en millones y el colesterol en decimales"* | **quality dimension** / **quality structure** | Guizzardi, *Ontological Foundations for Structural Conceptual Models*, 2005 (UFO / OntoUML) | Dos cualidades con estructuras de calidad distintas son ontológicamente distintas aunque las dos se representen con un número. Es la justificación formal de partir el catálogo en continuo / binario / nominal |
| 2 | **nivel de conocimiento** / **nivel de operación** — *lo que existe aunque no haya datos* / *lo que ocurrió* | **Knowledge Level** / **Operational Level** | Fowler, *Analysis Patterns*, 1996, cap. 2 §2.5 | *"Las instancias del nivel de conocimiento gobiernan la configuración de las instancias del nivel operacional."* Es el primer eje de la grilla |
| 3 | **el espejo** — el modelo que reproduce el dato de origen | **source-aligned data product** · **Conformist** · **Anemic Domain Model** · **codificación estructural / porosidad** | Dehghani, *Data Mesh*, 2022 · Evans, *DDD*, 2003 · Fowler, 2003 · Rivera, paper 1, 2026 | Cuatro nombres para cuatro caras del mismo defecto: alineado al origen y no al consumo, adopta el modelo ajeno tal cual, datos sin comportamiento, dimensionado al espacio de tipos del artefacto. La pasada 1 lo dibuja a propósito como línea base |
| 4 | **la intención** — lo que alguien declaró que quiere o debe pasar | **intentional moment** / **goal** · **commitment** | UFO-C (Guizzardi) · UFO-S, *A Commitment-based Reference Ontology for Services* | *"El contenido proposicional de una intención es un goal."* Es la columna del medio de la grilla, y la que Fowler cap. 3 no tiene |
| 5 | **la rutina tipo** / **el plan de Sonia** — la intención en cada nivel | **Protocol** / **Plan** | Fowler, *Analysis Patterns*, cap. 8 | El `Protocol` está en el nivel de conocimiento y el `Plan` en el operacional, y los dos son intención. Es la prueba de que los dos ejes de la grilla son independientes |
| 6 | **la propuesta y lo adoptado** — `ReservaPropuesta` / `ReservaDeclarada` | **Proposed Action** / **Implemented Action** | Fowler, *Analysis Patterns*, cap. 8 | Lo que la app calcula y ofrece contra lo que la persona hizo suyo. Colapsarlos en un `bool adoptada` es porosidad, y peor: un número de la app pasaría por decisión de la persona |
| 7 | **la familia de observación** — medir algo del sujeto en el tiempo | **Observations and Measurements**: `PhenomenonType`, `Quantity`, `Measurement`, `CategoryObservation` | Fowler, *Analysis Patterns*, cap. 3 — desarrollado modelando el NHS británico | `Analito` ↔ `PhenomenonType`, `Magnitud` + `UnidadDeMedida` ↔ `Quantity`, `AnalitoContinuo` ↔ `Measurement`. Exámenes llegó a la misma estructura sin haberlo leído |
| 8 | **el catálogo** — los analitos como instancias, no como clases | **Type Object** · **Power Type** · **Materialization** | Johnson y Woolf, 1997 · Odell, 1994 · Pirotte et al., VLDB 1994 | La relación entre una clase de categorías y una clase de objetos concretos. `Analito.ColesterolTotal` ↔ `Colesterol` |
| 9 | **la forma de un juicio** — *dirección + medida + objeto + contexto* | **desired outcome statement** | Ulwick, *Outcome-Driven Innovation* | Diseñado para ser *"medible, controlable, accionable, libre de solución y estable en el tiempo"*. El bloque 3 de la entrevista llegó a la misma estructura empíricamente |
| 10 | **completitud** — *cada dato que una pregunta necesita tiene origen* | **information completeness** | Dymitruk, *Event Modeling*, 2019 | *"Si un atributo no tiene evento de origen, descubriste o un evento faltante, o un campo faltante, o una decisión faltante."* Ese *"decisión faltante"* es nuestro juicio sin declarar |
| 11 | **el hecho** — la huella de un verbo que ocurrió | **domain event** | Brandolini, *EventStorming*, 2013 | Verbo en pasado, en la voz del negocio. La lista de verbos de la pasada 1 es una lista de eventos de dominio — con una diferencia: ver *propio* nº 25, las dos fuentes |
| 12 | **la barra de activo** — tipos nombrados incluso cerca del polo de datos, si cada uno se gana su lugar | **E40** | Guía de dominios de Puppeteer (Rivera), E40 *"SAY the domain"* | **Decidido el 2026-09-07 como regla por defecto de la pasada 4, con frontera.** Una fila que **juzga** por sí misma —rango, dirección, qué severidad o acción elige— es una clase nombrada; una que sólo **parametriza** una operación compartida —cadencia, fórmula, monto— es instancia del catálogo. Los analitos de Exámenes juzgan → clases; los rubros de la municipalidad parametrizan → `Rubro.Basura`. El Curador (nº 15) queda como piso |
| 13 | **porosidad** — la estructura como primario, la operación como derivado | **anti-porous architecture** — *horizontal sparsity*, *vertical sparsity* | Rivera, *Anti-porous architecture*, 2026, paper 1 | Un modelo dimensionado al espacio de tipos del artefacto. Horizontal: campos poblados sólo por algunas filas. Vertical: operaciones descartadas. El espejo de la pasada 1 es la codificación porosa, a propósito |

## En debate

Verificados en septiembre de 2026 y **sin decisión** sobre si se adoptan. Cada fila es una discusión
abierta; se cierra moviéndola arriba o borrándola, con la razón en el commit.

| # | Nuestro nombre | Término publicado | Fuente | La correspondencia, y qué hay que decidir |
|---|---|---|---|---|
| 14 | **el juicio firmado** / **la firma** / **sin firmar** | **Knowledge Source** + **Authority Requirement** | OMG, *Decision Model and Notation* (DMN), Decision Requirements Diagram | Un Knowledge Source es *"external authorities, documents, committees, or policies that regulate a decision"*, y la Authority Requirement es la arista que lo une a la decisión. **La firma es notación estándar, no metáfora nuestra.** A decidir: ¿la hoja de firmas se dibuja como DRD? ¿Se adopta el término o sólo se cita? |
| 15 | **el test del Curador** — *quitá el nombre y movés las constantes a datos, ¿se pierde una regla?* | **Replace Subclass with Fields** — inverso: **Replace Type Code with Subclasses** | Fowler, *Refactoring*, catálogo | *"Subclasses differing only in their constant-returning methods → replace with fields and delete the subclasses."* Es el test palabra por palabra, con entrada de catálogo. **Ya decidido que es el piso y no el techo** (nº 12); a decidir si se renombra o se cita |
| 16 | **la columna de contraste** / **el juicio** — la comparación de un hecho contra una intención o contra su historia | **Value Ascription** / **Risk Assessment** | Sales, Baião, Guizzardi, Almeida, Guarino y Mylopoulos, *The Common Ontology of Value and Risk* (COVER), 2018 — especificado en OntoUML | Su "perspectiva relacional" son relaciones que involucran agentes **y los juicios que hacen** — la palabra es *judgements*. Es lo más cerca que la familia UFO llega de nuestro contraste, y viene del mismo grupo que la nº 1. A decidir: ¿*ascripción* como nombre del contraste? |
| 17 | **la pregunta de la ausencia** — *"¿hace cuánto que no…?"*, `ControlPendiente` | **care gap** (salud) · **omission event** (general) | calidad asistencial · literatura de event outliers | *"A clinically recommended service that a patient has not received within the timeframe specified by evidence-based guidelines."* Omisión: un evento que debía ocurrir y falta; comisión: uno inesperado que ocurrió. A decidir: *care gap* es legible pero es de salud; *omisión* es general pero frío |
| 18 | **cuándo dejar de iterar** — las cuatro preguntas de parada | **theoretical saturation** | Glaser y Strauss, *grounded theory*, 1967 | ⚠️ **El concepto tiene nombre; el test no.** La literatura dice que *"the indicators of saturation are not always clear-cut and can be subjective"*. Nuestras cuatro preguntas son una **operacionalización** de la saturación — eso es una afirmación más fuerte que *"adoptamos un criterio publicado"*, y hay que escribirla así. Matiza pero no cierra el hueco 2 del 01 |
| 19 | **rigidez** — *¿una instancia puede dejar de ser esto sin dejar de existir?* | **rigid** / **anti-rigid** — Kind, Subkind, Phase, Role | Guizzardi, OntoUML | Pregunta que la pasada 4 **no hace** y el Curador no cubre: `CuentaMorosa : Cuenta` pasa el Curador —carga la regla de la mora— y falla rigidez, y en C# eso es fatal. Corrida contra los cinco dominios del repo en septiembre de 2026: **cero casos**, porque la grilla ya manda lo que va y viene a la columna de contraste. A decidir: entra como **control** en la pasada 4 |
| 20 | **el relator** — *¿en virtud de qué alguien es socio, contribuyente, paciente?* | **Relator** | Guizzardi, OntoUML | Un Role exige un relator que lo medie. La Metodología nunca pregunta *"¿en virtud de qué?"*, y una vez habría atajado la membresía perdida del gimnasio (`Membresia` es el relator de *socia*). **Sin control negativo todavía**: `Paciente` en Exámenes también es un role y ahí no hay relator — puede faltar una clase o puede ser ruido. No entra a ninguna guía hasta correrlo contra los cinco dominios |
| 21 | **hueco con dueño** — *satisfecho no es sin preguntas abiertas: es que están a la vista y tienen a quién preguntarle* | **Architecture Decision Record** / **decision log** — con estado *proposed*, nivel de confianza y **criterios de revisita** | Microsoft, Azure Well-Architected Framework, *Maintain an ADR*; la organización `architecture-decision-record` en GitHub. *(Atribución habitual a Nygard, 2011 — no verificada acá)* | La guía de Microsoft pide registrar **el nivel de confianza** de una decisión y **criterios concretos para revisitarla**, y dice que el valor está en **la alternativa rechazada** — que es nuestro *lo que se rechazó, con motivo*. La diferencia: un ADR registra **decisiones**; el `HUECOS.md` registra **no-decisiones** con dueño. A decidir: ¿el `HUECOS.md` toma la forma ADR — estado, confianza, criterio de revisita? |
| 22 | **el default silencioso** — *un juicio inventado con mejores modales: no falla nunca y da una respuesta creíble y equivocada* | **silent failure** (el anti-patrón, nombre informal) · **fail-stop** (nuestra regla) | Literatura de sistemas confiables: *fail-silent* / *fail-stop* / *fail-operational*. El anti-patrón sólo tiene nombre informal en la práctica | ⚠️ **Cuidado con *fail-silent*: significa lo contrario de lo que suena.** En esa literatura *fail-silent* es la propiedad **buena** — el componente da la respuesta correcta o no da nada. Nuestro default silencioso es lo opuesto: da una respuesta plausible y equivocada, y eso ahí se llama corrupción silenciosa. La formulación práctica más exacta: *"trades a crash you would notice for a wrong answer you would not"*. **Nuestra regla —o el objeto no se construye, o la respuesta dice que no sabe— es semántica fail-stop.** A decidir: adoptar *fail-stop* para la regla; **no adoptar *fail-silent* nunca** |
| 23 | **proponé, no preguntés en frío** — *nadie tiene un umbral en la cabeza; traé uno y dejá que lo muevan* | **straw man approach** (análisis de negocio) · *anchoring* como mecanismo | BA Times, *The Strawman: When a Wrong Makes a Right*; técnica catalogada en análisis de negocio. *(Anchoring: Tversky y Kahneman — el mecanismo cognitivo, no verificado acá)* | *"Presentar requisitos deliberadamente incompletos o incorrectos para provocar una respuesta"* — es la regla del bloque 3 y del bloque 5, con nombre. Y trae una advertencia que la técnica conoce y nosotros no habíamos escrito: **el ancla sesga** — un valor propuesto se corrige menos de lo que debería. A decidir: adoptar el nombre, y agregar la advertencia al guion |

## Propios

No se encontró nombre publicado. **Son el aporte**, y conviene reclamarlos como tales en vez de
dejarlos sonar como si fueran adoptados.

| # | Nuestro nombre | Qué es | Lo más cercano que se encontró |
|---|---|---|---|
| 24 | **la grilla 2×3** — y en particular **el eje hecho / intención / contraste** | Cruzar el eje de Fowler (nº 2) con el papel que cada cosa juega. Las tres celdas tienen nombre publicado (nº 11, 4, 16); **el eje que las cruza con el de Fowler no** | Nada. El 01 §5 lo registra como hueco 1: *"nadie combina las dos entradas"* |
| 25 | **la lista de verbos con dos fuentes** | Los verbos de la institución salen del artefacto; los de la persona, de la conversación. Anotar sólo los primeros deja al dominio sin la mitad híbrida | EventStorming produce eventos de dominio (nº 11) pero de **una** fuente: la conversación |
| 26 | **el criterio no-espejo** — *tapá la columna de contraste; si lo que queda es el artefacto, no hay dominio* | La versión verificable de *"¿hay una pregunta que el portal no contesta?"*. La celda que lo garantiza es **operación / contraste** | Nada operativo. *Information completeness* (nº 10) es de cobertura, no de suficiencia |

*Tres que estaban acá el 2026-09-07 por la mañana —hueco con dueño, default silencioso, proponé no
preguntés— se movieron a **en debate** el mismo día, al encontrarles nombre publicado (nº 21–23).*

---

## Cómo se cambia este archivo

- **Mover una fila de *en debate* a *adoptado*** es una decisión del equipo, y va en el commit con la razón.
- **Agregar una fila** exige la fuente verificada — no de memoria. Si es de memoria, va a *propios* con *"sin verificar"* hasta que alguien la busque.
- **Un término acuñado que aparezca en las guías y no esté acá** es un defecto de este archivo, no de la guía. Es lo que el observable 15 va a chequear cuando exista.
