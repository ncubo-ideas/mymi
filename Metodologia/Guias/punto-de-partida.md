# Punto de partida — con qué se arranca

El primer paso del trabajo, antes de cualquier capítulo numerado y antes de una línea de código.

Sale con un archivo: **`Preguntas.md`**, en el directorio del negocio. De ahí en adelante todo
depende de él — el capítulo 3 lo usa como insumo para construir, y la auditoría lo usa como contrato
para decir si la app hizo lo que dijo.

---

## 1. Nadie se salta este paso, y menos el que sabe programar

La tentación es obvia: leer la arquitectura, entender cómo se arma un módulo, abrir el IDE y empezar
a escribir clases. Es el camino de menor resistencia para alguien técnico, y produce exactamente el
fracaso que esta Metodología trata de evitar — **una app impecablemente construida que no contesta nada
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

- Qué le **contesta** hoy a la persona lo que ya existe —el portal, la app del proveedor, el papel—.
  Sirve para saber qué **no** hay que volver a empaquetar. Es distinto de qué le *entrega*: el bloque
  1 separa las dos.
- Quién, adentro de la institución, sabe **interpretar** los datos. Esa persona va a hacer falta en
  el bloque 3, y si no aparece, la entrevista se detiene ahí.

> **De dónde sale este capítulo.** Se escribió a partir de la doctrina, y después se **corrió** — una
> entrevista completa sobre un dominio de presupuesto personal, el 2026-08-26. Esa corrida encontró
> seis defectos en el guion y los seis están corregidos acá. Lo que todavía no se probó es si el
> `Preguntas.md` que sale alcanza para modelar sin volver a preguntar; ése es el eslabón siguiente.

---

## 3. La entrevista

> Para tener abierta durante la sesión, sin el desarrollo: [hoja-de-la-entrevista.md](hoja-de-la-entrevista.md).

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
| **No hay institución** | La persona registra todo. El journal es enteramente de procedencia `TÚ`, no hay URL2 ni token, y **deja de ser reconstruible desde el primer día** |
| **Híbrida** | La institución manda lo que puede y la persona aporta lo que la institución no puede saber. Con las mismas consecuencias que el caso anterior sobre el respaldo |

**Después, y son dos preguntas distintas:**

> *¿Qué le **entrega** hoy la institución: un papel, un portal, un correo?*
> *¿Y qué le **contesta**? ¿Qué pregunta suya responde sin que tenga que hacer cuentas?*

**No las mezcles.** Entregar datos no es contestar preguntas, y el filtro del bloque 2 opera sobre lo
segundo. Un banco entrega movimientos; si además ya te muestra el gasto por categoría, eso sí es una
pregunta contestada.

Todo lo que aparezca en la **segunda** queda descartado de entrada: si el portal ya lo contesta,
reproducirlo no agrega valor, lo reempaqueta.

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
