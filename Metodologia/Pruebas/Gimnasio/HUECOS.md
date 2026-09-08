# Huecos — lo que el `Preguntas.md` no alcanzó a decidir

El dominio se construyó el **2026-08-28** con [`Preguntas.md`](Preguntas.md) en la mano y **sin volver
a preguntarle nada a nadie**. Cada vez que hubo que decidir algo que el archivo no decía, se anotó acá
con la decisión que se tomó para poder seguir.

**Catorce huecos en el v1, y uno más que el v2 descubrió.** Que aparezcan no invalida el método: **doce
se descubrieron construyendo, no discutiendo** —nueve al modelar, dos al atarlo al perform y uno al
revisar la Parte 1 contra las clases—, y eso era justamente lo que se quería saber. Los otros dos
venían **declarados sin firmar** desde la entrevista. **El v2 (2026-09-07) cerró el 9 y el 12** —la
membresía— y abrió el 15.

> **Este archivo cambió de forma el 2026-09-01**, cuando el `HUECOS.md` pasó a ser doctrina en el
> [capítulo 3](../../Guias/03-construir-la-app.md) §8 bis. Dos cosas:
>
> - **Entró la columna de origen**, y con ella los dos juicios sin firmar que hasta ahora vivían sólo
>   en el `Preguntas.md`. Un solo registro de lo no decidido, que es lo que consume el bloque 5.
> - **Los hallazgos sobre el motor se fueron a las guías**, que es donde le sirven a los demás
>   negocios. Abajo quedan los punteros y lo que todavía es de acá.

> **El estado del modelo — v2, 2026-09-07:** 25 clases, todas `internal`, cero dependencias, **69
> comprobaciones en verde** (46 del v1 + 23 de la membresía), ni una referencia al reloj, y el ciclo
> con journal **corrido dos veces sin que los números se muevan**. El v1 (`gimnasio-v1`) tenía 19 y 46.

---

## La tabla

La columna **origen** distingue lo que la entrevista dejó anotado sin firmar de lo que nadie había
previsto. Se cierran distinto: lo declarado lo firma quien sabe del negocio, lo descubierto se lee
como propuesta en el bloque 5. La define el [capítulo 3](../../Guias/03-construir-la-app.md) §8 bis.

| # | Qué falta | Origen | Dónde muerde | Qué se decidió para seguir |
|---|---|---|---|---|
| **1** | **Cómo se agrupan las edades** para el rango de grasa | descubierto | `PorcentajeDeGrasa.ClaveDeReferencia` | La clave lleva la **edad exacta**. Ver abajo: se dejó feo a propósito |
| **2** | Si Sonia declara **a qué valor quiere llegar**, o sólo una dirección | descubierto | `Meta` | El valor es **opcional**, y cuando no está **se dice** en vez de inventarlo |
| **3** | Cuántos **días de gracia** hay entre el fin de un plan y su medición | descubierto | `Trayectoria.PlanesQueTerminaronSinMedicion` | Entra **por parámetro**. Sin él, la ausencia no se puede calcular |
| **4** | Qué pasa con **dos metas sobre la misma medida** | descubierto | `Objetivo.Agregar` | Se **rechaza la segunda**. Es honesto; si es correcto lo decide el negocio |
| **5** | Qué pasa si **una medida no tiene meta** | descubierto | `Objetivo.DireccionPara` | **Revienta.** No se inventa una dirección — pero eso deja la medida sin tendencia |
| **6** | Qué se hace con la **semana incompleta** de un plan | descubierto | `Plan.SemanasQueDuro`, y desde el v2 `Plan.DiasEsperadosDescontando` | División entera: **13 semanas y 4 días son 13 semanas**. Esos 4 días desaparecen del incumplimiento. **El v2 descuenta la mora con la misma ley** —15 días en mora son 2 semanas—, a propósito: una sola fuga en una sola dirección, no dos |
| **7** | Qué pasa con las **asistencias fuera de todo plan**, y con **planes que se solapan** | descubierto | `Trayectoria.IncumplimientoDe` | Un día que cae en dos planes **cuenta para los dos**; uno que no cae en ninguno **no lo cuenta nadie** |
| **8** | Si un **plan sin ejercicios prescritos** es válido | descubierto | `Plan` | Se **permite**. Un plan puede ser sólo frecuencia |
| ~~9~~ | ~~Si un período **suspendido por atraso de pago** cuenta como incumplimiento~~ — **CERRADO el 2026-09-07, v2.** No cuenta: los días en mora dentro del plan se descuentan de lo esperado, en semanas enteras | descubierto | `Trayectoria.IncumplimientoDe`, `Plan.DiasEsperadosDescontando`, `Membresia.DiasEnMoraEntre` | Medido contra el mismo plan del v1: *esperaba 39, faltaron 9* → *esperaba 33, descontó 6, faltaron 3*. Seis de los nueve días no eran incumplimiento. Juicio 10 del `Preguntas.md`, **firmado por Jorge el 2026-09-07** en sus tres partes |
| **10** | **Cómo se identifica un plan.** El gimnasio no le pone nombre al papel | descubierto | `Trayectoria.PlanQueEmpezo` | Se usa la **fecha de inicio**. Si dos planes empezaran el mismo día, se cae — y nada lo impide |
| **11** | Que las cuatro medidas necesitaban un **resolutor total** para poder nombrarse desde un comando | descubierto | `Medida.Crear` | Se agregó. No se ve modelando en C#; sólo aparece al atarlo al perform |
| ~~12~~ | ~~**La membresía no está modelada**, estando declarada en la Parte 1~~ — **CERRADO el 2026-09-07, v2.** Entró con la regla del descarte corregida: `Membresia`, `EstadoDeMembresia`, `CambioDeEstado`, `Pago`, `Cobro`, `PeriodoEnMora` | descubierto | antes no mordía en ninguna clase; ahora `Trayectoria.Membresia` | De él salió la corrección del bloque 1 del [punto de partida](../../Guias/punto-de-partida.md), y del v2 salió el hueco 15 |
| **15** | **¿La bandera del gimnasio llega con la fecha en que cambió, o sólo como estado actual?** El v2 asume fecha en el evento. Si el feed dijera sólo *"estado: en mora"*, la única fecha sería la de recepción — y eso es leer el reloj | descubierto — al leer la respuesta a N1 | `CambioDeEstado.Fecha`, y todo `PeriodoEnMora` que sale de ella | Se asume **fecha en el evento**. Sin ella no se puede derivar ningún período honestamente | **el artefacto del v3** |
| **13** | **Cuándo una medición no es creíble** — juicio 2 | **declarado** | `Referencia` | Valores de un experto IA, por parámetro. ⚠️ **Sin firmar**: nadie del gimnasio los validó |
| **14** | **Qué es ruido y qué merece aviso** — juicio 4 | **declarado** | `Referencia`, `Urgencia` | Igual que el 13. ⚠️ **Sin firmar** |

---

## Los tres que hay que mirar de cerca

### 1 · El tramo de edad, y por qué se dejó feo

El `Preguntas.md` dice que el rango de grasa **se mueve con la edad**. No dice cómo se agrupan las
edades. Hoy la clave sale así:

```
grasa:femenino:42
```

Eso significa **una referencia por cada año de vida**, que es impracticable. Y se dejó así a
propósito: si la clave hubiera salido `grasa:femenino:40-49` con tramos inventados por mí, el hueco
quedaba tapado y nadie lo vería nunca. **Que la clave se vea mal es lo que obliga a decidir el tramo.**

### 2 · «Qué me falta» necesita un número que nadie declaró

La pregunta 4 del `Preguntas.md` es *"¿qué me falta para cumplir mi objetivo?"*. Al modelar aparece
que **el objetivo, tal como se declaró, es una dirección** — *bajar de peso* — y una dirección no
tiene un "cuánto falta".

Se resolvió dejando el valor opcional y **contestando que no se sabe** cuando no está, en vez de
inventar una meta. Es la misma salida que tomó Presupuesto con el rendimiento de un bien.

> **Pero la pregunta original queda a medias**, y eso es lo que hay que llevarle al negocio: o Sonia
> declara un número, o la pregunta se reformula a *"¿voy hacia donde quiero?"*, que es otra cosa.

### 6 y 7 · El incumplimiento tiene dos fugas que nadie vio

Las dos salieron de escribir `IncumplimientoDe`, y ninguna estaba en el archivo:

**La semana incompleta.** Un plan del 5 de enero al 6 de abril dura 13 semanas y pico. Los días de
ese "pico" no se cuentan como esperados, así que **si faltó justo esos días, no aparece**.

**Los planes que se solapan.** Si dos planes cubren el mismo martes, ese martes cuenta como
asistencia de los dos. Y si Sonia va un día que no cae en ningún plan, **ese día no lo cuenta nadie**
— entrenó y para el modelo no existe.

Ninguna de las dos es grave con los datos de hoy. Las dos se vuelven graves apenas los planes dejen
de ser perfectamente consecutivos, y **nada garantiza que lo sean**.

---

## Lo que este ejercicio le agregó a la Metodología

Tres cosas que no salieron del gimnasio sino de **modelarlo**, y que valen para cualquier dominio:

1. **Un juicio declarado puede tener una excepción que lo rompe**, y hay que dejarle lugar en el
   modelo. La dirección la da el objetivo — salvo la masa muscular. Si `DireccionDeseable` hubiera
   sido un método del objetivo en vez de virtual en la medida, esa verdad no habría tenido dónde
   vivir más que en un `if` por tipo.

2. **Dos juicios sobre el mismo número no se colapsan en uno.** La lectura sale del perfil; la
   tendencia, del objetivo. Hay una prueba con un valor **alto y mejorando** y otra con uno **normal
   y empeorando**: si se hubieran fundido, la app mentiría por omisión en la mitad de los casos.

3. **Dejar un hueco visible en el código vale más que taparlo bien.** La clave
   `grasa:femenino:42` es el mejor recordatorio del hueco 1 que cualquier `TODO`.

---

# Lo que encontró atarlo al perform

**Las 46 comprobaciones estaban en verde y el dominio no se podía hospedar.** Eso es lo que el
capítulo 3 §8 avisa —*"un dominio no está terminado hasta que un perform lo tocó"*— y acá se cumplió
cuatro veces seguidas.

> **Cuatro de los cinco hallazgos ya son doctrina, y por eso acá quedan como punteros.** Es la regla
> del [capítulo 3](../../Guias/03-construir-la-app.md) §8 bis: un hallazgo sobre el motor metido
> adentro del `HUECOS.md` de un negocio es un hallazgo que nadie más va a encontrar. Se escribieron en
> las guías el 2026-09-01.

| Qué pasó | Dónde vive ahora |
|---|---|
| El parámetro `@genero` tapó al tipo `Genero`, y el error acusaba a `String` | [Regla 10](../../Guias/reglas-del-substrato.md#10--un-parámetro-le-puede-robar-el-nombre-a-un-tipo) — **ya lo decía**, ver abajo |
| Un `ToString()` sobrescrito no se llama solo desde el `print` | [Regla 13](../../Guias/reglas-del-substrato.md#13--el-print-proyecta-un-solo-nivel), último punto |
| Tres journals envenenados: el `Check` preguntaba **otra cosa** que la que el dominio rechazaba | [Regla 17](../../Guias/reglas-del-substrato.md#17--el-check-tiene-que-preguntar-exactamente-lo-que-el-dominio-rechaza) |
| `Asistio` no podía fallar, no llevaba `Check`, y contó los mismos 30 días dos veces | [Regla 18](../../Guias/reglas-del-substrato.md#18--todo-comando-necesita-check-incluso-el-que-no-puede-fallar) |

Los dos últimos son los que valen, porque **corrigen a la regla 1**: se la cumplió al pie de la letra
y aun así se envenenaron tres journals. Y el de `Asistio` fue el peor de toda la corrida justamente
porque **no falló**:

```
primera corrida:  {"esperados":39,"fue":30,"faltaron":9,"cumplio":false}
segunda corrida:  {"esperados":39,"fue":60,"faltaron":0,"cumplio":true}
```

Le dijo a Sonia que cumplió su plan cuando le faltaron nueve días. **Una respuesta creíble y
equivocada es peor que una excepción, porque nadie la va a investigar.**

## El único que se queda acá, porque no es del motor

**La regla 10 estaba completa y se rompió igual.** Termina diciendo *"ni siquiera con otra
capitalización"*, y está así desde que se escribió el documento —**antes** de esta corrida—. El
parámetro se llamaba `genero` en minúscula, el tipo `Genero`, y alcanzó. El comentario de
`GimnasioAPI.cs` que decía que la estaba evitando se escribió **antes** de romperla.

> Lo que eso muestra no es un hueco de la regla: es que **la prosa correcta no evitó el error**. No
> pide más documentación. Pide que la colisión se impida donde se escribe el nombre del parámetro —
> un chequeo, o un generador que nunca emita uno que colisione con un tipo del dominio.

## Y dos cosas que el perform pidió y el dominio no tenía

**El plan necesitaba un nombre.** En C# alcanzaba con la referencia al objeto; desde un script hay
que poder decir *cuál* plan. Se usó la fecha de inicio, que es lo único que lo distingue — y eso abre
el **hueco 10**: si dos planes empezaran el mismo día, esto se cae, y nada lo impide.

**Las cuatro medidas necesitaban un resolutor total.** `new Cintura(88)` no se puede escribir desde un
script: un ternario no unifica subclases (regla 12) y un conjunto cerrado sin resolutor es
inalcanzable (regla 11). De ahí `Medida.Crear(nombre, valor)` — el **hueco 11**.

---

## El estado final

| | |
|---|---|
| Comprobaciones del dominio puro | **46, en verde** |
| El ciclo con journal | corre, y **corrido dos veces los números no se mueven** |
| Los dos juicios, cruzando el perform | `"estado":"alto"` · `"movimiento":"mejora"` · `"urgencia":"positiva"` |
| La ausencia | `{"elPlan":[{"empezo":"04/07/2026","termino":"07/06/2026","semanas":12}]}` |
