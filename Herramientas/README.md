# Herramientas

## `manifiesto-de-ejemplo.json` — lo que se le comparte a una institución

Un manifiesto **real y completo**, con todos los campos comentados, para que una institución que va a
publicar su módulo lo copie y le cambie los valores. Al lado va
`manifiesto-de-ejemplo.view.json`, que es la vista que el campo `view` nombra.

**Todo lo nuevo es opcional.** Un manifiesto que solo traiga `manifest`, `module`, `title`,
`institution`, `substrate`, `capabilities` y `tools` instala igual y funciona igual. Cada grupo que
se agregue muestra una cosa más en la pantalla.

### Qué se ve, y dónde

La institución **declara** y el Host **compone**: ella provee el material, él decide el lugar, el
tamaño y el contraste. En ningún campo hay una medida ni una regla de estilo.

| campo | dónde aparece | tope |
|---|---|---|
| `brand.shortName` | **la línea grande** de la tarjeta, el detalle, el alta y el journal | 28 caracteres |
| `brand.logo` | la caja del ícono en esas mismas cuatro pantallas | png/jpg/webp · 64 KB · 512 px |
| `brand.color` | el tinte de esa caja, y nada más | `#rrggbb` |
| `brand.banner` | la foto detrás de la tarjeta del inicio, con el nombre y el eslogan encima | png/jpg/webp · 200 KB · 1600 px |
| `brand.font` | la tipografía del **nombre de marca** en la tarjeta y el detalle de ese módulo; el resto de la tarjeta va en la letra de Gnothi | woff2/woff/ttf/otf · 160 KB · un archivo con el peso del nombre |
| `title` | la segunda línea, y el botón «Abrir …» | 32 caracteres |
| `institution` | la línea grande **cuando no hay** `brand.shortName` | — |
| `publisher.domain` | la franja de procedencia — **se contrasta** contra quien sirvió el bundle | un dominio, sin `https://` |
| `publisher.legal` · `taxId` | esa misma franja, junto al dominio | 64 caracteres |
| `publisher.version` · `released` | esa misma franja | semver + `aaaa-mm-dd` |
| `about.description` · `doesNotDo` | el detalle y la pantalla del alta | 180 y 120 caracteres |
| `about.category` | de ahí sale el ícono **cuando no hay logo** | lista cerrada |
| `about.currency` · `units` · `locale` | no se ven: los usa el asistente para no inventar unidades | — |
| `journal.retention` · `expectedSize` · `updates` | el bloque del registro en el alta, y el journal | — |
| `view` | la mitad de arriba del detalle del módulo | ver el `.view.json` |

**Lo que nunca se ve, y no es un olvido:** `assistantHint`, `tone`, `persona` y cualquier campo que
sea una *instrucción*. El manifiesto declara **hechos verificables sobre la institución**, nunca
instrucciones para el Host ni para el modelo — eso sería inyección de prompt con formato de contrato.
La regla está escrita en `Gnothi.Host/Manifest.cs`, al lado de los campos.

### Los comentarios van como claves, no como `//` de línea

El lector de C# admite las dos formas (`ReadCommentHandling = Skip`), pero
`sincronizar-manifiesto.mjs` usa `JSON.parse`, que **no** admite comentarios de línea y se rompe. Un
comentario como clave (`"//substrate": "..."`) es JSON legítimo y sobrevive a las dos herramientas.
Ese es el motivo real de la convención, y conviene no romperla.

### Un tope que se pasa no tumba el módulo: se dice

`Manifest.Problems()` los enumera y `ModuleCatalog.Load()` los saca como warnings — el mismo verbo
que `Capabilities.Excesses()`. Dejar a la persona sin su laboratorio porque a alguien se le fue un
renglón sería peor que el renglón.

Para verlo, con un manifiesto que se pase de todo a propósito:

```bash
dotnet Gnothi.Host/bin/Debug/net9.0/Gnothi.Host.dll <carpeta-de-modulos> < /dev/null
```

```
warning: malo: brand.shortName is 49 characters and the cap is 28
warning: malo: brand.logo is '.svg' and only png, jpg and webp are accepted -- an svg executes
warning: malo: about.category 'farmacia' is not in the list the Host knows, so it falls to the generic icon
```

El ejemplo de este directorio no produce ninguno.

> **Estos avisos van en inglés a propósito**, y no entran en el catálogo de textos: hablan de
> `brand.shortName` y de `about.category`, así que son diagnóstico para quien **empaqueta** el
> módulo, no copy para quien lo usa. Estaban en español dentro de una lista cuyo resto era inglés.
> Ver [las reglas de localización](../Metodologia/Guias/reglas-de-localizacion.md).

## `sincronizar-manifiesto.mjs` — el manifiesto, proyectado desde el módulo

El problema que resuelve: `Modulos/<x>/manifest.json` es lo que la **Puerta 2** lee para armar el
catálogo que le pasa al modelo, y es una **copia** del módulo que se edita a mano. Se
desincroniza en silencio — un fantasma que el modelo llama y falla, una tool nueva que nunca le
llega. El guardián de la suite lo **ataja**; esto lo **evita**.

```bash
node Herramientas/sincronizar-manifiesto.mjs            # todos los módulos
node Herramientas/sincronizar-manifiesto.mjs examenes   # uno
node Herramientas/sincronizar-manifiesto.mjs --check    # no escribe; sale 1 si algo cambiaría
```

Arranca cada módulo por MCP, le pide su `tools/list`, y reescribe el bloque `tools` del
manifiesto con lo que el módulo expone de verdad.

**Sincroniza (viene del módulo):** el conjunto de tools (agrega nuevas, quita fantasmas), y por
cada parámetro su `type` y su `enum` — que el `inputSchema` del módulo ya trae, así que los enums
no se declaran a mano.

**Preserva (contenido curado):** todo el encabezado y los comentarios `//`, la `description` y el
`title` de cada tool que ya existía, y la `description` de cada parámetro. Para una tool **nueva**
toma la descripción del código como punto de partida — es el único lugar donde el idioma puede
entrar mezclado, y `--check` lo hace visible antes de mergear.

**Requisitos:** el DLL del módulo compilado (si su `.cs` es más nuevo, recompilar primero) y
`node`. No lee el journal, solo enumera tools, así que corre con una carpeta de datos vacía.
Dónde está el DLL lo lee de `Modulos/<x>/installation.json` —`shell.arguments[0]`, relativo a la
carpeta del módulo si no es absoluto, la misma regla que aplica el Host—, así que no hay una segunda
lista que mantener. Si ese `bin/Debug` está tomado por un proceso —los MCP de una sesión abierta— y
compilaste a otra carpeta, `node Herramientas/sincronizar-manifiesto.mjs compras --dll=<ruta>` usa
ese dll y escribe igual el manifiesto de `Modulos/compras`.

**Idempotente:** correrlo dos veces seguidas no produce un segundo diff. Bueno para un hook de
pre-commit o un paso de CI con `--check`.

Deja al guardián de la suite —`Gnothi.Assistant.Tests/Conformance.cs`, que arranca cada módulo y
cruza su manifiesto contra su `tools/list` real— como la red: esto mantiene sincronizado, el guardián
avisa si alguien igual editó a mano y se desvió.

## `revisar-las-caras.mjs` — que la cara híbrida siga diciendo lo que dice su app

Cada negocio tiene **dos pantallas del mismo hecho**: la app suelta en Blazor (`<X>.Wasm`, que es la
fuente del diseño) y la cara híbrida (`<X>.Cara`, html y js planos que corren dentro de Mymi). Nada
las ata. Se escribieron a mano el 2026-09-14, las apps se localizaron el 16 y el 17, y el 18 **sólo
una de las cinco hablaba inglés adentro de Mymi**. Cuatro días bastaron.

```bash
node Herramientas/revisar-las-caras.mjs              # las cinco
node Herramientas/revisar-las-caras.mjs compras      # una o varias
node Herramientas/revisar-las-caras.mjs --silencio   # sólo los hallazgos
```

Sale con código 1 si encuentra algo, para que sirva de compuerta.

### Qué revisa, y son las tres formas en que se desincronizaron de verdad

| | qué compara | por qué duele |
|---|---|---|
| **la hoja** | que `css/app.css` de la cara sea idéntica a la de su app, y que el html la enlace | ahí viven el color, la tipografía y el layout de la institución; entresacar reglas a mano es decidir cómo se ve ella |
| **las clases** | que toda clase que la cara escribe tenga regla en alguna hoja que viaja | es el modo mudo de verse mal: el bloque sale sin caja, sin color o sin espacio, **sin un solo error en consola** |
| **los catálogos** | que los dos idiomas tengan las mismas claves, que ninguna que la cara pide falte, y que lo compartido con la app diga **exactamente** lo mismo | una cara con su propia redacción es una segunda institución |

Lo de las clases no es teórico: `inicio-atencion-detalle` dejaba la caja de lectura de Delfos pelada,
y `apunte` —que escribe el puente del Host en las **cinco** caras— no estaba en tres hojas, así que
un fallo se veía igual que una nota.

### Qué no puede revisar

**Si la cara lee un campo que la superficie no serializa.** Eso sólo se ve corriendo: `h.stays.stays`,
`h.plate.plate` y `x.cuando` pintaban vacío para siempre. Al tocar una cara, leer el record de
`Vistas/`, no lo que parece razonable.

Tampoco juzga el diseño. Dice si la cara se desvió de su app, no si las dos se ven bien.

## `replicar-seccion-gnothi.mjs` — el paso a paso «Se usa con Gnothi», en cada página

Cada página de institución (`<Negocio>/PaginaDeDescarga/descarga/index.html`) trae la misma
sección `#gnothi`: primero Gnothi (una vez, de la tienda o de la descarga para Windows), después
la institución (un toque), en tres paneles elegidos por el aparato de la persona. La estrenó La
Guaria a mano; las otras cuatro salen de este molde, con la **voz** de cada página (usted, vos,
inglés) y su **paleta** mapeada a variables propias, para que la sección vista la marca de cada una.

```bash
node Herramientas/replicar-seccion-gnothi.mjs
```

Empalma la sección en cada página que **todavía no la tiene** y ajusta el llamado a la acción de
la portada; una página que ya la trae no se toca. Si el flujo cambia —un rótulo del alta, un paso
nuevo—, cambia en el molde, se quita la sección vieja de las páginas y se vuelve a correr; la de
La Guaria se edita a mano y le sirve de referencia visual.

Los tres descargables que la sección ofrece (`descargas/gnothi/Gnothi.Host.zip`,
`descargas/<modulo>.gmod` y `descargas/movil/<modulo>.gmod`) **no van al repo**: se arman con
`Gnothi.Mobile/armar-bundle.ps1` antes de levantar URL1, y el `nginx.conf` de cada página dice
cuál es cuál. La forma hospedada lleva el origen adentro y tiene que ser el que sirve la página.
