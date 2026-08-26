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

## 7. Antes de cerrar la lista de requerimientos

- [ ] ¿Cada requerimiento arranca de una frase que la persona diría, no de una entidad?
- [ ] ¿Hay al menos un requerimiento que **use la serie** y no el último valor?
- [ ] ¿Hay al menos un **cruce** que ningún proveedor podría contestar solo?
- [ ] ¿Hay alguna pregunta que la **IA conteste y la pantalla no muestre**?
- [ ] ¿Se sacó todo dato que no agrega valor **en este dominio**, y quedó anotado como candidato a cruce?
- [ ] ¿El modelo tiene herencia que carga verdad del negocio, y no campos sueltos?
- [ ] ¿Sobrevive el filtro *"¿esto no se lo contestaba ya el portal del proveedor?"*
- [ ] ¿La pantalla se decidió **al final**, y muestra menos de lo que el modelo sabe?

---

## 8. Lo que este capítulo no cubre

- **Cómo se comparte un dato entre dominios.** §1 abre el problema, el anexo A tiene que resolverlo.
  Hasta entonces, la instrucción es anotar el candidato y no forzarlo.
- **Cómo se negocia con el proveedor** qué eventos publica en URL2. Si el modelo necesita un dato que
  la institución no manda, eso es una conversación, no un requerimiento.
- **Settings de qué ve la IA.** La regla dice *"todo lo que el dueño permita"*, y el mecanismo del
  permiso está listado como pendiente de diseño en `casos-de-uso-y-plan.md`.
