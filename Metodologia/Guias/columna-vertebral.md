# La columna vertebral — las cuatro pasadas

Cómo se construye el esqueleto de un dominio que **agrega valor**: las cosas que el papel trae con su
escala, la grilla que separa lo que pasó de lo que alguien quiere y de la comparación entre los dos,
los juicios firmados, y qué merece ser una clase. Es lo que se hace **entre** la entrevista y el
código.

**Con qué llegás:** dos cosas, y las dos en la mesa. **El papel que la institución le manda a la
persona** —la hoja del laboratorio, el estado de cuenta, el plan del gimnasio, el recibo municipal— y
**la descripción funcional** de la app, en prosa, aunque sea vaga.

**Con qué salís:** el `Preguntas.md` del negocio con sus dos primeras partes llenas —lo que el modelo
sabe, con dónde vive cada cosa; y los juicios, cada uno con firma o con dueño—, la lista de clases con
**la razón** de cada una, un `HUECOS.md` con lo que nadie decidió, y una **hoja de firmas**.

> **La idea que ordena las cuatro pasadas, en una frase:** lo que el papel trae se puede copiar; **lo
> que vale no está en el papel** — hay que preguntarlo, y nadie lo puede inventar. Por eso hay pasadas
> que se pueden apurar y una que no.

**Cómo se relaciona con las otras guías:**

| Guía | Qué papel juega acá |
|---|---|
| [Punto de partida](punto-de-partida.md) y su [hoja](hoja-de-la-entrevista.md) | Son **el guion** de lo que se pregunta en las pasadas 1 y 3. Esta guía no repite esas preguntas: dice qué se hace con las respuestas |
| [Capítulo 4](04-requerimientos-que-agregan-valor.md) | El porqué del valor: la serie, los cruces, la ausencia. Esta guía lo da por leído |
| [Capítulo 3](03-construir-la-app.md) | Construir. Empieza cuando esta guía termina |
| [Glosario](glosario.md) | Casi todo lo que esta guía nombra tiene un nombre publicado; el glosario dice cuál, de dónde, y si ya lo adoptamos |

---

## 0. Antes de empezar · ¿qué hace tu dominio?

No preguntes si *"se parece al laboratorio"*. Preguntá cuatro cosas, porque cada una trae una forma
distinta y casi todos los dominios necesitan más de una:

| ¿El dominio…? | Ejemplo | Lo que va a necesitar |
|---|---|---|
| **mide** algo de la persona a lo largo del tiempo | el colesterol, la cintura, el gasto del mes | un catálogo de qué se mide, la medición con su fecha, y un rango contra el que leerla |
| **prescribe** algo que después se cumple o no | el plan del gimnasio, el calendario de cobro | lo prescrito, lo que pasó, y la comparación entre los dos |
| deja que la persona **declare hacia dónde quiere ir** | bajar de peso, apartar plata | la meta declarada, y el movimiento leído contra ella |
| registra un **intercambio** entre dos partes | el pago, la compra | lo que cada parte puso |

Marcá las que apliquen. El gimnasio tiene las cuatro; el laboratorio, dos. Eso ya te dice cuántas
columnas de la grilla vas a tener llenas.

---

## 1. El mapa

| | Pasada | Qué entra | Qué sale | ¿Se puede apurar? |
|---|---|---|---|---|
| **1** | **El espejo** | El papel, campo por campo | La lista de lo que el papel trae, cada cosa con su escala, y los verbos | Sí — es la más barata; nadie dice *"no sé"* |
| **2** | **La grilla** | La lista | Cada cosa en una de seis casillas, y cuál columna quedó vacía | Sí — la hace quien modela |
| **3** | **Los juicios** | La descripción funcional y una conversación | Un juicio por cada cosa que se sigue en el tiempo, con firma o con dueño | **No.** Es la única que no se puede derivar de nada |
| **4** | **La especialización** | Los juicios | Qué merece clase propia y qué es una fila del catálogo, con la razón | Sí — con los juicios en la mano es casi mecánica |

<!-- regla: el-orden-no-se-negocia -->
**El orden no se negocia, y la pasada 3 es la razón.** No podés decidir si una fila merece clase hasta
saber si tiene comportamiento propio — y el comportamiento **son** los juicios. Especializar antes de la
3 es adivinar.

---

## 2. Pasada 1 · El espejo

<!-- regla: el-papel-en-la-mesa -->
**Se hace con el papel en la mesa.** No una descripción del papel: el papel. Las preguntas son las del
bloque 1 de la [hoja](hoja-de-la-entrevista.md) —*¿esto qué es, en tus palabras?*, *¿viene siempre?*—
más cuatro que esta pasada agrega porque las corridas las hicieron falta:

| Por cada campo, además | Qué caza |
|---|---|
| *¿En qué escala vive?* | Si es un número continuo, un entero, un sí/no, una de un conjunto cerrado, texto libre. **Dos cosas que viven en escalas distintas son cosas distintas aunque las dos sean números** — el conteo de glóbulos en millones y el colesterol en decimales no son el mismo tipo. Tiene nombre publicado: [espacio de valor](glosario.md#adoptados) |
| *¿Significa lo mismo en todas las filas?* | En el recibo municipal, `Periodo` era *mes* en unas filas y *número de trimestre* en otras. Hicieron falta seis papeles para estar seguros. Un importador que no lo vea carga abril donde dice cuarto trimestre |
| *¿Este papel es una foto o un hecho?* | La consulta del portal es el estado de hoy, y mañana no existe; el comprobante ocurrió y tiene número. **La historia de la app sale de los hechos** |
| *¿Hay algo escrito en el sobre y no en el papel?* | El modo del pago —vencido, pendiente, anual— no estaba en el recibo: estaba en el nombre del archivo |

<!-- regla: los-verbos-con-dos-fuentes -->
**Y una más por cada campo: *¿qué hizo alguien para que esto exista?*** Eso da **el verbo** — el
laboratorio *midió*, el gimnasio *prescribió*, la municipalidad *puso al cobro*. Anotá la lista de
verbos al lado de la de campos. **Y tiene dos fuentes:** los verbos de la institución salen del papel;
los de la persona —*declaró su sexo*, *apartó plata*, *tiene una finca*— salen de la conversación y no
están en ningún papel. Anotar sólo los primeros deja al dominio sin la mitad.

<!-- regla: los-papeles-siguen-llegando -->
**Los papeles siguen llegando después de esta pasada**, y está bien. En la municipalidad llegó uno en la
pasada 1, uno en la 2 y cinco a mitad de la 2, y cada uno cambió algo. Cada papel nuevo reabre esta
pasada para sus campos.

<!-- regla: dibuja-el-espejo -->
**Dibujá lo que el papel trae, tal cual** — una caja por cosa, con sus campos. Ese dibujo es **el
espejo**: si al final el modelo se le parece, no agregaste nada. Se guarda para compararlo en la pasada
4. Tiene varios nombres en la literatura, ninguno halagador — están en el [glosario](glosario.md#adoptados).

<!-- regla: el-papel-manda-que-existe -->
**El espejo se copia entero; el modelo no.** Esta pasada lee **todo** lo que el papel trae, sin filtrar
— es lo barato, y es la línea base. Pero **el papel manda qué existe; la persona manda qué entra**: en
la pasada 3 un campo que ninguna pregunta necesita **se anota y no entra**. Salió del gimnasio v3, donde
Jorge sacó dos campos que el papel traía —el número de la máquina, una meta del aparato— con una
pregunta: *"¿de qué serviría para saber si estoy mejorando?"*. Lo que no vale es sacar algo porque la
institución ya lo muestra (ver la [hoja](hoja-de-la-entrevista.md), bloque 1); sacarlo porque ninguna
pregunta lo usa es la pasada 3 haciendo su trabajo.

**Terminaste cuando:** cada campo del papel está en la lista con su escala anotada, y la lista de verbos
tiene los de la institución **y** los de la persona.

---

## 3. Pasada 2 · La grilla

Acá el modelo deja de ser plano. Cada cosa de la pasada 1 va a **una** de seis casillas, que salen de
cruzar dos preguntas:

<!-- regla: los-dos-ejes -->
- **¿Existe aunque no haya datos, o es algo que pasó?** *El colesterol* existe aunque a esta persona
  nunca se lo hayan medido; *el colesterol de 280 del 4 de julio* pasó. Lo primero es **conocimiento**;
  lo segundo, **operación**. Tiene nombre publicado desde hace treinta años:
  [nivel de conocimiento / nivel de operación](glosario.md#adoptados).
- **¿Es un hecho, es algo que alguien quiere, o es la comparación entre los dos?** *Fue el martes* es
  un **hecho**. *Quiere ir tres veces por semana* es una **intención** — algo que alguien declaró. *Le
  faltaron nueve días* es un **contraste** — un hecho leído contra una intención, o contra su propia
  historia.

| | Hecho | Intención | **Contraste** |
|---|---|---|---|
| **Conocimiento** — existe sin datos | el analito, la medida, el producto, el ejercicio | la rutina tipo, el calendario del trámite | el rango de referencia, el umbral de aviso, la cadencia esperada, la dirección deseable |
| **Operación** — pasó | la lectura, la asistencia, la compra, el cobro | el plan de esta persona, su meta, su presupuesto del mes | la tendencia, el incumplimiento, la variación del mes, el vencimiento próximo |

<!-- regla: la-columna-de-contraste -->
> ### La columna de la derecha es donde está el valor
>
> **El papel trae hechos, y a veces intenciones.** Puede traer reglas de contraste —un reporte de
> laboratorio imprime los rangos al lado de cada valor— y puede traer **sus propias evaluaciones** —el
> gimnasio manda *"en mora"*, el laboratorio marca *H* o *L*—. Lo que **nunca** trae es una evaluación
> **sobre la historia de la persona** —*"viene bajando"*— ni **contra lo que ella quiere** —*"le
> faltaron nueve días"*—. La institución evalúa contra sus reglas, en un punto; la app evalúa sobre la
> serie y contra lo declarado.
>
> Así que **la casilla que garantiza que el modelo no es un espejo es *operación / contraste***. Si esa
> casilla queda vacía, el modelo es un listado con mejor tipografía, por bien escrito que esté.
>
> Y cuando la institución manda su evaluación, **eso llega como hecho**: la app la registra, no la
> recalcula. El gimnasio es la autoridad sobre su mora; la app no vuelve a cruzar cobros contra pagos
> para discutírsela.

<!-- regla: contraste-computado-no-journaleado -->
**Los contrastes se calculan, no se guardan.** *Cómo vengo* calcula la tendencia sobre la serie cada vez
que se pregunta; no la guarda. Están en una columna al lado de los hechos porque tienen otro **papel**,
no porque se persistan igual.

<!-- regla: contraste-dominio-o-superficie -->
**Y hay que decidir si un contraste va al dominio o a la superficie**, porque los dos lugares son
legítimos:

| Va al **dominio** si… | Va a la **superficie** si… |
|---|---|
| es una noción que el negocio **nombra y sobre la que decide** — *"viene bajando"*, *"es un hallazgo"*, *"le faltaron nueve días"* | es un **recorte para una vista** — *"los últimos cinco"*, *"el subtotal"*, *"las líneas con descuento"* |
| tiene una regla propia en el nivel de conocimiento — un umbral, una cadencia | no tiene regla: es filtrar o sumar lo que ya está |

<!-- regla: contraste-no-se-renderiza -->
Lo que un contraste **no** hace en ningún caso es renderizarse solo: el dominio expone la hoja —el
estado, el nombre, la etiqueta— y la superficie decide qué imprime. Un `ToString()` como única salida es
el dominio decidiendo por la superficie
([regla 13 del substrato](reglas-del-substrato.md#13--el-print-emite-valores-no-material)).

<!-- regla: nombrar-no-es-renderizar -->
> **Nombrar no es renderizar, y por eso el capítulo 0 y esta guía no se contradicen.** El
> [capítulo 0](00-arquitectura-clientecentrica.md) dice que el dominio *"arma su propia lectura"* —*"viene
> bajando"*, *"lleva 15 días sin aparecer"*—: eso es **nombrar la hoja**, producir el veredicto que sólo
> el dominio sabe decir. Esta guía dice que el contraste no se renderiza solo: eso es **no componer la
> vista** —qué se muestra, cómo se formatea, para quién—. El dominio produce el valor con nombre; la
> superficie arma la frase final. Un tipo que sólo se deja leer por `ToString()` colapsa las dos: obliga
> al dominio a renderizar porque no dejó ninguna hoja para elegir.

<!-- regla: la-app-propone-la-persona-adopta -->
> ### Cuando la intención no está declarada: la app propone, la persona adopta
>
> La fila de intención puede quedar vacía — nadie declaró cuánto quiere apartar por mes. La app no puede
> inventar la declaración. Lo que sí puede es **calcular una propuesta** y ofrecerla. Eso son **dos
> cosas, en dos casillas**: la propuesta es un **contraste** —un número que la app calculó, que se
> recalcula y no se guarda—; lo adoptado es una **intención** —algo que la persona hizo suyo, un comando
> con fecha—. Colapsarlas en una sola cosa con un *"adoptada: sí/no"* haría que un número de la app
> pasara por decisión de la persona.

**Terminaste cuando:** toda cosa de la pasada 1 quedó en **una** casilla, y la columna de contraste
tiene **al menos una** en cualquiera de sus dos filas. Si está vacía, volvé a la descripción funcional:
ahí está la pregunta que el portal no contesta.

---

## 4. Pasada 3 · Los juicios

**Es la única que no se puede derivar, y por eso la única que no se puede apurar.**

Por cada cosa que la app va a **seguir en el tiempo**, se declara un juicio. **Las preguntas son las
siete del bloque 3 de la [hoja](hoja-de-la-entrevista.md)** —dirección, identidad del hecho, ritmo,
ruido, alcance, urgencia, lo propio— y ahí está también cómo se hacen sin agotar a la persona:
**proponiendo un valor, no preguntando en frío**. Esta guía no las repite. Lo que agrega:

<!-- regla: la-forma-de-un-juicio -->
**Escribí cada juicio en una forma fija:** *dirección de mejora + medida + objeto + contexto*.
*"**Reducir** la **cantidad de días** entre **una medición y la siguiente** **cuando el valor está fuera
de rango**."* Obliga a que sea medible y a que no traiga la solución adentro. Tiene nombre publicado:
[la forma de un juicio](glosario.md#adoptados).

<!-- regla: identidad-en-dos-niveles -->
**Una de las siete preguntas tiene un gemelo que aparece recién al construir: la identidad del hecho.**
En la pasada 3 se pregunta por *magnitud* —*un salto del 80% probablemente no es el mismo cobro*—. Al
atar el journal, la misma pregunta vuelve como *comando* —*el mismo recibo dos veces es el mismo hecho*—,
y eso pide un `Check` por identidad:
[la regla 18 del substrato](reglas-del-substrato.md#18--todo-comando-necesita-check-incluso-el-que-no-puede-fallar).
Son la misma pregunta en dos niveles. Saltearla no se ve modelando —el dominio compila y las
comprobaciones pasan— y sólo muerde cuando un hecho llega dos veces: un replay, un feed que repite. Le
costó identidad a dos dominios seguidos: `Asistio` en el gimnasio contó 30 días dos veces, y
`AdoptarReserva` en la municipalidad entró de más. Por eso **todo comando que el dominio pueda rechazar
se guarda con un `Puede…` y un `Check` antes de correr** —la columna vertebral lo nombra acá; el
[capítulo 3](03-construir-la-app.md) y las [reglas del substrato](reglas-del-substrato.md) lo desarrollan.

<!-- regla: cuatro-reglas-de-la-pasada-3 -->
**Y cuatro reglas que esta pasada no puede romper:**

1. **No se juzga un dato por plausibilidad.** Salió de romperla: con dos papeles escribí que *"todo se
   duplicó en dos años"* era improbable. Cinco recibos después, todo se había duplicado. Cuando un
   número parece absurdo, la respuesta es *"¿hay otro papel?"*, no *"probablemente es al revés"*.
2. **No se pregunta en frío: se propone.** Umbrales, ruido y cadencia son conocimiento experto; quien
   está enfrente casi nunca los tiene. Se trae un valor —de otro negocio, de la literatura, de un
   experto— y se deja corregir. Corregir un número es fácil; inventarlo, no. Y **el ancla sesga**: un
   valor propuesto se corrige menos de lo que debería, así que proponé sabiendo eso.
3. **Lo que no se sepa entra por parámetro y se marca ⚠️ sin firmar.** El dominio se queda con la forma
   y hace la comparación; el número viene de afuera y se cambia sin tocar una clase.
4. **Un juicio sin firmar no tiene valor por defecto silencioso.** O el objeto no se construye sin él,
   o la respuesta dice que no sabe. Un default es un juicio inventado con mejores modales: nunca falla,
   y contesta creíble y equivocado cada vez.

<!-- regla: la-hoja-de-firmas -->
> ### La hoja de firmas
>
> Lo que sale de esta pasada es **una hoja de firmas**: cada juicio con su valor propuesto, de dónde
> salió la propuesta, qué decide en el modelo, y una casilla para la firma. Vive en la bitácora del
> negocio. **La persona firma lo que sabe y deja en blanco lo que no** — lo que quede en blanco entra
> por parámetro, marcado, con dueño. Ninguna casilla en blanco bloquea construir; lo que bloquea es un
> valor inventado.

<!-- regla: cada-juicio-una-pregunta -->
**Y cada juicio firmado produce al menos una pregunta que el dominio contesta** — con nombre, y sin
reventar para ninguna entrada: *¿estoy en mora?*, *¿cuántos días descontó?*, *¿viene bajando?*. Es la
prueba de que el juicio sirve para algo: si firmarlo no cambia ninguna respuesta, ¿para qué se firmó?
Un dominio que no contesta preguntas es un listado, por muchos juicios que lleve adentro.

**Terminaste cuando:** cada juicio está escrito en la forma fija, cada uno tiene **o una firma o un
dueño con nombre**, y cada uno produce **al menos una pregunta** que el dominio va a contestar. Ninguno
inventado.

---

## 5. Pasada 4 · La especialización

Con los juicios en la mesa, la pregunta *"¿una clase por fila?"* se vuelve contestable. **Y la
respuesta no es sí ni no: son dos preguntas en este orden, y una frontera.**

<!-- regla: opina-o-parametriza -->
> **1 · ¿Esta fila *opina* por sí misma?** — tiene su propio rango, su dirección deseable, elige qué
> severidad o qué acción corresponde ante un hallazgo, sabe qué necesita del perfil para leerse.
> → **Es una clase con nombre.** Aunque hoy sólo difiera de su hermana en los valores: el dominio es un
> activo que se lee y se dice, y una clase con nombre es donde el comportamiento futuro va a tener casa.
>
> **2 · El piso — si no opina, ¿se pierde algo al quitarle el nombre?** Quitá el nombre de la clase y
> movés sus constantes a datos del catálogo. Si **no se pierde nada** —la fila no sabe nada de sí misma—
> es una **fila del catálogo**, no una clase. Ése es el único caso en que se colapsa. Tiene nombre
> publicado: [el test del Curador](glosario.md#en-debate).
>
> **La frontera**, que salió de probar la regla contra dos dominios: una fila que sólo **parametriza**
> una operación que todas comparten —una cadencia, una fórmula de cobro, un monto— **no opina**.
> `Sodio` opina —tiene su rango, su dirección, y su serie elige qué severidad y qué acción— → clase.
> `Basura` parametriza —mensual, por unidad— una misma operación de cobro → `Rubro.Basura`, una fila
> del catálogo. **Un analito carga juicios; un rubro carga parámetros.** Lo que sí merece clase en la
> municipalidad es lo que opina: la cadencia mensual y la trimestral leen el período distinto y vencen
> distinto; la tarifa por unidad y la sobre base imponible explican un aumento distinto.

El tell mecánico sigue valiendo, en una dirección: si aparece *"si es de tipo X, esta lógica; si es de
tipo Y, otra"*, eso es comportamiento y **pide** clase. Lo que ya no vale es la inversa —*"sólo
constantes → colapsá"*—: una constante que es conocimiento del dominio se gana la clase; una constante
suelta sin significado propio, no.

<!-- regla: opina-vive-en-la-clase -->
**Y una segunda pregunta después de *"¿opina?"*: ¿la opinión vive en la clase, o la hicimos clase y la
dejamos afuera?** Una fila puede pasar *"opina"* en el papel —tiene una propiedad que la distingue— y que
el comportamiento que esa propiedad implica lo ejecute un tercero. El tell es mecánico: **si otra clase
hace `if (x.EsAsi) … else …` sobre tu bandera, el conocimiento no está donde la clase dice que está.** La
`LeyDeCalculo` de la municipalidad lo tenía: exponía un `RespondeAlAvaluo` y otra clase ramificaba sobre
él para elegir la explicación del aumento. Opinar de verdad es que el comportamiento viva adentro —la
`Cadencia` devuelve el `Mes` o el `Trimestre`, sin que nadie afuera pregunte `EsMensual`—. Una clase que
opina en el papel pero no en el comportamiento es una clase anémica con ropa de clase, la misma porosidad
que la pasada 4 quiere evitar (guía de dominios, **E40** y **E24**).

<!-- regla: comparaciones-guardan-el-tipo -->
**Cuando un tipo abstracto lleva varias comparaciones, todas guardan que los dos lados sean del mismo
tipo — no sólo algunas.** Y la respuesta al desajuste depende de la operación: la **igualdad** entre
clases distintas es un *no* honesto —`false`—; el **orden** entre clases distintas no existe —*¿abril es
anterior al segundo trimestre?*— y preguntarlo es un bug de quien llama, así que **revienta**, no
devuelve `false`. En la municipalidad `EsElMismoQue` guardaba el tipo y `EsAnteriorA` no, y ordenaba dos
alfabetos por el mismo primitivo. No lo agarró ningún test —el único camino que llega ahí ya fuerza el
mismo tipo—: salió leyendo las dos comparaciones lado a lado. Por eso va con su **control negativo**: una
comprobación que afirme que la comparación cruzada revienta.

<!-- regla: fase-no-es-subtipo -->
> **Y un control que ninguna de las dos preguntas hace: ¿una instancia puede *dejar de ser* esto sin
> dejar de existir?** Si sí, es una **fase**, no un tipo. Una membresía entra y sale de *en mora*; una
> cuenta entra y sale de *morosa*. Eso pasa las dos preguntas de arriba —carga la regla de la mora— y en
> código es imposible: la instancia tendría que cambiar de clase. Una fase va como **estado
> calculado**, en la columna de contraste, con las operaciones que dependen de él gateadas — no como
> subclase. El glosario lo tiene como [rigidez](glosario.md#en-debate), en debate.

**Terminaste cuando:** cada clase **opina** por sí misma —un rango, una dirección, una severidad, un
cálculo que explica—; la que sólo parametriza se colapsó a fila del catálogo; y ninguna clase es una
fase. Y ahora sí: **poné el dibujo del espejo al lado del modelo. Si se parecen, el problema está en la
pasada 3.**

---

## 6. Cuándo se deja de iterar

Todas las metodologías dicen *"es iterativo"* y ninguna dice cuándo parar. Ésta sí: **cuatro
preguntas, y las cuatro tienen que dar que sí.**

<!-- regla: las-cuatro-preguntas-de-parada -->
| | La pregunta | Si da que no |
|---|---|---|
| **1 · Completitud** | ¿Cada dato que una pregunta necesita tiene origen — un campo del papel **o** un juicio declarado? **Y al revés:** ¿cada campo que entró lo necesita alguna pregunta? | Descubriste un campo faltante, un evento faltante o **una decisión faltante**. Va al `HUECOS.md`. Al revés: un campo que nadie usa — se anota y sale |
| **2 · No-espejo** | ¿La columna de contraste tiene al menos una cosa, y ninguna venía en el papel? | Volvé a la descripción funcional |
| **3 · Justificación** | ¿Cada clase opina por sí misma, y ninguna es una fase disfrazada? | Volvé a la pasada 4 |
| **4 · Huecos con dueño** | ¿Todo lo que quedó sin decidir está **escrito y con dueño**? | Escribilo. Un `HUECOS.md` vacío en un dominio joven es sospechoso, no admirable |

**La 4 es la que hace que el proceso termine.** *Satisfecho con el dominio* no significa *sin preguntas
abiertas*: significa que las preguntas abiertas están a la vista y tienen a quién preguntarle.

---

## 7. Si lo corre un agente

Las pasadas 1, 2 y 4 se pueden automatizar bastante. **La 3 no**: es elicitación, y su costo es el que
es. Un ciclo donde un agente construye y otro revisa se ve así, y **se detiene en la pasada 3 para que
la persona firme**:

<!-- regla: el-contrato-de-cada-pasada -->
| Pasada | El constructor recibe | Tiene que producir | Tiene **prohibido** |
|---|---|---|---|
| **1** | El papel, literal | La lista con escalas y verbos, y el dibujo del espejo | **Inventar cualquier campo que no esté en el papel** |
| **2** | La lista | La grilla de seis casillas | Decidir clases. **Y llenar la columna de contraste por su cuenta** — si no encuentra qué poner, lo dice, no lo inventa |
| **3** | La descripción funcional y las respuestas | La hoja de firmas: juicios en forma fija, con valor propuesto | **Inventar un umbral.** Lo que no le dijeron va sin firmar y a la vista |
| **4** | Los juicios firmados | La lista de clases con la razón de cada una | Crear una clase que no pase el piso |

**El revisor** recibe lo que el constructor produjo, esta guía y el glosario, y **marca con la regla
citada** — nunca corrige en silencio. Después de la pasada 4 corre las cuatro preguntas de parada.

<!-- regla: la-clausula-de-tope -->
**Y una cláusula de tope:** si después de **tres vueltas** el revisor marca lo mismo, el hueco va al
`HUECOS.md` **con dueño** y el ciclo para. Sin eso, un juicio que nadie firma es un bucle infinito.

**La prohibición de la pasada 3 es la que sostiene todo el método.** Un agente que rellena un umbral
razonable produce un modelo que compila, corre, y miente.

---

## 8. Cuándo terminaste

- [ ] ¿Se leyó **el papel campo por campo**, y cada campo tiene su escala?
- [ ] ¿La lista de verbos tiene los de la institución **y** los de la persona?
- [ ] ¿Toda cosa de la pasada 1 quedó en **una** casilla de la grilla?
- [ ] ¿La columna de contraste tiene al menos una cosa que **no venía en el papel**?
- [ ] ¿Cada juicio está en la forma fija, y tiene **firma o dueño**? ¿Ninguno inventado, ninguno con
      default silencioso?
- [ ] ¿Cada juicio produce **al menos una pregunta** que el dominio contesta, con nombre y para
      cualquier entrada?
- [ ] ¿Cada clase **opina** por sí misma? ¿Ninguna es una fase?
- [ ] ¿Pusiste el espejo al lado del modelo, y **no** se parecen?
- [ ] ¿Las cuatro preguntas de parada dan que sí?
- [ ] ¿Lo leyó y lo aprobó quien conoce el negocio, sin que nadie le tradujera nada?

---

## 9. Qué sigue

| Si venís a | Seguí por |
|---|---|
| Construir con esto en la mano | [Capítulo 3](03-construir-la-app.md) |
| Ver el porqué de cada pasada, con las fuentes | [Investigación 02](../Investigacion/02-columna-vertebral.md) |
| Saber cómo se llama cada cosa en la literatura | [Glosario](glosario.md) |
| Ver una corrida completa, en vivo, con papeles reales | La [bitácora de la municipalidad](../Pruebas/Municipalidad/BITACORA.md) |

---

## De dónde sale esta guía

Es la versión en lenguaje llano de la doctrina que está en
[`Investigacion/02`](../Investigacion/02-columna-vertebral.md), con sus citas y con el registro de cómo
fue cambiando. Se corrió tres veces antes de escribirse como guía: contra `Examenes`, el dominio de
referencia —once de once decisiones coinciden—; contra la municipalidad, en vivo, con papeles reales
—una hora doce, dos *"no sé"*, seis juicios firmados sin corregir—; y sobre una rebanada del gimnasio
—la membresía del v2—. **Como guía se corrió dos veces el 2026-09-08**: la municipalidad, de la pasada 4
al dominio corriendo; y el **gimnasio v3**, la primera corrida completa con un papel real — veinte
minutos del papel al verde, y de ahí salió la regla *el papel manda qué existe; la persona manda qué
entra*. Ver su [bitácora](../Pruebas/Gimnasio/BITACORA.md).

Lo que no resuelve, dicho para que nadie lo descubra tarde: **no produce el modelo completo**, sólo la
columna vertebral — los casos de borde y el ciclo de vida aparecen construyendo y van al `HUECOS.md`;
**no sustituye la conversación**; y **no garantiza que el modelo sea el mejor, sólo que no es un
espejo**. Para lo demás hace falta alguien que conozca el negocio mirando el resultado — que es
exactamente para lo que el dibujo existe.
