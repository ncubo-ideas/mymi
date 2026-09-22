# El tour del teléfono — relevo para la próxima sesión

Escrito el 2026-09-21, el mismo día que nació. Es **otra lámina**, con su propia URL: no es una
versión móvil del tour de escritorio, y no se republica sobre él.

**Publicado en:** https://claude.ai/artifact/6UJVpBx4WWpg7G8rFPDfwX (privado, **v2**)
**Hermano:** el tour de escritorio vive en `../tour/`, con su propio `RELEVO.md`. Mucho de lo que
vale para los dos —la lección de qué es un «tour», las trampas de publicar, el arreglo del `catch`
de la cara— está allá y no se repite acá.

---

## Qué es

Un **teléfono dibujado en la página**, con la UI de Mymi corriendo adentro y un guía de globos a la
derecha que señala lo único tocable en cada paso. Diez pasos, del `.gmod` que manda el laboratorio
hasta el interruptor por consulta.

La metáfora es la misma que la del hermano —un aparato que se usa, no un documento que se lee—,
pero el aparato es otro y **la UI es otra**, no una adaptación de la del escritorio.

---

## Las tres decisiones que lo definen

Se preguntaron y se decidieron el 2026-09-21:

1. **Recrear la UI, no envolver la real.** Ver abajo, porque el camino descartado sigue disponible
   y es mejor de lo que parece.
2. **Teléfono dibujado en la página**, no la página a pantalla completa en un teléfono. Así se abre
   desde cualquier lado, igual que el hermano dibuja un monitor.
3. **El mismo arco que el de escritorio** — alta, pantalla de la institución, asistente — pero con
   lo que el teléfono tiene de propio.

---

## ⚠️ EL CAMINO QUE SE DESCARTÓ, y por qué conviene tenerlo a mano

**`Gnothi.Mobile/Assets/ui.html` ya corre solo.** No hace falta el Host:

```js
// ui.html:1073
if (!navigator.userAgent.includes('GnothiHost')) return mock(op, extra || {});
```

Fuera del Host **todo** cae a `mock()`, que ya implementa el alta con archivos, `setTool`,
`setModule`, `quitar`, los proveedores y la propuesta de actualización. O sea: la app de verdad,
corriendo, sin recrear nada.

**El único obstáculo es el catálogo de textos**, y está declarado en el propio código:

> *«LA MAQUETA NO TIENE CATALOGO, y devuelve uno vacio a proposito en vez de inventar uno.
> Consecuencia […]: lo que arma el JavaScript sale como su clave, «alta.paso.revisando» en vez de
> la frase. La maqueta existe para iterar CSS y disposicion […]; para leer copy hay que abrirla en
> el telefono.»*

Un tour es 100 % leer, así que con claves en vez de frases no sirve. **Pero el catálogo existe**:
`Gnothi.Host/textos.json`, 64 KB y 1023 claves. Hacer que `mock('textos')` lo devuelva —en vez de
`{}`— haría hablar a la maqueta entera.

Si alguna vez esta lámina se vuelve pesada de mantener, ése es el camino: **envolver la UI real y
darle el catálogo**. Sería la única de las dos láminas donde no hay recreación que se desincronice.

---

## Los dos idiomas — v2

**Arranca en INGLÉS**, con un botón que vuelve al español. Todo el reparto —qué se traduce, qué no,
de dónde sale el `en`, y cómo hace la cara para seguir al aparato— está explicado en
`../tour/RELEVO.md`, sección «Los dos idiomas», y vale igual acá. No se repite.

**Lo propio del teléfono son dos cosas:**

**`pre`, la mitad chica del nombre en dos niveles**, tampoco se traduce. «Laboratorio Clínico»,
«Supermercado», «Estaciones», «Municipalidad de» salen de `institution`, que es voz de la
institución (regla 4). Una tarjeta que dijera «Supermarket / La Guaria» sería el Host poniéndole
palabras en la boca. El escritorio no tiene este caso, porque muestra `Manifest.Identity` a secas.

**Las tres pestañas de la barra de abajo son marcado fijo**, no las repinta `pintar()`. Llevan
`data-nav` y las recorre `pintarPagina()`. Si se agrega una cuarta, hay que ponerle su `data-nav` o
se queda en inglés para siempre.

Y el reloj de la barra de estado sigue al idioma del aparato (`toLocaleTimeString`), que es la
regla 8: el formato viaja con el render.

---

## Lo real contra lo recreado

| | |
|---|---|
| **Real, copiado del repo** | `cara/` entero — el `web/` del `examenes.gmod` del 21/9/2026. Es **la misma copia que usa el hermano**, con el arreglo del `catch` adentro |
| **Real, extraído del repo** | los cinco `logo-*.png` (los `brand/logo.png` de cada bundle) y `mymi-mark.png`, que es `Gnothi.Mobile/Assets/img/gnothi-mark.png` |
| **Recreado** | `index.html` — el chasis del teléfono y las cuatro pantallas de `ui.html` |
| **Escrito para la demo** | `cara/simulador.js`, el puente falso, y las tres líneas de `cara/index.html` que lo cargan |

**LA CARA ESTÁ DUPLICADA**, y conviene saberlo antes de tocarla: `tour/cara/` y `tour-movil/cara/`
son dos copias del mismo `web/`. Arreglar una no arregla la otra. Cuando se recopie del bundle, hay
que recopiar **las dos** — o resolver el duplicado, que es lo que dice «Lo que falta».

---

## Lo que se sacó de `ui.html`, con línea

Recrear fiel salió de mirar el original, no de aproximar. Lo que se usó:

| qué | dónde |
|---|---|
| Paleta: `--paper:#F5F3FB`, `--off:#C0442E`, `--off-soft:#F9E7E2` | `:root` de `ui.html` |
| Barra inferior de tres, 60 px | `<nav>` (867) y su CSS |
| Nombre en dos niveles | `tiers()` (925) |
| La tarjeta: icono, nombre, lema, candado, «Abrir →» | el `grid.innerHTML` (1601-1623) |
| Cambio de vista por clase `on` | `gn.nav()` (1518) |
| El interruptor: 46×28, perilla 22, `#D4D0E3`, corre 18 px | `.sw` (350-356) |

Y los textos salen de `Gnothi.Host/textos.json`, no inventados: `inicio.titulo` («Mis conexiones»),
`inicio.varias` («{n} conectadas»), `inicio.conectar`, `nav.volver` («Todas tus instituciones»),
`detalle.permiso.uno` / `.varios`, `detalle.abrir.de` / `.sub`, `detalle.consultas.de` / `.lead` /
`.apagada`, `asistente.sub`, `asistente.vacio`, `asistente.placeholder`, `asistente.traza`,
`asistente.pensando`.

**EL HOST HABLA DE TÚ Y LA INSTITUCIÓN DE VOS.** «Pregunta algo sobre tus instituciones» contra
«Entendé cómo venís». No es una inconsistencia: son dos voces, por las reglas de localización. Los
globos del guía son la tercera voz —la del narrador— y van en voseo. Al escribir texto nuevo hay
que saber en cuál de las tres se está.

---

## Las cuatro diferencias con el tour de escritorio

Esto es lo que hace que las dos láminas existan, y lo que hay que cuidar si se toca una:

| | escritorio | teléfono |
|---|---|---|
| **El nombre** | `Manifest.Identity` a secas: «Delfos» | dos niveles: «Laboratorio Clínico» chico sobre «Delfos» |
| **Abrir la app** | dos pasos: la tarjeta lleva al detalle, y ahí está «Abrir la pantalla» | **uno**: tocar la tarjeta abre la app. El candado es el que va al detalle |
| **Quién pregunta** | Claude, **otra app**, por MCP. 12 herramientas en su catálogo | el asistente **de Mymi**. 10 consultas, y la **traza** dice cuál se usó |
| **El consentimiento** | no se puede mostrar: `ModuleScreen.razor` recorre `EnabledTools()` y no hay ningún control | **el interruptor, que acá existe** — `gn.setTool` en `ui.html` |

La cuarta es la razón de ser de esta lámina. El tour de escritorio **tuvo que quitar** un
interruptor que había dibujado, porque en Windows no existe (ver el RELEVO del hermano). Éste lo
muestra, y el globo lo dice: *«Falta lo que el escritorio no puede mostrar, porque ahí no existe:
el candado.»*

---

## El recorrido, paso por paso

| # | dónde | qué pasa |
|---|---|---|
| 0 | Inicio | Las cuatro que ya estaban, cada una con su color y su lema |
| 1 | Inicio | «Conectar» — llegó la quinta, 235 KB, sin motor |
| 2 | hoja | Pegar el enlace o elegir el archivo |
| 3 | hoja | Lo que trae: quién publica, versión, red, consultas, registro, tamaño, firma |
| 4 | Inicio | La tarjeta de Delfos → **abre su app directo** |
| 5 | la cara | La pantalla del laboratorio, a teléfono entero. Nueve consultas al dominio |
| 6 | pestaña | El asistente **de Mymi** |
| 7 | Asistente | `examenes_resumen` → conteos, con su traza |
| 8 | Asistente | `lista_de_examenes` → la puerta se abre **y aun así no alcanza** |
| 9 | Inicio → detalle | El candado, y el interruptor. Apagar «Mis examenes» |
| — | remate | 1 permiso activo, nueve consultas, y el asistente choca |

El argumento del paso 8 es el mismo del hermano y por las mismas razones: `lista_de_examenes` da
filas de **examen**, no la serie del analito, ni el «nivel de siempre», ni el techo esperable. Eso
es `[View]` y el modelo no lo ve. Está explicado en detalle en `../tour/RELEVO.md`.

---

## Cómo verlo

Igual que el hermano —ver su RELEVO, sección «CÓMO VER LA PÁGINA»— pero con **su propio puerto**,
para poder tener los dos levantados a la vez:

```
tour de escritorio → http://localhost:8777/
tour del teléfono  → http://localhost:8779/
```

Un servidor estático de 30 líneas con `System.Net.HttpListener` de PowerShell, en background. En
esta máquina no hay node ni python.

**Para recorrerlo sin hacer 20 clics a mano**, el guía deja siempre un solo elemento con
`data-vivo="1"`, así que desde la consola:

```js
document.querySelector('[data-vivo="1"]').click()        // el paso señalado
document.querySelector('#globo .seguir').click()         // los globos de sólo leer
```

---

## Lo que falta

1. **La cara duplicada.** `tour/cara/` y `tour-movil/cara/` son la misma copia en dos lugares. O se
   recopian las dos cada vez, o se resuelve: una carpeta compartida no sirve —cada artifact publica
   sus propios archivos—, así que lo realista es un paso de copiado que las sincronice desde el
   bundle.
2. **Las otras cuatro instituciones no tienen cara**, igual que en el hermano: están en la lista y
   en el catálogo del asistente, pero su «Abrir» no lleva a ninguna pantalla. Sus botones quedan
   inertes, que es el diseño del tour.
3. **La pestaña Ajustes está de adorno.** Muestra el proveedor y nada más; el recorrido no pasa por
   ahí. En `ui.html` hay bastante más —llave, modelo, journal, quitar un módulo.
4. **El alta no muestra lo mejor que tiene el mock.** El de `ui.html` simula elegir entre **tres
   archivos**: dos módulos —uno ya instalado— y un `fotos-vacaciones.zip` que no es un módulo. La
   aduana rechazando lo que no corresponde es un buen paso y acá no está.
5. **El sello de la cabecera** repite el del hermano, `83b68be+`. El `+` es el arreglo del `catch`
   sin commitear; cuando eso entre, hay que actualizar **los dos**.

---

## Cómo republicar

```
url: https://claude.ai/artifact/6UJVpBx4WWpg7G8rFPDfwX
```

**NO es la URL del hermano.** Publicar esta carpeta sobre `6jt4cPXvDqoEeSU2WpZGdk` pisaría el tour
de escritorio.

El resto —que el servicio exige leer la versión viva entera, que los archivos no mencionados en
`files` se conservan, que las fuentes tienen que estar bajo el working directory o el scratchpad—
está en `../tour/RELEVO.md`, sección «Cómo republicar», y vale igual acá.

---

## ⚠️ NADA DE ESTO ESTÁ COMMITEADO

Al nacer la v1, en `trim/spike-wasm-mobile`: esta carpeta está **sin trackear**, igual que
`../tour/`, y el arreglo de `Examenes/App/Examenes.Cara/` sigue modificado sin commitear.

Las dos láminas publicadas sobreviven porque están en sus URLs. El fuente no.
