# Punto de partida — con qué se arranca

El primer paso del trabajo, antes de cualquier capítulo numerado y antes de una línea de código.

Sale con un archivo: **`Preguntas.md`**, en el directorio del negocio. De ahí en adelante todo
depende de él — el capítulo 3 lo usa como insumo para construir, y la auditoría lo usa como contrato
para decir si la app hizo lo que dijo.

---

## 1. Nadie se salta este paso, y menos el que sabe programar

La tentación es obvia: leer la arquitectura, entender cómo se arma un módulo, abrir el IDE y empezar
a escribir clases. Es el camino de menor resistencia para alguien técnico, y produce exactamente el
fracaso que este Taller trata de evitar — **una app impecablemente construida que no contesta nada
que la persona no tuviera ya.**

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

- Qué le entrega **hoy** la institución a la persona. Sirve para saber qué **no** hay que volver a
  empaquetar.
- Quién, adentro de la institución, sabe interpretar los datos. Esa persona va a hacer falta.

---

## 3. La entrevista

Es una conversación, no un formulario. El orden importa: cada bloque depende del anterior.

### Bloque 1 · Qué ya existe

> *¿Qué le da hoy la institución a la persona: un papel, un portal, un correo?*
> *¿Qué puede consultar ahí?*

Todo lo que aparezca acá queda **descartado de entrada**. Si el portal ya lo contesta, reproducirlo
no agrega valor: lo reempaqueta.

### Bloque 2 · Las preguntas

> *Olvidate de la app. **¿Qué diría esta persona en voz alta**, hablando de lo suyo?*

No *"listar exámenes por fecha"*. Sino *"¿me está subiendo el colesterol o fue una mala lectura?"*.

Se insiste hasta juntar tres a cinco. Y a cada una se le pasan tres filtros:

| Filtro | Si falla |
|---|---|
| ¿La respuesta **cambia lo que la persona hace**? | Es información decorativa |
| ¿Alcanza con **un solo dato**, o hace falta la historia? | Si alcanza con uno, el proveedor probablemente ya se lo daba |
| ¿El portal **ya se la contesta**? | Volviste al bloque 1 |

**Una de las preguntas tiene que ser sobre una ausencia.** *"¿Hace cuánto que no me mido esto?"*,
*"¿qué me falta?"*, *"¿se me venció algo?"*. Es la clase de pregunta que ninguna pantalla puede
contestar —una pantalla renderiza lo que tiene— y si la lista no tiene ninguna así, lo que se está
diseñando es probablemente un listado con mejor tipografía.

### Bloque 3 · Los juicios

Éste es el bloque que no se puede saltar ni improvisar, y el que requiere a quien sabe del negocio.
Para cada magnitud que apareció en el bloque 2:

> *¿Hacia dónde es **deseable** que se mueva? ¿Subir, bajar, o mantenerse?*
> *¿Qué cambio es **demasiado rápido para ser real**, y a partir de ahí conviene sospechar del dato?*
> *¿Cada cuánto conviene **volver a medirlo**? ¿Depende del último valor?*
> *¿Qué variación es **ruido** y cuál es significativa?*
> *¿Qué hace que un hallazgo sea **urgente** y otro no?*
> *¿Qué sabe **esta** magnitud que ninguna otra sabe?*

Esa última pregunta es la más productiva de todo el capítulo, porque decide qué merece ser una clase
propia. En el laboratorio, la hemoglobina sabe el **grado de anemia**; no existe el grado de anemia
de un colesterol. Cuando una magnitud no tiene respuesta a esa pregunta, probablemente no necesitaba
existir por separado.

**Si el entrevistado no sabe contestar un juicio, no se inventa.** Se anota como pendiente y se va a
buscar a quien sabe. Un juicio inventado no se nota hasta que alguien toma una decisión con él.

### Bloque 4 · Qué se descarta

> *¿Qué dato te parece importante pero, siendo honesto, no le sirve a la persona **en este dominio**?*

El caso canónico: el **precio del examen**. Es real, está en la factura, y no aporta nada a la salud.
Pero no sobra — **pertenece a otro dominio**, y en Presupuesto Personal es de los datos más
relevantes. Va a la lista de candidatos a cruce, no a la basura.

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

El capítulo 4 desarrolla cómo se escribe cada parte, y trae el ejemplo trabajado completo.

---

## 5. Cuándo terminaste

- [ ] ¿Hay tres a cinco preguntas, cada una en la voz de la persona y no en la de un sistema?
- [ ] ¿Al menos una es sobre una **ausencia**, que ninguna pantalla podría mostrar?
- [ ] ¿Cada juicio del bloque 3 tiene respuesta, o está anotado como pendiente con **nombre y
      apellido** de quién la tiene?
- [ ] ¿Está escrito lo que se **descartó**, y por qué?
- [ ] ¿Los candidatos a cruce quedaron anotados en vez de forzados adentro?
- [ ] ¿Lo leyó y lo aprobó quien conoce el negocio, sin necesidad de que nadie le tradujera nada?

Ese último es el que de verdad cierra el paso. Si hizo falta explicarle el archivo a la persona que
lo va a aprobar, el archivo está mal escrito.

---

## 6. Qué sigue

| Si venís a | Seguí por |
|---|---|
| Entender dónde corre todo esto y por qué la historia está disponible | Capítulo 0 — es contexto, no requisito |
| Escribir bien el `Preguntas.md` | Capítulo 4 |
| Construir | Capítulo 3, con el `Preguntas.md` en la mano |
