# B6 · Los cuatro caminos de la PWA — la narrativa de los dibujos

Los dibujos: **https://claude.ai/code/artifact/08915f09-f2b6-4334-9a26-91e9afabaaf6**

Cuatro formas de que la app de una institución y Gnothi convivan en el teléfono de Sonia. Están
dibujadas como **cuatro láminas que contestan las mismas cinco preguntas, en el mismo orden y en el
mismo renglón**. Eso es deliberado: la diferencia entre caminos **se ve** comparando el mismo renglón
de una lámina a otra, en vez de leerse.

**Por qué existe este documento y no solo el dibujo:** el dibujo se puede perder o quedar viejo, y la
narrativa —qué se contesta, con qué respaldo, y qué es medición contra qué es propuesta— tiene que
vivir en el repo.

---

## Las convenciones de lectura

**Propiedad, por color.** Lo de **Sonia**, lo de **la institución**, lo de **Gnothi**. En cada lámina
las cosas están pintadas de quien son, así que «qué le pertenece a ella» se ve sin leer.

**Un expediente dibujado = una copia real** del registro de Sonia en el aparato. Se cuentan. Es el
renglón que más separa los caminos.

**Salida a la red, tres estados** — y la distinción entre el segundo y el tercero es la que más se
malinterpreta:

| estado | qué significa |
|---|---|
| antena entera | tiene red y la usa |
| **antena tachada** | **no puede salir** — no tiene interfaz ni DNS. Es una **forma**, y está medido |
| **antena punteada gris** | **podría salir, se acordó que no.** Tiene red y no hay forma de quitársela. Es una **palabra** |

**Las cinco preguntas, siempre en este orden:** qué es de ella · cuántos expedientes hay · quién
puede salir a la red · qué tiene que hacer ella · qué gana y qué pierde.

---

## El cuadro comparado

| | 01 · se muda adentro | 02 · su carpeta + copia | 03 · su carpeta, sin copia | 04 · solo la cara |
|---|---|---|---|---|
| **expedientes** | 1, en Gnothi | **2** | 1, en su carpeta | 1, en Gnothi |
| **¿carpeta suya?** | no | **sí** | **sí** | no |
| **red de la app** | **no puede** | sale sola | sale sola | podría; se acordó que no |
| **ella tiene que** | aceptar el alta | elegir carpeta, y reconceder si cambia | elegir carpeta | aceptar el alta |
| **desfase** | no | **sí** | no | no |
| **¿la app sirve sin Gnothi?** | **no** | sí | sí | **no** |
| **estado** | andando y medido | andando hoy | medido; pide cambio al motor | propuesta, sin construir |

---

## 01 · La app se muda adentro de Gnothi

**Andando y medido.** Cuatro instituciones, cada una con la app web que ya había publicado, **la
misma y sin recompilar**, corriendo dentro de Gnothi.

La lámina está partida en **antes → el alta → después**, porque lo que importa de este camino no es
el resultado sino **el traslado**, y eso faltaba en la primera versión del dibujo.

**Antes, suelta:** tiene su ícono, **sale sola a la red**, y su registro está *dentro de la app* —
el almacén privado del navegador, que ni Gnothi ni ningún otro programa puede abrir.

> **Aclaración de vocabulario:** en la conversación esto se nombra «OPFS», pero el prototipo **no usa
> OPFS** — grep sobre todo el código: cero ocurrencias de `opfs`, `navigator.storage`,
> `storage.getDirectory` (ver `B5` y `F1`). Lo que hay es el almacén privado del origen, y para el
> dibujo lo que importa es **la propiedad, no el API**: nadie más lo abre. Se usan las palabras de la
> propia app —«dentro de la app… tu asistente no lo puede leer»— en vez del acrónimo.

**En el alta, Sonia acepta el traslado** después de ver en una lista qué va a poder contestar el
módulo. Nada se mueve antes de eso.

**Después, adentro:** el mismo artefacto, sin recompilar; **pierde la red** —solo `lo`, sin DNS,
medido— y desde ahí los eventos los trae Gnothi contra una dirección que la institución declaró y no
puede cambiar en marcha. El expediente queda en los archivos de Gnothi.

### Lo que no se muda, y por qué

**Su registro anterior queda afuera**, y la causa es mecánica: estaba atado al **origen** de la app
suelta, y al mudarse el origen cambió. Adentro **empieza de cero** y reconstruye la historia
bajándola otra vez del proveedor.

Y ahí hay una consecuencia que sale de una medición propia: está medido que **el proveedor olvida y
el journal no** (`F1`, «el ciclo completo de datos»: tras un reinicio el mock perdió un evento que el
journal conservó). Si empieza de cero, **lo que el servidor de la institución ya no tenga, no
vuelve.**

### Y lo único que SÍ tendría que pasar con ella — todavía sin implementar

El expediente guarda **dos procedencias**, y esto es del código, no del diseño en abstracto:

| procedencia | qué es | de dónde vuelve |
|---|---|---|
| `INSTITUCION` | lo que publica el laboratorio | **del proveedor** |
| **`TU`** | **lo que declaró Sonia** — su sexo, una corrección a un dato del laboratorio | **de ningún lado** |

`ExamenesAPI.cs:29` — *«LO QUE LA PERSONA DICE DE SI MISMA. Procedencia TU, y por eso es un comando
local»*. `ExamenesAPI.cs:134` — *«LO QUE PUBLICA EL LABORATORIO sobre el paciente. Procedencia
INSTITUCION»*. Y `Eventos/DatosDePacienteActualizados.cs`: *«las dos afirmaciones conviven en el
journal con su procedencia»* y **«para interpretar gana ella»**.

**Sus aserciones son el único pedazo del expediente que no se puede reconstruir**, porque el
laboratorio nunca las tuvo. Y no son un detalle cosmético: cuando las dos fuentes se contradicen
**gana lo que dijo ella**, así que al empezar de cero **el módulo vuelve a interpretar con los rangos
equivocados** hasta que ella lo declare otra vez — es exactamente el bloque «falta un dato tuyo»
reapareciendo después del traslado.

**Ese traspaso no está implementado.** Es el pendiente que la lámina 01 marca con un signo de
pregunta.

---

## 02 · Su carpeta, y una copia adentro de Gnothi

**Andando hoy**, con el motor tal como está publicado — y es el único camino que **no necesita nada
de nadie**.

La app sigue siendo suya y vive afuera. Escribe en **una carpeta que Sonia eligió** y que ella puede
abrir con cualquier explorador de archivos. Gnothi **se trae una copia** a su propia área, y ahí la
lee el módulo.

- **Dos expedientes** en el aparato: el suyo y la copia.
- **Las dos salen a la red.** La de la app es la del navegador y **no hay forma de quitársela**.
- Ella **elige la carpeta una vez**; si la revoca o se muda, hay que volver a pasar por el alta.
- Y lo que el asistente contesta es **de la última vez que ella abrió Gnothi**.

Los ocho «peros» medidos de este camino están en `C8`.

---

## 03 · Su carpeta, y Gnothi lee ahí mismo

**Medido de punta a punta**, y es el que **pide un cambio al motor**.

Idéntico al 02 con una diferencia: **no hay copia**. El módulo lee el expediente donde Sonia lo
tiene, sin moverlo y sin tocarlo. En el dibujo, el hueco donde en la lámina 02 está la copia aparece
**tachado y en la misma posición exacta** — la diferencia entre los dos caminos se ve sin leer nada.

- **Un expediente**, en su carpeta, al día. Sin desfase.
- **La red no cambia respecto del 02**: este camino cambia *de quién es* el expediente y *cuántos
  hay*, no quién sale a la red.
- Pierde autonomía: **depende de un cambio en el motor que todavía no está publicado**.

El pedido, con su evidencia y su costo, está en el `PEDIDO-2026-09-09`.

---

## 04 · La app es solo la cara

**Propuesta, sin construir.** Es la forma que planteó el arquitecto del motor, y su motivación es
otra que las anteriores: **un solo dueño del journal**, porque el motor asume un solo juego de
semáforos y dos procesos con el suyo propio no se coordinan.

La app conserva ícono y pantalla, y nada más: **le pregunta a Gnothi qué pintar**. El motor y el
expediente viven solo en Gnothi, que decide dónde.

- **Un expediente**, con un solo dueño.
- **Es el camino que menos le pide a Sonia**: acepta el alta y listo — nada de carpetas, conceder ni
  refrescar.
- **Pero la app tiene red igual.** «Solo sale Gnothi» acá es un **acuerdo, no un impedimento**, y se
  ve en el inspector en dos toques.
- Y **la app deja de servir sola**: sin Gnothi no tiene nada que pintar.

---

## Lo que estas cuatro láminas dejan claro de un vistazo

**La garantía de red solo existe en el 01**, y existe porque **Gnothi es quien sirve la página** —
por eso puede poner el CSP y hacérselo cumplir a la institución. En los otros tres la app se sirve
sola, y ahí lo más que hay es una promesa suya.
Ver `F1`, «la red de una PWA instalada no se puede acotar desde afuera».

**La carpeta de Sonia solo existe en el 02 y el 03.** En el 01 y el 04 su expediente vive donde
Gnothi decide. Eso es una diferencia de propiedad, no de implementación, y conviene no decirla al
pasar.

**Y el 02 es el único con dos copias**, que es el precio de no pedirle nada a nadie.
