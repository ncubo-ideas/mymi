# Herramientas — los chequeos que se corren solos

Lo que se puede verificar **sin que nadie opine**. Los
[observables](../Guias/observables.md) miran el código de los negocios; esto mira **las guías
mismas**, que hasta ahora no tenían ningún chequeo.

| | Qué verifica | Cuándo correrlo |
|---|---|---|
| [`verificar-enlaces.sh`](verificar-enlaces.sh) | Que cada enlace de los `.md` apunte a un archivo que existe, y que cada ancla `#...` corresponda a un encabezado real | Después de tocar cualquier guía |

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
