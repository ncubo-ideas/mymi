# Huecos — lo que el `Preguntas.md` no alcanzó a decidir

El dominio se construyó el **2026-08-28** con [`Preguntas.md`](Preguntas.md) en la mano y **sin volver
a preguntarle nada a nadie**. Cada vez que hubo que decidir algo que el archivo no decía, se anotó acá
con la decisión que se tomó para poder seguir.

**Once huecos.** Que aparezcan no invalida el método: **los once se descubrieron construyendo, no
discutiendo** — nueve al modelar y dos más al atarlo al perform, y eso era justamente lo que se
quería saber.

> **El estado del modelo:** 19 clases, todas `internal`, cero dependencias, **46 comprobaciones en
> verde**, ni una referencia al reloj, y el ciclo con journal **corrido dos veces sin que los
> números se muevan**.

---

## La tabla

| # | Qué faltaba | Dónde muerde | Qué se decidió para seguir |
|---|---|---|---|
| **1** | **Cómo se agrupan las edades** para el rango de grasa | `PorcentajeDeGrasa.ClaveDeReferencia` | La clave lleva la **edad exacta**. Ver abajo: se dejó feo a propósito |
| **2** | Si Sonia declara **a qué valor quiere llegar**, o sólo una dirección | `Meta` | El valor es **opcional**, y cuando no está **se dice** en vez de inventarlo |
| **3** | Cuántos **días de gracia** hay entre el fin de un plan y su medición | `Trayectoria.PlanesQueTerminaronSinMedicion` | Entra **por parámetro**. Sin él, la ausencia no se puede calcular |
| **4** | Qué pasa con **dos metas sobre la misma medida** | `Objetivo.Agregar` | Se **rechaza la segunda**. Es honesto; si es correcto lo decide el negocio |
| **5** | Qué pasa si **una medida no tiene meta** | `Objetivo.DireccionPara` | **Revienta.** No se inventa una dirección — pero eso deja la medida sin tendencia |
| **6** | Qué se hace con la **semana incompleta** de un plan | `Plan.SemanasQueDuro` | División entera: **13 semanas y 4 días son 13 semanas**. Esos 4 días desaparecen del incumplimiento |
| **7** | Qué pasa con las **asistencias fuera de todo plan**, y con **planes que se solapan** | `Trayectoria.IncumplimientoDe` | Un día que cae en dos planes **cuenta para los dos**; uno que no cae en ninguno **no lo cuenta nadie** |
| **8** | Si un **plan sin ejercicios prescritos** es válido | `Plan` | Se **permite**. Un plan puede ser sólo frecuencia |
| **9** | Si un período **suspendido por atraso de pago** cuenta como incumplimiento | `Trayectoria.IncumplimientoDe` | **No se modeló.** Ya venía declarado como pendiente 2 en el `Preguntas.md` |
| **10** | **Cómo se identifica un plan.** El gimnasio no le pone nombre al papel | `Trayectoria.PlanQueEmpezo` | Se usa la **fecha de inicio**. Si dos planes empezaran el mismo día, se cae — y nada lo impide |
| **11** | Que las cuatro medidas necesitaban un **resolutor total** para poder nombrarse desde un comando | `Medida.Crear` | Se agregó. No se ve modelando en C#; sólo aparece al atarlo al perform |

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

## 1 · El parámetro `@genero` tapó al tipo `Genero`

```
Unknown property or method 'PorNombre' on type 'String'
```

Es la **regla 10** del substrato, y me mordió **después de escribir un comentario diciendo que la
estaba evitando**. Vale anotar el detalle que la regla no dice: **la colisión es insensible a
mayúsculas.** El parámetro se llamaba `genero` en minúscula y el tipo `Genero`; alcanzó.

## 2 · Un `ToString()` no alcanza para imprimir

```
'Print estado' emits a value of type 'Lectura', which is material, not a value.
The projection is the actor's to author, not the material's: emit the leaves the
view needs, or state the rendering explicitly (for instance '.ToString()').
```

`Lectura`, `Tendencia` y `Urgencia` tienen `ToString()` sobrescrito **justamente para poder
imprimirse**, y no se llama solo. Hay que escribir `lectura.ToString()` en el script.

## 3 · Tres journals envenenados por el mismo error, que no es el que la regla 1 describe

La regla 1 dice *"guardá con un `Check` todo comando que el dominio pueda rechazar"*. Lo hice. Y aun
así:

```
Rehydration execution failed. EntryId=55,
Cause: Ya hay una meta para 'cintura'.
```

**Lo que falta en la regla es que el `Check` tiene que preguntar EXACTAMENTE lo que el dominio va a
rechazar.** Mis Checks preguntaban cosas razonables pero distintas:

| Comando | El `Check` preguntaba | El dominio rechazaba por |
|---|---|---|
| Agregar meta | ¿hay objetivo? | la meta **ya existía** |
| Prescribir ejercicio | ¿existe el plan? | el ejercicio **ya estaba prescrito** |
| Terminar plan | ¿ya terminó? | ...y también si **terminaría antes de empezar** |
| Registrar medida | ¿el nombre existe? | ...y también si **ya estaba en esa hoja** |

Un `Check` que pregunta otra cosa **deja pasar el comando**, el dominio revienta a mitad de la
escritura, y esa entrada se replaya en cada arranque para siempre.

De ahí salió la simetría que el dominio tiene ahora: **por cada comando, un predicado total en la
raíz que junta todas las razones por las que ese comando fallaría.** `PuedeDeclararPerfil`,
`PuedeRegistrarPlan`, `PuedePrescribir`, `PuedeTerminarPlan`, `PuedeRegistrarMedida`,
`PuedeRegistrarAsistencia`.

## 4 · Y el peor fue el comando que no podía fallar

`Asistio` no rechaza nada: agrega un día a una lista. Por eso no le puse `Check`. En la segunda
corrida contó los mismos 30 días otra vez:

```
primera corrida:  {"esperados":39,"fue":30,"faltaron":9,"cumplio":false}
segunda corrida:  {"esperados":39,"fue":60,"faltaron":0,"cumplio":true}
```

**No falló.** Le dijo a Sonia que cumplió su plan cuando le faltaron nueve días. Una respuesta creíble
y equivocada es peor que una excepción, porque nadie la va a investigar.

> **La lección, y va más allá de este dominio:** un comando que no puede fallar **igual necesita
> idempotencia si el mismo hecho puede volver a llegar**. Y siempre puede: un replay es exactamente
> eso.

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
