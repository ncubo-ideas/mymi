# Taller

Todo lo que hace falta para que alguien que llega nuevo pueda construir una app en este framework.

No es una carpeta de documentos. Es el lugar donde están las herramientas: las guías, lo que prepara
el escritorio, ejemplos mínimos para tocar y romper, recetas para traer datos de una institución, y
las plantillas del esqueleto de un negocio nuevo. Se llama Taller porque parte de eso todavía va a
ser un instalador o un agente, y el nombre tenía que sobrevivir a eso.

---

## Qué hay y qué falta

```
Taller/
├── Guias/         los capítulos, el guion de la entrevista y los observables    ← hay contenido
├── Pruebas/       los sujetos con los que se falsan las guías                   ← hay contenido
├── Ambiente/      preparar el escritorio: qué se baja, qué se verifica    (paso 3)
├── Ejemplos/      lo mínimo que se toca y se rompe, con sus .md           (paso 4)
├── Recetas/       MySQL, SQL Server, un API de ejemplo                    (paso 6)
└── Plantillas/    el esqueleto de un negocio nuevo, con los cinco nodos
```

`Guias/` y `Pruebas/` tienen contenido. El resto está nombrado y no construido, a propósito: es más
útil ver la forma completa y saber qué falta que encontrarse carpetas vacías sin explicación.

**`Ejemplos/` y `Pruebas/` no son lo mismo**, y conviene tenerlo claro desde el principio: los
ejemplos son chiquitos y están para enseñar; las pruebas son completas y están para **falsar** — se
sigue la guía hasta el final y se ve dónde se cae. Un dominio de prueba puede ser grande y feo, y no
importa.

---

## Por dónde entrar

Empezá por [`Guias/`](Guias/README.md). Su índice dice qué capítulo leer según a qué venís, y —esto
importa— **marca qué está verificado y qué no**. No todo tiene el mismo respaldo, y el que lo lea
tiene derecho a saberlo.

Si venís a escribir código ya, el capítulo 3. Si venís a decidir qué modelar, el 4. Si nunca viste
la arquitectura, el 0 primero.

---

## El Taller no es dueño de los negocios del repo

`Examenes/`, `Compras/` y `HolaMundo/Facturas/` son negocios de verdad, con sus dueños y sus
milestones. El Taller **no los administra y no depende de ellos**: si mañana se mueven, se renombran
o se borran, acá no se rompe nada.

Los ejemplos del Taller son propios y deliberadamente **chiquitos** — lo mínimo que se pueda leer de
una sentada, tocar, romper y volver a armar. Un negocio real no sirve para eso: enseña demasiadas
cosas a la vez.

Lo que sí hace el Taller es **citarlos**, cuando un negocio real resolvió algo que vale mostrar. En
esos casos la cita dice de dónde sale, para que se pueda ir a verlo.

---

## Antes de commitear acá

El repo verifica el formato del mensaje de commit con un hook, y se activa **una vez por clon**:

```bash
git config core.hooksPath .githooks
```

Conventional Commits: el **tipo en inglés**, la **descripción en español**, minúscula, sin punto
final, asunto de 72 caracteres como máximo. El alcance de lo que se toca acá es `taller`:

```
docs(taller): capitulo 0, el host lleva el sustrato y la app es carga
feat(taller): la receta de mysql, con su verificacion
```

Si el asunto no calza, el hook rechaza el commit y explica qué faltaba.
