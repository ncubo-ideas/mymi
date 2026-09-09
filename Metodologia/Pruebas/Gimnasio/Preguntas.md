# Preguntas — Gimnasio

Lo que esta app se compromete a poder contestar. Salió de la entrevista del **2026-08-28**, antes de
escribir una línea de código.

La persona es **Sonia**: entrena varias veces por semana, sigue un plan que le arma el gimnasio,
tiene un perfil con sus medidas, y tiene objetivos.

**Este dominio NO es híbrido, y se decidió a propósito.** Todo llega del gimnasio; Sonia no aporta
nada. La decisión salió de una restricción de producto: *"muchos usuarios no van a estar gastando
tiempo en alimentar con datos la app"*. Tiene dos consecuencias que se aceptan con los ojos abiertos:

> **El journal vuelve a ser reconstruible.** Borrar la carpeta es volver a bajar los datos, no perder
> el trabajo de nadie. El respaldo es opcional — al revés que en [Presupuesto](../Presupuesto/Preguntas.md).
>
> **Y si el gimnasio no lo manda, no existe.** Eso mató una pregunta entera (ver abajo).

---

# Parte 1 · Lo que el modelo sabe

Capacidad, no preguntas. Declarado esto, las preguntas se componen encima.

La columna de la derecha se llena **al modelar**, no en la entrevista, y es lo que hace corrible el
[observable 13](../../Guias/observables.md). **No es una clase por renglón** —la asistencia es un
miembro de la raíz y está bien que lo sea—: lo que no puede quedar es **vacío**.

| | Qué es | Dónde vive |
|---|---|---|
| **El plan** | Lo que el gimnasio le indica hacer, con su frecuencia esperada. **Desde el v3 (2026-09-08), como el papel lo trae:** cada ejercicio con **series × rango de repeticiones y descanso**, o **minutos** si es cardio. El número de máquina que el papel trae **no entra**: Jorge lo sacó (hueco 21). **El peso, sólo cuando la máquina lo requiere** —Jorge, 2026-09-08, hueco 17 cerrado—: por eso es opcional por ejercicio. La tercera forma —repeticiones **y** tiempo— Jorge la nombró y la retiró el mismo día (hueco 27). **Se actualiza**, y esa actualización es un hecho: es la progresión | `Plan`, `EjercicioPrescrito`, `Prescripcion` → `PrescripcionDeFuerza`, `PrescripcionDeCardio` |
| **La asistencia** | Los días que Sonia entró. El gimnasio los tiene y los manda. *No está en las capturas del v3* | `Trayectoria.Asistio` — una lista, **no merece clase propia** |
| **La medición** | Una hoja con medidas. **No es un evento de calendario: ocurre al final de un plan.** **Desde el v3:** las mide **un aparato**, que manda seis —IMC, grasa, agua, metabolismo basal, grasa visceral, masa ósea— **cada una con su evaluación al lado** (*Increased*, *High*, *Insufficient*; la cuarta, *Didn't reach goals*, Jorge la sacó: hueco 20). **El IMC lo manda el gimnasio**, y el peso ya no lo calcula (hueco 19). La cintura **se mide con cinta**, aparte; la masa muscular **no la manda el aparato** y quedó sin fuente en ningún papel | `Medicion`, `Medida` → las cuatro del v2 y `MedidaDelAparato` → `IndiceDeMasaCorporal`, `Agua`, `MetabolismoBasal`, `GrasaVisceral`, `MasaOsea`; `EvaluacionDelAparato` |
| **El perfil** | Género, edad, altura. **No es decoración — sin él ningún número se puede interpretar** | `Perfil`, `Genero` |
| **El objetivo** | Lo que Sonia quiere lograr. Es lo que le da **signo** a cada medida. *El aparato tiene metas propias —"Didn't reach goals"— que no son las de Sonia (hueco 20)* | `Objetivo`, `Meta` |
| **El rango de referencia** | Cada medida tiene valores **normales, bajos y altos**, y dependen del perfil. Es la misma forma que el analito de [Exámenes](../../Guias/04-requerimientos-que-agregan-valor.md). **Desde el v3, para las seis del aparato la lectura la manda el aparato y la app no la recalcula** (juicio 11) | `Referencia`, `Lectura` — sólo para lo que el aparato no evalúa |
| **La membresía** | Pagos, próximo cobro, y **la bandera** *Activo* / *No Activa* que el gimnasio manda — **sin fecha**, según el papel (hueco 15, cerrado). *La entrevista decía "en mora"; la app no dice por qué no está activa, y el modelo pasó de hablar de mora a hablar de **inactividad***. **Desde el v3:** el **producto** contratado y el **día de vencimiento**; y cada cobro con **el período que cubre, su etiqueta de mes y su estado** | `Membresia` —el relator de *socia*—, `EstadoDeMembresia`, `CambioDeEstado`, `Pago`, `Cobro`, `EstadoDeCobro`, `Contratacion`; el período en mora **se deriva**: `PeriodoEnMora`. **Desde el v2, 2026-09-07** |

> **Este renglón dijo ❌ del 28 de agosto al 7 de setiembre, y ese ❌ produjo la corrección del bloque 1
> del punto de partida.** La membresía quedó declarada acá y a la vez abajo, en *"lo que el gimnasio ya
> contesta, y por eso se descarta"*, y el dominio v1 se construyó sin ella — así que **un período en
> mora contaba como incumplimiento del plan** (huecos 9 y 12). El v2 lo cerró con la regla corregida:
> *descartado como pantalla no es descartado del modelo*. Medido: el mismo plan que en el v1 daba
> *"esperaba 39, faltaron 9"* da en el v2 *"esperaba 33, descontó 6 por mora, faltaron 3"*.
>
> Se dejó el ❌ a la vista mientras estuvo, por la misma razón que la clave `grasa:femenino:42`: un
> hueco visible vale más que uno bien disimulado. Y se deja esta nota por la razón contraria: que se
> vea que se cerró, y con qué.

## Dos juicios sobre el mismo número, y no se reemplazan

Esto salió al traer el precedente de Exámenes, y es lo que más forma le da al modelo:

| | Qué contesta | De dónde sale | Es sobre |
|---|---|---|---|
| **La lectura** | ¿está normal, bajo o alto? | **El perfil** — género, edad | el **estado** de hoy |
| **La tendencia** | ¿se mueve como Sonia quiere? | **El objetivo** | el **movimiento** |

**Ninguno se deduce del otro, y hacen falta los dos.** Un % de grasa puede estar *alto* y **mejorando**;
otro puede estar *normal* y **yendo hacia donde ella no quiere**. Una app que sólo diga una de las dos
cosas miente por omisión en la mitad de los casos.

> Es la diferencia con Exámenes, y conviene tenerla presente: **un laboratorio tiene un solo juicio**
> —el valor contra su rango—, porque un paciente no declara hacia dónde quiere que vaya su colesterol.
> Acá Sonia sí declara un objetivo, y eso agrega un eje entero.

## Lo que el gimnasio ya contesta, y por eso se descarta **como pantalla**

Entregar datos no es contestar preguntas, y esto es lo segundo:

- Cuántas veces fue en la semana o el mes
- Las métricas del perfil, en serie — *"le da una hoja al iniciar y otra después de un período"*
- Si está atrasada en pagos, si pagó por adelantado, cuándo es el próximo cobro

> **Descartado como pantalla, no del modelo.** Corregido el 2026-09-07 con el v2. Los tres renglones
> entran al dominio —el tercero es la membresía entera—; lo que no se hace es una pantalla que repita
> lo que el gimnasio ya muestra. Leerlo como descarte total fue lo que costó la membresía.

## Lo que NADIE tiene

**Qué hizo Sonia adentro del gimnasio.** El gimnasio le da un plan y ve que cruzó la puerta; entre la
puerta y la salida no registra nada. Con la decisión de cero aporte, **ese hueco se queda vacío** — no
se llena pidiéndole a ella que lo llene.

> **Confirmado con el papel del v3 (2026-09-08):** la app del gimnasio tiene **una casilla por ejercicio**
> para marcarlo hecho, y Jorge aclaró que **queda en el teléfono** — el gimnasio no se entera (hueco 16,
> cerrado). La decisión se sostiene, ahora con respaldo.

---

# Parte 2 · Los juicios que el dominio toma

Ninguno se deduce de los datos: existen porque alguien los decidió.

| # | El juicio | La decisión |
|---|---|---|
| 1 | **Hacia dónde es deseable que se mueva una medida** | **Lo da el objetivo, no la medida.** Subir de peso es buena noticia para quien necesita subir y mala para quien necesita bajar. **Sin objetivo declarado, la app no puede opinar sobre ningún número** |
| 2 | **Cuándo una medición no es creíble** | Depende del **tiempo transcurrido** y de **qué medida es**. Valores por defecto abajo — ⚠️ sin firmar |
| 3 | **Cada cuánto se espera una medición** | **Al final de cada plan. No es el calendario.** Los planes suelen ser trimestrales pero no es fijo |
| 4 | **Qué es ruido y qué merece aviso** | Valores por defecto abajo — ⚠️ sin firmar |
| 5 | **Se vigila cada medida por separado, o el conjunto** | **Por separado.** Ver la nota de abajo: acá es sensato y en presupuesto no lo era |
| 6 | **Qué hace urgente un hallazgo** | **Magnitud + signo respecto del objetivo.** Y existe la **urgencia positiva**: 2 cm menos de cintura avisa igual que 2 cm más |
| 7 | **Qué sabe cada medida que ninguna otra sabe** | **Su propio rango de referencia, y qué parte del perfil necesita para leerlo.** Ver la tabla |
| 8 | **Qué es normal, bajo o alto** | **Lo decide la medida contra el perfil**, igual que un analito de laboratorio. Es un juicio **distinto** del 1, y los dos hacen falta |
| 9 | **De dónde sale que la membresía está en mora** — *v2, N1* | **Lo dice el gimnasio**, con una bandera *en mora* / *al día* que cambia con fecha. La app **no** deduce mora de cobros y pagos: la política de cuándo una cuenta está en mora es del gimnasio. El período en mora se **deriva** de dos cambios de bandera. **Firmado por Jorge, 2026-09-07** |
| 10 | **Si un período en mora cuenta como incumplimiento del plan** — *v2, N2* | **No.** Los días en mora dentro del plan no se esperan: se descuentan del plan en semanas enteras, con la misma ley con que el plan cuenta su duración. Es la *Suspension* de Fowler cap. 8. **Firmado por Jorge el 2026-09-07, en sus tres partes:** el fondo; la granularidad en **semanas enteras**, la misma ley con que el plan cuenta su duración; y que **una asistencia durante la mora cuenta igual** — un hecho no se borra porque contradiga una regla |
| 11 | **Qué se hace con la evaluación que el aparato pone al lado de cada valor** — *v3, N7* | **Llega como hecho y no se recalcula.** Para las seis medidas del aparato la app no compara contra un rango propio —`Leer` revienta—; sigue calculando la lectura sólo para lo que el aparato no mide. El juicio 8 deja de necesitar umbrales para esas seis. **Firmado por Jorge el 2026-09-08** |
| 12 | **Cómo se lee la progresión de lo prescrito** — *v3, N8* | Sobre lo que el papel trae: **volumen** —series por repeticiones máximas— para fuerza, **minutos** para cardio; **subir** es la dirección. Fuerza y cardio no se comparan entre sí. El peso, si algún día viene, se compara aparte. **Firmado por Jorge el 2026-09-08** |
| 13 | **Con qué palabras se nombra el estado de la membresía** — *v3, N9* | **Las del gimnasio.** El papel dice *Activo*: `al-dia` se renombró. Y Jorge dio la otra el mismo día: **"No Activa"** — así que *en mora* salió, y con ella el nombre de todo lo derivado: el modelo descuenta **inactividad**, no mora, porque la bandera no dice la razón. **El juicio 10 se conserva con ese alcance.** Y la bandera **no trae fecha**: la fecha es la del evento del feed o la de la lectura del importador; el dominio la recibe. **Firmado por Jorge el 2026-09-08** |
| 14 | **Qué trae un cobro** — *v3, N10* | Etiqueta de mes, monto, vencimiento, **el período que cubre** y su estado. La identidad sigue siendo el vencimiento. El estado es un conjunto cerrado de **una** palabra vista: *Abierto*. Un cobro abierto y vencido **no** es mora: la mora la dice el gimnasio (juicio 9). **Firmado por Jorge el 2026-09-08** |
| 15 | **Cómo entran el producto y el día de vencimiento** — *v3, N11* | Como una **contratación** con fecha: el producto es texto —se vio uno, no hay catálogo— y el día un entero del mes. El mismo producto otra vez no entra. **Firmado por Jorge el 2026-09-08** |

### Las medidas nuevas no tienen dirección propia, a propósito

*Masa ósea sube y punto* y *grasa visceral baja y punto* son plausibles y **nadie las firmó**. Hasta
entonces las cinco medidas del aparato siguen el juicio 1: la dirección la da el objetivo, y sin meta
declarada la app **revienta** en vez de opinar. Un default sería un juicio inventado con mejores modales.

### El 1 tiene una excepción, y rompe la regla

**La masa muscular es la única donde la dirección NO depende del objetivo**: subir es bueno en los dos
casos. El modelo tiene que dejar espacio para las dos formas — no alcanza con colgar la dirección del
objetivo y listo.

### Por qué el 5 acá es distinto de Presupuesto

En presupuesto se decidió vigilar **el total**, porque con doce gastos vigilándose **todos los meses**
algo dispara siempre y nadie mira la app a los tres meses. Acá son pocas medidas **cada trimestre**.

> **El juicio de alcance no es universal: depende del ritmo.** Vigilar cada cosa por separado es
> sensato cuando el ritmo es lento. Eso salió de esta entrevista y no estaba en ninguna guía.

### El 7, desarrollado

Un número solo no significa nada. Cada medida sabe **qué necesita para poder leerse**:

| Medida | Necesita del perfil | Lo propio |
|---|---|---|
| **Peso** | la altura | Sola no dice nada: **su rango sólo existe combinada con la altura** |
| **% de grasa** | género y edad | Su rango saludable **se mueve con la edad**, y es distinto entre hombres y mujeres |
| **Cintura** | género | Es la única que dice algo de salud **sin compararse consigo misma**: tiene un umbral de riesgo propio |
| **Masa muscular** | — | La única cuya **dirección** no depende del objetivo |

Y esto es lo que decide qué merece ser una clase: **cada medida arma su propia lectura**, porque cada
una necesita algo distinto del perfil y tiene un rango que ninguna otra tiene. Una sola clase
`Medida` con un `tipo` adentro no puede hacer eso sin convertirse en un `switch` — es la porosidad
que el capítulo 4 manda evitar.

> **Y con el límite de Exámenes, que ya está corregido en el capítulo 0 §8:** la medida **nombra** su
> lectura —*"alto"*, *"normal"*— porque es lo único que puede decirlo mejor que nadie. Lo que **no**
> inventa es el consejo. Y el número viaja siempre al lado de la palabra.

---

## ⚠️ Los valores del 2 y del 4 NO están firmados

**Quién los da: un experto IA.** No hay una persona del gimnasio que los haya validado.

Por eso el modelo los **recibe por parámetro y no los lleva como constantes**, y por eso esta sección
está marcada. La línea que hay que sostener:

> **La IA da los valores; el dominio se queda con la forma y hace la comparación.** Si la IA hiciera
> la comparación, el juicio no quedaría en el journal y la app contestaría distinto mañana sobre los
> mismos datos. Con los valores por parámetro, la app es **reproducible** y la IA es **reemplazable**.

**Juicio 2 — no es creíble si:**

| Medida | Umbral |
|---|---|
| Peso | cambió más de **1 % del peso corporal por semana** |
| % de grasa | saltó más de **5 puntos** entre dos mediciones de un trimestre |
| Circunferencias | cambió más de **1 cm** con menos de **2 semanas** de diferencia |
| Masa muscular | igual que % de grasa — sale del mismo aparato |

**Juicio 4 — ruido y aviso:**

| Medida | Ruido | Merece aviso |
|---|---|---|
| Cintura | < 1 cm | **≥ 2 cm** |
| Peso | ± 1 kg en un trimestre | sostenido en la misma dirección dos mediciones seguidas |
| % de grasa | < 1 punto | **≥ 2 puntos** en un plan |
| Masa muscular | < 0,5 kg | ≥ 1 kg |

**La regla que los une, y ésa sí se sostiene sin firma:** por debajo del error del instrumento no hay
noticia. Un aviso que dispara dentro del margen de error entrena a la persona a ignorar los avisos.

---

# Parte 3 · Lo que la UI muestra

Se decide **al final**, y va a mostrar menos que lo que el modelo sabe. Pendiente.

Un requisito ya conocido, **corregido el 2026-09-07 al revés de como estaba escrito**: los pagos, el
próximo cobro y la mora **se modelan y no se muestran**. El gimnasio los contesta completos —por eso
no hay pantalla de pagos—; y el dominio **sí** los necesita: sin la mora no puede decir cuándo un
incumplimiento no lo fue. La versión anterior de esta línea —*"se muestran pero no se modelan"*— era
el error del bloque 1 escrito en la Parte 3.

---

# Las preguntas que originaron todo esto

1. *"¿Cómo vengo desde que entré al gimnasio hasta hoy?"* — el progreso a lo largo de la serie
2. *"¿Cómo cambió lo que el gimnasio me prescribe?"* — la progresión del plan. **Reformulada por segunda
   vez el 2026-09-08**: decía *"¿cuánto subió lo que me pide levantar?"* y el recorte no mostraba peso.
   Jorge aclaró el mismo día que **el peso existe cuando la máquina lo requiere**, así que la pregunta
   tiene las dos lecturas: volumen o minutos para todos, y kilos para los ejercicios que lo traen
3. *"¿Cuántos días incumplí el plan?"* — **la ausencia**
4. *"¿Qué me falta para cumplir mi objetivo?"* — **la ausencia**, y la que obliga a que el objetivo exista
5. *"¿Terminó mi plan y nadie me midió?"* — **la ausencia que salió del juicio 3.** Ninguna pantalla la
   puede mostrar, porque una pantalla renderiza lo que tiene

### Las dos que no sobrevivieron, y por qué

**«¿Aumenté o disminuí el peso que levanto?»** — **muerta.** El gimnasio no sabe con cuánto peso
entrenó, y con cero aporte nadie lo va a registrar. Se reformuló a la **2**, que es la progresión de lo
**prescrito** — un hecho observado y no un asumido. No es lo mismo, y el archivo lo dice.

**«¿Qué combinación de ejercicios me conviene para mis metas?»** — **estaba prematura**, y al mirarla
de cerca resultó ser la ambición que se rechaza. Ver abajo.

---

# Lo que se rechazó

⚠️ **Ningún dato.** Igual que en presupuesto: hay dominios donde este filtro no pesa. Lo que se
descarta acá es **una ambición**, que es más difícil de ver y más cara de dejar pasar:

| Se descarta | Por qué |
|---|---|
| **Ser un entrenador** | La app **no arma planes, no recomienda ejercicios y no dice qué debería hacer Sonia**. Puede decir *"llevás dos planes sin que te midan"* o *"la cintura bajó 2 cm"* — hechos. El plan lo hace el gimnasio, y recomendar sobre el cuerpo de una persona no es lo que este dominio nombra |

Sin esto escrito, en tres meses alguien pide que sugiera rutinas.

---

# Candidatos a cruce

| El gimnasio pone | El otro módulo pone | De dónde vendría |
|---|---|---|
| La cuota de la membresía | qué lugar ocupa en el presupuesto del mes | Presupuesto |
| El % de grasa, la cintura | los valores de laboratorio del mismo trimestre | Exámenes |
| Los días que entrenó | qué comió esas semanas | Compras |

El cruce con **Exámenes** es el más fuerte: el perfil corporal y el perfil bioquímico se leen juntos y
hoy viven en dos papeles que nadie junta.

---

# Verificación

| Se dijo | Dónde se comprueba |
|---|---|
| La dirección la da el objetivo | Ninguna medida tiene una dirección fija propia — **salvo la masa muscular**, que sí, y por eso hay dos formas |
| **La lectura y la tendencia son dos** | Existen dos operaciones distintas y **ninguna se deduce de la otra**. Hay una prueba con un valor *alto* que **mejora**, y otra con uno *normal* que **empeora** |
| Cada medida arma su propia lectura | No hay una clase `Medida` con un `switch` por tipo adentro. Cada una es su propia clase, con su rango |
| El perfil es entrada obligatoria | Ninguna lectura se puede pedir sin género, edad y altura. **No compila** una interpretación sin perfil |
| El ritmo es del plan | La ausencia se calcula contra **el fin de un plan**, no contra una cantidad de días |
| Los umbrales entran por parámetro | Ninguna constante numérica de las tablas del 2 y el 4 vive en el dominio |
| La urgencia tiene signo | El tipo distingue urgencia **positiva** de negativa, no sólo severidad |
| El dominio no lee el reloj | Ninguna clase referencia `DateTime.Now`. Las fechas entran por parámetro |
| Hay una pregunta que sólo contesta la IA | Al menos una tool que ninguna vista renderiza. Candidata: la **5** |
| El journal es reconstruible | Nada del modelo depende de un hecho que sólo Sonia pudo aportar — **y la membresía tampoco**: bandera, pagos y cobros llegan del gimnasio |
| **La mora no cuenta como incumplimiento** *(v2)* | `IncumplimientoDe` descuenta las semanas en mora dentro del plan. Hay una prueba con quince días en mora que **espera 33 y no 39**, y dice que descontó 6 |
| **La misma bandera dos veces se rechaza** *(v2)* | Es el mismo hecho otra vez —regla 18—. Hay una prueba, y el ciclo con journal lo muestra: la segunda corrida rechaza las tres banderas |
| **Sin bandera no hay "activo" por defecto** *(v2)* | `EstaEnMora()` **revienta** si el gimnasio nunca mandó la bandera; `EstabaEnMoraEl(fecha)` es total y da `false`; el descuento es total y da cero |
| **La evaluación del aparato es hecho, no juicio** *(v3)* | Ninguna de las cinco medidas nuevas se puede `Leer` contra una `Referencia`: **revienta**. Hay una prueba con *high* hoy **y mejorando** en la serie: los dos juicios siguen siendo dos, y el primero ya no lo calcula la app |
| **Una palabra que el papel no mostró revienta** *(v3)* | `EvaluacionDelAparato.PorNombre("normal")`, `EstadoDeCobro.PorNombre("pagado")` y `EstadoDeMembresia.PorNombre("al-dia")` revientan; los `Check` los rechazan antes |
| **Fuerza y cardio no se comparan** *(v3)* | Un ejercicio que pasó de minutos a series aparece en dos planes y **no** tiene progresión, y se dice |
| **Sin peso no hay "cuánto subió lo que levanto"** *(v3)* | `CuantoSubioLoIndicado` revienta si un plan no lo indica; con peso en los dos, vuelve a dar 15 |
| **Sin dirección firmada, no se opina** *(v3)* | `ComoVengo("masa-osea")` sin meta **revienta** |
| **Dos cobros sobre el mismo día son un error** *(v3)* | `PuedeAnunciarCobro` rechaza el que se solapa; el `Check` lo dice; el `medir` lo muestra rechazado |
| **La palabra del v2, *en-mora*, ya no existe** *(v3)* | `EstadoDeMembresia.PorNombre("en-mora")` revienta; el conjunto es *activo* / *no-activa* |

---

# Pendientes, con nombre

| # | Qué falta | Quién puede saberlo |
|---|---|---|
| 1 | **Firmar los umbrales del juicio 2 y el 4.** Hoy los da un experto IA y nadie del gimnasio los validó | Sin asignar. **Es un dominio de salud: conviene que tenga nombre antes de producción** |
| ~~2~~ | ~~Si un período **suspendido por atraso de pago** cuenta como incumplimiento~~ — **CERRADO el 2026-09-07** (v2, juicio 10): no cuenta. La membresía entró al dominio con la bandera del gimnasio, y el incumplimiento descuenta la mora | Jorge — **firmado el 2026-09-07** |
| 3 | Qué pasa cuando Sonia tiene **más de un objetivo**, y se contradicen entre sí | Sin asignar. El juicio 1 depende de que haya **un** objetivo que dé signo |
| ~~4~~ | ~~**Firmar los juicios 11 a 15** del v3~~ — **firmados por Jorge el 2026-09-08**, N8 con la precisión de la tercera forma. **Sigue abierta la dirección de las cinco medidas del aparato** | **Jorge** |
| ~~5~~ | ~~**¿Qué me cuesta cada día que voy?**~~ — **DESCARTADA por Jorge el 2026-09-08:** *"el cobro no es tan importante para esta app; tal vez para la de presupuesto. Ésta es cliente-céntrica del gimnasio"*. Es un cruce, y va en *Candidatos a cruce* | — |
| ~~6~~ | ~~**Si los objetivos se asocian a medidas.**~~ Jorge había dicho *"por ahora no asociemos"*; al ver que eso deja la tendencia sin *mejora / empeora*, la urgencia sin signo y la pregunta 4 sin respuesta, **decidió no desasociar** — 2026-09-08. El modelo queda como está: una `Meta` por medida | — |
