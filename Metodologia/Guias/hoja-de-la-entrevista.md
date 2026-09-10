# Hoja de la entrevista

Una página para tener abierta durante la sesión. El desarrollo está en
[punto-de-partida.md](punto-de-partida.md); esto es sólo lo que se pregunta.

**Antes de empezar, decilo en voz alta:** no hace falta saber nada técnico. Se sale de acá con un
archivo en el idioma de quien contesta, y es contra ese archivo que después se verifica si la app
hizo lo que prometió.

---

## Antes de empezar · Con qué llega

**No es un bloque** y por eso no lleva número: no se sale de acá con nada escrito.
Son **cinco bloques**, del 1 al 5, igual que en el capítulo.

> *Contame qué querés que sea esta app, como se lo contarías a alguien en un ascensor.*

Vago está bien. *"Somos un laboratorio y queremos darle a los pacientes una app donde vean sus
exámenes"* es una entrada válida — todavía no sirve para construir nada, y convertirla es el trabajo
de la próxima hora.

---

<!-- regla: quien-es-la-institucion -->
## Bloque 1 · Qué ya existe

> **¿Quién es la institución acá?**

No lo des por sentado. La respuesta cambia la forma de la app entera:

| | Qué implica |
|---|---|
| **Hay una** y publica datos | La forma completa: página de descarga, endpoint de eventos, token |
| **No hay** | La persona registra todo |
| **Híbrida** | La institución manda lo que puede y la persona aporta lo que ella no puede saber |

<!-- regla: respaldo-si-la-persona-aporta -->
> ⚠️ **El corte que importa no es «¿hay institución?» sino «¿la persona aporta algo?»** — y agarra a
> los dos últimos por igual. **En cuanto la persona aporta un hecho, el registro deja de ser
> reconstruible y el respaldo deja de ser opcional.** El híbrido es el más frecuente y donde más fácil
> se pasa por alto, porque *"hay institución"* suena a que los datos están a salvo.

> *¿Qué le **entrega** hoy esa institución a la persona: un papel, un portal, un correo?*
>
> *¿Y qué le **contesta**? ¿Qué pregunta suya responde sin que tenga que hacer cuentas?*

<!-- regla: entregar-no-es-contestar -->
**No las mezcles.** Entregar datos no es contestar preguntas. Lo que aparezca en la **segunda** queda
descartado **como funcionalidad**: si el portal ya lo contesta, hacer esa pantalla no agrega nada.

<!-- regla: descarte-es-de-la-pantalla -->
> ⚠️ **Descartado como funcionalidad NO es descartado del modelo.** Es el error que costó la
> membresía del gimnasio: quedó declarada en la Parte 1 y a la vez en *"lo que el gimnasio ya
> contesta"*, y el dominio salió sin ninguna clase de membresía ni de pago. **Nada se descarta del modelo
> por estar en la pantalla de la institución**; lo que se descarta es hacerle una pantalla.

<!-- regla: lo-que-entra-lo-decide-la-pregunta -->
> **Y lo que entra lo decide la pregunta.** Todo lo que la institución manda **se lee**, campo por
> campo; entra al modelo lo que **alguna pregunta necesita**; lo que ninguna usa se anota y no entra.
> El número de la máquina venía en el plan del gimnasio y no dice si la persona mejora: afuera. **El
> papel manda qué existe; la persona manda qué entra.**

<!-- regla: trae-el-papel -->
### Y no cierres el bloque sin el papel

> *Traé lo que la institución le manda hoy —la hoja, el correo, el estado de cuenta— y leámoslo
> **campo por campo**.*

Es lo más barato de toda la sesión y lo único donde **nadie dice "no sé"**. Por cada campo, dos
preguntas y nada más:

| | |
|---|---|
| *¿Esto qué es, en tus palabras?* | Da el sustantivo |
| *¿Viene siempre, o a veces falta?* | Da si es obligatorio — y a veces destapa una ausencia para el bloque 2 |

De acá sale la **Parte 1** del `Preguntas.md`. Sin este paso esa parte se llena con lo que haya
aparecido suelto en la conversación, que es exactamente cómo se pierde un sustantivo.

---

## Bloque 2 · Las preguntas

> *Olvidate de la app.* **¿Qué diría esta persona en voz alta**, hablando de lo suyo?*

No *"listar exámenes por fecha"*. Sino *"¿me está subiendo el colesterol o fue una mala lectura?"*.

<!-- regla: tres-filtros -->
Insistí hasta juntar **tres a cinco**. A cada una:

- ¿La respuesta **cambia lo que hace**? → si no, es decoración
- ¿Alcanza con **un solo dato**, o hace falta la historia? → si alcanza con uno, el portal ya lo daba
- ¿El portal **ya se la contesta**? → volviste al bloque 1

<!-- regla: filtros-no-a-la-capacidad -->
> ⚠️ **Los filtros se aplican a las PREGUNTAS, nunca a la capacidad.** Es el error más fácil de
> cometer y descarta cosas imprescindibles. La clasificación de gastos la hacen hoy muchos bancos, y
> aun así el modelo la necesita: sin ella no hay cómo contestar *"¿se incrementó un gasto fijo?"*.
> **La Parte 1 no se filtra por lo que el portal ya muestra.** Es la misma regla del bloque 1, dicha
> en general. El único filtro que sí le cabe es el de la pregunta: lo que ninguna pregunta usa no entra.

<!-- regla: pregunta-de-ausencia -->
> **Una tiene que ser sobre una AUSENCIA.** *"¿Hace cuánto que no…?"*, *"¿qué me falta?"*, *"¿se me
> venció algo?"*. Ninguna pantalla puede contestar eso, porque una pantalla renderiza lo que tiene.
> Si no sale ninguna así, probablemente estén diseñando un listado con mejor tipografía.

---

## Bloque 3 · Los juicios

**El bloque que no se improvisa** — y el más caro. Son siete preguntas **por cada cosa que se sigue
en el tiempo**: con seis cosas son 42, y así se siente. Léelo entero antes de empezarlo.

<!-- regla: no-preguntes-en-frio -->
> ### No preguntes en frío: proponé
>
> Umbrales, ruido, cadencia y urgencia **no son conocimiento de producto**, son conocimiento experto
> del dominio. Quien está sentado enfrente casi nunca los tiene, y tenerlos no era su trabajo.
>
> Así que **traé un valor propuesto a la mesa** —de otro negocio, de un experto, de la literatura— y
> dejá que lo muevan. Es la misma regla del bloque 5: *nadie tiene un umbral en la cabeza.*
>
> Lo que no se sepa **entra por parámetro y se marca ⚠️ sin firmar**. La firma es asincrónica: no
> bloquea la sesión ni bloquea construir.

Para cada cosa que la app va a seguir en el tiempo:

<!-- regla: siete-juicios -->
> **1 · Dirección.** *¿Hacia dónde es deseable que se mueva: subir, bajar, o mantenerse dentro de
> algo?* Ojo: no siempre es binario.
>
> **2 · Identidad del hecho.** *¿Qué cambio es tan grande que probablemente **no sea la misma cosa**?*
>
> **3 · Ritmo esperado.** *¿Cada cuánto se espera que esto vuelva a ocurrir? ¿Es fijo, o depende del
> último valor?* **De acá sale la pregunta sobre la ausencia.**
>
> **4 · Ruido.** *¿Qué variación es normal y cuál merece que se avise?*
>
> **5 · Alcance.** *¿Se vigila cada cosa por separado, o el conjunto?* Vigilar cada una suena más fino
> y suele ser peor: algo dispara todos los meses y nadie mira la app a los tres.
>
> **6 · Urgencia.** *¿Qué hace que un hallazgo sea urgente — el tamaño del número, o la clase de cosa?*
>
> **7 · Lo propio.** *¿Qué sabe **esta** cosa que ninguna otra sabe?*

La **7** es la más productiva de la sesión: decide qué merece ser una clase propia.

<!-- regla: no-se-inventa -->
> ### Si no saben, NO SE INVENTA
>
> Se anota como pendiente **con nombre y apellido de quién lo sabe**. Un juicio inventado no revienta
> nunca: da una respuesta creíble y equivocada cada vez.
>
> <!-- regla: sin-default-silencioso -->
> Y **un juicio sin firmar tampoco tiene un valor por defecto silencioso** en el código: o el objeto
> no se construye sin él, o la respuesta dice que no sabe. Un default es un juicio inventado con
> mejores modales.

<!-- regla: regla-de-corte -->
> ### Si te empantanás, cerrá el bloque
>
> Un *"no sé"* **abre** una pregunta nueva —*¿y quién lo sabe?*— en vez de cerrar. Cuando tres
> seguidos vengan así, anotá los tres con su dueño y **seguí**. Este bloque vuelve en el 5, con
> propuestas concretas sobre la mesa, y ahí rinde mucho más que insistiendo ahora.

---

<!-- regla: que-se-descarta -->
## Bloque 4 · Qué se descarta

> *¿Qué dato te parece importante pero, siendo honesto, no le sirve a la persona **en este dominio**?*

Va a la lista de **candidatos a cruce**, no a la basura. El precio de un examen no aporta nada en
salud y es de los datos más relevantes en presupuesto personal.

Si no sale casi nada, no es que la entrevista falló: hay dominios donde este filtro no pesa.

---

## Bloque 5 · La revisión

**No es parte de la misma sentada.** Va **después de la primera pasada de modelado**, y son quince
minutos. Una sola pasada nunca alcanza: modelar siempre destapa lo que la entrevista no preguntó.

<!-- regla: bloque-5-con-huecos -->
> **Se entra con el `HUECOS.md` en la mano, y ése es todo el guion.** Es lo que quien modela fue
> anotando mientras construía: lo que quedó **declarado sin firmar** en la entrevista y lo que
> apareció **descubierto** al escribir las clases. **Cada renglón es una pregunta** — hay que
> reformularla según la regla de abajo antes de decirla en voz alta.
>
> Si está vacío, o no se modeló o no se anotó mientras se modelaba. Nunca es que no hubo huecos.

<!-- regla: quien-es-clase-lo-decide -->
> ### La regla que gobierna este bloque
>
> **La persona contesta sobre el negocio. Quién se vuelve una clase lo decide quien modela.**
>
> La prueba: **¿podría contestarla alguien que nunca programó?** Si la pregunta dice *clase*, *tipo*,
> *entidad*, *dominio* o *sustantivo*, está mal formada — reformulala antes de hacerla.

<!-- regla: tres-cacerias -->
### Tres cacerías

**1 · Una palabra apareció en las preguntas y nadie la desarrolló.**

> *¿Qué te preguntás sobre **X** que no te preguntás sobre **Y**?*
> *Si te pregunto cuánto vas a recibir el mes que viene, ¿de qué estarías seguro y de qué no?*
> *¿Cuáles llegan solos y cuáles hay que salir a buscarlos?*

La primera es la más productiva de todas: es *"¿qué sabe esto que ninguna otra cosa sabe?"* dicho en
idioma de negocio.

**2 · Al modelar hizo falta un dato que nadie mencionó.**

<!-- regla: barrido-de-la-institucion -->
**Antes de preguntar, volvé a mirar qué manda la institución.** Un estado de cuenta trae débitos **y
créditos**: si el modelo sólo tiene gastos, le falta la mitad de lo que ya está entrando — y eso se ve
sin entrevistar a nadie. Es el mismo movimiento del bloque 1, ahora con el modelo escrito al lado.

> *¿De dónde sale…?*
> *¿Eso viene en lo que te manda la institución, o lo sabés vos?*

**3 · Se tomó un juicio por obvio.**

Todo lo que quien modela decidió solo, **se lee en voz alta como propuesta** y se deja corregir:

> *Dejamos que un cargo que no llega se avise a los tres días. ¿Te suena bien, o depende de cuál?*

<!-- regla: proponer-el-umbral -->
No preguntes *"¿cuál es el umbral?"* — nadie tiene un umbral en la cabeza. Proponé uno y dejá que lo
muevan.

<!-- regla: mal-vs-bien-formada -->
### Cómo se ven las dos formas

| Mal formada | Bien formada |
|---|---|
| ¿Qué **clases** de ingreso hay? | *Si te pregunto cuánto vas a recibir el mes que viene, ¿de qué estarías seguro?* |
| ¿Es el préstamo una **cosa del dominio**? | *¿Te preguntás cuánto te falta para terminar de pagar algo?* |
| ¿Lo que se aparta **es algo** con saldo? | *Cuando te pasa un imprevisto, ¿de dónde sale la plata?* |
| ¿Quién **fija** el presupuesto? | *Cuando decís que algo "no estaba presupuestado", ¿contra qué lo comparás?* |

<!-- regla: entrevistado-generoso -->
> **Y una advertencia.** Si contesta todo rápido y sin dudar, sospechá. **Una persona generosa
> contesta igual una pregunta mal formada**, y eso esconde el defecto en vez de mostrarlo. Pasó en la
> corrida donde se descubrió esta regla.

---

## Cuando terminaste

- [ ] ¿Se leyó **el papel, campo por campo**, y cada campo está en la Parte 1? <!-- regla: cierre-papel -->
- [ ] ¿Tres a cinco preguntas, en la voz de la persona? <!-- regla: cierre-preguntas -->
- [ ] ¿Al menos una sobre una **ausencia**? <!-- regla: cierre-ausencia -->
- [ ] ¿Cada juicio contestado, o anotado con **quién** lo sabe? <!-- regla: cierre-juicios -->
- [ ] ¿Está escrito lo que se **descartó**, y por qué? ¿Y quedó claro que se descartó **la pantalla**,
      no el dato? <!-- regla: cierre-descartado -->
- [ ] ¿Los **candidatos a cruce** quedaron anotados, en vez de forzados adentro? <!-- regla: cierre-cruces -->
- [ ] ¿**Cada palabra** que aparece en las preguntas tiene algo detrás? *(la que se salta más)* <!-- regla: cierre-palabras -->
- [ ] ¿Lo aprobó quien conoce el negocio, **sin que nadie le tradujera nada**? <!-- regla: cierre-aprobado -->

Y al modelar, no en la sesión:

- [ ] ¿**Cada renglón de la Parte 1** dice dónde vive, y **ninguno quedó vacío**? No es una clase por
      renglón —puede ser un miembro, o un conjunto cerrado—; es que nada declarado esté en ninguna
      parte. La casilla de las palabras mira las preguntas y **no alcanza**: *"membresía"* no estaba
      en ninguna pregunta del gimnasio y se perdió igual <!-- regla: cierre-parte-1 -->
- [ ] ¿Los juicios sin firmar están **marcados como tales**, y ninguno tiene un **valor por defecto
      silencioso** en el código? <!-- regla: cierre-sin-firmar -->

Y después de modelar, el bloque 5. **Nunca alcanza con una pasada.**

El último es el que de verdad cierra: si hizo falta explicarle el archivo a quien lo va a aprobar, el
archivo está mal escrito.

---

<!-- regla: bloques-no-son-una-direccion -->
**Los bloques no van en una sola dirección.** Es normal que el 3 haga aparecer una pregunta que
pertenecía al 2. Cuando pase, volvé. Un bloque cerrado no está clausurado.
