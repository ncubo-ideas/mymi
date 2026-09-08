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
warning: malo: brand.shortName tiene 49 caracteres y el tope es 28
warning: malo: brand.logo es '.svg' y solo se aceptan png, jpg y webp -- un svg ejecuta
warning: malo: about.category 'farmacia' no esta en la lista que el Host conoce
```

El ejemplo de este directorio no produce ninguno.

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

**Idempotente:** correrlo dos veces seguidas no produce un segundo diff. Bueno para un hook de
pre-commit o un paso de CI con `--check`.

Deja a `Gnothi.Assistant.Tests/GuardianDeManifiestos` como la red: esto mantiene sincronizado,
el guardián avisa si alguien igual editó a mano y se desvió.
