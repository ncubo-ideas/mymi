# Capítulo 4 — Requerimientos que agregan valor

Cómo decidir qué entra en el modelo y qué no. Es el capítulo que decide si la app vale algo, y se
escribe **antes** de tocar código.

> Sale de `casos-de-uso-y-plan.md` §1–§2 y de las reglas del equipo. Los ejemplos son de los cuatro
> dominios demo; el método es general.

---

## 1. La pregunta que ordena todo

> **¿Este dato le sirve a la persona *en este dominio*?**

El ejemplo que lo aclara todo: una app de laboratorio clínico **no** debe girar alrededor del **precio
del examen**. Ese dato existe, es real y está en la factura — y no agrega absolutamente nada al
dominio de la salud. Lo que agrega valor ahí es el diagnóstico, la serie histórica, el cruce con otras
cosas de tu vida, si estás llegando a tus metas de salud.

Ahora, cuidado con la conclusión fácil. **El precio no sobra: pertenece a otro dominio.** En
presupuesto personal ese mismo número es de los más relevantes. No es un dato malo; es un dato que
está en la app equivocada.

Esa distinción es la que abre el problema de compartir información entre apps, y va en el anexo A. Por
ahora la regla operativa es simple: **cuando un dato te parezca importante pero no le sirva a la
persona en este dominio, no lo metas — anotalo como candidato a cruce.**

---

## 2. El valor está en la serie, no en el dato

El caso del laboratorio, textual del equipo:

> Colesterol de 200 a 280 **en un día** = error de lectura.
> De 200 a 280 **en un año** = te cambió la vida.
> **El valor de hoy no dice nada sin la serie.**

De ahí sale un criterio duro para revisar requerimientos: **si tu app sólo muestra el último valor, no
agregaste nada.** Eso ya se lo daba el proveedor, en papel, hace veinte años.

Lo que la persona no tenía antes es la **serie completa, en su poder, cruzable**. Si un requerimiento
no usa la historia, sospechá de él.

---

## 3. Los cruces son el producto

Los ejemplos que el equipo eligió no son adornos: son la justificación de toda la arquitectura.

| Cruce | Por qué nadie más puede hacerlo |
|---|---|
| Manual dice 18 km/l, tus cargas dicen 9 — *"¿te compraste un 4x4?"* | El concesionario no ve tus cargas. La gasolinera no ve tu manual |
| *"Estás comprando cosas grasosas y tenés el colesterol alto"* | El supermercado no ve tu laboratorio |
| *"Comprá frijoles y huevos: tenés anemia"* | Idem, al revés |
| La salmuera que le sube la presión al hipertenso sin que lo sepa | Requiere el detalle del producto **y** el diagnóstico |

**Prueba de fuego para un requerimiento:** si la institución ya te lo contesta en su portal, no
agregaste valor — envolviste lo que ya existía. El valor aparece donde **hacen falta dos dueños de
datos que no se hablan entre sí**, y la persona es el único punto donde ambos llegan.

---

## 4. Vista 360, no sistema de información

El equipo lo dice sin vueltas: *"la app es un dashboard, no un sistema de información"*. Bonita,
corta, con el logo del proveedor. **"No es meter el sistema contable en el celular."**

Y la regla que más cuesta interiorizar:

> **La IA tiene acceso a todo lo que el dueño permita, aunque la pantalla muestre poco.**

La pantalla y el modelo **no** tienen por qué coincidir. Modelás rico y mostrás poco.

De hecho, la prueba que el equipo se puso para el siguiente paso del hola-mundo es exactamente ésa:
**una pregunta que la UI no muestre.** *"¿Para cuándo repongo las pastillas?"* *"¿Cuándo compré la
olla arrocera y cuándo vence la garantía?"* Si la IA la contesta del journal y la pantalla nunca la
mostró, el modelo está haciendo su trabajo.

**Consecuencia de diseño: modelás para las preguntas, no para las pantallas.** Una pantalla se cambia
en una tarde; un modelo que no guardó el dato no lo puede inventar después.

---

## 5. Cómo se escribe un requerimiento así

Un método, en orden. Cada paso es una pregunta, no un artefacto.

**1 · Empezá por la frase que la persona diría en voz alta.**
No *"listar exámenes por fecha"*. Sino *"¿me está subiendo el colesterol o fue una mala lectura?"*.

**2 · Preguntá qué tiene que saber el modelo para contestarla.**
Para ésa: la serie de valores, la fecha de cada uno, el rango normal según edad, y qué examen es cuál.
Ahí aparecen las clases.

**3 · Preguntá de dónde sale cada cosa.**
¿Llega por URL2 como evento? ¿Se deriva de lo que ya hay? ¿O falta y hay que pedirlo al proveedor?

**4 · Si la respuesta necesita otro dominio, paralo.**
No es requerimiento de esta app. Es un candidato a cruce → anexo A. Anotalo, no lo fuerces.

**5 · Recién ahora pensá la pantalla.**
Y aceptá que la pantalla muestre menos que lo que el modelo sabe. Es lo normal, no una carencia.

### Los tres filtros

Antes de aceptar un requerimiento, pasalo por esto:

| Filtro | Si falla |
|---|---|
| ¿La respuesta **cambia lo que la persona hace**? | Es información decorativa |
| ¿Necesita la **serie**, o basta el último valor? | Si basta el último, probablemente el proveedor ya lo daba |
| ¿El proveedor **ya se la contesta** en su portal? | No agregaste valor, lo reempaquetaste |

> **Los filtros se aplican a las PREGUNTAS, nunca a la capacidad.** Es el error más fácil de cometer y
> descarta cosas imprescindibles. La clasificación de gastos, por ejemplo, la hace hoy la app de
> muchos bancos — y aun así el modelo la necesita, porque sin ella no hay cómo contestar *"¿se
> incrementó un gasto fijo?"*. No es una pregunta que la app conteste: es lo que le permite contestar
> las otras.
>
> Y el primer filtro **no pesa lo mismo en todos los dominios**. En salud rechaza el precio del examen
> de un plumazo; en presupuesto personal no rechaza casi nada, porque el dominio **es** la plata. Si
> al terminar no descartaste casi nada, no es que la entrevista salió mal: es este efecto.

---

## 5 bis. Un ejemplo trabajado, de punta a punta

La doctrina de arriba se lee en cinco minutos y no enseña a hacerlo. Esto sí: **una pregunta, seguida
hasta el fondo**, con lo que quedó afuera — que es la mitad que nunca se escribe.

### La pregunta

> *"¿Me está subiendo el colesterol, o fue una mala lectura?"*

Dicha así, por la persona. No *"listar exámenes por fecha"*, que es como la habría escrito un sistema.

### Qué tiene que saber el modelo

Se desarma preguntando **qué haría falta para contestarla**, sin pensar todavía en clases:

| | |
|---|---|
| Los valores anteriores, **no sólo el último** | Sin la serie no hay "subiendo" |
| **Cuándo** fue cada uno | Es la mitad de la respuesta, y se ve abajo por qué |
| Qué es normal **para esta persona** | El rango depende de la edad |
| Qué examen es cuál | Dos analitos distintos no forman una serie |

### El juicio que la hace posible

Acá está el corazón, y es lo que ninguna herramienta de datos produce sola:

> Colesterol de **200 a 280 en un día** = error de medición.
> De **200 a 280 en un año** = te cambió la vida.

Mismo número, lectura opuesta. Eso no sale de cortar, agrupar ni comparar: sale de que alguien decidió
que existe un **umbral de verosimilitud entre magnitud y tiempo**. Es un juicio, y sin él la pregunta
no tiene respuesta — sólo tiene un número.

Y fijate qué le exige al modelo: una operación que mira **pares consecutivos** para detectar lo
imposible, y **los extremos** para la dirección. Dos recorridos distintos de la misma serie.

### La decisión de diseño que fuerza

Ésta es la parte que sorprende, y por eso vale el ejemplo.

Un laboratorio real reporta en **seis formas distintas**: un número con unidad, un número sin unidad,
cruces de una tira reactiva, un conteo por campo, un positivo/negativo, un color. Si la clase base
`Examen` declarara un `decimal Valor`, **cuatro de las seis tendrían que devolver cero y mentir**.

Así que `Examen` **no declara un valor medido**. Ésa es la decisión que ordena todo el dominio, y no
se llegó a ella pensando en clases: se llegó **desde la pregunta**, notando que "¿está subiendo?" sólo
significa algo para algunas formas de resultado y para otras no significa nada.

De ahí sale también por qué los nitritos **no son un `bool`**: el vocabulario pertenece al analito
—*positivo/negativo* en unos, *reactivo/no reactivo* en otros—, la polaridad no es obvia —en Rh lo
común es positivo y no es un hallazgo—, y falta un tercer estado: **`NoRealizado` no es lo mismo que
negativo**. Preguntar el resultado de una prueba que no se corrió tiene que **reventar**, no contestar
"no".

### Lo que quedó afuera, y por qué

| Se propuso | Por qué no entró |
|---|---|
| **El precio del examen** | No agrega valor en salud. **No sobra: pertenece a otro dominio** — en presupuesto personal es de los datos más relevantes. Va a candidatos a cruce |
| **Una lista con la tendencia de cada analito** | Es lo que la pantalla ya renderiza. Su propia descripción tenía que aclarar que *"no es la lista que la app ya le muestra"* — y cuando algo necesita defenderse de eso, ya se contestó solo |
| **El perfil del paciente** en el sistema del laboratorio | La app no es cliente de los servicios generales de la institución |

Lo que reemplazó a la segunda es la mejor pregunta del dominio: **"¿hace cuánto que no me mido
esto?"**. Una pantalla no la puede contestar, porque una pantalla renderiza datos que tiene y eso es
una **ausencia** — una conclusión sobre el expediente **más el calendario más la periodicidad que cada
analito define para sí mismo**. Y esa periodicidad no es fija: un colesterol en rango se repite cada
cinco años, uno alto todos los años.

### El mismo camino, cuando la respuesta no está

El ejemplo de arriba es el caso feliz. Éste es el que se va a parecer más a tu primera vez.

En un dominio de presupuesto personal apareció la pregunta *"si tomo este préstamo, ¿qué gastos puedo
reducir?"*. Se desarmó igual, y el modelo necesitaba saber **qué gasto es recortable**. La respuesta
fue: se deduce para el consumo discrecional —cine, delivery, restaurante— y **para todo lo demás la
tiene que marcar la persona**.

Perfecto hasta ahí. Pero faltaba una segunda mitad que nadie preguntó: **cuánto se puede reducir de
cada uno.** ¿El 100 % del streaming? ¿El 30 % de los restaurantes?

Sin ese dato, la app **no puede decir "te alcanza"**. Y lo que se hizo —esto es lo que hay que
copiar— fue **no inventarlo**: la respuesta pone sobre la mesa cuánto hay en cada estado y **declara
que falta un dato**. Tres montos, no uno: lo que se sabe recortable, lo comprometido, y **lo que nadie
clasificó todavía**.

Ese tercer estado es la lección. Con un booleano —recortable sí o no— lo no marcado contaría como
**no recortable**, y la app daría una respuesta más chica que la verdad **sin decir que fue por
ignorancia**. Ése es el error caro: no revienta, subestima.

> **La regla que sale de acá:** cuando un juicio falta, la respuesta correcta no es la más
> conservadora — es la que **dice que falta**.

---

## 6. El dominio rico es lo que hace posible el valor

Nada de lo anterior se sostiene sobre un modelo pobre. Las reglas del equipo, que son las mismas que
el capítulo 3 §3 desarrolla en detalle:

**Herencia de verdad, no estructura compartida.** `Examen → ExamenDeSangre → Colesterol`. El color es
característica del examen de orina, **no un campo suelto** que todos los exámenes arrastran vacío.

**"USD no es un dato, es una clase."** Un dominio que sólo dice cantidad y precio es un dominio pobre,
y un dominio pobre no puede contestar nada interesante.

**Modelá el detalle que la historia necesita.** Del dominio de abarrotes: SKU como clase,
ingredientes, sal, **peso escurrido** — la lata dice 400 g pero son 240 g de garbanzos —, país de
origen. Sin ese detalle, *"la salmuera que te sube la presión"* es imposible de decir.

**Pocas clases, mucho detalle.** Textual: *"parece que hay 30 exámenes; en realidad se modelan 2 o
3"*. La riqueza va en profundidad, no en cantidad.

**Los cálculos paramétricos quemados están bien para el demo.** Edad 60, rangos por edad. No son
deuda: están ahí para **demostrar que la regla vive en el dominio y no en los datos**.

**Los datos del demo se orquestan como casos de prueba.** Si la historia necesita que el carro fallara
a tantos kilómetros, ese evento se siembra. No es hacer trampa: es guionar la demostración.

---

## 6 bis. Cómo se comprueba que esto se cumplió

Todo lo de arriba es doctrina, y una doctrina que nadie puede verificar se cumple las primeras dos
semanas. Cada afirmación de este capítulo tiene —o no tiene— un lugar donde mirarse, y **decir cuál
es cuál es parte del capítulo**.

| Lo que este capítulo pide | Dónde se mira | |
|---|---|---|
| Hay al menos una pregunta que **usa la serie** | Existe una operación que recibe **dos o más** mediciones de la misma cosa. Si todas reciben una, el modelo no mira series | **se puede** |
| Hay una pregunta que **sólo contesta la IA** | Existe una tool que **ninguna vista renderiza**. Se compara la lista de tools contra la de pantallas | **se puede** |
| El modelo sabe **más** de lo que la pantalla muestra | El dominio expone más operaciones públicas que las que consume la carátula | **se puede** |
| Los **juicios viven en el dominio** | Umbrales, periodicidades y direcciones son miembros de clases del dominio, **no constantes en la carátula ni tablas en la UI** | **se puede** |
| El dominio **no lee el reloj** | Ninguna clase del dominio referencia el reloj del sistema. La fecha entra por parámetro | **se puede** |
| **Cada clase justifica existir** | Tendría que tener al menos un miembro que ninguna otra tenga… | **no se puede** |
| La pregunta **cambia lo que la persona hace** | … | **no se puede** |

Las dos últimas son doctrina y hay que decirlo así. *"Cada clase tiene algo que ninguna otra tiene"*
es el criterio correcto —la hemoglobina sabe el grado de anemia, y no existe el grado de anemia de un
colesterol— pero **decidir si un miembro es un juicio propio o un accesor disfrazado exige entender el
negocio**. Un chequeo automático daría ruido en las dos direcciones.

**Y hay una trampa al escribir estos chequeos**, medida: un `grep` ingenuo cuenta **comentarios**. El
`DateTime.Now` que "encontró" en un dominio estaba dentro de un comentario que explicaba *por qué* ese
dominio no llama a `DateTime.Now`. Lo irónico es dónde falla: **cuanto mejor documentado está el
código, más falsos positivos produce** — o sea, justo en los proyectos que uno usa para calibrar.

El instrumento completo, con su clasificación y la corrida contra tres negocios reales, está en
[observables.md](observables.md).

---

## 7. Antes de cerrar la lista de requerimientos

- [ ] ¿Cada requerimiento arranca de una frase que la persona diría, no de una entidad?
- [ ] ¿Hay al menos un requerimiento que **use la serie** y no el último valor?
- [ ] ¿Hay al menos un **cruce** que ningún proveedor podría contestar solo?
- [ ] ¿Hay alguna pregunta que la **IA conteste y la pantalla no muestre**?
- [ ] ¿Se sacó todo dato que no agrega valor **en este dominio**, y quedó anotado como candidato a cruce?
- [ ] ¿El modelo tiene herencia que carga verdad del negocio, y no campos sueltos?
- [ ] ¿Sobrevive el filtro *"¿esto no se lo contestaba ya el portal del proveedor?"*
- [ ] ¿La pantalla se decidió **al final**, y muestra menos de lo que el modelo sabe?
- [ ] ¿Hay al menos una pregunta sobre una **ausencia** — algo que falta, que no pasó, que se venció?
- [ ] Cada juicio que quedó sin respuesta, ¿está anotado como **pendiente con nombre y apellido** de
      quién la tiene, en vez de resuelto con lo más razonable?
- [ ] ¿Los filtros se aplicaron a las **preguntas** y no a la capacidad?

---

## 8. Lo que este capítulo no cubre

- **Cómo se comparte un dato entre dominios.** §1 abre el problema, el anexo A tiene que resolverlo.
  Hasta entonces, la instrucción es anotar el candidato y no forzarlo.
- **Cómo se negocia con el proveedor** qué eventos publica en URL2. Si el modelo necesita un dato que
  la institución no manda, eso es una conversación, no un requerimiento.
- **Settings de qué ve la IA.** La regla dice *"todo lo que el dueño permita"*, y el mecanismo del
  permiso está listado como pendiente de diseño en `casos-de-uso-y-plan.md`.
