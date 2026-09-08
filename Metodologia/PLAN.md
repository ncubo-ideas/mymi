# Plan — la guía de la columna vertebral

*Escrito el 2026-09-07 con las tres decisiones de Jorge ya tomadas. Se actualiza en cada hito; el
histórico de cambios lo lleva la carpeta, no este archivo.*

## Qué se está construyendo, en una frase

**Una guía que produce la columna vertebral de un dominio** — los sustantivos con su espacio de valor,
la grilla, los juicios firmados y la especialización — de forma que un ciclo constructor ↔ revisor la
pueda correr, con la persona firmando lo que no se puede derivar. Todo lo demás —proveedor, teléfono,
superficies— es **enchufable** y va después.

**Por qué la columna vertebral primero:** es ~75 % del trabajo, y es la parte que ninguna herramienta
puede derivar del dato ([01 §3](Investigacion/01-estado-del-arte.md)). El sustrato se genera; los juicios se firman.

## Las tres decisiones tomadas el 2026-09-07

| | Decisión | Consecuencia inmediata |
|---|---|---|
| **Especializar** | **Barra de activo por defecto (E40), con frontera.** Clase cuando la fila **juzga** por sí misma —rango, dirección, qué severidad o acción elige—; instancia del catálogo cuando sólo **parametriza** una operación compartida —cadencia, fórmula, monto—. El Curador (*Replace Subclass with Fields*) queda como **piso**. *La frontera salió el mismo día, de probar la regla contra la municipalidad: literal, habría hecho diez clases de los diez rubros* | Resuelve **T3** a favor del código: `Sodio` y `Potasio` se quedan como clases; los rubros siguen como `Rubro.X`. ✅ Propagado a 02 (pasada 4, criterios), 03 (tabla), 04 (once de once), 05 (nota de control), glosario nº 12 |
| **Versionado** | **Tags sobre una sola carpeta.** `gimnasio-v1` = `8ada9a0` (2026-09-01). La comparación es `git diff gimnasio-v1..gimnasio-v2 -- Metodologia/Pruebas/Gimnasio` | Sin copias — una lista copiada siempre deriva (lección del observable 14). Las versiones viejas siguen alcanzables por las herramientas |
| **Firmas** | **Jorge firma todo de nuevo** para el gimnasio v2 | La corrida tiene una compuerta humana real antes de la pasada 4. El constructor **propone** valores; nada avanza sin firma |

## Principios que la guía tiene que hacer cumplir

1. **Consultable.** Cada juicio produce al menos una **consulta nombrada con predicado total** (E41). Un dominio que no contesta preguntas es un DTO.
2. **Rico y especializado.** Un dominio genérico tiende a la porosidad (paper 1). La barra de activo es el default; el Curador es el piso.
3. **Con nombres publicados.** Cada término acuñado tiene su entrada en el glosario con el término estándar, la fuente y un estado —`adoptado` / `en debate`—. Así *"no estoy seguro de Knowledge Source"* es una frase que abre un debate, no una duda sin dónde caer.
4. **Versionable y comparable.** Cada corrida deja un tag y una bitácora. El avance se mide con `git diff`.
5. **Documentable hacia afuera.** Las bitácoras se escriben para que sirvan de materia prima de un artículo, una demo o un video para la web de nCubo.

---

## Horizonte 1 · Mañana — algo que se pueda mostrar

| # | Qué | Estado |
|---|---|---|
| 1 | `git tag gimnasio-v1 8ada9a0` | ✅ hecho el 2026-09-07 (local; se pushea con el v2) |
| 2 | **Gimnasio v2** — rehacer el bloque 1 con la regla del descarte corregida y correr las cuatro pasadas. Pasadas 1 y 2 sin firma; pasada 3 **propone** en forma ODI con valor sugerido; **Jorge firma**; pasada 4. *El gimnasio no tiene artefacto: este v2 es el **mínimo** —misma entrevista, regla corregida— y mide el método; el v3 con artefacto mide el papel* | ✅ **Construido y verificado el 2026-09-07** sobre N1 (firmada) y N2 (implícita): 25 clases, 69 comprobaciones, el ciclo con journal dos veces. *Esperaba 39, faltaron 9* → *esperaba 33, descontó 6, faltaron 3*. **N2 firmada** el mismo día en sus tres partes —fondo, semanas enteras, las asistencias en mora cuentan—, sin cambio de código. **Commiteado en `37ba8eb` y taggeado `gimnasio-v2`** el 2026-09-07; `git diff gimnasio-v1..gimnasio-v2 -- Metodologia/Pruebas/Gimnasio` = 15 archivos, +1.024/−27. N4, N6 y los ocho viejos siguen sin firma |
| 3 | `Metodologia/Guias/glosario.md` v0 — 26 entradas en tres estados: **13 adoptadas, 10 en debate, 3 propias**. En `Guias/` para que lo indexe su README. Las tres búsquedas pendientes cerraron el mismo día — y una avisa que *fail-silent* significa lo contrario de lo que suena | ✅ **commiteado** el 2026-09-07 — `4770052`, con el índice del README. Sin pushear |
| 4 | **Bitácora** de la corrida — qué se hizo, qué encontró, cuánto tardó. El 05 de la municipalidad es el prototipo | ✅ `Pruebas/Gimnasio/BITACORA.md`, con la tabla v1/v2 y el hallazgo medido |
| 5 | Actualizar la pasada 4 del **02** y las notas de T3 en **02 / 03 / 04** con la decisión de especialización | ⏳ |

**El control de la corrida está en la respuesta esperada:** con la regla del descarte corregida, **la
membresía tiene que aparecer**. Si no aparece, falló el método o la corrida, y se sabe antes de firmar
nada.

**Mañana la orquestación es a mano** — el constructor y el revisor son pasadas separadas de un mismo
agente, y la persona firma. De esa corrida salen los contratos que el ciclo automático necesita. No se
promete el ciclo automático para mañana.

## Horizonte 2 · Esta semana — la guía formal y el ciclo

| # | Qué |
|---|---|
| 6 | **El 02 se vuelve guía en el repo.** Las cuatro pasadas entran a `Metodologia/Guias/` en lenguaje llano con enlaces al glosario; el 02 queda como la justificación con citas. **Un solo dueño por cosa.** ✅ **Hecho el 2026-09-07, adelantado**: [`Guias/columna-vertebral.md`](Guias/columna-vertebral.md), y el orden de trabajo del README de las guías la pone en el paso 1 con la entrevista como guion. La guía **no repite** las siete preguntas de la hoja —las enlaza— para no abrir una tercera copia que derive. **Sin correr como guía todavía: el v3** |
| 7 | Terminología formal en el **01** y el **02**: término publicado en la primera mención, el nuestro entre paréntesis, enlace al glosario |
| 8 | **El ciclo constructor ↔ revisor** escrito como contratos y corrido con `Workflow` sobre un dominio |
| 9 | **Cláusula de tope** en la guía: *si después de N vueltas el revisor marca lo mismo, el hueco va al registro con dueño y el ciclo para.* ✅ En la guía, §7, con N = 3 |
| 10 | **Observable 15**: todo término acuñado de las guías tiene entrada en el glosario. Con su control negativo |
| 11 | El criterio de **consultabilidad** explícito en la guía (principio 1). ✅ En la guía, pasada 3: *cada juicio firmado produce al menos una pregunta que el dominio contesta* |
| 12 | Reescribir el **análisis de enfoque** que se perdió con el scratchpad, ya con la decisión parcial: el sustrato es enchufable, la guía se enfoca en los juicios |

## Horizonte 3 · Después

| # | Qué |
|---|---|
| 13 | **Gimnasio v3 — confirmado el 2026-09-07.** Las cuatro pasadas con un artefacto real. **La app del teléfono de Jorge es genérica y no sirve** —sería otra institución—; la fuente del artefacto está por definir (¿la hoja del gimnasio del peer del 28-08? ¿capturas del portal del gimnasio de Jorge?). No comparar v1↔v3: v2 mide el método, v3 mide el papel |
| 13b | **Municipalidad** como segundo laboratorio versionado — la columna vertebral ya está en papel ([05](Pruebas/Municipalidad/BITACORA.md)) |
| 14 | **Prueba 3** — la misma sesión con alguien de fuera del equipo |
| 15 | **Para Álvaro**: T3 (ya resuelto por la decisión de especialización — se le comunica, no se le pregunta), G1 (E39 no apunta a E36), G2 (E29 sin discriminador) |
| 16 | **G3, el relator** — control negativo contra los cinco dominios antes de proponerlo |
| 17 | **Espejo público** — el repo está en Azure DevOps; si los laboratorios van a la web de nCubo hace falta decidir dónde se publican |
| 18 | Borrador del **artículo**, armado desde las bitácoras |

## Diferido a propósito — es enchufable

Cap. 5 (proveedor) · cap. 6 (`cargo`, bloqueado en las 4 preguntas para Álvaro) · el teléfono · iOS /
Android · cap. 4 contrafactual · cap. 9 · anexo A · `chore/gitignore` · README raíz · los 5 observables
bloqueados afuera.

**No se pierden: se ordenan después de la columna vertebral.**

---

## El ciclo constructor ↔ revisor

```
CONSTRUCTOR pasada 1  →  REVISOR: obs. 13 · criterio de salida 1 (todo campo con espacio de valor)
CONSTRUCTOR pasada 2  →  REVISOR: criterio no-espejo (columna de contraste ≥ 1, ninguna del artefacto)
CONSTRUCTOR PROPONE los juicios de la pasada 3, en forma ODI, con valor sugerido
        ↓
   ★ LA PERSONA FIRMA ★     ← el ciclo se detiene acá. No se automatiza.
        ↓
CONSTRUCTOR pasada 4  →  REVISOR: las 4 preguntas de parada + las 6 dimensiones + consultabilidad
   ¿las 4 dan sí?  →  fin.     Si no  →  vuelve a la pasada que falló.
   ¿N vueltas con la misma marca?  →  el hueco va al registro con dueño y el ciclo para.
```

| Rol | Recibe | Produce | Tiene prohibido |
|---|---|---|---|
| **Constructor** | El contrato de la pasada ([la guía, §7](Guias/columna-vertebral.md)) | Los artefactos de la pasada + Mermaid | Inventar un campo, llenar la columna de contraste solo, inventar un umbral, crear una clase que no pase el piso |
| **Revisor** | Los artefactos + el glosario + las guías | Marcas con la regla citada; recomendación de vuelta | Corregir en silencio — toda corrección va con la regla que la justifica |
| **La persona** | Los juicios propuestos en forma ODI | Firmas, correcciones a los valores, o *"no sé"* con dueño | — |

**La pasada 3 es una compuerta humana, y lo dice la guía:** *"La 3 no: es elicitación."* El ciclo no
produce un dominio: produce **una hoja de firmas y un dominio construido sobre ella.**

---

## Cómo se mide el avance

- **Entre versiones de un laboratorio:** `git diff gimnasio-v1..gimnasio-v2 -- Metodologia/Pruebas/Gimnasio`. Lo que cambió es lo que la guía mejoró.
- **De una corrida:** la bitácora — duración, cuántos *"no sé"*, cuántos firmados sin corregir, cuántos huecos y cuántos con dueño. Misma tabla que el [05](Pruebas/Municipalidad/BITACORA.md).
- **De la guía:** los observables — hoy 14, el 15 esta semana — y `verificar-enlaces.sh`.
