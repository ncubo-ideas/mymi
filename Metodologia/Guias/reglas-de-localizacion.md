# Las reglas de la localización

Qué texto se traduce, qué texto no, y quién es el dueño de cada uno.

> **Esto es una referencia, no un tutorial.** Se lee entero una vez —son dos páginas— y después se
> entra por el índice. Si estás por tocar el Host, una carátula, un dominio o un manifiesto y hay
> texto en el medio, la respuesta está acá.

**El alcance es `es` y `en`, y nada más.** Varias de estas reglas se relajarían con un tercer idioma
y otras se endurecerían; están escritas para dos.

---

## Índice

| | Regla | Cuándo te muerde |
|---|---|---|
| **1** | [Tres dueños, y ninguno traduce al otro](#1--tres-dueños-y-ninguno-traduce-al-otro) | La regla madre. Todas las demás salen de ésta |
| **2** | [Antes de traducir algo, decidí en cuál de las cuatro categorías cae](#2--las-cuatro-categorías) | Siempre. Es el primer paso, no un paso opcional |
| **3** | [El journal no se traduce nunca](#3--el-journal-no-se-traduce-nunca) | Al ver una pantalla en inglés con datos en español |
| **4** | [La voz de la institución no se traduce](#4--la-voz-de-la-institución-no-se-traduce) | Con el slogan, y con cuatro campos más que no son obvios |
| **5** | [La frase del dominio sí se traduce: el marco, no los valores](#5--la-frase-del-dominio-sí-se-traduce-el-marco-no-los-valores) | **El trabajo real.** Es la categoría que cuesta |
| **6** | [Un conjunto cerrado no es dato del registro](#6--un-conjunto-cerrado-no-es-dato-del-registro) | **La trampa.** A ojo es indistinguible del dato |
| **7** | [La clave es la frase entera, nunca palabras sueltas](#7--la-clave-es-la-frase-entera-nunca-palabras-sueltas) | Con género y concordancia |
| **8** | [El formato de números y fechas viaja con el render](#8--el-formato-de-números-y-fechas-viaja-con-el-render) | Con la coma decimal, y ya mordió una vez |
| **9** | [El dominio no lee la cultura de la máquina](#9--el-dominio-no-lee-la-cultura-de-la-máquina) | Silencioso: la consola y el wasm dicen distinto |
| **10** | [Un solo catálogo para el Host, proyectado por cara](#10--un-solo-catálogo-para-el-host-proyectado-por-cara) | Cuando la segunda cara empieza a divergir |
| **11** | [La Wasm sigue a la persona, y el bundle embarca los dos idiomas](#11--la-wasm-sigue-a-la-persona-y-el-bundle-embarca-los-dos-idiomas) | Al construir una carátula nueva |
| **12** | [`about.locale` es el idioma del registro, no el de la prosa](#12--aboutlocale-es-el-idioma-del-registro-no-el-de-la-prosa) | Al suponer que el manifiesto ya dice más de lo que dice |
| **13** | [Al journal no le entra texto renderizado](#13--al-journal-no-le-entra-texto-renderizado) | Al segundo idioma, y ya es tarde |

---

## 1 · Tres dueños, y ninguno traduce al otro

| | de quién es | sigue a |
|---|---|---|
| **el idioma del registro** | la institución | nada: es un hecho fechado |
| **la voz de la institución** | la institución | lo que ella eligió |
| **el idioma del Host** | Gnothi | **la persona** |

**El Host se localiza a sí mismo, la institución se localiza a sí misma, y ninguno traduce al otro.**

Que en una misma pantalla convivan un Host en inglés y una tarjeta en español **no es un defecto**:
el Host no puede ponerle palabras en la boca a una institución. Es el mismo corte que ya está escrito
en [`Manifest.cs`](../../Gnothi.Host/Manifest.cs) —*"la institucion DECLARA, el Host COMPONE"*— y la
misma razón por la que no existe ni va a existir un `assistantHint`.

Hay además una razón que llega después: **cuando el manifiesto se firme, un texto traducido por el
Host es un texto fuera de la firma.**

## 2 · Las cuatro categorías

Antes de traducir cualquier cosa, decidí en cuál cae. **Es el primer paso.** Casi todos los errores
de esta guía son haberse saltado este paso.

| categoría | ejemplo | ¿se traduce? | regla |
|---|---|---|---|
| **UI propia de la carátula o del Host** | *"Lo que falta"*, *"Agregar"*, *"Tus instituciones"* | **sí** | 10, 11 |
| **Dato del journal** | *"leche"*, *"Bienes inmuebles"*, *"Sotaventos Motors"* | **no** | 3 |
| **Voz de la institución** | *"La Guaria y vos"*, `about.description` | **no** | 4 |
| **Frase que compone el dominio** | *"En bienes-inmuebles, un período pasó de 20 000 en 2024-T2 a…"* | **sí** | 5, 6, 7 |

Las dos del medio se parecen y no son lo mismo: el dato lo **asentó** la institución en el registro,
la voz la **declaró** en su manifiesto. Coinciden en que ninguna se traduce, y difieren en por qué.

## 3 · El journal no se traduce nunca

El journal es un log append-only y reproducible. Un valor que salió de ahí se muestra **tal como se
asentó**, aunque la pantalla esté en el otro idioma.

Traducir al mostrar sería mostrar algo distinto de lo que la institución registró. Que se vea raro
—una pantalla en inglés que dice `leche`— es **correcto**: es el registro de la persona, no texto
nuestro.

## 4 · La voz de la institución no se traduce

No se traduce nada de esto, y los últimos cuatro son los que se olvidan:

- `brand.slogan` — *"La Guaria y vos"*
- `brand.shortName` y `institution`
- **`title`** del módulo — *"Mi plan de mantenimiento"*
- **`about.description`** y **`about.doesNotDo`**
- **`descripcion` y `titulo` de cada tool**, y las instrucciones del servidor MCP

Los tres últimos grupos caen acá aunque **no** los lea la persona: la carátula MCP es voz de la
institución hablándole al modelo, y sigue el idioma del módulo, no el de quien pregunta.

> **Corolario para los topes.** `MaxShortName` (28) y `MaxSlogan` (40) en
> [`Manifest.cs:211`](../../Gnothi.Host/Manifest.cs) están medidos en caracteres de español para
> la grilla de dos columnas del teléfono. **Siguen valiendo tal cual**, justamente porque estos
> campos no se traducen.

## 5 · La frase del dominio sí se traduce: el marco, no los valores

Ésta es la categoría que no es ni UI ni registro ni voz de la institución: **la escribimos nosotros**,
no está guardada en ningún lado y se compone al leer. Por ejemplo
[`Variacion.Dice()`](../../Municipalidad/Dominio/Variacion.cs):

> `"En bienes-inmuebles, un período pasó de 20 000 en 2024-T2 a 36 000 en 2025-T2: un 80 % más. Es
> un salto, y este rubro se cobra sobre la base imponible…"`

Y acá está la costura que hace que no se resuelva con buscar-y-reemplazar: **esa frase contiene dato
del journal.** `bienes-inmuebles` es el rubro y salió del registro. Se traduce **el marco**; los
valores viajan intactos:

```
"En {rubro}, un período pasó de {a} en {p1} a {b} en {p2}: {cambio}. {explicación}"
"In {rubro}, one period went from {a} in {p1} to {b} in {p2}: {cambio}. {explicación}"
```

**La regla:** el dominio deja de componer *un string* y compone **una clave estable más los valores
ya tipados** que van en los huecos. Es literalmente lo que `Dice()` ya arma — sólo que hoy concatena
al final.

- **La regla se queda en el dominio.** Él sigue decidiendo *qué* frase y *con qué* valores. Lo único
  que se va es la redacción.
- **El renderizador vive en `App/`**, uno por idioma.
- **Dejá el `Dice()` actual** como el render `es` sobre esa estructura. Nada se rompe de golpe.

Las dos salidas fáciles son las dos malas: meterle `CultureInfo` al dominio (ver la 9) o sacar las
frases y que **cada carátula recomponga** — que es la *frase rival* que
[`Dichos.cs`](../../Municipalidad/Dominio/Dichos.cs) existe para evitar, multiplicada por tres
carátulas.

> **El volumen está muy desparejo**, y conviene saberlo antes de estimar. Métodos que componen frase:
> Municipalidad **24**, Concesionario **16**, Cargas **3**, y Compras y Exámenes **0** — no porque no
> tengan, sino porque las arman inline y **hay que inventariarlas primero**.

## 6 · Un conjunto cerrado no es dato del registro

**La trampa de toda esta guía.** Hay valores que parecen dato del journal y no lo son:

`Atrasada` · `Por vencer` · `urgente` · `positivo` · `no realizado` · `cinta de cargas`

Ninguno se leyó del registro: los elige el dominio adentro, de una lista cerrada. **Se traducen.** Y
en una pantalla son indistinguibles a ojo de un rubro que sí vino del journal.

La forma ya está inventada en el repo, en
[`DueState.cs:25`](../../Concesionario/Dominio/DueState.cs):

```csharp
internal static readonly DueState Overdue = new DueState("Overdue", "Atrasada");
//                                                        ^código     ^dicho
```

`Name` es lo único que cruza el wire; `HowItIsSaid` es cómo se dice. El código es identidad y **no se
toca**; el dicho es lo que se traduce.

> **La regla práctica:** si el valor tiene un código estable al lado, se traduce. Si salió del
> registro, no.

## 7 · La clave es la frase entera, nunca palabras sueltas

*"Atrasada"* vs *"Atrasado"*. El español concuerda en género y el inglés no, así que un catálogo que
arma frases pegando fragmentos produce concordancias rotas que ningún test ve.

Se resuelve solo mientras **cada clave sea una frase completa**. La tentación de partirlas para
"reusar" aparece apenas el catálogo crece: no.

Los plurales sí se pueden resolver a mano, porque `es` y `en` tienen los dos dos formas. El patrón
que ya usa Cargas (`BoletosEmitidos == 1 ? "boleto" : "boletos"`) sobrevive; sólo necesita el par por
idioma. **No hace falta maquinaria tipo ICU.**

## 8 · El formato de números y fechas viaja con el render

`6,2 %` en español es `6.2%` en inglés. `₡172.630` lleva punto de miles en Costa Rica y coma en medio
mundo.

[`Dichos.cs:41`](../../Municipalidad/Dominio/Dichos.cs) pone la coma decimal a mano hoy, y está
bien que exista **un solo lugar** que lo haga — pero ese lugar se va con el renderizador, no se queda
en el dominio.

Ya mordió una vez: [`Plata.cs:14`](../../Compras/Dominio/Plata.cs) documenta que la misma app
mostraba `₡172,630` en un lado y `₡32.910` en el otro.

## 9 · El dominio no lee la cultura de la máquina

Nada de `CultureInfo.CurrentCulture` ni `CurrentUICulture` adentro de `Dominio/`. **Nunca.**

Tres razones, y las tres están medidas:

1. Una frase que dijera `6,2 %` en una máquina y `6.2 %` en otra sería **la misma frase dicha de dos
   maneras** — el argumento con el que se escribió `Dichos`.
2. La consola es el arnés del dominio. Un dominio que depende de la cultura de la máquina deja de ser
   medible por consola, que es como se valida todo acá.
3. La cultura ambiente en el browser es la **del aparato**, que no es necesariamente el idioma que la
   persona eligió en Gnothi. Formatear con ella es acertar por casualidad.

> **Ojo con el flag que parece gratis.** `InvariantGlobalization` está **prohibido** en las Wasm, y
> no por localización: está MEDIDO que el ctor estático del Parser de Puppeteer pide la cultura
> `en-us` y tira `CultureNotFoundException` si no está. El aviso está en los `.csproj` de Compras,
> Exámenes, Cargas y Facturas —
> [ejemplo](../../Compras/App/Compras.Wasm/Compras.Wasm.csproj)— y **falta en los de Concesionario
> y Municipalidad**. Es justo el flag que uno agrega para bajar el payload.

## 10 · Un solo catálogo para el Host, proyectado por cara

El Host tiene dos superficies de texto y **ninguna comparte catálogo con la otra hoy**:

| superficie | qué es | estado |
|---|---|---|
| [`Gnothi.Mobile/Assets/ui.html`](../../Gnothi.Mobile/Assets/ui.html) | 1 771 líneas. **iOS no tiene copia**: [`Gnothi.iOS.csproj:148`](../../Gnothi.iOS/Gnothi.iOS.csproj) enlaza el mismo archivo | en español |
| [`Gnothi.Desktop/Components/`](../../Gnothi.Desktop/Components/ModuleList.razor) | 726 líneas en cinco `.razor` | **en inglés** |

O sea que **el Host ya se contradice consigo mismo según la cara**. Localizarlo no es agregar
idiomas: es dejar de tener dos.

La forma: `data-i18n` más **un JSON por idioma**, y que Blazor lea **ese mismo JSON** en vez de un
`.resx`. Un catálogo, proyectado por cara. Es el mismo argumento del `ToolIndex` —índice compartido,
proyección por puerta—, y dos catálogos divergen: eso ya pasó, y es la tabla de arriba.

Una sola localización de `ui.html` sirve a los dos teléfonos.

## 11 · La Wasm sigue a la persona, y el bundle embarca los dos idiomas

**Decidido:** la carátula Wasm se muestra en el idioma de la persona, no en el de la institución.

De ahí salen dos consecuencias que hay que respetar al construir una carátula nueva:

- **El módulo embarca `es` y `en` en su bundle.** No tiene red (`capabilities.red: false`), así que el
  Host no le puede conseguir la traducción. Es material que la institución publica, como el logo.
- **`<html lang="es">` no se hardcodea.** Hoy está fijo en `ui.html` y en los cuatro bundles de
  `Gnothi.Mobile/bundles/*/web/index.html`, y el Desktop dice `lang="en"`.

> **Lo que todavía no existe:** no hay ningún canal que le diga a la carátula qué idioma quiere la
> persona. Ver [lo que falta construir](#lo-que-falta-construir).

## 12 · `about.locale` es el idioma del registro, no el de la prosa

Es el error de lectura más fácil de cometer, porque el campo se llama `locale` y parece decir más de
lo que dice.

[`about.locale`](../../Gnothi.Host/Manifest.cs) declara **en qué idioma está escrito el journal**,
y el Host lo usa para **una sola cosa**: pegarle una frase a los parámetros de texto libre
([`ToolIndex.cs:146`](../../Gnothi.Host/ToolIndex.cs)).

> *This module's record is written in es-CR; pass free-text values in that language.*

Eso resuelve el caso medido el 2026-09-11: el modelo manda `"milk"` contra un journal que dice
`"leche"`, el módulo hace `IndexOf` y no encuentra nada. **No dice nada sobre el idioma del título, de
las descripciones ni de las frases.** Eso hoy sólo está escrito como comentario en prosa, bajo la
llave `"//idioma"` de [`Concesionario/manifiesto.json`](../../Concesionario/manifiesto.json), y el
Host no lo lee.

> **Y hay un caso que `about.locale` no cubre.** Ese hallazgo fue con el modelo en el medio. Las
> carátulas de Compras, Exámenes, Cargas y Concesionario tienen **búsqueda de texto libre propia**: si
> la Wasm pasa a inglés y el matching no se toca, se reproduce el mismo fallo **sin** modelo que lo
> arregle. Quien localice una carátula con buscador tiene que mirar el matching en la misma pasada.

## 13 · Al journal no le entra texto renderizado

Corolario de la 3 y de la 5, y se paga tarde: si una frase ya renderizada se persiste, cambiar el
idioma **reescribe la historia**.

Claves y valores adentro; texto renderizado afuera.

---

## Lo que falta construir

**Un canal que le diga a la carátula el idioma de la persona.** No existe. Hay dos formas y hay que
elegir una antes de que alguien improvise:

| | qué cuesta | qué implica |
|---|---|---|
| **El idioma es el del aparato** | **cero**: `navigator.language` ya funciona en todos los WebViews | sin contrato nuevo entre Host y módulo |
| **Un selector adentro de Gnothi** | el Host se lo pasa al módulo | el lugar ya existe: [`BundleServer.cs:141`](../../Gnothi.Movil/BundleServer.cs) ya reescribe el documento para inyectar el watchdog después de `<head>`, y el CSP se calcula sobre el documento ya modificado |

**Recomendación: arrancar por el aparato**, y agregar el selector sólo si alguien lo pide.

**Un campo que declare el idioma de la prosa** (`about.speaks`), si alguna vez hace falta que el
modelo lo sepa. Con default a `locale` cuando falta — la regla del propio manifiesto: los grupos son
opcionales *"para que el Host los estrene sin huerfanar a nadie"*.

---

## Checklist para una carátula nueva

1. ¿Cada texto que pusiste cae en una de las **cuatro categorías** de la regla 2? Si no sabés en cuál,
   ése es el que va a estar mal.
2. ¿El buscador de texto libre matchea contra el journal? Entonces **no** traduzcas lo que se compara
   (regla 12).
3. ¿Hay `CultureInfo.Current*` en `Dominio/`? Sacalo (regla 9).
4. ¿El `.csproj` tiene el aviso de `InvariantGlobalization`? Copialo del de Compras.
5. ¿El bundle embarca los dos idiomas (regla 11)?
6. ¿Alguna clave del catálogo es media frase? Unila (regla 7).

---

## Qué está decidido y qué está medido

| | |
|---|---|
| **El alcance `es`/`en`** | **Decidido** por Renán, 2026-09-14 |
| **La Wasm sigue a la persona** | **Decidido** por Renán, 2026-09-14 |
| **El journal y la voz de la institución no se traducen** | **Decidido**, y confirmado por Kenneth en la misma conversación |
| **Reglas 1, 4, 12** | **Leídas del código que corre**: `Manifest.cs`, `ToolIndex.cs`, y la suite de `Gnothi.Assistant.Tests` que fija el texto exacto del hint |
| **Regla 6** | **Leída del código**: `DueState` ya parte código de dicho. Lo que **no** está hecho es generalizarlo |
| **Regla 5** | **Propuesta, no construida.** La forma clave-más-valores no existe en ningún dominio todavía |
| **Regla 9, el flag** | **MEDIDO** en Exámenes: `CultureNotFoundException` en el ctor del Parser |
| **Regla 8** | **MEDIDO**: `₡172,630` contra `₡32.910` en la misma app |
| **Regla 10, las dos superficies** | **Contadas** el 2026-09-14: 1 771 líneas y 726 líneas, y el enlace de iOS leído del `.csproj` |
| **El volumen de frases por dominio** | **Contado** el 2026-09-14 con un grep de métodos que componen frase. Compras y Exámenes dan 0 y eso es un **falso 0**: hay que inventariarlas |

Cuando esta guía dice algo que no verificó, lo dice en el lugar donde lo dice.

---

## El orden de trabajo acordado

> **Al 2026-09-18 los cuatro pasos estan hechos o casi**: el Host, Municipalidad, Cargas, Compras y
> Concesionario estan localizados y **solo falta Examenes**. Las dos cosas que esta guia lista como
> «lo que falta construir» ya existen, y la regla 5 dejo de ser una propuesta: hay cuatro
> implementaciones corriendo. Que forma quedo adoptada, que falta y que trampas ya se pagaron esta en
> [`localizar-una-caratula.md`](localizar-una-caratula.md).


1. **El Host primero** — `ui.html` y Desktop sobre un JSON compartido (regla 10). No depende de nadie
   y de paso cierra la contradicción que ya existe.
2. **Acordar la forma clave-más-valores** de la regla 5 en Municipalidad, que es donde Kenneth ya está
   trabajando: el dominio más chico y el que más frases compone es el mejor lugar donde equivocarse.
   **Esa forma es la que copian los otros cuatro**, como se copia la carpeta de un negocio.
3. **Un solo negocio de prueba** con esa forma — Cargas, que tiene 3 frases — antes de tocar
   Concesionario y Exámenes.
4. **Compras y Exámenes al final**, porque hay que inventariar sus frases primero.

> Municipalidad la está haciendo Kenneth y **no entra en el trabajo de los demás**.
