# Contraste con los ocho papers de Puppeteer

**Veredicto:** los hallazgos de la investigación **no contradicen la doctrina de ningún paper**. Aparecen
**dos tensiones reales**, las dos en el dominio del gimnasio —el que construí yo, no `Examenes`—, las
dos detectables leyendo el código, y las dos con la misma raíz que un caso que el paper 1 usa como
ejemplo negativo. Aparece **un vacío** en la metodología que los papers hacen visible: es *noun-first*
y la doctrina es *verb-first*. Y aparecen **cinco refuerzos**, uno de los cuales le da nombre a la
pasada 1.

*Contrastado el 2026-09-03 contra `puppeteer-papers` (Rivera, 2026, ocho preprints v0.1–0.4 y la
monografía). Lo leído: paper 1 completo; §4–§5 del 8; §5 y §9 del 3; §1.1 y §4 del 2; barrido por
términos con contexto en 4, 5, 6 y 7.*

---

## 0 · Qué toca cada paper

| # | Paper | Qué toca de la investigación | Veredicto |
|---|---|---|---|
| 1 | Anti-porosidad | La grilla, el catálogo de instancias, la columna de contraste, la tesis del no-algoritmo, **el `Plan` del gimnasio** | Sin contradicción · **una tensión** (T1) · un refuerzo por analogía |
| 2 | Separabilidad programa–valor | Riqueza de verbos; *"el DSL invoca, no transcribe"* | Refuerzo · una restricción para el generador |
| 3 | Reacciones y la partición | *"Un dominio que cierra"*; cita las mismas fuentes que el doc 1 | Refuerzo doble |
| 4 | Continuidad entre actores | — | No toca |
| 5 | El journal como sustrato | *"Derived state"* es la capa 2 del sustrato, no derivación de dominio | No toca |
| 6 | Síntoma infraestructural | *"Cuando la interacción se modela antes que el dominio…"* | **Refuerzo fuerte** de *"la pantalla al final"* |
| 7 | Después del sustrato | — | No toca |
| 8 | Inferencia sin autoridad | Dónde va el contraste (dominio vs actor); **el `ToString()` del gimnasio** | Sin contradicción · **una tensión** (T2) · da el criterio que faltaba |

---

## 1 · Ninguna contradicción de doctrina — y tres lugares donde parecía haberla

### 1.1 «Roles, no conceptos» (paper 1 §4.1) contra el nivel de conocimiento

El paper 1 dice que el dominio se parte *"por roles —operaciones que actúan— y no por entidades
—sustantivos que existen—"*. Mi grilla tiene un **nivel de conocimiento** hecho de sustantivos:
`Analito`, `Medida`, `Producto`. ¿Contradicción?

No, y por tres razones que se pueden verificar:

- **El paper 1 habla de la jerarquía de estados del ciclo de vida** (`Draft`/`Requested`/`Paid`), y
  ahí prescribe subtipos por operación. El nivel de conocimiento de Fowler no es eso: son *"las reglas
  generales que gobiernan"* la configuración del nivel operacional (§2.5). Son **dos cortes
  distintos**, no dos respuestas a la misma pregunta.
- **La propia doctrina del equipo ya lo resolvió.** La guía de dominios, **E39**, prescribe
  *Definition* (instancia canónica compartida, comparada por referencia) + *Occurrence* (entidad
  mutable con identidad). Eso **es** conocimiento + operación. Y E39 cita a los papers.
- **Una `Lectura` con tres habitantes (normal/bajo/alto) es exactamente lo que el paper 1 pide**: *"la
  variante que la operación produjo (instancia de tipo suma)"*. La columna de contraste está llena de
  tipos suma cerrados.

Lo que sí hay que cuidar: la frase *"roles, no conceptos"* **se puede malinterpretar** como una
prohibición de tener catálogo. No lo es, pero la metodología debería decir explícitamente que el
nivel de conocimiento es *reglas que gobiernan*, no *sustantivos por gusto*.

### 1.2 La columna de contraste contra «las proyecciones van del lado de lectura» (paper 1 §6, paper 8 §4)

Ésta era la que más me preocupaba. El paper 1 dice que las proyecciones estructurales se derivan
*"sólo donde el consumo downstream las requiere"* y que *"los joins migran al lado de lectura"*. El
paper 8 dice que el dominio sabe *qué existe* y el actor autora *qué se vuelve observable*, y su
ejemplo —`subtotal = UnitPrice * Units`— es **una derivación hecha en el `print` del actor, no una
clase del dominio**. Mi metodología pone `Tendencia`, `Incumplimiento`, `EvaluacionDeRendimiento`
**adentro de `Dominio/`**.

**No es contradicción, y el paper 8 lo dice con todas las letras:** el borde dominio/actor es
**blando**, *"se dibuja por concepto"*, y empujar una derivación al dominio (`order.DiscountedItems()`)
es *"una alternativa legítima y no un error"* **cuando** es *"una noción que el dominio genuinamente
podría poseer — que valga darle forma en su representación de escritura"*.

Ése es **el criterio que a mi documento le faltaba**, y hay que agregarlo:

> **Un contraste va al dominio si es una noción que el negocio nombra y sobre la que decide.** *"Viene
> bajando"*, *"le faltaron nueve días"*, *"esto es un hallazgo"* — el gimnasio y el laboratorio hablan
> así. Un contraste va al actor si es un **recorte para una vista** — *"las líneas con descuento"*,
> *"los últimos cinco"*.

Y dos precisiones que los papers obligan a escribir:

- **Los objetos de contraste son respuestas computadas, nunca hechos journaleados.** `ComoVengo()`
  calcula la `Tendencia` sobre la serie; no la guarda. Eso es exactamente E24 —*"una respuesta
  computada no es porosidad"*— y el paper 8 —*"una derivación, no un campo almacenado"*—. Mi grilla
  los pone en una columna al lado de los hechos, y un lector puede creer que se persisten. Hay que
  decir que no.
- **Lo que el paper 8 sí prohíbe es que el dominio sea el que decide la rendición.** Y el gimnasio se
  lo deja al material en un punto: la etiqueta del juicio sólo sale por `ToString()` — ver T2.

### 1.3 «No hay algoritmo» contra la autopersistencia (paper 1 §4.0, §2.3)

Mi doc 1 sostiene que **el valor agregado no se puede derivar del dato** porque no está en el dato. El
paper 1 §4.0 cuenta que en 2005 *"el estado completo del autómata se pudo recuperar sin inspeccionar su
código"* replayando el log. ¿Eso no es derivar mucho de poco?

Es lo contrario, y es un refuerzo por analogía. El paper 1 §2.3 lo formula así: *"el estado se puede
derivar de las operaciones por replay, pero las operaciones no se pueden derivar del estado — la
función es muchos-a-uno."* **Mi tesis es la misma asimetría, un nivel más arriba:** los hechos se
derivan de los juicios aplicados, pero los juicios no se derivan de los hechos. Un `280` no dice si
preocupa que suba o que baje. La autopersistencia recupera *estado* de *operaciones*; nadie recupera
*juicios* de *datos*. Las dos afirmaciones son la misma flecha.

---

## 2 · Dos tensiones reales — las dos en el gimnasio, las dos mías

### T1 · `Plan` es porosidad horizontal en el agregado (paper 1 §2.1)

```csharp
internal bool     Termino { get; private set; }
internal DateTime Fin     { get; private set; }   // sin significado hasta que Termino == true
```

Es **literalmente** el patrón que el paper 1 §2.1 nombra como defecto: *"un discriminador `status` y
columnas pobladas condicionalmente"* — un campo (`Fin`) cuyo significado depende de un hermano
(`Termino`). El paper lo aplica a tablas, pero su criterio de demarcación (§3) incluye explícitamente
*"el agregado en memoria"*. Y es la **misma forma** que el paper 1 le señala a `Order.Description` de
`dotnet-eShop` en su Apéndice B.

Tres fuentes independientes prescriben lo mismo:

| Fuente | Qué dice |
|---|---|
| Paper 1 §4.1 | *"Un tipo suma, no una tabla"*: cada estado un subtipo con sus propios campos |
| Fowler cap. 8, §8.2 | **Completed and Abandoned Actions** — el plan terminado es *otra cosa* que el plan abierto |
| Guía de dominios **E29** | *"En una transición terminal, soltá la identidad del objeto para que no pueda seguir actuando"* |

**La forma correcta:** `PlanAbierto` → `Terminar(fin)` produce un `PlanTerminado` que **sí** tiene `Fin`
y **no** tiene `Prescribir()`. El `SemanasQueDuro()` que hoy revienta con *"este plan no ha terminado"*
deja de necesitar el `if`: sólo existe en el tipo donde tiene sentido.

**Costo de no arreglarlo:** hoy el gimnasio se sostiene porque `SemanasQueDuro()` **revienta** si el
plan está abierto. Es la salida honesta (E32: bug del llamador → throw), pero es una guarda que el
tipo debería hacer innecesaria.

> **Precisión medida el 2026-09-04, que baja la urgencia sin cambiar el diagnóstico.** `Fin` se lee
> tres veces y **las tres están protegidas**: `Trayectoria.cs:152` bajo un `if (!elPlan.Termino) throw`,
> `Trayectoria.cs:198` bajo un `if (!p.Termino) continue`, y el `print` de `GimnasioAPI.cs:286` dentro
> de un `foreach` sobre `PlanesQueTerminaronSinMedicion(...)`, donde todos terminaron. **Hoy no produce
> ningún `01/01/0001`**; el riesgo es para el cuarto llamador.
>
> Y hay un arreglo mínimo antes del rediseño: **guardar `Fin`** con su consulta de presencia, como ya
> hacen sus dos hermanos —`SemanasQueDuro()` y `PesoIndicadoDe()` lanzan con *"Preguntar X antes"*—.
> `Fin` es el único de los tres miembros condicionales que no cumple el contrato de su propia clase
> (E41). Seis líneas contra un rediseño de tipos.
>
> **No se hizo, y es decisión tomada:** el gimnasio es dominio de prueba, no producto.
>
> Nota para el hallazgo sobre la ambigüedad de **E29**: `Plan.Terminar()` **es** una transición terminal, y cae
> del lado en que el individuo **persiste** —un plan terminado sigue siendo ese plan—. O sea que acá
> corresponde el primer mecanismo de E29 (transicionar el tipo), nunca el segundo (soltar la
> referencia). El documento lo leyó bien; E29 no da el criterio.

### T2 · Los tipos de juicio del gimnasio no exponen su etiqueta (paper 8 §4)

```csharp
// Lectura.cs, Tendencia.cs, Urgencia.cs
private readonly string estado;                        // el nombre del estado, privado
public override string ToString() { return estado; }   // la única vía hacia él
```

**Los tres sí exponen hojas**, y decir que *"se renderizan solos"* fue una exageración mía: `Lectura`
da `ValorMedido`, `ValorComparado`, `QueSeComparo`, `EsBajo()`, `EsNormal()`, `EsAlto()`; `Tendencia`
da `Cambio`, `NombreDeLaMedida`, `Mejora()`, `Empeora()`; `Urgencia` da `Avisa()`, `EsPositiva()`,
`EsNegativa()`. Lo que **ninguno** expone es su **etiqueta**: `estado` y `nombre` son privados y el
único camino al texto es el `ToString()`. Es **un miembro que falta en cada uno**, no un rediseño.

> **`Direccion` y `Genero` no entran acá.** Son conjuntos cerrados, y un conjunto cerrado que dice su
> propio nombre por `ToString()` es el **caso legítimo**: tiene una sola rendición y es del dominio.
> La tensión es con los **juicios**, que la superficie puede querer mostrar de varias formas.

El paper 8 §4 nombra exactamente esto como el síntoma del borde dominio/actor: *"un objeto de dominio
que se renderiza a sí mismo… el material habló como si tuviera la autoridad de la proyección"*. Y
E2/E24: `ToString` *"sólo para intención de dominio o debugging — nunca un volcado de estado"*.

Lo interesante es que **el motor ya lo había detectado**. En la corrida del perform, `print lectura
estado` falló con *"emits a value of type 'Lectura', which is material, not a value. The projection is
the actor's to author, not the material's"* — que es el paper 8 §4 dicho por el runtime. **La lección
que saqué fue la equivocada:** escribí en la regla 13 *"hay que escribir `lectura.ToString()` en el
script"*. La lección correcta, leída con el paper 8, es la contraria:

> **Exponé la hoja y dejá que el actor la elija.** `print lectura.Estado estado`, no
> `print lectura.ToString() estado`. El dominio dice *qué existe* (`Estado`, `Nombre`); el actor decide
> *qué se vuelve observable*.

Y la prueba de que es lo correcto está en `Examenes`: `Nitritos` expone `Etiqueta` y `Expresado` como
propiedades públicas. **`Examenes` lo hace bien; el gimnasio no.** El `ToString()` puede quedarse para
debugging, pero no puede ser la única vía de salida.

> **Nota sobre el repo — corregido el 2026-09-04.** La regla 13 de `reglas-del-substrato.md` tenía el
> último punto redactado con la lección equivocada. Ya se reemplazaron sus dos frases finales: ahora
> dice que de las dos salidas que ofrece el mensaje del motor **la primera es la buena y la segunda es
> la escapatoria**, que escribir `.ToString()` en el script cambia *quién llama* y no *quién decide*, y
> lleva el tell —*si la única vía al texto es `ToString()`, el actor no tiene entre qué elegir*— con su
> excepción declarada para los conjuntos cerrados. El hecho mecánico y el mensaje del motor quedaron
> intactos.
>
> **El código del gimnasio no se tocó**, y es decisión tomada: el gimnasio es dominio de prueba, no
> producto. Su valor es como evidencia de que la guía dejó pasar esto, y esa evidencia la guarda git.

---

## 3 · El vacío: la metodología es *noun-first*, la doctrina es *verb-first*

Éste es el hallazgo estructural del contraste, y no es una contradicción sino una **omisión** que los
papers vuelven visible.

Mis cuatro pasadas inventarian **sustantivos** (1), los clasifican (2), les agregan juicios (3) y
deciden subtipos (4). En ningún momento inventarian **operaciones**. Y la doctrina entera es
operación-primero:

- Paper 1 §1.1: *"la operación es el primario representacional"*; la estructura es lo que se deriva.
- Paper 2 §4: *"un verbo nombra una transacción de dominio completa"*; la riqueza está en los verbos.
- E24 (guía de dominios): *"la operación es el primario, no el estado"*.
- **Y Event Modeling, que yo mismo recomendé en el doc 1 como la metodología más cercana, es
  evento-primero**: parte de *qué pasó*, no de *qué hay*.

**Dos consecuencias:**

**Primera — la pasada 1 tiene nombre en el paper 1, y no es halagador.** El diagrama del espejo —`Examen`
con `Fila` y sus campos— **es la codificación estructural**: la forma dimensionada al espacio de tipos
del artefacto. Es decir, **la pasada 1 dibuja la representación porosa a propósito**, y las pasadas 3 y
4 son la inversión anti-porosa. Decirlo así hace que el método y el paper hablen el mismo idioma, y
explica por qué *un modelo que al final se parece al de la pasada 1* falló: **se quedó estructural**.

**Segunda — a la pasada 1 le falta media lista.** Por cada campo del artefacto hay que preguntar
también **qué operación lo produjo**: *¿qué hizo alguien para que este campo exista?* El laboratorio
**midió**; el gimnasio **prescribió**, **registró la entrada**, **midió**; el banco **cobró**. Esa
lista de verbos es:

- lo que el paper 1 llama el *script* — lo que de verdad se journalea;
- lo que las reglas 17 y 18 del substrato necesitan para diseñar un `Puede<Comando>` por comando;
- y lo que distingue un hecho (*ocurrió una operación*) de una intención (*alguien declaró*) en la
  grilla de la pasada 2 — que hoy se decide a ojo.

Sin la lista de verbos, la grilla se llena mirando sustantivos y adivinando su papel. Con ella, el
papel se lee: **un hecho es la huella de un verbo que ocurrió; una intención es la huella de un verbo
que se declaró.**

---

## 4 · Cinco refuerzos

**4.1 · «La pantalla al final» la dice un paper de Puppeteer.** Paper 6, línea 307: *"Cuando la
interacción se modela antes que el dominio —cuando los agregados toman la forma de la pantalla en vez
de que la pantalla renderice la salida del actor— el mismo patrón de compensación reaparece: componentes
de UI, view-models, DTOs de request y objetos de estado-de-paso se acumulan alrededor de un dominio que
ya no cabe en su propio sustrato."* Es la Parte 3 del `Preguntas.md` (*"se decide al final"*) y la
pasada 5 de Fowler cap. 4, con la autoridad de la propia serie.

**4.2 · «Un dominio que cierra» y el criterio de parada.** Paper 3 §5.1: lo que cierra es *"la frontera,
no el crecimiento conceptual"*; *"«tarea hecha» es una oración organizacional, no de ingeniería"*. Es
exactamente mi cuarta pregunta de parada: *satisfecho ≠ sin preguntas abiertas; significa que las
abiertas tienen dueño.*

**4.3 · Las mismas fuentes.** Paper 3 §9.2 cita a Evans (2003), Vernon (2013) y Fowler
(*AnemicDomainModel*, 2003) para *"la realidad industrial: el dominio no existe como biblioteca, existe
como clases dispersas"*. Son las fuentes de la Parte 0 de mi doc 1. Coincidencia independiente.

**4.4 · Una restricción para el generador.** Paper 2 §1.1: *"esto no es doble programación, no es una
capa de traducción… el DSL es el lenguaje por el que la implementación se invoca."* Un generador de
andamiaje **puede** emitir dominio, API y manifiesto; **no puede** emitir scripts DSL que transcriban
el C#. Eso sería la lectura errónea que el paper 2 se anticipa a descartar.

**4.5 · El contraste contra la propia historia es legítimo.** Paper 8 §5 distingue al observador que
*infiere* una historia que no le contaron (síntoma) del que *recibe testimonio*. El supermercado que
dice *"comprabas leche cada cinco días y hace doce que no"* no infiere: **el actor tiene la narración**
en su journal. Es testimonio, no inferencia. Con una reserva del propio paper que aplica a la Puerta 1
—una IA externa leyendo el journal cruza un límite de confianza y *"debe verificar"*—, fuera del alcance
de esto.

---

## 5 · Qué cambia, y dónde

| Dónde | Qué | Estado |
|---|---|---|
| `02-columna-vertebral.md`, pasada 1 | Agregar la **lista de verbos** al lado de la de campos; nombrar el diagrama del espejo como *la codificación porosa* | ✅ aplicado |
| `02-columna-vertebral.md`, pasada 2 | Decir que los contrastes son **respuestas computadas, no hechos journaleados**, y dar el **criterio del paper 8** para decidir dominio vs actor | ✅ aplicado |
| `01-estado-del-arte.md`, fuentes | Los ocho papers como fuente, con DOI | ✅ aplicado |
| **Repo** · `reglas-del-substrato.md`, regla 13 | Último punto: *"exponé la hoja"*, no *"escribí `.ToString()`"* | ✅ **hecho el 2026-09-04** — reemplazadas las dos frases finales |
| **Repo** · `Metodologia/Pruebas/Gimnasio/Dominio/Plan.cs` | T1: mínimo, guardar `Fin` como sus dos hermanos; completo, `PlanAbierto`/`PlanTerminado` | 🚫 **no se hace** — código de dominio de prueba, no producto |
| **Repo** · `Lectura.cs`, `Tendencia.cs`, `Urgencia.cs` | T2: exponer `Etiqueta` (una línea por tipo); el `ToString()` queda para debugging | 🚫 **no se hace** — misma razón |
| **Repo** · `GimnasioAPI.cs` | Los `print … .ToString()` pasarían a `print … .Etiqueta` | 🚫 **no se hace** — dependía del anterior |
| **Repo** · `Examenes/Dominio/` — sodio, potasio, cloro, glucosa, BUN, proteínas y sus seis series | **T3**, hallazgo de la [prueba 1](04-prueba-1-examenes.md): doce clases que difieren sólo en constantes. **Resuelto el 2026-09-07 por la decisión de especialización** —barra de activo por defecto, Curador como piso—: **se quedan como clases**; cada una declara su rango y su dirección, y las series eligen su propia `Severidad`/`Accion`/`Plazo`. E39 lo presentaba como *caveat*; acá es el default | ✅ decidido — 🗣 falta comunicárselo a Álvaro, no preguntárselo |

---

## Fuentes

- Rivera, A. (2026). *Anti-porous architecture* (v0.2-draft). Zenodo. https://doi.org/10.5281/zenodo.20404863 — §1.1, §2.1, §3, §4.1, §6, Apéndice B.
- Rivera, A. (2026). *Program–value separability*. https://doi.org/10.5281/zenodo.20740697 — §1.1, §4.
- Rivera, A. (2026). *Reactions and the partition*. https://doi.org/10.5281/zenodo.20792156 — §5.1, §9.2.
- Rivera, A. (2026). *Most infrastructure layers are symptoms of the persistence model*. https://doi.org/10.5281/zenodo.20317450 — §8 (línea 307).
- Rivera, A. (2026). *Inference without Authority*. https://doi.org/10.5281/zenodo.21499637 — §4, §5, §6.
- Papers 4, 5 y 7 revisados por barrido de términos; no tocan los hallazgos.
- Guía de dominios del equipo (`puppeteer-domain-modeling`): E2, E24, E29, E32, E39.
