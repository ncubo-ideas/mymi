# Herramientas — los chequeos que se corren solos

Lo que se puede verificar **sin que nadie opine**. Los
[observables](../Guias/observables.md) miran el código de los negocios; esto mira **las guías
mismas**, que hasta ahora no tenían ningún chequeo.

| | Qué verifica | Cuándo correrlo |
|---|---|---|
| [`verificar-enlaces.sh`](verificar-enlaces.sh) | Que cada enlace de los `.md` apunte a un archivo que existe, y que cada ancla `#...` corresponda a un encabezado real | Después de tocar cualquier guía |
| [`verificar-hoja.sh`](verificar-hoja.sh) | Que la hoja de la entrevista y el punto de partida declaren las **mismas reglas**. Es el [observable 14](../Guias/observables.md) | Después de tocar cualquiera de los dos |
| [`revisar-acoplamiento.sh`](revisar-acoplamiento.sh) | Avisa si un commit tocó la hoja o el capítulo **pero no el otro**. Heurística, no observable: nunca falla | Antes de abrir un PR |

---

## `verificar-enlaces.sh`

```bash
bash Metodologia/Herramientas/verificar-enlaces.sh Metodologia
```

```
-----
enlaces revisados: 108    rotos: 0
```

Sale con **código 1** si encontró algo roto, así que sirve igual desde un hook o desde CI.

### Por qué hace falta

**Un ancla rota no da error en ningún lado.** El markdown se ve bien, el enlace se ve bien, y al
hacer click el navegador no salta. No hay nada que avise.

Y las guías dependen mucho de las anclas: las reglas del substrato se enlazan **por número y título**
desde tres archivos distintos. Renombrar el título de una regla —que es exactamente lo que pasa
cuando una regla se corrige— rompe todos esos enlaces en silencio.

### Lo que NO hace

- No valida enlaces a internet
- No detecta **anclas duplicadas**: si dos encabezados dan el mismo slug, GitHub le agrega `-1` al
  segundo y esto no lo sabe
- **Ignora lo que esté adentro de un bloque de código**, a propósito: ahí el markdown es un ejemplo,
  no un enlace. Es lo que permite que este mismo README muestre enlaces rotos sin dispararse
- Sólo mira el árbol que se le pase por argumento

---

---

## `verificar-hoja.sh`

```bash
bash Metodologia/Herramientas/verificar-hoja.sh
```

Compara los **marcadores de regla** de los dos archivos — comentarios HTML, invisibles al leer:

```
<!-- regla: filtros-no-a-la-capacidad sesion -->
```

Dos reglas: **todo marcador de la hoja está en el capítulo** (la hoja no tiene doctrina propia), y
**todo marcador `sesion` del capítulo está en la hoja** (lo que hace falta en vivo llega al guion).

### Por qué marcadores y no comparar el texto

Los dos documentos dicen lo mismo con palabras distintas **a propósito**: uno es doctrina y el otro
es guion de sesión. *"Pero no se pregunta en frío: se propone y se deja corregir"* contra *"No
preguntes en frío: proponé"*. Un diff daría cientos de diferencias legítimas y ninguna real.

Comparar significados necesita que un humano juzgue — o sea que **no es observable**. La salida es
volverlo una declaración, que es el argumento del manifiesto: *porque es una declaración, se puede
verificar*.

### El piso

Si cualquiera de los dos archivos trae menos de **20** marcadores, el script **falla** en vez de
aprobar. Sin eso, un par sin marcadores pasa perfecto —cero marcadores, cero divergencias, código
0—, y borrar los marcadores sería la forma más fácil de tener el chequeo en verde.

### Lo que NO caza

**Una regla que pierde contenido sin desaparecer.** Si el capítulo deja de decir algo dentro de una
regla que sigue estando en los dos lados, esto no lo ve — compara presencia, no contenido. Pasó de
verdad, y es el defecto residual conocido.

---

## `revisar-acoplamiento.sh`

```bash
bash Metodologia/Herramientas/revisar-acoplamiento.sh 20
```

Cubre el agujero del anterior: **el marcador lo pone la misma persona que se olvida de sincronizar.**
Esto no depende de que nadie declare nada — mira los commits y avisa cuando uno tocó la hoja o el
capítulo pero no el otro.

**Es una heurística, no un observable.** Tiene falsos positivos legítimos —un typo en el capítulo no
obliga a tocar la hoja— así que dice *"revisar"* y **sale siempre con código 0**. Corrido sobre la
historia real marcó tres commits, y dos eran exactamente los que habían introducido divergencias.

---

## La regla de esta carpeta

> **Un chequeo no está probado hasta que se le mete algo roto a propósito.**

No es una frase bonita: `verificar-enlaces.sh` **mintió entero dos veces** antes de servir.

**La primera vez reportó las 22 anclas como rotas**, incluidas las que funcionaban. El bug era que
colapsaba los espacios: GitHub no lo hace, y `## 13 · El print` da `13--el-print` con **dos** guiones
—el punto medio desaparece y sus dos espacios quedan—.

**La segunda vez reportó cero rotas, y también estaba mal.** Al meter comillas tipográficas dentro de
una clase `[...]` de `sed`, en locale de bytes el multibyte se parte y se come bytes sueltos de otros
caracteres. Ninguna ancla resolvía y el script no tenía cómo notarlo.

**Y la tercera falla la encontró este README**, apenas se escribió: los enlaces rotos del ejemplo de
abajo estaban adentro de un bloque de código y el script los contó como reales. O sea que cualquier
guía que **muestre** markdown se reportaba rota. De ahí salió que ignore los bloques de código.

Las tres son la misma que la tabla de observables ya advierte —*"un `grep` no es un observable"*— y
las tres se cazan igual: **inyectar un enlace roto a propósito y confirmar que salta.**

Cada vez que se toque este script, el control negativo se vuelve a correr. Estas cuatro líneas van
pegadas al final de cualquier guía, **fuera de un bloque de código**, y tienen que dar exactamente
dos rotos:

```bash
# agregar al final de cualquier .md, correr, y confirmar que reporta 2 rotos
[a](reglas-del-substrato.md#99--no-existe)     <- ancla rota
[b](no-existe.md)                              <- archivo que no existe
[c](reglas-del-substrato.md#18--todo-comando-necesita-check-incluso-el-que-no-puede-fallar)
[d](../Pruebas/)                               <- las dos ultimas TIENEN que pasar
```

Los dos últimos importan tanto como los primeros: un verificador que marca todo como roto es tan
inútil como uno que no marca nada, y es el error que este script ya cometió.

### Y `verificar-hoja.sh` se probó antes de escribirlo

Con la misma disciplina, aplicada al revés: **se reconstruyó el estado del repo de antes de arreglar
las nueve divergencias**, se marcó, y se corrió contra eso. Cazó siete de las nueve, y las dos que no
quedaron escritas arriba en vez de disimuladas.

Cuando se toque, los cuatro controles se vuelven a correr:

| | Esperado |
|---|---|
| Los archivos de hoy | 0 divergencias, código 0 |
| El estado de `b6b48dc`, marcado | 8 divergencias, código 1 |
| Dos `.md` cualesquiera sin marcadores | **falla por el piso**, código 1 |
| `revisar-acoplamiento.sh` sobre la historia | marca `b6b48dc` y `2451225` |
