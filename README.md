# Gnothi

El universo de datos de una persona, en su propio dispositivo. Cada institución le entrega un
**módulo** con el registro de ella —su laboratorio, su supermercado—, **Gnothi.Host** los junta
en una sola superficie para su IA, y **ella** decide qué se ve.

Un directorio por negocio, nombres en español adentro. La excepción es `Gnothi.Host/`, que está
en inglés: no entiende de dominio, así que tampoco tenía por qué hablar el idioma del negocio.

## Qué hay acá

| Carpeta | Qué es |
|---|---|
| `Gnothi.Host/` | **El Host.** Servidor MCP hacia el asistente y cliente MCP hacia cada módulo. Un proxy que **agrega**: junta las tools de N módulos en una sola lista |
| `Modulos/` | Un directorio por módulo instalado, con su `manifest.json` (lo escribe la **institución**) y su `installation.json` (lo escribe el **Host**) |
| `Examenes/` | El negocio del laboratorio clínico → [Examenes/README.md](Examenes/README.md) |
| `Compras/` | El negocio del supermercado |
| `HolaMundo/` | El ejemplo de referencia (`Facturas`). **No se lista en `Gnothi.slnx`** a propósito: nuestro dominio está basado en él, pero no es parte de él |
| `.githooks/` | El hook de Conventional Commits |
| `.mcp.json` | Un servidor MCP **por negocio**, para que una IA vea varios a la vez |

---

## Setup por clon

Dos líneas, una sola vez, después de clonar. Ninguna de las dos es opcional.

```bash
git config core.hooksPath .githooks
git config core.longpaths true
```

**La primera** activa el hook que verifica Conventional Commits antes de que el commit exista.
Va como hook y no como norma escrita porque una norma que nadie verifica se cumple las primeras
dos semanas. El formato está explicado en `.githooks/commit-msg`.

**La segunda** es por las rutas largas de Windows, y merece su propia sección.

---

## Clonar en Windows

Windows corta las rutas en **260 caracteres**. La ruta más larga de este repo mide **85**, así
que el límite solo muerde cuando la **raíz del clone** es profunda —un `OneDrive - <nombre largo
de la empresa>` con carpetas anidadas se come 150 sin esfuerzo. El checkout falla con
`Filename too long`, normalmente señalando el `wwwroot/lib/` de un Blazor WASM, que es donde
viven las rutas más largas.

**La forma barata de que no pase: cloná en una raíz corta.** `C:\src\Gnothi` deja 240
caracteres de margen y el problema no existe.

### Si ya falló

`core.longpaths` sirve igual después del fallo: el clone quedó a medias, no roto.

```bash
git -C Gnothi config core.longpaths true
git -C Gnothi reset --hard
```

Y para que no falle de entrada, se puede pasar en el propio clone:

```bash
git clone -c core.longpaths=true https://ncubo.visualstudio.com/Gnothi/_git/Gnothi
```

### Lo que `core.longpaths` NO arregla

Ese flag le habla **a git**. No le habla a MSBuild, ni a Visual Studio, ni al SDK de .NET, que
siguen atados a 260. En un Blazor WASM eso importa, porque el build genera rutas bastante más
profundas que las del repo:

```
obj/Debug/net9.0/compressed/<hash>/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css.br
```

O sea que con solo `longpaths` el fallo se muda del checkout al build. Lo único que destraba
también las herramientas es habilitarlo en Windows, **como administrador y por máquina**:

```bash
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v LongPathsEnabled /t REG_DWORD /d 1 /f
```

### Y por eso no versionamos librerías enteras

`index.html` de cada WASM referencia **un** archivo de Bootstrap:

```html
<link rel="stylesheet" href="lib/bootstrap/dist/css/bootstrap.min.css" />
```

Así que es el único que se versiona. Traer el `dist/` completo son 44 archivos y 8.4 MB de
output de build de un tercero —variantes RTL, source maps, `bootstrap-grid`,
`bootstrap-reboot`, `bootstrap-utilities`, el bundle de JS— que nadie referencia, y son además
las rutas más largas del repo. Si hace falta otro archivo de la librería, se agrega **ese**.
