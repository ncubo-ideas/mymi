# Hoja de la entrevista

Una página para tener abierta durante la sesión. El desarrollo está en
[punto-de-partida.md](punto-de-partida.md); esto es sólo lo que se pregunta.

**Antes de empezar, decilo en voz alta:** no hace falta saber nada técnico. Se sale de acá con un
archivo en el idioma de quien contesta, y es contra ese archivo que después se verifica si la app
hizo lo que prometió.

---

## Bloque 0 · Con qué llega

> *Contame qué querés que sea esta app, como se lo contarías a alguien en un ascensor.*

Vago está bien. *"Somos un laboratorio y queremos darle a los pacientes una app donde vean sus
exámenes"* es una entrada válida — todavía no sirve para construir nada, y convertirla es el trabajo
de la próxima hora.

---

## Bloque 1 · Qué ya existe

> **¿Quién es la institución acá?**

No lo des por sentado. La respuesta cambia la forma de la app entera:

| | Qué implica |
|---|---|
| **Hay una** y publica datos | La forma completa: página de descarga, endpoint de eventos, token |
| **No hay** | La persona registra todo. El registro deja de ser reconstruible **desde el día uno** |
| **Híbrida** | La institución manda lo que puede y la persona aporta lo que ella no puede saber |

> *¿Qué le **entrega** hoy esa institución a la persona: un papel, un portal, un correo?*
>
> *¿Y qué le **contesta**? ¿Qué pregunta suya responde sin que tenga que hacer cuentas?*

**No las mezcles.** Entregar datos no es contestar preguntas. Lo que aparezca en la **segunda** queda
descartado: si el portal ya lo contesta, reproducirlo no agrega nada.

---

## Bloque 2 · Las preguntas

> *Olvidate de la app.* **¿Qué diría esta persona en voz alta**, hablando de lo suyo?*

No *"listar exámenes por fecha"*. Sino *"¿me está subiendo el colesterol o fue una mala lectura?"*.

Insistí hasta juntar **tres a cinco**. A cada una:

- ¿La respuesta **cambia lo que hace**? → si no, es decoración
- ¿Alcanza con **un solo dato**, o hace falta la historia? → si alcanza con uno, el portal ya lo daba
- ¿El portal **ya se la contesta**? → volviste al bloque 1

> **Una tiene que ser sobre una AUSENCIA.** *"¿Hace cuánto que no…?"*, *"¿qué me falta?"*, *"¿se me
> venció algo?"*. Ninguna pantalla puede contestar eso, porque una pantalla renderiza lo que tiene.
> Si no sale ninguna así, probablemente estén diseñando un listado con mejor tipografía.

---

## Bloque 3 · Los juicios

**El bloque que no se improvisa.** Para cada cosa que la app va a seguir en el tiempo:

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

> ### Si no saben, NO SE INVENTA
>
> Se anota como pendiente **con nombre y apellido de quién lo sabe**. Un juicio inventado no revienta
> nunca: da una respuesta creíble y equivocada cada vez.

---

## Bloque 4 · Qué se descarta

> *¿Qué dato te parece importante pero, siendo honesto, no le sirve a la persona **en este dominio**?*

Va a la lista de **candidatos a cruce**, no a la basura. El precio de un examen no aporta nada en
salud y es de los datos más relevantes en presupuesto personal.

Si no sale casi nada, no es que la entrevista falló: hay dominios donde este filtro no pesa.

---

## Bloque 5 · La revisión

**No es parte de la misma sentada.** Va **después de la primera pasada de modelado**, y son quince
minutos. Una sola pasada nunca alcanza: modelar siempre destapa lo que la entrevista no preguntó.

> ### La regla que gobierna este bloque
>
> **La persona contesta sobre el negocio. Quién se vuelve una clase lo decide quien modela.**
>
> La prueba: **¿podría contestarla alguien que nunca programó?** Si la pregunta dice *clase*, *tipo*,
> *entidad*, *dominio* o *sustantivo*, está mal formada — reformulala antes de hacerla.

### Tres cacerías

**1 · Una palabra apareció en las preguntas y nadie la desarrolló.**

> *¿Qué te preguntás sobre **X** que no te preguntás sobre **Y**?*
> *Si te pregunto cuánto vas a recibir el mes que viene, ¿de qué estarías seguro y de qué no?*
> *¿Cuáles llegan solos y cuáles hay que salir a buscarlos?*

La primera es la más productiva de todas: es *"¿qué sabe esto que ninguna otra cosa sabe?"* dicho en
idioma de negocio.

**2 · Al modelar hizo falta un dato que nadie mencionó.**

> *¿De dónde sale…?*
> *¿Eso viene en lo que te manda la institución, o lo sabés vos?*

**3 · Se tomó un juicio por obvio.**

Todo lo que quien modela decidió solo, **se lee en voz alta como propuesta** y se deja corregir:

> *Dejamos que un cargo que no llega se avise a los tres días. ¿Te suena bien, o depende de cuál?*

No preguntes *"¿cuál es el umbral?"* — nadie tiene un umbral en la cabeza. Proponé uno y dejá que lo
muevan.

### Cómo se ven las dos formas

| Mal formada | Bien formada |
|---|---|
| ¿Qué **clases** de ingreso hay? | *Si te pregunto cuánto vas a recibir el mes que viene, ¿de qué estarías seguro?* |
| ¿Es el préstamo una **cosa del dominio**? | *¿Te preguntás cuánto te falta para terminar de pagar algo?* |
| ¿Lo que se aparta **es algo** con saldo? | *Cuando te pasa un imprevisto, ¿de dónde sale la plata?* |
| ¿Quién **fija** el presupuesto? | *Cuando decís que algo "no estaba presupuestado", ¿contra qué lo comparás?* |

> **Y una advertencia.** Si contesta todo rápido y sin dudar, sospechá. **Una persona generosa
> contesta igual una pregunta mal formada**, y eso esconde el defecto en vez de mostrarlo. Pasó en la
> corrida donde se descubrió esta regla.

---

## Cuando terminaste

- [ ] ¿Tres a cinco preguntas, en la voz de la persona?
- [ ] ¿Al menos una sobre una **ausencia**?
- [ ] ¿Cada juicio contestado, o anotado con **quién** lo sabe?
- [ ] ¿Está escrito lo que se **descartó**, y por qué?
- [ ] ¿**Cada palabra** que aparece en las preguntas tiene algo detrás? *(la que se salta más)*
- [ ] ¿Lo aprobó quien conoce el negocio, **sin que nadie le tradujera nada**?

Y después de modelar, el bloque 5. **Nunca alcanza con una pasada.**

El último es el que de verdad cierra: si hizo falta explicarle el archivo a quien lo va a aprobar, el
archivo está mal escrito.

---

**Los bloques no van en una sola dirección.** Es normal que el 3 haga aparecer una pregunta que
pertenecía al 2. Cuando pase, volvé. Un bloque cerrado no está clausurado.
