# Herramientas

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
