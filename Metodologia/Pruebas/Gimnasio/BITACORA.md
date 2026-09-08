# Bitácora — Gimnasio

El cuaderno de laboratorio de este sujeto: cada corrida de la guía sobre él, con lo que se propuso,
lo que se firmó, lo que encontró y cuánto tardó. Se versiona con el sujeto — el tag de cada versión
(`gimnasio-v1`, `gimnasio-v2`…) lo incluye — y `git diff` entre dos tags es la comparación entre
versiones de la guía.

| Versión | Tag | Fecha | Qué cambió en la guía | Qué cambió en el dominio |
|---|---|---|---|---|
| **v1** | `gimnasio-v1` = `8ada9a0` | 2026-08-28 → 09-01 | La primera corrida del punto de partida con un peer del equipo. Sin papel: sólo la entrevista | 19 clases, 46 comprobaciones, el ciclo con journal. **Sin membresía** — huecos 9 y 12 |
| **v2** | `gimnasio-v2` = `37ba8eb` | 2026-09-07 | La regla del descarte del bloque 1, corregida: *descartado como funcionalidad no es descartado del modelo*. **Misma entrevista, sin papel** — mide el método, no el artefacto | **Construido y verificado el mismo día**: la membresía entra —la bandera *En mora* / *Al día*, pagos, cobros, el período en mora derivado— y el incumplimiento la descuenta. 19 → 25 clases; 46 → 69 comprobaciones; el ciclo con journal dos veces sin que los números se muevan |
| **v3** | *(pendiente)* | — | Las cuatro pasadas con un **artefacto real**. La fuente está por definir: la app del teléfono de Jorge es genérica y no sirve | — |

**No se compara v1 con v3 directo:** v2 mide el método con el mismo insumo; v3 mide lo que el papel
agrega. Dos variables, dos corridas.

---

## v2 · Lo que la corrida encontró — 2026-09-07

> **Qué midió y qué no, dicho con precisión.** El v2 corrió contra **dos** cosas: la regla del descarte
> del bloque 1 corregida —una guía **vieja**, el punto de partida— sobre las 19 clases del v1, que se
> construyeron el 28 de agosto antes de que las cuatro pasadas existieran; y **las cuatro pasadas sobre
> una rebanada**: la membresía, seis clases de veinticinco, con su pasada 1, su grilla, su hoja de
> firmas y su especialización. **No es una corrida de la columna vertebral sobre el gimnasio.** Ésa es
> el v3, contra [`Guias/columna-vertebral.md`](../../Guias/columna-vertebral.md), que se escribió el
> mismo día justamente por esto.

**La respuesta esperada estaba fijada de antemano y apareció.** Con la regla del descarte corregida y
la misma entrevista del 28 de agosto, la membresía entró al modelo. Lo medido:

| | v1 (`gimnasio-v1`) | v2 |
|---|---|---|
| Clases del dominio | 19 | **25** — `Membresia`, `EstadoDeMembresia`, `CambioDeEstado`, `Pago`, `Cobro`, `PeriodoEnMora` |
| Comprobaciones en verde | 46 | **69** |
| El incumplimiento del plan 1, con 15 días en mora adentro | *esperaba 39, fue 30, **faltaron 9*** | *esperaba **33**, descontó **6**, fue 30, **faltaron 3*** |
| El ciclo con journal, segunda corrida | rechaza todo, números quietos | rechaza todo —incluidas las tres banderas y el pago—, números quietos |

**Seis de los nueve días que el v1 le cobraba a Sonia no eran incumplimiento: estaba en mora y no
podía entrar.** Es exactamente la *"respuesta creíble y equivocada"* que el hueco 9 describía desde
el 28 de agosto, ahora medida y corregida.

**Lo que N1 cambió respecto de la hoja de firmas tal como se escribió:** el gimnasio no manda una
suspensión con fechas sino **una bandera que cambia** —*en mora*, después *al día*—. El hecho es el
cambio con su fecha; el período se deriva de dos cambios. Es la figura 8.4 de Fowler al pie de la
letra, y eliminó N3: quién está en mora lo decide el gimnasio. Y destapó un hallazgo para la doctrina:
*en mora* **es una evaluación** que la institución manda — el 02 decía que el artefacto nunca trae una;
ahora dice que trae las suyas, contra sus reglas, en un punto, y que la app evalúa sobre la serie y
contra lo declarado.

**Lo que quedó fuera del código, a propósito:** `Urgencia()` sobre la membresía (N4, sin firmar) y el
cobro que no llegó (N6, opcional). **Y lo que se asumió:** que la bandera llega con fecha — hueco 15,
dueño: el artefacto del v3.

**Estado:** código, `Preguntas.md` y `HUECOS.md` actualizados y verificados; **N1 y N2 firmadas
explícitamente el 2026-09-07** — N2 en sus tres partes, las tres tal como estaban implementadas, así
que no hubo cambio de código. Sin commitear y sin tag `gimnasio-v2` hasta que Jorge lo decida.

---

## v2 · La hoja de firmas

*La pasada 3 del v2, escrita el 2026-09-07 para que Jorge la firme antes de la pasada 4. Nada de lo
que está acá entra al código sin firma. Un juicio sin firmar entra por parámetro y se marca ⚠️.*

*Revisada el mismo día con la respuesta de Jorge a N1: el gimnasio manda **una bandera** —*En mora*,
que después cambia a *Al día*—, no una suspensión con fechas. Eso rehízo la pasada 1 y la 2, eliminó
N3 y reformuló N4.*

## Qué es este documento

El v2 aplica **una sola corrección** al gimnasio del 1 de setiembre: la regla del bloque 1 que decía
*descartado como funcionalidad = descartado del modelo*. Con la regla corregida, **la membresía entra
al modelo** — estaba declarada en la Parte 1 del `Preguntas.md` y a la vez en *"lo que el gimnasio ya
contesta"*, y el dominio se construyó sin ella. Es el [hueco 12](HUECOS.md), y su consecuencia es el
hueco 9: **hoy un período en mora cuenta como incumplimiento del plan.**

Meter la membresía obliga a decidir cosas que la entrevista del 28 de agosto no decidió. Éstas son. Cada
una viene **con un valor propuesto** —regla *proponé, no preguntés en frío*— y la fuente de la
propuesta, para que corregir sea fácil e inventar no haga falta.

**La respuesta esperada de la corrida está fijada de antemano**, y es lo que la hace un control:
después del v2, la fila *membresía* de la Parte 1 tiene algo en *dónde vive*, los huecos 9 y 12 cierran,
las 46 comprobaciones siguen en verde, y el ciclo con journal corrido dos veces sigue rechazando todo
en la segunda.

> **Lo que este documento no puede hacer, y hay que decirlo:** el gimnasio **no tiene artefacto**. Los
> sustantivos de abajo salen de la entrevista y de una respuesta de Jorge, no de un papel leído campo
> por campo. **Su forma es propuesta, no leída.** El v3 los confirma o los corrige con un artefacto
> real —la app del teléfono de Jorge es genérica y no sirve; la fuente está por definir—, y lo que
> corrija es exactamente la medida de lo que una entrevista sin papel pierde.

---

## Pasada 1 · lo que el gimnasio manda de la membresía

Lo dijo la entrevista —*"pagos, próximo cobro, atrasos"*— y lo precisó Jorge el 2026-09-07:

| Palabra | Espacio de valor | Verbo que lo produce | Quién lo tiene |
|---|---|---|---|
| **la bandera** — *En mora* / *Al día* | **un conjunto cerrado de dos**, y **cambia con fecha** | el gimnasio **la cambió** el día D | el gimnasio. **Es su evaluación**, y llega como hecho |
| **el pago** | una fecha y un monto | el gimnasio **registró un pago** | el gimnasio |
| **el próximo cobro** | una fecha de vencimiento y un monto | el gimnasio **anunció un cobro** | el gimnasio |
| **el atraso** — *"si está atrasada"* | **no se calcula acá**: es la bandera | — | el gimnasio lo dice; la app **no lo recalcula** |
| **el período en mora** — lo que el hueco 9 llamaba *suspensión* | desde–hasta, **derivado** de dos cambios de bandera | — es una comparación sobre los cambios | se calcula |

**Ninguno lo aporta Sonia.** El journal sigue siendo reconstruible.

> **Un hallazgo de doctrina, que va al 02:** *en mora* es una **evaluación** que el gimnasio hace de la
> cuenta de Sonia — y el 02 decía que el artefacto *"nunca trae una evaluación"*. Sí trae: la suya,
> contra sus reglas, en un instante. Lo que la institución no hace es evaluar **sobre la historia de la
> persona** ni **contra su intención**. Ésa es la columna de contraste de la app; la del gimnasio llega
> como hecho. Es la geometría del paper 8: el gimnasio es la autoridad sobre su mora, y la app la
> registra en vez de recalcularla.

## Pasada 2 · dónde cae cada cosa

| | Hecho | Intención | Contraste |
|---|---|---|---|
| **Conocimiento** | **`EstadoDeMembresia`** — conjunto cerrado {`AlDia`, `EnMora`} | *la cadencia esperada del cobro* (N6, opcional) | — |
| **Operación** | **`CambioDeEstado`** (bandera + fecha) · `Pago` | `Cobro` — lo que el gimnasio dice que **va a** cobrar | **`PeriodoEnMora`** — derivado de dos cambios · **`Incumplimiento`**, que cambia · **la situación actual**, calculada |

Tres cosas que la grilla decide sola:

- **Es la figura 8.4 de Fowler, literal.** `Action Status` tiene instancias —*proposed, started,
  suspended…*— y `Suspension` es un `Time Period` sobre la acción. Acá el estado es la bandera, y el
  período en mora es el tiempo entre dos cambios. **El hecho es el cambio; el período se deriva.**
- **`Cobro` es intención y `Pago` es hecho**: la *Proposed Action* del gimnasio y su *Implemented
  Action* (fig. 8.2). Mismo par que `ReservaPropuesta` / `ReservaDeclarada` en la municipalidad.
- **"En mora" es una fase, no un tipo.** La membresía entra y sale; no puede ser `MembresiaEnMora :
  Membresia`. Es la pregunta de rigidez (glosario nº 19), y la forma es la de E36: el estado en un
  campo, cambiado por una transición nombrada, y el actor pregunta `EstaEnMora()`, no recibe el objeto.

```mermaid
classDiagram
    direction LR
    class Membresia {
        <<OPERACIÓN · el relator de "socia">>
        EstaEnMora() bool
        EstabaEnMoraEl(fecha) bool
        DiasEnMoraEntre(desde, hasta) int
        PeriodosEnMora() lista
    }
    class EstadoDeMembresia {
        <<CONOCIMIENTO · conjunto cerrado>>
        AlDia
        EnMora
    }
    class CambioDeEstado {
        <<HECHO · la bandera cambió>>
        fecha
        estado
    }
    class Pago {
        <<HECHO>>
        fecha
        monto
    }
    class Cobro {
        <<INTENCIÓN · lo que el gimnasio va a cobrar>>
        vence
        monto
    }
    class PeriodoEnMora {
        <<CONTRASTE · derivado de dos cambios>>
        desde
        hasta — o abierto
    }
    class Incumplimiento {
        <<CONTRASTE · ya existe, CAMBIA>>
        diasEsperados — descontando la mora
        diasQueFue
    }
    Membresia --> CambioDeEstado : la historia de la bandera
    Membresia --> Pago
    Membresia --> Cobro
    CambioDeEstado --> EstadoDeMembresia
    PeriodoEnMora --> CambioDeEstado : entre uno y el siguiente
    Incumplimiento --> PeriodoEnMora : descuenta los días
    note for CambioDeEstado "LO QUE EL GIMNASIO MANDA.<br/>Su evaluación, con fecha.<br/>La app no la recalcula: la registra."
    note for Incumplimiento "ES EL HUECO 9.<br/>Hoy cuenta los días en mora<br/>como incumplidos."
```

---

## Pasada 3 · los juicios, para firmar

| # | El juicio | Propuesta | De dónde sale | Qué decide en el modelo | Firma |
|---|---|---|---|---|---|
| **N1** | **Origen de la suspensión** | ✅ **Contestado por Jorge:** el gimnasio manda la bandera *En mora* / *Al día*, con fecha. El período en mora se deriva de dos cambios | Jorge, 2026-09-07 | `CambioDeEstado` es hecho; `PeriodoEnMora` es contraste derivado; **la app no calcula atraso propio** | ✅ |
| **N2** | **¿Un período en mora cuenta como incumplimiento?** *(pendiente nº 2 del `Preguntas.md`)* | **No.** *Reducir los días esperados de un plan por cada día que la membresía estuvo en mora dentro del plan* | Hueco 9 desde el 28-08, y Fowler 8.4: durante la suspensión las acciones propuestas no se esperan. **Firmado el 2026-09-07 en sus tres partes:** el fondo; **semanas enteras**, la misma ley que la duración (hereda a propósito la fuga del hueco 6); y **las asistencias durante la mora cuentan** — un hecho no se borra porque contradiga una regla | `IncumplimientoDe(plan)` calcula los esperados sobre los días **no en mora**. Es el cambio que cierra el hueco 9 | ✅ |
| ~~N3~~ | ~~Gracia antes del atraso~~ | **Eliminado por N1.** Quién está en mora lo decide el gimnasio; la app no tiene gracia que declarar | — | Un parámetro menos | — |
| **N4** | **Aviso al pasar a mora.** Cuando la bandera cambia a *En mora*, ¿la app avisa? ¿Lo decide el monto o la clase? | **Avisa, y lo decide la clase**: un cambio a mora es aviso negativo, del mismo peso por ₡5.000 que por ₡50.000. Un solo nivel | Juicio 6 en los dos dominios anteriores: *la clase, no el monto* | `Membresia.Urgencia()` → negativa si `EstaEnMora()`, ninguna si no | ☐ |
| **N5** | **Lo propio.** ¿Qué sabe la membresía que nada más sabe? | **Cuándo el plan estuvo en mora — y por eso cuándo un incumplimiento no fue incumplimiento** | Juicio 7. Y es el **relator** de *socia* (glosario nº 20) | `Membresia` se gana la clase: **juzga** (N2, N4), no sólo parametriza | ☐ |
| **N6** | **Ritmo del cobro** *(opcional)* | **Mensual**, por parámetro. Da la ausencia *"¿el gimnasio dejó de cobrarme?"* = *"¿sigo siendo socia?"* | — | Si se firma, `CobroQueNoLlego(fecha, cadencia)`. Si no, va al v3 | ☐ |

**Lo que el v2 NO hace, y ya estaba decidido:** no hay pantalla de pagos, cobros ni mora. El gimnasio
los contesta completos. **El dominio los sabe; el actor no los muestra.**

### Un hueco nuevo, descubierto al leer la respuesta de N1

**¿La bandera llega con la fecha en que el gimnasio la cambió, o sólo como estado actual?** Si el
feed dice *"estado: en mora"* sin fecha, la única fecha disponible es la de recepción — y eso es leer
el reloj, que el dominio no hace. El v2 asume **fecha en el evento**. Es un hueco de forma del dato,
**descubierto**, y su dueño es **el artefacto del v3**.

### Consultabilidad — cada juicio produce una consulta con predicado total

| Juicio | Consulta nombrada | Total porque… |
|---|---|---|
| N1 | `Membresia.EstaEnMora()` → `bool` · `EstabaEnMoraEl(fecha)` → `bool` | sin cambios registrados, `false` |
| N2 | `Membresia.DiasEnMoraEntre(desde, hasta)` → `int` · `Trayectoria.IncumplimientoDe(plan)` | sin mora descuenta 0; la segunda sigue guardada por `Termino` |
| N1 | `Membresia.PeriodosEnMora()` → lista de períodos, el último puede estar abierto | vacía si nunca hubo mora; el actor la proyecta hoja por hoja (regla 13) |
| N4 | `Membresia.Urgencia()` | siempre devuelve una `Urgencia` |
| N6 | `Membresia.CobroQueNoLlego(fecha, cadencia)` | sólo si se firma |

### Los comandos nuevos y su `Check` — reglas 17 y 18

| Comando | `Puede…` en la raíz | Qué rechaza |
|---|---|---|
| `CambiarEstadoDeMembresia(estado, fecha)` | `PuedeCambiarEstado` | estado desconocido (resolutor total, regla 11); **el mismo estado que ya tiene** — es el mismo hecho otra vez, **identidad**, regla 18; una fecha anterior al último cambio |
| `RegistrarPago(fecha, monto)` | `PuedeRegistrarPago` | monto ≤ 0; el mismo pago dos veces (**identidad**) |
| `AnunciarCobro(vence, monto)` | `PuedeAnunciarCobro` | monto ≤ 0; un cobro que ya existe con ese vencimiento (**identidad**) |

---

## Los juicios del 28 de agosto, para volver a firmar

Jorge decidió firmar todo de nuevo. Los ocho de la Parte 2 del `Preguntas.md`, con la decisión que
tienen hoy:

| # | El juicio | La decisión de hoy | Firma |
|---|---|---|---|
| 1 | Hacia dónde es deseable que se mueva una medida | **Lo da el objetivo**, no la medida. Excepción: masa muscular, que sube y punto | ☐ |
| 2 | Cuándo una medición no es creíble | Depende del tiempo y de qué medida. **Valores abajo, ⚠️ sin firmar** | ☐ |
| 3 | Cada cuánto se espera una medición | **Al final de cada plan.** No es calendario | ☐ |
| 4 | Qué es ruido y qué merece aviso | **Valores abajo, ⚠️ sin firmar** | ☐ |
| 5 | Se vigila cada medida por separado o el conjunto | **Por separado** — el ritmo es lento | ☐ |
| 6 | Qué hace urgente un hallazgo | **Magnitud + signo.** Existe la urgencia positiva | ☐ |
| 7 | Qué sabe cada medida que ninguna otra sabe | Su rango y qué parte del perfil necesita | ☐ |
| 8 | Qué es normal, bajo o alto | **La medida contra el perfil.** Distinto del 1; hacen falta los dos | ☐ |

**Los valores del 2 y del 4** — los dio un experto IA el 28 de agosto y **nadie del gimnasio los
validó**. Entran por parámetro; firmarlos hoy no los quema en el código, los saca del ⚠️:

| Medida | No es creíble si… | Ruido | Merece aviso | Firma |
|---|---|---|---|---|
| Peso | cambió más de **1 % del peso corporal por semana** | ± 1 kg en un trimestre | sostenido en la misma dirección dos mediciones seguidas | ☐ |
| % de grasa | saltó más de **5 puntos** entre dos mediciones de un trimestre | < 1 punto | **≥ 2 puntos** en un plan | ☐ |
| Cintura | cambió más de **1 cm** con menos de **2 semanas** de diferencia | < 1 cm | **≥ 2 cm** | ☐ |
| Masa muscular | igual que % de grasa — mismo aparato | < 0,5 kg | ≥ 1 kg | ☐ |

**Si alguno no se firma, queda ⚠️ y por parámetro, exactamente como hoy.** Eso no bloquea el v2.

---

## Después de firmar — lo que la pasada 4 hace con esto

1. `Preguntas.md`: la fila *membresía* de la Parte 1 pasa de ❌ a `Membresia`, `EstadoDeMembresia`, `CambioDeEstado`, `Pago`, `Cobro`; N1, N2, N4, N5 (y N6 si se firma) entran a la Parte 2; el pendiente nº 2 se cierra.
2. `HUECOS.md`: los huecos 9 y 12 cierran hacia el `Preguntas.md`, con la fecha. Entra el hueco nuevo — *¿la bandera trae fecha?* — como **descubierto, dueño: el artefacto del v3**.
3. Dominio: `EstadoDeMembresia` (cerrado, con resolutor total), `CambioDeEstado`, `Pago`, `Cobro`, `PeriodoEnMora`, `Membresia`; `Trayectoria` gana la membresía y los tres `Puede…`; `IncumplimientoDe` descuenta la mora. **Ninguna constante nueva en el dominio.**
4. `GimnasioAPI`: tres comandos con `Check`, tres consultas. `Medir`: los pasos nuevos, y la segunda corrida tiene que seguir rechazando todo.
5. Consola: *"un plan con dos semanas en mora espera dos semanas menos"*, *"la misma bandera dos veces se rechaza"*, *"un período en mora abierto se descuenta hasta el fin del plan"*.
6. `git tag gimnasio-v2`, y el diff contra `gimnasio-v1` es la primera comparación entre versiones.

---

**Fuentes de las formas propuestas.** Fowler, *Analysis Patterns*, cap. 8 — leído de las **figuras** del suplemento UML (De Swert), no del texto: fig. 8.2 *Proposed / Implemented Action*, fig. 8.4 `Action Status` = {proposed, started, completed, suspended, abandoned} y `Suspension` como `Time Period` sobre la acción. **La definición en prosa queda por verificar contra el libro.** La rigidez, de OntoUML (glosario nº 19). La forma de la fase, de E36. La autoridad del gimnasio sobre su mora, del paper 8.

*Firmado por ____________________ el ____ / ____ / 2026.*
