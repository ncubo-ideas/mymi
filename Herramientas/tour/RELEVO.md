# El tour guiado — relevo para la próxima sesión

Escrito el 2026-09-21 y actualizado el mismo día hasta la v22. Lo que sigue es el estado real,
incluidos **los errores que se cometieron y cómo se detectaron**, porque son la parte del relevo
que ahorra tiempo.

**Publicado en:** https://claude.ai/artifact/6jt4cPXvDqoEeSU2WpZGdk (privado, **v22**)
**Archivos:** esta carpeta (antes vivían en un scratchpad de sesión, que es efímero)

**HAY UN SEGUNDO TOUR, y es otra lámina con su propia URL:** el del teléfono, en
`../tour-movil/`, con su propio `RELEVO.md`. Simula la UI de `Gnothi.Mobile/Assets/ui.html`, que no
es ésta, y muestra el paso del consentimiento que acá tuvo que sacarse. **No se republica uno sobre
el otro.**

**Y la cara está duplicada:** `tour/cara/` y `tour-movil/cara/` son copias del mismo `web/`.
Recopiar una no arregla la otra.

---

## Qué es

Una página web que **simula el escritorio de una computadora** con Mymi instalado, y guía a
quien la abre con clics: se abre Mymi, se agrega una institución, se entra a la pantalla del
laboratorio, y una app externa —Claude— le pregunta al Host por MCP.

**Toda la página es el monitor.** No hay texto explicativo al costado: hay un escritorio con
fondo, barra de tareas, reloj e íconos, y un globo que señala lo único clickeable en cada paso.

### Lo que NO es, y costó tres intentos entenderlo

El usuario pidió esto tres veces con palabras distintas y se entregó otra cosa dos veces:

1. **Primer intento:** un panel de instrumentos con seis bloques simultáneos. Respuesta:
   *"no se parece en nada a un demo interactivo, solo muchas cosas en pantalla"*.
2. **Segundo intento:** un recorrido de cuatro pasos con la cara al lado de un guía de texto.
   Respuesta: *"mi idea de tour era simular el desktop de una computadora y simular los clicks
   y demas, no una pantalla llena de texto"*.
3. **Tercero:** el escritorio simulado. Ese era.

**La lección:** cuando pide un "tour" o una "demo interactiva", la metáfora es **una computadora
que se usa**, no un documento que se lee. El texto va en globos de una o dos frases.

---

## ⚠️ CÓMO VER LA PÁGINA — esto destraba todo lo demás

La sesión que escribió la v13 nunca pudo ver la página renderizada y **todas** sus correcciones
visuales salieron de capturas del usuario. No hace falta:

- El navegador de la sesión **no tiene sesión en claude.ai**, así que la URL publicada no abre.
  No intentes iniciar sesión.
- Los archivos de esta carpeta **son byte por byte lo que está publicado** (el servicio sólo
  agrega el envoltorio `<!doctype html>…` y el `</body></html>` final). Verificalo con `diff`
  contra el HTML que baja `action:"read"` del artifact, saltando la primera línea.
- El pane del navegador muestra `file://` como captura estática y **no deja interactuar**. Hay
  que servir la carpeta por HTTP.
- **En esta máquina no hay node ni python** (el `python.exe` de `WindowsApps` es el stub de la
  Store). Sí hay `dotnet`. Lo que funcionó fue un servidor estático de 30 líneas con
  `System.Net.HttpListener` de PowerShell, corriendo en background sobre
  `http://localhost:8777/`. No necesitó permisos de administrador.

Con eso el tour se recorre entero, se puede instrumentar el puente desde la consola y se ven los
errores que la página se traga. Una sesión que pueda hacer esto va mucho más rápido.

---

## La cara recopiada — v20-v21, 2026-09-21

La copia del tour es ahora **el `web/` entero del `examenes.gmod` recién armado**, verificado con
`diff --strip-trailing-cr` contra el bundle: los ocho archivos idénticos, y el `index.html`
distinto **sólo en tres líneas** — el `<script src="simulador.js">` inyectado antes de `puente.js`.

Trajo cuatro archivos que la copia vieja no tenía: `idioma.js`, `texto/es-CR.js`, `texto/en.js` y
`brand/banner.jpg` (27 KB, no lo referencia nada, va por completitud).

### Lo que hubo que cambiarle al simulador, y cómo se detectó

**Los conjuntos cerrados pasaron a guion.** `consulte-pronto`, `en-ascenso`, `en-descenso`. La
copia vieja usaba espacios, y con espacios `saludo()` cae al texto por defecto sin avisar. Se
encuentran con `grep -o "'[a-z]\+-[a-z]\+'" cara.js`.

**`haySerie` faltaba en `presencia` y en `constancia`.** Éste no avisa: `repartir()` (cara.js:595)
lee `r.presencia.haySerie` / `r.constancia.haySerie`, y `!!undefined` es falso, así que el «Color
de la orina» —con cinco observaciones— caía en «todavía no podemos ver la tendencia» diciendo que
no hay datos suficientes, **con los datos ahí**. La cara vieja no lo miraba; por eso el campo
faltante sobrevivió sin síntoma hasta recopiarla.

La regla es `HaySerie => CantidadDeMediciones >= 2` (`HechosDeLaSerie.cs:55`), y en el simulador se
**deriva** de la cantidad de puntos, pruebas u observaciones, así que no se puede desincronizar.

### Dos cosas que cambiaron de comportamiento y NO son un error

- **«Merece atención» ahora tiene dos tarjetas, no tres.** La cara nueva reparte por
  `l.pideAlgo || l.severidad === 'urgente'` (cara.js:599), no por la forma del dato. Su propio
  comentario explica el cambio: antes «un binario o un nominal caían SIEMPRE en atención por ser
  binario o nominal, y eso es clasificar por la forma del dato en vez de por lo que el dominio
  dictaminó».
- **Eso rompe la verificación cruzada** que servía con la cara vieja (tres hallazgos = tres
  tarjetas). Ya no coinciden, **y está bien**: son dos criterios distintos, que es exactamente la
  distinción implementada en la v18. Atención es `pideAlgo`; fuera de rango es hallazgo.

### Una tensión del producto que queda a la vista

«Color de la orina» aparece bajo **«lo demás está bien»** con el chip **«en rango»**, aunque su
última observación sea «ámbar oscuro», fuera de lo esperable — y aunque cuente como hallazgo en
`analitosFueraDeRango`. El chip es la etiqueta **fija de la sección**, no un cálculo por analito
(cara.js:507), y el reparto lo decide `pideAlgo`. No es un defecto de la lámina; es el módulo con
estos datos sembrados. Si se capturan datos de una corrida real (pendiente 4), puede desaparecer.

### El sello — v21

Decía `UI según fase0/modulos-hospedados-en-ios · 98dac3a`, una rama que ya no es la del trabajo.
Ahora dice:

```
UI según trim/spike-wasm-mobile · 83b68be+ · 21/9/2026
```

**El `+` no es adorno.** La cara copiada incluye tres archivos —`cara.js` y los dos catálogos de
texto— con el arreglo del `catch`, que en `83b68be` **todavía no está**. Un sello con el commit
pelado estaría señalando un código que no es el que se ve. El pie de la lámina lo explica en una
frase. **Cuando esos cambios se commiteen, el sello pasa al commit nuevo y el `+` se va.**

### ⚠️ NADA DE ESTO ESTÁ COMMITEADO

`git status` al cerrar la v21, en `trim/spike-wasm-mobile`:

```
 M Examenes/App/Examenes.Cara/cara.js
 M Examenes/App/Examenes.Cara/texto/en.js
 M Examenes/App/Examenes.Cara/texto/es-CR.js
?? Herramientas/tour/
```

O sea: el arreglo del módulo está modificado sin commitear, y **toda esta carpeta está sin
trackear**. `Gnothi.Mobile/bundles/` no figura porque está ignorado —es artefacto de build—, así
que el `.gmod` rearmado tampoco viaja en un commit.

Si se pierde el working tree, se pierde todo el trabajo del 21/9. La lámina publicada sobrevive
—está en la URL—, pero el fuente no.

---

## Los dos idiomas — v22 / móvil v2

**Las dos láminas arrancan en INGLÉS**, con un botón que vuelve al español. El selector es de la
lámina, no del producto: el Host de verdad **negocia** con la persona y no tiene botón de idioma.

La regla que gobierna todo esto es `Metodologia/Guias/reglas-de-localizacion.md`. **Leerla antes
de tocar un texto**; son dos páginas y evita el error de traducir de más.

### Qué se traduce y qué no

| | ¿se traduce? | dónde vive en la lámina |
|---|---|---|
| UI del Host | **sí** | `TEXTO`, con el `en` **copiado** de `Gnothi.Host/textos.json` |
| voz del narrador (los globos) | **sí** | `TEXTO` |
| **voz de la institución** | **NO** | `INSTITUCIONES`: nombre, lema, `title`, y el título y la descripción de cada consulta |
| **dato del journal** | **NO** | lo que devuelve el simulador y pinta la cara |

Así que en la pantalla inglesa se leen «Delfos», «Tu salud, en tus manos», «Tus examenes, al dia»,
«Como vengo» y «Mis examenes» **en español**, y la frase de Claude dice *«the latest **Perfil
lipídico** from 10 September 2026: **Colesterol total** 214 mg/dL»*. **No es un defecto**: es la
regla 1 — *«el Host se localiza a sí mismo, la institución se localiza a sí misma, y ninguno
traduce al otro»* —. El globo del paso 4 lo dice en voz alta, en los dos idiomas.

**El `en` de la UI no está traducido acá: está copiado.** Cada clave del diccionario lleva al lado
la clave de `textos.json` de donde sale. Si alguna suena rara, se arregla **allá** y se vuelve a
copiar, nunca al revés. Se verificó que las 16 claves que usan las láminas tienen las dos columnas.

### Cómo sigue la cara, que es lo delicado

`idioma.js` de la cara negocia con `navigator.languages`, y la regla 11 dice —listado en «lo que
falta construir»— que **no hay ningún canal** para decirle a una carátula qué idioma quiere la
persona.

La lámina **no inventa ese canal**. Lo que hace es simular el **aparato**: `cara/idioma-del-aparato.js`
redefine `navigator.languages` adentro del marco, antes de que `idioma.js` lo lea, y la cara
negocia como negociaría de verdad en un aparato configurado así. El iframe se carga con
`cara/index.html?lang=en|es`.

Ese archivo es de la lámina, como `simulador.js`, y va **primero en el `<head>`** de
`cara/index.html`: `idioma.js` resuelve el catálogo al cargarse y no lo vuelve a mirar. Con eso la
copia de la cara pasó de tres líneas agregadas a **cinco**.

### Dos sombras que aparecieron al agregar `t()`

`superficie(t)` y `var t = herramientas()` **tapaban** la función de textos dentro de su alcance.
La segunda reventaba. Si se agrega otra función de una letra, conviene buscar antes si el nombre
ya está usado como variable local.

### Cambiar de idioma reinicia el recorrido

A propósito: los globos ya dichos quedarían en el idioma viejo, y media conversación de Claude
traducida a mitad de camino sería un aparato que no existe.

### Lo que NO se movió

- `retencion` y `refresco` («~2 MB al año, 5 años», «al abrir la app») son la promesa que la
  institución declaró en su manifiesto. El **marco** de la frase se traduce y los **valores** no,
  que es la regla 5: en el móvil se lee *«They keep it 5 años. It updates al abrir la app.»*
- El mensaje de error del Host (`module 'examenes' does not expose '…'`) sale en inglés siempre:
  es diagnóstico, no copy.
- El sello de la cabecera, que es una marca técnica.

---

## Lo real contra lo simulado

| | |
|---|---|
| **Real, copiado del repo** | `cara/` entero — el `web/` del `examenes.gmod`: `index.html`, `cara.js`, `cara.css`, `css/app.css`, `puente.js`, `idioma.js`, `texto/es-CR.js`, `texto/en.js`, `brand/banner.jpg` y las cuatro fuentes. **Recopiado el 21/9/2026; ver «La cara recopiada»** |
| **Real, extraído del repo** | `mymi.png`, el PNG de 256 px que lleva adentro `Gnothi.Desktop/mymi.ico` (el `mymi.png` de `wwwroot` no está versionado); los cinco `logo-*.png`, copias de los `brand/logo.png` de cada bundle |
| **Escrito para la demo** | `cara/simulador.js` — el puente falso y las dos tools — y **tres líneas** en `cara/index.html` que lo cargan |
| **Recreado** | `index.html` — el escritorio, la ventana de Mymi y la de Claude |

**El puente falso es lo único que hizo falta escribir.** `puente.js` es el real, sin una línea
cambiada; se le inyecta un `window.gnothiPuente` de mentira antes de cargarlo:

```js
window.gnothiPuente.consultar(id, vista, argumentosJSON)   // pregunta una [View]
window.gnothiPuente.pull(id, cursor)                        // le pide al cartero
window.gnothiPuente.listo()                                 // la página avisa que existe
// la respuesta aterriza en window.gnothi.respuesta(id, json, error)
```

La cara pide **cuatro vistas, no 21**: `historial`, `alertas`, `analitos` y `tendencia{analito}`
—ésta una vez por analito—, o sea **9 llamadas por apertura** con los 6 analitos sembrados
(3 + 6). El globo del paso 6 decía «once», que es la cuenta de cuando había ocho analitos;
corregido en la v15. **Si cambia la cantidad de analitos sembrados, ese texto hay que tocarlo.**

---

## Las cinco instituciones (v17)

La lámina ya no muestra una sola. **«Mis conexiones» arranca con cuatro instituciones ya
instaladas** —súper, gasolinera, taller y municipalidad— y el tour agrega la quinta, Delfos, con
su alta y todo. Eso hace **visible** la primera frase del tour: con la lista vacía había que
creerle a un globo; con cuatro rubros que no se parecen en nada, se ve.

Todo sale de `Gnothi.Mobile/bundles/hibrida/<modulo>/manifest.json`, y está en la constante
`INSTITUCIONES` de `index.html`:

| módulo | identidad | color | lema | versión · dominio | `.gmod` | consultas |
|---|---|---|---|---|---|---|
| `examenes` | Delfos | `#0e2f55` | Tu salud, en tus manos | 2.4.0 · delfos.cr | 234 KB · 19 | `resumen`, `lista_de_examenes` |
| `compras` | La Guaria | `#2f6b3f` | Tu canasta, más cerca de vos | 1.2.0 · laguaria.cr | 1,5 MB · 62 | `resumen`, `lista_de_compras` |
| `cargas` | Faro | `#0B3C49` | Rendimiento real, tramo a tramo | 1.0.3 · faro.cr | 192 KB · 15 | `resumen`, `lista_de_vehiculos` |
| `dealership` | Sotaventos Motors | `#1f3a5f` | Tu carro, siempre listo | 3.1.0 · sotavento.com | 306 KB · 20 | `resumen`, `hasta_donde_leyo` |
| `municipalidad` | Monteverde | `#2C306C` | Lo que el portal no te dice | 1.0.0 · monteverde.example | 142 KB · 14 | `resumen`, `lista_de_vencimientos` |

Los cinco declaran `network:false` y `folder:own`, y cada uno trae su `brand/logo.png`, copiado
al tour como `logo-<marca>.png`.

**La llave del consentimiento es `modulo.tool`, no el nombre de la tool**: `resumen` existe en las
cinco, y apagar la de una no puede apagar las otras cuatro. El nombre público en MCP es
`modulo_tool` (`Module.PublicName`, `Module.cs:101`), y por eso Claude ve **doce** herramientas:
2 del Host + 2 × 5.

**Sólo Delfos tiene la cara simulada.** Las otras cuatro existen como tarjeta y como catálogo; su
botón «Abrir» está inerte, que es el diseño del tour —sólo lo señalado es clickeable—. Simular las
otras cuatro caras es trabajo grande: cada una necesita su propio puente falso con datos sembrados
de su dominio, no una copia.

### El manifiesto: cuál es el que vale

**Hay tres manifiestos de exámenes en el repo y la v13 leyó el equivocado.** El que vale es el
del bundle que se instala, porque es el que viaja adentro del `.gmod`:

`Gnothi.Mobile/bundles/hibrida/examenes/manifest.json`

| | valor correcto |
|---|---|
| Identidad | **Delfos** (`brand.shortName`) |
| Institución | Laboratorio Clínico Delfos |
| Título del módulo | `Tus examenes, al dia` — va en la línea chica de la tarjeta |
| Logo | `brand/logo.png` — una gota azul con hoja, fondo transparente |
| Color | `#0e2f55` |
| Lema | «Tu salud, en tus manos» |
| Categoría | `laboratorio` |
| **Tools** | **`resumen` («Como vengo») y `lista_de_examenes` («Mis examenes»)**, las dos con `input: []` |
| `.gmod` | **234 KB** (240 448 bytes), **19 archivos** |
| Publisher | `delfos.cr`, **versión 2.4.0** |
| Journal | retención 5 años, ~2 MB al año |

**Corrección al relevo anterior:** decía versión *3.10.0* y **está mal**. El 3.x es el
`substrate.range` (`>=3.0.0 <4.0.0`), que es el sustrato que el módulo pide, no su versión.

Los otros dos son distractores:
- `Modulos/examenes/manifest.json` — copia vieja y traducida. Dice *"Clinical laboratory (POC
  mock)"* y declara **cinco** tools (`que_me_falta_medir`, `evaluar_tendencia`, `velocidad_de`,
  `presencia_de`, `constancia_de`). **Es la que usaba la v13.**
- `Examenes/manifiesto.json` — el fuente en esquema español.

### Cómo resuelve el Host la identidad

`Manifest.Identity` (`Gnothi.Host/Manifest.cs:85`) es `brand.shortName` si lo hay y
`institution` si no. Las cuatro pantallas del escritorio —`ModuleList`, `ModuleScreen`, `Alta`
y `Cara.razor`— leen **esa misma propiedad**, y por eso ninguna cae en un nombre distinto. Acá
hay `shortName`, así que en el escritorio dice **«Delfos»** en todas.

El teléfono lo parte en dos niveles («Laboratorio Clínico» chico sobre «Delfos» grande,
`ui.html` → `tiers()`). **El escritorio no**, y esta lámina simula el escritorio.

La tarjeta de `ModuleList.razor` es, campo por campo: logo declarado (o el dibujo del rubro si
no hay), `Manifest.Identity`, `brand.slogan` si lo hay, y en la línea chica `manifest.title` con
la cuenta de consultas encendidas.

---

## El argumento del tour, y por qué antes era falso

Con las cinco tools inventadas, el remate era *"el modelo no puede darte la tendencia"* — y
`evaluar_tendencia` figuraba entre ellas, **así que el argumento era falso**.

Con las dos reales el contraste es otro, es verdadero, y es más fuerte porque **la puerta se
abre y aun así no alcanza**:

- «¿Cómo vengo con mis exámenes?» → `examenes_resumen` → conteos: cuántos exámenes hay, cuántos
  analitos fuera de rango, hasta qué evento leyó la app.
- «¿Mi colesterol viene subiendo?» → `examenes_lista_de_examenes` → filas de **examen** (nombre,
  fecha, resultado). Claude puede leer los valores, **y aun así no puede decir si eso es una
  subida**: no tiene la serie del analito en el tiempo, ni el «nivel de siempre», ni el techo
  esperable.

Eso último es `InterpretacionDeAnalito` / `TendenciaDeAnalito`, que son `[View]`. Lo dice el
propio dominio en `Examenes/App/Examenes.App/Vistas/Vistas.cs`: *«la vista que NO tiene
equivalente en una pantalla de resultados: un laboratorio entrega valores, no tendencias»*.

Las formas de las dos tools salen de `Examenes/App/Examenes.App/ConsultasDelModulo.cs`:

```
resumen           -> { hayDatos, examenes, analitosFueraDeRango, ultimoEventoLeido }
lista_de_examenes -> { hayDatos, examenes: [ ExamenEnLista ] }
ExamenEnLista     =  { Nombre, Fecha, Resultado, AportaSerieContinua }
```

En el simulador se derivan de los mismos datos sembrados que pinta la cara, para que las dos
puertas no se puedan contradecir.

El paso 9 apaga **`lista_de_examenes`**, que es la que acababa de traer filas de verdad: apagar
la que no sirvió no probaría nada.

---

## Decisiones del usuario, ya tomadas

- **Escritorio simulado con clics guiados**: sólo el elemento correcto es clickeable, con halo
  pulsante. No escritorio libre.
- **Arranca con Mymi ya instalado**, no desde la instalación del MSI.
- **El asistente es una app externa** —Claude Desktop— hablándole al Host por MCP, **no** el
  asistente interno de Mymi. Eso fue una corrección explícita: *"yo no hablo del asistente
  dentro de Mymi, sino que haya una app externa como claude o Chatgpt que mediante el MCP de
  Gnothi le hace consultas"*.
- **La cara va copiada y congelada**, con la fecha del snapshot a la vista. Se acepta la deuda
  de recopiarla cuando cambie.
- Público: **personas técnicas**, no usuarios finales.

## La UI de Mymi, verificada contra el repo

De `Gnothi.Desktop` (tras el merge del 2026-09-21 ya está todo en `trim/spike-wasm-mobile`):

- Se llama **Mymi** de cara a la persona; Gnothi quedó en el código y el protocolo
- Header con la mascota (56 px, **sin** `border-radius` — «el logo va suelto, sin teja»)
- Tabs horizontales: **Conexiones · Asistente · Ajustes**
- Paleta: `--brand:#6C4BD9`, `--ink:#241F3D`, `--panel:#F8F6FD`; botones píldora con sombra violeta
- Pantalla: **«Mis conexiones»** — *"Lo que cada institución te entregó. Mymi lee lo que ya está
  en este aparato."*
- Tarjeta con **franja izquierda del color que declara la institución**
- Dentro del módulo: **la pantalla de la institución va PRIMERO**, y debajo las consultas con
  botón **«Consultar»** (no "Ejecutar"). Hay también «Poner un icono en el escritorio»
- La cara toma **la ventana entera**, sin header ni tabs, con una barra: *← Volver a Gnothi ·
  <institución> · dentro de Gnothi, tu registro queda en esta computadora*

**Inconsistencia del repo, no de la lámina:** el header dice *Mymi* pero esa barra dice *"Volver
a Gnothi"* y *"dentro de Gnothi"*. Quedaron sin migrar cuando cambió la marca.

## El consentimiento: dónde se apaga de verdad

Acá también hubo dos intentos fallidos. Primero se puso una pantalla de "Permisos" inventada;
después, leyendo un comentario de `ModuleList.razor`, se lo cambió a **editar `installation.json`
en el Bloc de notas** — y el usuario lo rechazó dos veces, la segunda con énfasis: *"DE NUEVO,
esto no se hace desde el installation.json!!"*.

**Lo correcto:** un **interruptor por consulta**, en la pantalla de detalle de la institución.
Vive en `Gnothi.Mobile/Assets/ui.html` con `gn.setTool(modulo, tool, checked)`, y sus textos
están en `Gnothi.Host/textos.json`:

- `detalle.consultas.de` → «Consultas de {quien}»
- `detalle.consultas.lead` → «Puedes preguntarle a tu asistente sobre:»
- `detalle.consultas.apagada` → «Una consulta apagada desaparece del catálogo: el asistente no la
  ve y no puede pedirla.»

Su CSS es `.sw` en `ui.html` (46×28, perilla de 22 que corre 18 px, pista `#D4D0E3`). Allá es un
`<label>` con un `<input type=checkbox>`; en la lámina es un `<button role="switch">`, así que el
estado se lee de `aria-checked` en vez de `:checked`.

### ✅ CONTESTADO, y la respuesta es que en el escritorio NO está — 2026-09-21

Ya no hay que preguntarlo: se corrió Mymi de verdad en Linux y se miró la pantalla. La captura
está en `Documentacion/Jonathan/imagenes/2026-09-21-vm-como-vengo.png`, y la evidencia entera en
`F9-la-vm-de-linux.md`.

`PaginaDeLaInstitucion` en el escritorio muestra, y nada más:

```
← Todas tus instituciones
Delfos · Tus examenes, al dia

  La pantalla de Delfos
  La cara que la institución diseñó, corriendo dentro de Mymi con el motor prestado.
  [ Abrir la pantalla ]

  Como vengo
  Cuantos examenes tiene registrados la persona en este dispositivo, cuantos analitos
  estan fuera de rango, y hasta que evento del laboratorio leyo su app.
  [ Consultar ]

  Mis examenes
  La lista de examenes que la app del laboratorio tiene registrados en este dispositivo,
  con su fecha y su resultado.
  [ Consultar ]
```

**Ni un interruptor.** El comentario de `ModuleList.razor` tenía razón: el consentimiento por
consulta es de la cara de los teléfonos. Así que la lámina, que simula un **escritorio**, está
mostrando algo que ahí no existe.

### ✅ RESUELTO EN LA LÁMINA — v17

Se verificó también desde el código, además de la corrida: `ModuleScreen.razor` recorre
`module.EnabledTools()` —ya filtrado por consentimiento— y no ofrece ningún control para apagar;
en todo `Gnothi.Desktop` no hay un `setTool` ni un `role="switch"`.

**El paso 9 ahora muestra EL EFECTO en vez del control**, y sale más fuerte que antes. La persona
apagó «Mis examenes» desde su teléfono, y al volver a Mymi la pantalla tiene **una sola consulta**:
la apagada no está atenuada, **no está**. Después, del otro lado, Claude ve once herramientas y no
doce. Una decisión, dos puertas, y ninguna de las dos es donde se decidió.

Eso lo permite `Module.EnabledTools()` (`Module.cs:107`), que filtra para las dos puertas. Su
comentario es lo que el paso enseña:

> *Disabled returns empty: the module never finds out, and cannot tell "I was switched off" apart
> from "nobody asked". That indistinguishability is the property, not a side effect.*

El CSS `.switch` que la v14 había copiado del teléfono se **quitó** junto con el botón: era código
muerto y, peor, sostenía una pantalla que no existe.

---

## Bugs que ya se arreglaron (para no repetirlos)

**`alertas` reventaba y un `catch` vacío se lo tragaba.** (v14) En `simulador.js` la vista
`alertas` hacía `ANALITOS['Colesterol total']` y `ANALITOS['Sangre en la orina']`, con espacios,
pero las claves son `ColesterolTotal` y `SangreEnOrina`. Tiraba *"Cannot read properties of
undefined (reading 'lectura')"* en **las dos pasadas**, y el `.catch(function () { /* el hero se
queda con su frase por omisión */ })` de `cara.js` lo silenciaba — pero no hay frase por omisión:
el HTML se queda con el texto de carga. Resultado: en el paso 6, que es el clímax, el titular
decía **«Preguntándole a Gnothi…»** para siempre, la fecha del último chequeo quedaba oculta, la
línea secundaria vacía y la sección `#pedido` sin pintar. **No aparecía nada en la consola.**
Se encontró envolviendo `gnothiPuente.consultar` y `gnothi.respuesta` desde la página padre y
mirando el ida y vuelta.

**Faltaba el CSS del interruptor.** (v14) `consulta()` generaba `<button class="switch">` y no
había una sola regla `.switch`, así que se quedaba con el estilo global de `button`: una píldora
violeta rellena, sin perilla, **del mismo color encendida que apagada** — justo el control que el
paso 9 le pide a la persona que mire.

**`[hidden]` vencido por una clase con `display`.** `.ventana { display:flex }` le gana al
`display:none` que el navegador da al atributo `hidden`, así que la ventana estaba renderizada
encima del escritorio desde el arranque y se comía todos los clics. Síntoma: *"no funciona
ningún click"*. Arreglo: `[hidden]{display:none!important}` **antes** de cualquier regla con
`display`.

**Referencias muertas tras borrar elementos.** Al sacar el Bloc de notas quedaron `$('bloc')` y
`$('ic-json')` en el JS; una corría en el arranque, tiraba `TypeError` y el script moría entero
antes de armar un solo clic. Mismo síntoma. **Auditar los `$('id')` del script contra los `id`
del HTML antes de publicar.**

**`transform` de centrado peleado con la animación.** `.dlg` se centraba con
`translate(-50%,-50%)` y la animación de apertura animaba `transform`: durante esos 200 ms el
centrado no está aplicado, así que medir el diálogo daba un rectángulo corrido media pantalla y
el halo señalaba aire. Arreglo: centrar con `inset:0; margin:auto` y animar sólo la opacidad.

**El guía scrolleando el escritorio.** `traerAlaVista` subía buscando un ancestro con scroll y
encontraba `.escritorio`, que es `overflow:hidden` pero acepta `scrollTop` por código: movía el
escritorio entero y descolocaba el halo. Arreglo: considerar sólo `overflow` `auto` o `scroll`.

**El globo tapando lo que hay que leer.** Buscarle un lado libre alrededor del objetivo no
funciona: la ventana ocupa casi todo. Solución: **una columna fija a la derecha** para el globo,
y el escritorio en tres franjas que no se pisan — íconos hasta el 13%, ventanas en el medio,
globo a la derecha. (Por debajo de 820 px de ancho el layout cae a móvil y el globo vuelve a
pisar la ventana; es el ancho del pane, no un defecto de la lámina.)

**No usar `perl` con `$('…')` desde bash.** Bash se come las expansiones o el patrón no matchea
en silencio. Dos veces dejó el archivo corrupto. Usar el editor de archivos.

**Los textos y los números se desincronizaron, tres veces.** (v14–v16)

- En la v13 los globos ya decían «cuatro herramientas» y «tres, no cuatro» —los valores correctos
  con dos tools— mientras la barra de Claude mostraba 7 y 6.
- El globo del paso 6 decía «preguntó **once veces** al dominio»: la cuenta de cuando había ocho
  analitos sembrados. Son **nueve** (3 vistas fijas + 6 analitos).
- `alertas` devolvía «**Seis de tus ocho** resultados están dentro de lo esperado», y eso salía
  grande en el hero, contradiciendo a los seis analitos que la propia pantalla pinta abajo.

Los dos últimos ya **no se pueden desincronizar**: se derivan de `NOMBRES` y de `lectura.pideAlgo`
(«Cuatro de tus seis…»). El del paso 6 sigue escrito a mano en `index.html` — **si cambia la
cantidad de analitos sembrados, hay que tocarlo**.

La regla general: un número escrito a mano al lado de datos que cambian se desincroniza. Cuando
se corrija un texto, correr el recorrido entero y **leer los números en pantalla**, no sólo el
diff.

**«Fuera de rango» no es «pide algo».** (v18) El simulador derivaba `analitosFueraDeRango` de
`lectura.pideAlgo`, y son dos preguntas distintas. El dominio
(`Expediente.AnalitosFueraDeRango`, `Expediente.cs:746`) cuenta los analitos cuya **última
lectura es un hallazgo**, y **la tendencia no entra**: un colesterol que viene subiendo pero cuyo
último valor sigue bajo el techo *pide algo* y **no** está fuera de rango. `LoQueEstaEnRango`
(`Expediente.cs:718`) es su contracara, con la misma regla.

Cada forma de serie responde lo suyo, y el simulador ahora las imita una por una:

| forma | archivo | «hay algo que no debería estar» |
|---|---|---|
| continua | `SerieContinua.cs:91` | hay rango para esta persona **y** el último no es normal. **Sin rango no hay hallazgo** — eso no es «está bien», es que no se puede afirmar que esté mal |
| binaria | `SerieBinaria.cs:71` | la última está presente. Literal |
| nominal | `SerieNominal.cs:69` | la categoría de hoy no es de las esperables |

Con los datos sembrados da **3** —colesterol total sobre el techo, sangre en la orina presente,
color de orina fuera de lo esperable— y por lo tanto «**Tres** de tus seis resultados están dentro
de lo esperado». Antes decía 2 y «cuatro de seis».

**La verificación cruzada que conviene repetir:** los tres hallazgos coinciden exactamente con las
tres tarjetas que la cara pinta en «MERECE ATENCIÓN» **por su propia lógica**, que no es ésta. Dos
caminos independientes que dan lo mismo.

**Ojo con `referencias`:** `habitual` es el «nivel de siempre» que la pantalla dibuja de fondo,
**no** un límite. Sólo `piso` y `techo` son el rango. Contar `habitual` daría un hallazgo de más
en el colesterol.

---

## Lo que falta

1. ~~La cara copiada quedó vieja~~ — **recopiada en la v20**, ver «La cara recopiada».
2. **El `catch` mudo: arreglado EN EL MÓDULO REAL, falta que llegue al bundle.**

   La falla estaba en el fuente, `Examenes/App/Examenes.Cara/cara.js:727`, idéntica a la de la
   copia (el bundle es copia byte por byte del fuente: 43 807 los dos). Así que se arregló allá,
   no en la lámina.

   Eran dos cosas, no una: el `catch` no decía nada **y** su comentario afirmaba que «el hero se
   queda con su frase por omisión» cuando no hay tal frase — el titular se quedaba con el texto de
   **carga**, «Preguntándole a Mymi…», presentado como si fuera el resultado, para siempre y sin
   una línea en la consola.

   Lo que se hizo, en tres archivos:
   - `cara.js` — el `catch` pone el titular del fallo, limpia el secundario y manda el error a
     `contar(..., true)`, que es el canal que la otra cadena de `pintarTodo` ya usaba. Con la
     misma guarda de `pasada` que el resto, para que una pasada vieja que falla tarde no pise a
     la nueva.
   - `texto/es-CR.js` y `texto/en.js` — una cuarta clave `portada.noSePudoLeer`. **Tenía que ir
     en los dos**: el propio catálogo avisa que «una clave que está en un archivo y no en el otro
     es una frase que llega a medio escribir».

   **Lo que NO se puede hacer, y por eso es una cuarta frase y no una de las tres:** caer a «en
   general, tus resultados vienen bien» cuando el expediente no se pudo leer. Eso sería afirmar
   algo sobre lo que no se leyó.

   Probado sirviendo el fuente real con un puente falso que hace reventar `alertas`: el titular
   dice «No se pudo leer tu expediente ahora.», el error sale en `#conexion` con la clase `malo`,
   y el resto de la pantalla sigue pintando. Con `alertas` sana, todo igual que antes.

   **El bundle ya se rearmó**, con:

   ```
   .\armar-bundle.ps1 -Modulo examenes -Forma hibrida -Publicador "http://127.0.0.1:8091/"
   ```

   Dos cosas que conviene saber antes de correrlo:
   - **`-Publicador` es una URL completa, no un puerto.** Sin él cae al puerto del mock del
     catálogo, que no es el 8091 que quedó en el manifiesto.
   - **`-Forma hibrida` es obligatorio.** Por omisión arma `hospedada`, que es otra cosa.
   - La llave de firma se reusa si existe (`bundles/.keys/examenes.key.json`), así que no cambia
     la identidad de la institución. El `manifest.json` quedó **idéntico**, mismo md5: no se pisó
     nada.

   El `.gmod` pasó de 240 446 a 241 183 bytes, o sea de **234 a 235 KB**, y la lámina lo decía en
   dos lugares — corregido en la v19. Ahí se vio que el alta repetía a mano los datos que ya
   estaban en `INSTITUCIONES`; ahora los lee de ahí, así que ese número no se puede volver a
   desincronizar.
3. **Las caras de las otras cuatro instituciones.** Desde la v17 las cinco están en la lista y en
   el catálogo MCP, pero sólo Delfos tiene pantalla simulada. Cada una de las otras necesita su
   puente falso con datos de su dominio —cuatro simuladores, no cuatro copias— y las cinco caras
   juntas pesan ~2,7 MB en 100 archivos (La Guaria sola son 1,88 MB y 54 archivos). Cabe de sobra
   en el artifact; el costo es el trabajo y la deuda de recopiado, que se quintuplica.
4. Sembrar los JSON con la forma exacta de `InterpretacionDeAnalito` — hoy están inventados a
   mano y la cara pinta, pero conviene capturarlos de una corrida real.
5. ~~`analitosFueraDeRango` cuenta otra cosa~~ — **hecho en la v18**, ver abajo.

---

## Cómo republicar

Los archivos de esta carpeta se publican con la herramienta de Artifacts, pasando `index.html`
como página y el resto en `files`, **con la misma URL** para no crear una lámina nueva:

```
url: https://claude.ai/artifact/6jt4cPXvDqoEeSU2WpZGdk
```

**El servicio pide más que «leer el artifact antes», y rechaza hasta que se cumple:**

1. `action:"read"` del artifact guarda el HTML vivo en un archivo y exige **leerlo entero**
   (960 líneas en la v13) antes de dejar sobrescribir. Leer la cabecera no alcanza.
2. Si un archivo de `files` cambió en el servidor desde la última vez que se lo vio, hay que
   leerlo aparte con `action:"read"` + `path` — por ejemplo `cara/simulador.js`.
3. Si un intento se rechaza y se reenvía el **mismo** contenido sin un `read` en el medio, lo
   rechaza por duplicado. Hay que volver a hacer el `read` y recién ahí publicar.
4. Los archivos que **no** se mencionan en `files` se conservan. Conviene pasar sólo los que
   cambiaron, y así no hay que leer cada uno de los doce.
5. Las fuentes tienen que estar **dentro del working directory o del scratchpad**. Esta carpeta
   está fuera, así que hay que copiarla al scratchpad y publicar desde ahí.

**No se pierde nada por sobrescribir**: la copia de esta carpeta y la publicada son la misma,
verificable con `diff`.

---

## Los textos REALES de Mymi, capturados de una corrida — 2026-09-21

Todo lo de acá salió de Mymi corriendo en Linux dentro de la VM de demo, no del código. Sirve
para que la lámina diga lo mismo que dice el producto. Las cuatro capturas están en
`Documentacion/Jonathan/imagenes/2026-09-21-vm-*.png` y la evidencia en `F9-la-vm-de-linux.md`.

### La lista de instituciones vacía

```
Mis conexiones
Lo que cada institución te entregó. Mymi lee lo que ya está en este aparato.

        Todavía no hay ninguna institución en
             /opt/mymi/Modulos

  ┌ Agregar una institución ──────────────────────────────┐
  │ Pega el enlace que te dio la institución, o elige el  │
  │ paquete que te mandó.                                  │
  │ [ https://…/modulo.gmod, o el enlace de go.gnothi.app ]│
  │ ( Agregar )  ( Elegir el archivo… )                    │
  └────────────────────────────────────────────────────────┘
Recargar · vuelve a leer installation.json del disco
```

Las pestañas son **Conexiones · Asistente · Ajustes**.

### La hoja del alta — la pantalla que más dice de todo el producto

```
Delfos
Laboratorio Clínico Delfos S.A., 3-101-123456 · Declara delfos.cr y lo sirvió
examenes.gmod · v2.4.0
Tus resultados de laboratorio, con su historia y que te toca repetir. No interpreta
resultados ni reemplaza a tu medico.

Qué va a poder contestar
  Como vengo — Cuantos examenes tiene registrados la persona en este dispositivo,
  cuantos analitos estan fuera de rango, y hasta que evento del laboratorio leyo su app.
  Mis examenes — La lista de examenes que la app del laboratorio tiene registrados en
  este dispositivo, con su fecha y su resultado.

Dónde va a vivir tu registro
  /home/demo/Documents/Gnothi/examenes
  Unos ~2 MB al ano.Se actualiza al abrir la app.La institución lo guarda 5 anos.

Con quién va a hablar
  http://127.0.0.1:8091/expediente/1 — Es la única red que va a poder tocar, y la hace
  Mymi por él.

Lo que Mymi revisó
  · Lo sirvió examenes.gmod. Ese origen lo publica, pero el paquete no declara de qué
    origen es: no queda atado del todo.
  · La conexión no va cifrada: así solo sirve en desarrollo.
  · Firmado por una llave («examenes-20260921») que su origen no publica y que Mymi no conocía.
  · Trae la pantalla de la institución y su reglamento al lado: 19 archivos, 0,5 MB. El
    motor se lo presta Mymi; la pantalla se abre dentro de esta ventana.
  · No pide red, ni micrófono, ni parlante.
  · Sin ejecutables ni datos de muestra adentro.

( Instalar en esta computadora )  ( Ahora no )
```

**Ojo con el bug**: *«al ano.Se actualiza al abrir la app.La institución»* — tres frases pegadas
sin espacio. Está en el producto, no es un error de transcripción. Si la lámina lo copia tal
cual, copia el bug; conviene escribirlo bien y anotar que el producto lo tiene mal.

### La tarjeta, ya instalada

```
[logo] Delfos                                          ( Abrir )
       Tu salud, en tus manos
       Tus examenes, al dia · 2 permisos activos
```

Y debajo, el resultado del alta:

```
Listo: ya tienes «Delfos» en Mymi.
Comprobación de Delfos: {"module":"examenes","matches":true,
  "declared":["lista_de_examenes","resumen"],"actual":["lista_de_examenes","resumen"],
  "undeclared":[],"declaredButMissing":[],"detail":""}
```

### La cara de Delfos, con los datos puestos

```
← Volver a Mymi   Delfos        dentro de Mymi · tu registro queda en esta computadora

  (o) Laboratorio Clínico Delfos                                              ( ↻ )
      Al servicio de su salud desde 1987

  Hola.
  Entendé cómo venís, no solo cómo saliste hoy.

  TU ÚLTIMO CHEQUEO ─────────────────────────────────────────────── 28 abr 2026

  En general, tus resultados vienen bien.
  17 de tus 18 valores están en su rango.

  MERECE ATENCIÓN                                              ● 1 resultado
  ┌────────────────────────────────────────────────────────────────────────┐
  │ Colesterol total                                        212 mg/dL     │
  │ ↑ viene aumentando                                                    │
  │ 182 → 212                                                             │
  │ referencia: hasta 220 mg/dL                                           │
  │ [════════════════════════════════════════════════●══]                 │
  │ Tu resultado · 212 mg/dL          Referencia · hasta 220 mg/dL        │
  │ ┌ i ─────────────────────────────────────────────────────────────────┐ │
  │ │ Subió de 182 a 212 mg/dL en 734 días, todavía dentro de lo        │ │
  │ │ deseable.                                                          │ │
  │ │ Conversalo con tu médico en la próxima cita.                       │ │
  │ └────────────────────────────────────────────────────────────────────┘ │
  └────────────────────────────────────────────────────────────────────────┘

  LO DEMÁS ESTÁ BIEN
  Estos resultados están dentro de lo esperado.
```

Dos cosas que la lámina debería respetar:

**El voseo es de la institución, no del Host.** *«Entendé cómo venís»*, *«Conversalo con tu
médico»*. El Host habla de **tú neutro** (*«Pega el enlace»*, *«Puedes preguntarle»*) y la
institución habla como quiere: son dos voces distintas en la misma ventana, a propósito, y las
reglas de localización dicen que el Host no traduce ni corrige la voz de la institución.

**El 212 está DENTRO del rango** (hasta 220) y la cara igual lo levanta, porque lo que muestra es
**la tendencia**. El `[Query]` es consistente: `"analitosFueraDeRango": 0`. Eso no es una
contradicción y es justo el argumento del producto —la diferencia entre una hoja de resultados y
una app que tiene la serie—, así que si el tour menciona el 212 tiene que decir *«viene
aumentando»*, no *«está fuera de rango»*.

### Lo que contesta el `[Query]`

```json
{ "hayDatos": true, "examenes": 18, "analitosFueraDeRango": 0, "ultimoEventoLeido": 217 }
```

Y antes del primer refresh, con el journal vacío:

```json
{ "hayDatos": false, "carpeta": "/home/demo/Documents/Gnothi/examenes",
  "detalle": "The app has not recorded anything in this folder yet. Ask the person to
              open their exams app and press Update." }
```

Ese `detalle` está **en inglés a propósito**: es texto escrito para el modelo, no para el ojo, y
por regla no entra al catálogo de textos del Host. Si la lámina lo muestra, va en inglés.

### Un defecto más, visible en las capturas

El logo de Delfos **no pinta en los encabezados**: en la tarjeta de «Mis conexiones» la gota sale
bien, pero en el título de la hoja del alta y en el de la página de la institución sale un cuadro
vacío. La lámina, que copia la cara frozen del repo, no lo tiene — así que acá la lámina se ve
mejor que el producto. Vale saberlo antes de comparar las dos en una demo.
