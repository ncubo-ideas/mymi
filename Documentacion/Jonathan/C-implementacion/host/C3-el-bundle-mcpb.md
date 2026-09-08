# C3 · El Host empaquetado como extensión instalable

El `.mcpb` es el *«1 clic de instalación»* que las láminas dibujan. Acá está hecho y probado — y también
**descartado**: el botón de instalar nunca se habilitó. Quedó como un **intento fallido** que igual sirve,
porque es la prueba de que la costura de instalación es barata. El instalador **real** terminó siendo un
**MSI** (WiX v5, per-usuario). Esta página es la historia honesta del `.mcpb`; el MSI, la marca de
descubrimiento y las caras de escritorio viven en `C7`.

---

## Qué es un `.mcpb`

Un **zip con un `manifest.json` en la raíz**. Nada más exótico que eso.

```
gnothi.mcpb  (30 MB)
├── manifest.json
└── server/Gnothi.Host.exe     self-contained, 70 MB
```

El esquema, de la especificación del formato:

| campo | qué es |
|---|---|
| `manifest_version` | versión de la especificación |
| `name`, `version`, `description` | identidad |
| `author.name` | requerido |
| `server.type` | `node` \| `python` \| **`binary`** |
| `server.mcp_config` | `command`, `args`, `env` |
| `user_config` | lo que la aplicación anfitriona le pregunta a la persona |
| `compatibility` | plataformas, versión mínima del anfitrión |

Variables usables en `args` y `env`: `${__dirname}`, `${HOME}`, `${DESKTOP}`, `${DOCUMENTS}`,
`${pathSeparator}`, y **`${user_config.CLAVE}`**.

## Las tres decisiones del bundle

### Self-contained

El bundle no depende de que la máquina tenga .NET. Es lo que la especificación muestra como ejemplo del
tipo `binary`: *«un ejecutable autocontenido sin requisitos de runtime»*.

Y hay una razón medida: la aplicación anfitriona reporta **«Node.js: No encontrado (integrada 24.18.1)»** y
**«Python: No encontrado»**. Trae su propio Node para las extensiones de tipo `node`, pero para .NET no hay
nada integrado — un bundle que dependiera del `dotnet` del sistema sería frágil.

### Sin trimming, a propósito

El SDK de MCP descubre las consultas por **reflexión**, y el trimmer las poda **en silencio** — es la
restricción R7 de la especificación. Pesa más y arranca seguro.

Es exactamente el mismo motivo por el que el descriptor de consultas debería salir de un source generator
en vez de reflection. Ver `B2`.

### El `.mcpb` es el instalador del HOST, no de los módulos

Adentro no va ningún módulo. El manifiesto declara un `user_config` de tipo `directory` y la aplicación le
pregunta a la persona dónde están los suyos:

```json
"user_config": {
  "directorio_de_modulos": {
    "type": "directory",
    "title": "Carpeta de modulos instalados",
    "description": "Cada subcarpeta con manifiesto.json e instalacion.json es un modulo.",
    "required": true
  }
}
```

**Eso no es comodidad de empaquetado, es el modelo:** *«Instalador Gnothi, se instala UNA vez»*, y los
módulos se bajan aparte, de la web de cada institución.

Y el eco que vale notar: **`user_config` es el mismo corte que manifiesto/instalación.** El bundle declara
*qué* necesita; el anfitrión decide *con qué valor* y se lo pasa al lanzar. Ver `B4`.

---

## Cómo se armó, y los dos tropiezos

**No hacía falta Node.** El empaquetador oficial existe —`npm install -g @anthropic-ai/mcpb`, después
`mcpb pack`— pero como un `.mcpb` es un zip, se armó a mano.

### Tropiezo 1 · Las entradas del zip con backslash

`Compress-Archive` de Windows PowerShell 5.1 escribe `server\Gnothi.Host.exe`. **El formato zip pide barra
normal**, y un lector estricto no encuentra el archivo.

Hubo que armarlo entrada por entrada con `/`. Si alguien lo regenera y falla misteriosamente, es eso.

### Tropiezo 2 · El botón de instalar bloqueado

El manifiesto validó —la aplicación mostró **«todos los requisitos cumplidos»**, con autor, versión y
licencia leídos— pero el botón estaba muerto.

Existe en la aplicación la cadena *«para instalar esta extensión, actualizá a la última versión»*: hay un
**gate por versión**. Y durante la sesión la aplicación se actualizó sola, de una build a otra.

**No quedó confirmado que fuera eso.** Se exploró la hipótesis de que el tipo `binary` no estuviera
soportado y **la evidencia no la sostuvo** — los hits de `"binary"` en los recursos eran falsos positivos
(encodings de buffer, gramáticas de resaltado). Queda como hipótesis viva, no como conclusión.

**Actualización · el camino se descarta.** Se reintentó en otra sesión, con un bundle regenerado —
esta vez por el propio ejecutable, vía `--publicar-mcpb`— y **el botón sigue sin habilitarse**. Dos
intentos, dos builds distintos de la aplicación, mismo resultado. Deja de ser un tropiezo a
investigar: es un camino cerrado, y no vale gastarle más tiempo hasta que la aplicación cambie.

### Y el camino que sí funcionó

Registrar el Host **como servidor MCP local** en la configuración de la aplicación, sin bundle. Es
igual de válido para probar el escenario: **el `.mcpb` es empaquetado, no arquitectura.** El detalle de
rutas está en `D1`.

Con una mejora sobre la primera vez: ahora se le apunta al **ejecutable autocontenido** en vez de a
`dotnet <dll>`. Los dos funcionan, pero el primero no necesita el SDK instalado — que es la situación
de la persona real, no la de una máquina de developer.

```json
"gnothi": {
  "command": "C:\\Users\\<usuario>\\Downloads\\Gnothi.Host.exe",
  "args": ["...\\GnothiHolaMundo\\Modulos"]
}
```

⚠️ **La ruta del ejecutable no puede tener espacios**, porque la aplicación lanza el comando por
`cmd.exe`. Ver `F3`.

### Lo que este tropiezo demuestra, y es lo mejor de todo

**El `.mcpb` se descartó sin perder nada.** Cambiar el mecanismo de registro no tocó una línea del
despacho, del catálogo ni del módulo.

Eso es la costura de `01` cobrándose: *«¿quién le habla al Host desde afuera? ahí vive la capa
borrable»*. Un tropiezo en esa capa tiene que costar barato — y costó una entrada de JSON.

**Y lo volvió a probar, más fuerte.** El reemplazo del `.mcpb` no fue otro bundle: fue un **MSI** (WiX v5,
per-usuario), un mecanismo de instalación completamente distinto — y la capa borrable lo absorbió igual.
Dos cambios de mecanismo, cero líneas de despacho, catálogo o módulo tocadas. La costura de `01` no se
probó una vez: se probó dos. El MSI, con su marca de descubrimiento y sus caras de escritorio, está en
`C7`.

---

## Verificado

Extrayendo el bundle a un directorio limpio y corriéndolo como lo haría la aplicación:

```
extraído:
  .\manifest.json
  .\server\Gnothi.Host.exe

command 'server/Gnothi.Host' + .exe  ->  existe: True

initialize -> gnothi v0.1.0
tools/list -> 3 tools
tools/call -> total=27991.5  facturas=4  evento=104
```

## Si hay que rearmarlo

```bash
dotnet publish <ruta>/Gnothi.Host.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o <ruta>/mcpb/server
```

Después borrar el `.pdb`, y zipear el contenido de `mcpb/` con **`manifest.json` en la raíz** y las
entradas con **barra normal**.

## Un detalle pendiente

**No tiene ícono.** El bundle no trae `icon.png`, así que en la lista se ve un placeholder. Es agregar un
PNG y rearmarlo.

## Y una observación que vale para el pitch

Al instalar, la aplicación avisa en rojo:

> *«La instalación otorgará a esta extensión **acceso a todo lo que hay en tu computadora**.»*

Gnothi vende confinamiento —el módulo no tiene red, no sale de su carpeta, no tiene capacidades— y para
instalarlo hay que darle **todo**.

No es contradicción, es el diseño: **el Host es la pieza en la que se confía una vez**, y por eso tiene que
ser auditable, open source y con build reproducible. Pero verlo escrito por el propio sistema operativo, en
rojo, es la mejor ilustración del argumento que se va a conseguir. Ver `A1`.
