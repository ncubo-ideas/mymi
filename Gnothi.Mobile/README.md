# Gnothi.Mobile — el Host en un teléfono

**Lo que prueba:** que el núcleo del Host **no depende del escritorio**. Los mismos archivos
que corren en Windows corren dentro de una app de Android, descubren módulos, los cargan por
reflexión y los responden. Medido en un **Galaxy A54 físico** (SM-A546E, Android 16 / SDK 36,
arm64-v8a): **3 módulos, 8 consultas, 0 procesos hijos**.

Esto no es "una app que llama a un servidor". No hay servidor. El laboratorio, el
supermercado y el emisor de facturas están **adentro del teléfono**, cada uno leyendo su
propia carpeta.

---

## El código es el mismo, y eso es el punto

El `.csproj` no referencia a `Gnothi.Host`: lo **compila adentro**, archivo por archivo, con
`<Compile Include="..\Gnothi.Host\*.cs" />`. No es un atajo, son dos razones:

1. `Gnothi.Host` es un `Exe`, no una librería.
2. Las cáscaras siguen siendo `internal`, y la app necesita nombrar `StdioShell`.

**Lo que NO entra, y ahí está la prueba:** `Program.cs`, `ToolBridge.cs`, `HostTools.cs` y
`ConsentWatcher.cs`. Todo eso es la **Puerta 1**, que habla MCP. Si el núcleo compila sin el
SDK de MCP en el árbol, el núcleo es agnóstico del transporte — y compila.

`StdioShellStub.cs` es lo único propio de acá que suple algo del Host: un `StdioShell` que
tira `PlatformNotSupportedException`. **No es un atajo del spike, es el sistema operativo:**
una app con `targetSdk >= 29` no puede `exec()` archivos de su directorio de datos —es una
violación de W^X— y no hay CLI de dotnet en el aparato. En un teléfono la cáscara **tiene**
que ser en proceso.

---

## Correrlo desde cero

Escrito para alguien que **no tiene nada** de Android instalado. Verificado de punta a punta
el 2026-08-26 sobre un Galaxy A54, borrando los datos de la app antes de empezar.

### 1 · Lo que hace falta, y lo que NO

**No hace falta MAUI.** Este proyecto **no es MAUI**: es `net9.0-android` pelado, con
`Activity` y `LinearLayout` directos. Si ya tenés MAUI instalado no molesta, pero no es lo
que se necesita.

| | Qué | Cómo |
|---|---|---|
| 1 | El SDK de .NET | ya lo tenés si compilás el repo |
| 2 | El **workload de Android** | `dotnet workload install android` |
| 3 | Un **JDK 17** y el **SDK de Android** | el comando de abajo los baja solos |

Verificá lo que tenés:

```bash
dotnet workload list
```

Si en la lista aparece `android`, el paso 2 ya está. Lo que corrió esto en agosto de 2026 fue
.NET SDK `10.0.203`, workload `android 36.1.43`, OpenJDK `17.0.18` y Android SDK con
`platform-tools` y `build-tools 36.0.0`.

### 2 · Bajar el JDK y el SDK de Android sin instalar Visual Studio

El propio workload los descarga. Un solo comando, desde `Gnothi.Mobile/`:

```bash
dotnet build -t:InstallAndroidDependencies -f net9.0-android -p:AcceptAndroidSDKLicenses=True
```

Acepta las licencias de Google **en tu nombre** —por eso la bandera es explícita— y deja todo
en las rutas por omisión. Si preferís elegirlas, agregá `-p:AndroidSdkDirectory=...` y
`-p:JavaSdkDirectory=...`.

Si ya tenés Visual Studio con *"Desarrollo de aplicaciones móviles con .NET"*, ya está todo y
podés saltear este paso.

**`ANDROID_HOME` no hace falta**: el workload encuentra el SDK solo. Está medido — el build
funciona con la variable sin definir.

### 3 · El teléfono

Depuración USB activada (Ajustes → Acerca del teléfono → tocá *Número de compilación* siete
veces → Opciones de desarrollador → Depuración por USB). Conectalo y aceptá el diálogo de
autorización que aparece en la pantalla.

`adb` viene con el SDK, en `platform-tools`, y **normalmente no queda en el PATH**. En
Windows suele estar en:

```
C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
```

Comprobá que el teléfono se ve:

```bash
adb devices
```

### 4 · Instalar la app

```bash
dotnet build -t:Install -c Debug
```

### 5 · Armar y empujar los módulos

**El APK viaja vacío, y esto no es opcional.** Sin módulos vas a ver la app en blanco.

```bash
pwsh ./armar-paquete.ps1
```

El script publica los tres módulos del POC con todas sus dependencias, les escribe un
`installation.json` con rutas del teléfono, y les copia el journal desde `C:\Gnothi`. Si tus
journals están en otro lado: `pwsh ./armar-paquete.ps1 -Journals D:\loquesea`.

Salida esperada: **183 archivos, 19,7 MB**.

```bash
adb push paquete/Modulos /sdcard/Android/data/com.ncubo.gnothi.host/files/incoming/Modulos
```

```bash
adb push paquete/journals /sdcard/Android/data/com.ncubo.gnothi.host/files/incoming/journals
```

### 6 · Abrirla

Abrila desde el cajón de aplicaciones, o:

```bash
adb shell monkey -p com.ncubo.gnothi.host -c android.intent.category.LAUNCHER 1
```

**Lo que tenés que ver**, y es lo que salió en la corrida verificada:

```
Gnothi
The modules you installed on this phone
3 modules installed  -  183 files installed on this launch
```

Una caja **ASK** arriba, y tres tarjetas con icono —🩺 exámenes, 🧾 facturas, 🛒 compras— con
ocho botones entre todas. Tocá cualquiera: devuelve JSON leído del journal **del teléfono**.

`ASK` es la Puerta 2 y **se va a frenar en `no model key`**. Es correcto, no está roto: ver la
sección *"Lo que todavía no"* más abajo.

### Por qué dos pasos, y por qué `incoming/`

`adb` **no puede escribir** en el área privada de la app, y está bien así: si pudiera,
cualquiera de afuera instalaría un módulo. `incoming/` es el área de descarga —la que un
navegador o una tienda pueden tocar— y **el Host es el que instala desde ahí**. Ese ciclo de
vida es autoridad del Host, no del sistema operativo.

Es la misma forma en la que un módulo va a llegar en el producto real: alguien lo descarga, y
el Host lo adopta.

---

## Si algo sale mal

Todo esto lo pisamos nosotros, no son hipótesis.

**`error XA0010: No available device`.** El teléfono se desconectó o el demonio de adb se
reinició. `adb devices` para confirmar; suele volver solo.

**`adb: error: cannot stat '/c/Users/...'`.** Estás en Git Bash y adb no entiende rutas de
MSYS. Usá rutas de Windows: `C:/Users/...`.

**El publish falla con `MSB3021` / `MSB3027`, "being used by another process".** No es el
código: si tenés Claude Code o Claude Desktop abiertos, ellos ya levantaron los servidores MCP
de `.mcp.json` y esos procesos **tienen tomados los dll**. Cerralos, o:

```bash
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object { $_.CommandLine -match 'Mcp.dll' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
```

**La app abre y se cierra sola, sin log.** Buscá en `adb logcat` la línea
`No assemblies found ... Exiting`. Pasa si corriste `adb shell pm clear`: en Debug, .NET
Android usa *Fast Deployment* y los assemblies viven fuera del APK, en
`files/.__override__/`, que `pm clear` borra. Reinstalá con `dotnet build -t:Install`. Este
proyecto ya trae `EmbedAssembliesIntoApk` para que no pase.

**`am start` no hace nada.** El nombre de la Activity lleva un hash que **cambia en cada
recompilación** (`crc64a35e2aa9284b3252.MainActivity`). No lo copies: resolvelo cada vez.

```bash
adb shell cmd package resolve-activity --brief com.ncubo.gnothi.host
```

**Ver qué está pasando adentro:**

```bash
adb logcat -s GNOTHI:I
```

---

## Lo que se midió, y lo que costó

**`AssemblyDependencyResolver` no existe en Android.** Tira `PlatformNotSupportedException`
porque depende de `hostpolicy`, que en móvil no está. Le costó una sesión de depuración a
este spike, y el arreglo —resolver por vecindad, cada dependencia viaja en la carpeta del
módulo— vive en `InProcessShell.cs` y sirve en los **dos** hospedajes.

Y una vergüenza útil: **el compilador ya lo sabía**. `CA1416` lo avisa en cada build.

**El bloqueo wasm del motor NO aplica acá.** `Task.WaitAll` → *"Cannot wait on monitors"* es
específico de un solo hilo: pega en el wasm del navegador y en WASI, no en .NET Android
nativo. Medido: `RuntimeInformation.IsBrowser() = False`, 8 hilos reales.

---

## Lo que todavía no

La pantalla es una **sonda**, no la UI: rellena los parámetros con un valor de muestra y
muestra el JSON crudo. Quien va a decidir los argumentos de verdad es el modelo, por la
**Puerta 2** (`Gnothi.Assistant`) — y eso está frenado por una decisión que no es técnica:
en un teléfono no hay un Claude de escritorio que hable MCP, así que hace falta una llave de
modelo, y quién la pone (BYOK o nosotros) está abierto. Ver `NotasGnothiHost/11`.

**Este proyecto no está en `Gnothi.slnx` a propósito:** apunta a `net9.0-android` y rompería
el build de cualquiera que no tenga ese workload instalado.
