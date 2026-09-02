# Punto de partida — con qué se arranca

El primer paso del trabajo, antes de cualquier capítulo numerado y antes de una línea de código.

Sale con un archivo: **`Preguntas.md`**, en el directorio del negocio. De ahí en adelante todo
depende de él — el capítulo 3 lo usa como insumo para construir, y la auditoría lo usa como contrato
para decir si la app hizo lo que dijo.

---

## 1. Nadie se salta este paso, y menos el que sabe programar

La tentación es obvia: leer la arquitectura, entender cómo se arma un módulo, abrir el IDE y empezar
a escribir clases. Es el camino de menor resistencia para alguien técnico.

> **Corregido el 2026-09-01, porque este capítulo afirmaba algo que el repo no sostiene.** Decía que
> ese camino *"produce exactamente el fracaso que esta Metodología trata de evitar — una app
> impecablemente construida que no contesta nada que la persona no tuviera ya"*.
>
> **Los cuatro negocios se saltaron este paso entero, y los cuatro tienen su pregunta sobre una
> ausencia.** Medido en los nombres de sus tools: `que_me_falta_medir`, `que_dejaste_de_comprar`,
> `garantias_por_vencer`, `lo_que_falta_para_calcular`, `dias_hasta_la_proxima_carga`,
> `how_up_to_date_is_this`. Cuatro de cuatro fueron a buscarla sin que ninguna guía se las pidiera.
>
> **Lo que sí se sostiene es más chico y más cierto:** de esos cuatro, **ninguno dejó escrito lo que
> se proponía contestar**. El único `Preguntas.md` de un negocio real se escribió *después* del
> código, y aun así encontró **tres defectos en una app con 461 comprobaciones en verde**. Sin la
> intención declarada se puede leer el código, pero no hay contra qué compararlo — y la auditoría del
> paso 8 se queda sin vara.

**Ése es el primer argumento, y es el que hay que dar: este paso no te enseña a construir. Hace que
la intención quede escrita, para que la deriva se pueda ver.**

Hay una segunda razón, y es más dura. Lo que se decide acá **no es conocimiento técnico**:

- Que en la hemoglobina preocupa que **baje**, no que suba.
- Que un colesterol en rango se repite cada cinco años, y uno alto todos los años.
- Que el antígeno prostático se lee sobre el **tramo reciente**, no sobre la serie entera.

Nadie decide eso por ser buen programador. Y si lo inventa, el modelo queda plausible y equivocado —
que es la peor combinación, porque no falla: **miente**.

Así que este capítulo le dice al desarrollador técnico lo que más necesita oír: **andá a buscar esas
respuestas en vez de suponerlas.**

Lo que cambia entre un lector y otro no es si hacen este paso, sino qué papel juegan adentro:

| | El que conoce el negocio | El que va a construir |
|---|---|---|
| En la entrevista | Es el entrevistado. Aporta los juicios | Entrevista. O, si el dominio no es suyo, va a buscar a quien sabe |
| Frente a `Preguntas.md` | Lo aprueba: dice si eso es lo que pidió | Lo ejecuta: decide con qué modelo se vuelve posible |

---

## 2. Con qué llegás

**Con una descripción funcional en prosa. Nada más.** No hace falta saber qué es un WASM, ni un
journal, ni haber leído el capítulo 0.

Y está perfectamente bien que sea vaga. Ésta es una entrada válida:

> *"Somos un laboratorio clínico. Queremos darle a nuestros pacientes una app donde puedan ver sus
> exámenes."*

Eso todavía no sirve para construir nada —es un listado con otro nombre— pero **es el punto de
partida correcto**. Convertirlo en algo construible es el trabajo de este capítulo, no un requisito
para empezarlo.

Lo único que sí conviene traer:

- Qué le **contesta** hoy a la persona lo que ya existe —el portal, la app del proveedor, el papel—.
  Sirve para saber qué **no** hay que volver a empaquetar. Es distinto de qué le *entrega*: el bloque
  1 separa las dos.
- Quién, adentro de la institución, sabe **interpretar** los datos. Esa persona va a hacer falta en
  el bloque 3, y si no aparece, la entrevista se detiene ahí.

> **De dónde sale este capítulo, y qué encontró cada corrida.** Se escribió a partir de la doctrina y
> después se corrió tres veces:
>
> | | Con quién | Qué encontró |
> |---|---|---|
> | **2026-08-26** · presupuesto personal | quien escribió el guion | **Seis defectos en el guion**, los seis corregidos acá |
> | **2026-08-28** · gimnasio | un peer del equipo — la primera vez con alguien que no lo escribió | El `Preguntas.md` **sí alcanza para modelar**, pero destapó **once huecos**. De ahí salió el bloque 5. Y se perdió la membresía: ver el recuadro del bloque 1 |
> | **2026-09-01** · con una Product Owner | fuera del equipo | **La sesión es demasiado larga y se siente repetitiva**, y buena parte del bloque 3 es inconstestable por el rol. De ahí salen las correcciones del bloque 3 |
>
> Las tres corridas están reflejadas en el texto, cada una donde muerde.

---

## 3. La entrevista

> Para tener abierta durante la sesión, sin el desarrollo: [hoja-de-la-entrevista.md](hoja-de-la-entrevista.md).

**Cinco bloques, y el quinto no es del mismo día**: los cuatro primeros son la entrevista, y el
quinto se hace **después de la primera pasada de modelado**. Ese corte no es logística — es que hasta
que alguien no intenta construir la respuesta, no se ve qué falta para poder darla.

Es una conversación, no un formulario. El orden importa —cada bloque se apoya en el anterior— pero
**no es de una sola dirección**: es normal que el bloque 3 haga aparecer una pregunta que pertenecía
al 2, o que el 4 obligue a revisar una respuesta del 1. Cuando pase, se vuelve y se corrige. Un
bloque cerrado no está clausurado.

### Bloque 1 · Qué ya existe

**Primero, quién es la institución.** No lo des por sentado: en un laboratorio o un supermercado es
obvio, y hay dominios donde no. La respuesta cambia la forma de la app entera:

| | Qué implica |
|---|---|
| **Hay institución** y publica eventos | La forma completa: URL1, URL2, token, señuelo. Es la de Exámenes y Compras |
| **No hay institución** | La persona registra todo. El journal es enteramente de procedencia `TÚ`, y no hay URL2 ni token |
| **Híbrida** | La institución manda lo que puede y la persona aporta lo que la institución no puede saber |

> **El corte que importa no es «¿hay institución?» sino «¿la persona aporta algo?»**, y agarra a los
> dos últimos casos por igual:
>
> **En cuanto la persona aporta un hecho, el journal deja de ser reconstruible desde URL2.** Borrar la
> carpeta ya no es volver a bajar los eventos: es perder el trabajo de la persona, y **el respaldo
> deja de ser opcional**. Es una decisión de arquitectura que se toma en el minuto tres de la
> entrevista y después no se revisa, así que conviene decirla en voz alta cuando se toma.
>
> El caso híbrido es el más frecuente de los tres y es donde más fácil se pasa por alto, porque *"hay
> institución"* suena a que los datos están a salvo.

**Después, y son dos preguntas distintas:**

> *¿Qué le **entrega** hoy la institución: un papel, un portal, un correo?*
> *¿Y qué le **contesta**? ¿Qué pregunta suya responde sin que tenga que hacer cuentas?*

**No las mezcles.** Entregar datos no es contestar preguntas, y el filtro del bloque 2 opera sobre lo
segundo. Un banco entrega movimientos; si además ya te muestra el gasto por categoría, eso sí es una
pregunta contestada.

Lo que aparezca en la **segunda** queda descartado **como funcionalidad**: si el portal ya lo
contesta, reproducirlo no agrega valor, lo reempaqueta.

> ### Descartado como funcionalidad NO es descartado del modelo
>
> **Es la corrección más importante de este capítulo, y salió de una corrida.** El bloque 2 ya lo
> dice —*"los filtros se aplican a las preguntas, nunca a la capacidad… va a la parte 1, que no se
> filtra"*— pero está tres páginas más adelante, y la hoja que se usa en la sesión no lo llevaba. El
> resultado es que este párrafo se leía como un descarte total.
>
> **Lo que costó:** en la corrida del gimnasio (2026-08-28) la **membresía** quedó declarada en la
> Parte 1 —*"Pagos, próximo cobro, atrasos"*— y doce líneas más abajo, en *"lo que el gimnasio ya
> contesta, y por eso se descarta"*. El dominio se construyó **sin ninguna clase de membresía ni de
> pago**, y por eso hoy un período suspendido por falta de pago cuenta como incumplimiento del plan.
> Una respuesta creíble y equivocada, que es lo que esta Metodología existe para evitar.
>
> Que el gimnasio ya te diga si estás al día es razón perfecta para **no hacer una pantalla de
> pagos**. No es ninguna razón para que el dominio no sepa qué es una membresía.

### Y antes de cerrar el bloque: traé el papel

**No alcanza con saber que hay un papel. Hay que leerlo.**

> *Traé lo que la institución le manda hoy —la hoja, el correo, el estado de cuenta, el PDF— y
> leámoslo campo por campo.*

Es la pregunta más barata de toda la entrevista y la única donde **nadie contesta "no sé"**: lo que
está en la hoja está en la hoja. Y es lo único que produce la Parte 1 del `Preguntas.md` de forma
sistemática — hasta ahora esa parte se llenaba con lo que hubiera aparecido suelto en la conversación,
que es exactamente cómo se pierde un sustantivo.

Por cada campo, dos preguntas y nada más:

| | |
|---|---|
| *¿Esto qué es, en tus palabras?* | Da el nombre del sustantivo |
| *¿Viene siempre, o a veces falta?* | Da si es obligatorio, y a veces destapa una ausencia para el bloque 2 |

**Todo campo del artefacto entra al modelo**, lo conteste ya el portal o no. Lo que se decide después
es qué se muestra y qué se pregunta, no qué se sabe.

### Bloque 2 · Las preguntas

> *Olvidate de la app. **¿Qué diría esta persona en voz alta**, hablando de lo suyo?*

No *"listar exámenes por fecha"*. Sino *"¿me está subiendo el colesterol o fue una mala lectura?"*.

Se insiste hasta juntar tres a cinco. Y a cada una se le pasan tres filtros:

| Filtro | Si falla |
|---|---|
| ¿La respuesta **cambia lo que la persona hace**? | Es información decorativa |
| ¿Alcanza con **un solo dato**, o hace falta la historia? | Si alcanza con uno, el proveedor probablemente ya se lo daba |
| ¿El portal **ya se la contesta**? | Volviste al bloque 1 |

> **Los filtros se aplican a las preguntas, nunca a la capacidad.** Es el error más fácil de cometer
> y descarta cosas imprescindibles. La clasificación de gastos, por ejemplo, la hace hoy la app de
> muchos bancos — y aun así el modelo la necesita, porque sin ella no se puede contestar *"¿se
> incrementó un gasto fijo?"*. No es una pregunta que la app conteste: es lo que le permite contestar
> las otras. Va a la parte 1 del `Preguntas.md`, que no se filtra.
>
> Y suele haber un segundo argumento escondido: **no es la misma capacidad.** El banco clasifica por
> comercio; la persona clasifica por **propósito**. Un cargo a una farmacia puede ser salud o un
> regalo, y eso el banco no lo puede saber nunca.

> **El primer filtro no pesa lo mismo en todos los dominios.** En salud, *"¿le sirve a la persona en
> este dominio?"* rechaza el precio del examen de un plumazo. En presupuesto personal no rechaza casi
> nada, porque el dominio **es** la plata — y todo el peso cae en los otros dos filtros. Si al llegar
> al bloque 4 no hay casi nada descartado, no es que la entrevista salió mal: es este efecto.

**Una de las preguntas tiene que ser sobre una ausencia.** *"¿Hace cuánto que no me mido esto?"*,
*"¿qué me falta?"*, *"¿se me venció algo?"*. Es la clase de pregunta que ninguna pantalla puede
contestar —una pantalla renderiza lo que tiene— y si la lista no tiene ninguna así, lo que se está
diseñando es probablemente un listado con mejor tipografía.

### Bloque 3 · Los juicios

Éste es el bloque que no se puede saltar ni improvisar, y el que requiere a quien sabe del negocio.
Para cada magnitud que apareció en el bloque 2 — un analito, un gasto recurrente, una categoría de
compra, lo que sea que este dominio siga a lo largo del tiempo:

> **1 · Dirección.** *¿Hacia dónde es deseable que se mueva? ¿Subir, bajar, o mantenerse dentro de
> algo?* Ojo con asumir que es binario: hay magnitudes a las que alejarse les es malo en las dos
> direcciones.
>
> **2 · Identidad del hecho.** *¿Qué cambio es tan grande que lo más probable es que **no sea la
> misma cosa**?* En un laboratorio eso apunta a un error de medición. En un presupuesto el monto
> nunca está mal, pero un cargo doce veces mayor probablemente sea el pago anual y no el mensual que
> se disparó. **La pregunta es la misma; lo que revela cambia según el dominio.**
>
> **3 · Ritmo esperado.** *¿Cada cuánto se espera que esto vuelva a ocurrir? ¿Es fijo, o depende del
> último valor?* En salud es cada cuánto repetir la medición; en presupuesto, cada cuánto debería
> aparecer el cargo. **De esta respuesta sale la pregunta sobre una ausencia**, así que si quedó
> vaga, la app no va a poder detectar lo que falta.
>
> **4 · Ruido.** *¿Qué variación es normal y cuál merece que se avise?*
>
> **5 · Alcance de la vigilancia.** *¿Se vigila cada cosa por separado, o el conjunto?* Vigilar cada
> una con su propio umbral suena más fino y suele ser peor: con doce cosas vigilándose, algo dispara
> todos los meses y a los tres nadie mira la app. A veces lo correcto es vigilar el total y bajar al
> detalle sólo para explicar un movimiento.
>
> **6 · Urgencia.** *¿Qué hace que un hallazgo sea urgente y otro no — y lo decide el tamaño del
> número, o la clase de cosa?* En los dos dominios donde se corrió esto la respuesta fue **la clase**:
> un recibo de luz que no llegó es un aviso, una cuota de préstamo es otra cosa, y no por el monto.
>
> **7 · Lo propio.** *¿Qué sabe **esta** magnitud que ninguna otra sabe?*

Esa última pregunta es la más productiva de todo el capítulo, porque decide qué merece ser una clase
propia. En el laboratorio, la hemoglobina sabe el **grado de anemia**; no existe el grado de anemia
de un colesterol. Cuando una magnitud no tiene respuesta a esa pregunta, probablemente no necesitaba
existir por separado.

**Si el entrevistado no sabe contestar un juicio, no se inventa.** Se anota como pendiente **con
nombre y apellido de quién lo sabe**, y se va a buscar a esa persona. Un juicio inventado no se nota
hasta que alguien toma una decisión con él.

> ### Pero no se pregunta en frío: se propone y se deja corregir
>
> **Corregido el 2026-09-01, después de correr este bloque dos veces.** Tal como estaba escrito, este
> bloque es el más caro de la sesión y el que menos rinde, por dos razones que se midieron:
>
> **Escala mal.** Son siete preguntas **por cada magnitud**. En el gimnasio las magnitudes fueron
> seis, o sea **42 preguntas con la misma forma** dentro de una sentada de unas 64. Para quien
> contesta, la misma pregunta volviendo seis veces con otro sujeto es indistinguible de la
> repetición, y así se reportó las dos veces que se corrió.
>
> **Y un "no sé" cuesta más que un "sí".** Una respuesta cierra; una no-respuesta abre una pregunta
> nueva —*¿y quién lo sabe?*—. En una sesión donde la mayoría de este bloque es inconstestable, el
> guion gasta más tiempo en lo que no produce que en lo que sí.
>
> **Es inconstestable casi siempre, y no por culpa de nadie:** umbrales, ruido, cadencia y urgencia no
> son conocimiento de producto, son conocimiento **experto del dominio**. Un Product Owner no los
> tiene, y tenerlos no era su trabajo.
>
> **Cómo se hace entonces** —y salió del gimnasio, donde funcionó sin que nadie lo hubiera escrito:
>
> 1. **Quien modela propone un valor**, con la mejor fuente que tenga a mano —el precedente de otro
>    negocio, un experto, literatura del dominio—. Es la misma regla del bloque 5: *no preguntes
>    "¿cuál es el umbral?", nadie tiene un umbral en la cabeza; proponé uno y dejá que lo muevan.*
> 2. **El valor entra por parámetro, nunca quemado en el código.** El dominio se queda con la forma y
>    hace la comparación; el número viene de afuera y se puede cambiar sin tocar una clase.
> 3. **Se marca ⚠️ sin firmar** hasta que lo apruebe quien sabe. La firma es asincrónica: no bloquea
>    la sesión ni bloquea construir.
>
> Lo que **no** cambia: un juicio sin firmar **no tiene valor por defecto silencioso**. O el objeto no
> se construye sin él, o la respuesta **dice que no sabe**. Un default silencioso es un juicio
> inventado con mejores modales — ver el [capítulo 3](03-construir-la-app.md) §3.

> ### Y una regla de corte, para la sesión en vivo
>
> **Cuando vengan tres *"no sé"* seguidos, anotá los tres con su dueño y seguí.**
>
> Se deduce de lo de arriba pero hay que decirlo, porque en la mesa la tentación es la contraria:
> insistir, reformular, buscarle la vuelta. Y no hay vuelta que buscarle — si el conocimiento no está
> en la sala, no va a aparecer por preguntar distinto, y cada intento gasta el tiempo que necesitan
> los bloques que sí se pueden contestar.
>
> Lo que se pierde insistiendo se recupera solo: **este bloque vuelve en el 5**, con valores
> propuestos sobre la mesa, y ahí la misma pregunta rinde mucho más — porque corregir un número es
> fácil e inventarlo no.

### Bloque 4 · Qué se descarta

> *¿Qué dato te parece importante pero, siendo honesto, no le sirve a la persona **en este dominio**?*

El caso canónico: el **precio del examen**. Es real, está en la factura, y no aporta nada a la salud.
Pero no sobra — **pertenece a otro dominio**, y en Presupuesto Personal es de los datos más
relevantes. Va a la lista de candidatos a cruce, no a la basura.

### Bloque 5 · La revisión, después de modelar

**Una sola pasada nunca alcanza.** Modelar siempre destapa lo que la entrevista no preguntó — y no
porque la entrevista esté mal hecha: es que hasta que alguien no intenta construir la respuesta, no se
ve qué falta para poder darla.

Va **después de la primera pasada de modelado**, y son quince minutos, no otra entrevista.

> **Se entra con el `HUECOS.md` en la mano, y ése es todo el guion.** Es el registro que quien modela
> fue escribiendo mientras construía —lo define el [capítulo 3](03-construir-la-app.md) §8 bis— y
> trae las dos clases de cosa que este bloque necesita: lo que quedó **declarado sin firmar** en la
> entrevista, y lo que apareció **descubierto** al escribir las clases.
>
> No hace falta preparar preguntas: **cada renglón del archivo es una**, y hay que reformularla según
> la regla de abajo antes de decirla en voz alta. Si el `HUECOS.md` está vacío, o no se modeló, o no
> se anotó mientras se modelaba — nunca es que no hubo huecos.

> **La regla que gobierna este bloque, y es la que más fácil se rompe:**
>
> **La persona contesta sobre el negocio. Quién se vuelve una clase lo decide quien modela.**
>
> La prueba es literal: **¿podría contestarla alguien que nunca programó?** Si la pregunta contiene
> *clase*, *tipo*, *entidad*, *dominio* o *sustantivo*, está mal formada.

#### Por qué esto se rompe tan fácil

Porque para este punto **quien modela ya sabe qué le falta**, y lo que le falta tiene forma de
decisión de diseño. La tentación es preguntarla tal cual — *"¿el préstamo es una entidad?"*— y suena
razonable de este lado del escritorio.

| Mal formada | Bien formada |
|---|---|
| ¿Qué **clases** de ingreso hay y qué sabe cada una? | *Si te pregunto cuánto vas a recibir el mes que viene, **¿de qué estarías seguro y de qué no?** ¿Cuáles llegan solos y cuáles hay que salir a buscarlos?* |
| ¿Es el préstamo una **cosa del dominio**, o alcanza con su cuota? | *¿Te preguntás alguna vez cuánto te falta para terminar de pagar algo? ¿Te importa cuánto llevás pagado de intereses?* |
| ¿Lo que se aparta **es algo** con saldo, o sólo un número? | *Cuando te pasa un imprevisto, **¿de dónde sale la plata?*** |
| ¿Quién **fija** el presupuesto, por categoría o total? | *Cuando decís que un gasto "no estaba presupuestado", **¿contra qué lo estás comparando?*** |

Las de la derecha se contestan sin saber nada técnico, y **de sus respuestas se deduce lo mismo** —
esa es la prueba de que la de la izquierda era innecesaria.

#### Las tres cacerías

**1 · Una palabra apareció en las preguntas y nadie la desarrolló.**

Es la que más rinde, y se detecta mecánicamente: recorrer las preguntas y marcar cada sustantivo que
no tiene nada detrás. En un dominio de presupuesto personal, *"el alquiler que recibo"* estaba en la
pregunta 2 desde el primer día y **no había ningún tipo de ingreso en todo el modelo** — porque las
preguntas dirigieron los juicios, y nadie preguntó por el vocabulario.

> *¿Qué te preguntás sobre **X** que no te preguntás sobre **Y**?*

Ésa es *"¿qué sabe esto que ninguna otra cosa sabe?"* dicho en idioma de negocio, y es la más
productiva de la sesión.

**2 · Al modelar hizo falta un dato que nadie mencionó.**

> *¿De dónde sale…? ¿Eso viene en lo que te manda la institución, o lo sabés vos?*

Hay un barrido barato que conviene hacer antes de preguntar: **mirar qué manda realmente la
institución.** Un estado de cuenta bancario trae débitos **y créditos**; si el modelo sólo tiene
gastos, le falta la mitad de lo que ya está entrando, y eso no requiere entrevistar a nadie para
verlo.

**3 · Se tomó un juicio por obvio.**

Todo lo que quien modela decidió solo se **lee en voz alta como propuesta**, y se deja corregir:

> *Dejamos que un cargo que no llega se avise a los tres días. ¿Te suena bien, o depende de cuál?*

No preguntes *"¿cuál es el umbral?"*: nadie tiene un umbral en la cabeza. **Proponé uno y dejá que lo
muevan** — corregir es mucho más fácil que inventar, y lo que corrijan es información.

#### Y una advertencia sobre el entrevistado

**Si contesta todo rápido y sin dudar, sospechá.** Una persona generosa contesta igual una pregunta
mal formada — deduce qué le quisiste preguntar y responde a eso. El resultado es que **la pregunta
mal formada parece funcionar**, y el defecto queda escondido hasta que le toque a alguien menos
paciente.

---

## 4. Qué sale

Un `Preguntas.md` en el directorio del negocio, con tres partes que **no se enumeran igual**:

| Parte | Qué declara | ¿Se enumera? |
|---|---|---|
| **Lo que el modelo sabe** | Dimensiones, grano, atributos | **No.** Se declara la capacidad; las preguntas se componen encima |
| **Los juicios del dominio** | Los del bloque 3 | **Sí, y es lo importante.** No se componen ni se infieren: existen sólo si alguien los decidió |
| **Lo que la UI muestra** | Las pantallas | **Sí, pero es lo barato.** Una pantalla se cambia en una tarde |

Más lo rechazado (con su motivo, para que la discusión no se repita en tres meses) y los candidatos a
cruce.

Esa asimetría es el corazón del asunto. **No se enumeran las preguntas: se enumeran los juicios que
hacen que ciertas preguntas tengan respuesta.** Un modelo al que se le declaró la capacidad contesta
cosas que nadie previó; un modelo al que no se le declaró un juicio no lo puede inventar después.

> ### La Parte 1 lleva una columna que dice dónde vive cada renglón
>
> Un renglón por sustantivo, y **al lado dónde quedó en el dominio** — se llena **al modelar**, no en
> la entrevista:
>
> | | Qué es | Dónde vive |
> |---|---|---|
> | **El plan** | Lo que el gimnasio le indica hacer, con su frecuencia | `Plan` |
> | **La asistencia** | Los días que fue | `Trayectoria.Asistio` |
> | **La membresía** | Pagos, próximo cobro, atrasos | *(vacía)* |
>
> **No es «una clase por renglón», y conviene decirlo fuerte porque es el error obvio.** Un renglón
> puede resolverse con una clase, con varias, con **un miembro de otra clase** —la asistencia es una
> lista adentro de la raíz— o con **un conjunto cerrado o una tabla de valores**, como el repertorio.
> Exigir una clase por renglón presiona a inventar clases sin nada propio, que es justo lo que el
> capítulo 4 llama porosidad.
>
> **Lo que no puede quedar es vacío.** Un renglón sin nada al lado es un sustantivo declarado que no
> está en ninguna parte — **exactamente lo que pasó con la membresía del gimnasio**, y nadie lo vio
> porque no había dónde mirarlo.
>
> Con la columna se ve al primer vistazo, y además vuelve chequeable el
> [observable 13](observables.md): sin ella el chequeo tendría que adivinar, y *un `grep` no es un
> observable*. Escribirla es también el momento en que quien modela **se ve obligado a mirar los
> renglones uno por uno**, que es donde el defecto se caza gratis.

El capítulo 4 desarrolla cómo se escribe cada parte, y trae el ejemplo trabajado completo.

---

## 5. Cuándo terminaste

- [ ] ¿Se leyó **el papel campo por campo**, y cada campo quedó en la Parte 1? Es la casilla que
      verifica el paso más barato del bloque 1, y el único donde nadie contesta *"no sé"*
- [ ] ¿Hay tres a cinco preguntas, cada una en la voz de la persona y no en la de un sistema?
- [ ] ¿Al menos una es sobre una **ausencia**, que ninguna pantalla podría mostrar?
- [ ] ¿Cada juicio del bloque 3 tiene respuesta, o está anotado como pendiente con **nombre y
      apellido** de quién la tiene?
- [ ] ¿Está escrito lo que se **descartó**, y por qué?
- [ ] ¿Los candidatos a cruce quedaron anotados en vez de forzados adentro?
- [ ] ¿**Cada palabra** que aparece en las preguntas tiene algo detrás? Es el que más se salta, y el
      que dejó a un dominio de presupuesto **sin ningún tipo de ingreso** teniendo *"el alquiler que
      recibo"* escrito en una de sus preguntas
- [ ] ¿**Cada renglón de la Parte 1** dice dónde vive —una clase, varias, un miembro, un conjunto
      cerrado— y **ninguno quedó vacío**? No se pide una clase por renglón: se pide que nada declarado
      esté en ninguna parte. La casilla de arriba mira las preguntas y **no alcanza**: en el gimnasio
      *"membresía"* no aparecía en ninguna pregunta —estaba en la Parte 1 y en lo descartado—, así que
      la dejó pasar y el dominio salió sin ella. **Esta casilla se llena al modelar, no en la sesión**
- [ ] ¿Los juicios que quedaron sin firmar están marcados como tales, y **ninguno tiene un valor por
      defecto silencioso** en el código?
- [ ] ¿Lo leyó y lo aprobó quien conoce el negocio, sin necesidad de que nadie le tradujera nada?

Ese último es el que de verdad cierra el paso. Si hizo falta explicarle el archivo a la persona que
lo va a aprobar, el archivo está mal escrito.

Y ninguno de estos cierra el **paso**, sólo la primera pasada: falta el bloque 5, que se hace cuando
alguien ya intentó modelar con este archivo en la mano.

---

## 6. Qué sigue

| Si venís a | Seguí por |
|---|---|
| Entender dónde corre todo esto y por qué la historia está disponible | Capítulo 0 — es contexto, no requisito |
| Escribir bien el `Preguntas.md` | Capítulo 4 |
| Construir | Capítulo 3, con el `Preguntas.md` en la mano |
