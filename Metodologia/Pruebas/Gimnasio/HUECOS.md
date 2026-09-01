# Huecos — lo que el `Preguntas.md` no alcanzó a decidir

El dominio se construyó el **2026-08-28** con [`Preguntas.md`](Preguntas.md) en la mano y **sin volver
a preguntarle nada a nadie**. Cada vez que hubo que decidir algo que el archivo no decía, se anotó acá
con la decisión que se tomó para poder seguir.

**Nueve huecos.** Que aparezcan no invalida el método: **los nueve se descubrieron modelando, no
discutiendo**, y eso era justamente lo que se quería saber.

> **El estado del modelo:** 19 clases, todas `internal`, cero dependencias, **46 comprobaciones en
> verde**, y ni una referencia al reloj.

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
