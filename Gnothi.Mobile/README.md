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

## Correrlo

Hace falta el workload de Android (`dotnet workload install android`) y un teléfono con
depuración USB. **Son dos pasos, y el segundo no es opcional:** el APK viaja vacío, sin
módulos.

```bash
dotnet build -t:Install -c Debug
```

```bash
adb push paquete/Modulos  /sdcard/Android/data/com.ncubo.gnothi.host/files/incoming/Modulos
```

Después relanzá la app. El primer arranque copia lo que encuentre en `incoming/` al área
privada y **ahí recién** descubre los módulos.

### Por qué dos pasos, y por qué `incoming/`

`adb` **no puede escribir** en el área privada de la app, y está bien así: si pudiera,
cualquiera de afuera instalaría un módulo. `incoming/` es el área de descarga —la que un
navegador o una tienda pueden tocar— y **el Host es el que instala desde ahí**. Ese ciclo de
vida es autoridad del Host, no del sistema operativo.

Es la misma forma en la que un módulo va a llegar en el producto real: alguien lo descarga,
y el Host lo adopta.

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
