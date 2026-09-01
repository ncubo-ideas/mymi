# Gnothi.iOS — el Host en un iPhone

El espejo de `Gnothi.Mobile`. Misma app, mismo Host, misma cara — la otra plataforma.

## Qué se comparte, contado

| | | |
|---|---|---|
| `Gnothi.Host/*.cs` | el núcleo | idéntico |
| `Gnothi.Assistant/*.cs` | la Puerta 2 | idéntico |
| `Gnothi.Movil/*.cs` | la cáscara | idéntico |
| `Assets/ui.html` | la cara | **el mismo archivo**, no una copia |
| `MainActivity.cs` / `ViewController.cs` | el WebView | ~120 líneas cada uno |

Los dos `.csproj` traen **el mismo glob con las mismas exclusiones**. Si dejan de coincidir,
las dos plataformas dejan de correr el mismo Host — que es justamente lo que la suite de
conformance existe para impedir.

### Por qué no MAUI

La cara de Gnothi **ya es un HTML**, así que la UI ya está compartida. MAUI compartiría su
propio framework de UI —que acá no se usa— y **no** compartiría el puente del WebView, que es
API nativa de cada plataforma igual. Costo alto por compartir lo que ya estaba compartido.

La decisión original está escrita en `Gnothi.Mobile/MainActivity.cs`, de cuando se evaluó
Blazor para Android.

## Siri no ve los tools de los módulos, y no es algo que falte configurar

Si viniste buscando por qué el asistente del sistema no encuentra `revisar_mis_analitos` ni
ningún otro tool de un módulo: **está medido y está cerrado.** El detalle, con la evidencia,
está en [`Spikes/Lab.Siri/HALLAZGOS.md`](../Spikes/Lab.Siri/HALLAZGOS.md), hallazgo 7.

En corto. Las App Intents se extraen **en tiempo de compilación** a
`Metadata.appintents/extract.actionsdata`, que viaja dentro del bundle **firmado**. Los tools de
un módulo llegan por `Documents/incoming` **después** de instalar, cuando el bundle ya está
sellado. Es el espejo de lo que bloqueó en Android: allá la curaduría de Gemini, acá la firma de
código. Y hay un tope duro de 10 AppShortcut por app.

Lo que sí se puede, y el spike ya lo tiene andando: **una intent genérica compilada acá**, cuyo
`perform()` haga `ToolIndex.Of(dispatcher)` y despache — la forma es fija, el ruteo se resuelve
en runtime, y un módulo que llegue después queda alcanzable sin recompilar. Más un `AppEntity`
por negocio para el índice de Spotlight.

Lo que **no** hay que prometer en una demo: «cada tool de cada módulo aparece como una acción de
Siri».

## PARA CORRERLO

### 1 · Lo que hace falta antes

```
Mac con Xcode            (el mismo del spike de Siri)
.NET 9 SDK               https://dotnet.microsoft.com/download/dotnet/9.0
workload de iOS          dotnet workload install ios
acceso al feed privado   <- ESTE es el que sorprende, ver abajo
```

**El feed privado es el bloqueo real.** El proyecto referencia `Ncubo.Puppeteer`, que no está
en nuget.org: vive en Azure Artifacts de la organización. Sin el credential provider, el
`restore` falla con `NU1101: Unable to find package Ncubo.Puppeteer` — y **ese error no dice
que falta autenticación**, así que se pierde media hora buscándolo en el `.csproj`.

El instalador oficial de Microsoft para macOS:

```bash
sh -c "$(curl -fsSL https://aka.ms/install-artifacts-credprovider.sh)"
```

Instala en `~/.nuget/plugins`. La primera vez que corras `dotnet restore` te va a pedir que
abras una URL y pegues un código de dispositivo — con la cuenta de ncubo. Si preferís ver el
script antes de correrlo, bajalo con `curl -O` y leelo; es el mismo que documenta Microsoft.

### 2 · El paquete — te lo tiene que pasar Edgar

`Gnothi.Mobile/paquete/` **no está en git**: es salida de build, y además los journals viven
en la máquina de él. Te va a mandar un zip. Descomprimilo ahí:

```
Gnothi/
  Gnothi.Mobile/
    paquete/          <- acá
      Modulos/
      journals/
```

Si está cuando compilás, viaja adentro de la app y se siembra solo. Si no, la app arranca
igual pero **sin módulos**, y la sonda no puede correr los pasos 1-5 — que son el punto.

> **Ojo con las rutas.** El `installation.json` del paquete trae rutas absolutas de Android
> (`/data/user/0/...`). No las corrijas a mano: la app las reescribe al arrancar. El
> contenedor de iOS lleva un GUID que cambia en cada instalación, así que **no existe** una
> ruta que se pueda dejar escrita de antemano. En el log vas a ver `localizar: facturas -> ...`.

### 3 · Compilar y correr

```bash
cd Gnothi/Gnothi.iOS && dotnet build -t:Run -f net9.0-ios
```

Para elegir simulador o aparato:

```bash
dotnet build -t:Run -f net9.0-ios -p:RuntimeIdentifier=ios-arm64
```

### 4 · Los logs — acá está el resultado

El equivalente de `adb logcat -s GNOTHI`:

```bash
log stream --predicate 'processImagePath CONTAINS "Gnothi"' --style compact
```

**Todo lo que importa sale por ahí, con el prefijo `GNOTHI:`.** La pantalla es secundaria.

### 5 · Qué reportar

Lo de la sección «la sonda de AOT», abajo — **las dos banderas y los cinco pasos, textuales**.
Con eso alcanza; no hace falta interpretarlo.

Y aparte, dos sí/no:

- ¿pintó la pantalla, o salió en blanco? (si salió en blanco, el log dice `NO se encontro ...`)
- ¿aparecen los módulos con sus switches, como en Android?

## Cómo entran los journals y los módulos

Dos caminos, y el segundo es mejor que cualquier cosa que tenga Android:

1. **Desde el bundle.** Si `Gnothi.Mobile/paquete/` existe cuando se compila, viaja adentro de
   la app y se siembra en `Documents/incoming` la primera vez.
2. **Desde la app Archivos.** `Info.plist` declara `UIFileSharingEnabled` y
   `LSSupportsOpeningDocumentsInPlace`, así que la carpeta de la app **aparece en Archivos** y
   la persona deja el paquete ella misma. Sin cable, sin `adb`, sin un desarrollador al lado.

En Android el paquete se empuja por `adb` porque no hay otra forma, y eso siempre fue una
muleta de laboratorio. Acá no hace falta.

## La sonda de AOT — leer esto ANTES de sacar conclusiones

`Sonda.cs` corre al arrancar y escribe en el log. Contesta la pregunta que la nota 31 dejó
abierta: **si un módulo que llega después de instalar se puede cargar con el AOT obligatorio.**

Lo primero que imprime son las dos banderas del runtime:

```
IsDynamicCodeSupported = ?     se PUEDE generar codigo en runtime
IsDynamicCodeCompiled  = ?     ese codigo va a nativo (JIT) o se INTERPRETA
```

**Lo que se espera en un iPhone: `Supported=true` / `Compiled=false`.** Eso *es* el intérprete
de Mono: genera código sin compilarlo a nativo, que es lo que Apple prohíbe. Si `Supported`
sale `false`, el intérprete no está y no hay nada que cargar — revisar `MtouchInterpreter`.

Después corre los cinco pasos, **los mismos que se midieron en el A54** para que sean
comparables:

```
1) AssemblyLoadContext(isCollectible: true)
2) AssemblyDependencyResolver        <- en Android FALLA (PlatformNotSupported)
3) LoadFromAssemblyPath
4) GetTypes
5) reflexion sobre la superficie     <- este Android nunca tuvo que probarlo
```

El 5 es el que decide en iOS. Cargar los tipos no es ejecutarlos: el IL de ese ensamblado
**no** está compilado AOT, porque no existía cuando se compiló la app.

Si algo tira `ExecutionEngineException: Attempting to JIT compile method while running in
aot-only mode`, **el intérprete no está puesto.**

## AOT: lo que ya está medido y lo que falta

`Spikes/Lab.Aot` corre los analizadores desde Windows. Línea base al 2026-09-01:

```
37 warnings IL
  28  JsonSerializer     arreglo mecanico con JsonSerializerContext
   9  InProcessShell     el cargador dinamico
   0  Dispatcher, ModuleCatalog, Module, ToolIndex, HostTools, ConsentWatch, IShell
```

**El núcleo pasa limpio.** Detalle, fuentes y la cláusula 2.5.2 de Apple: `NotasGnothiHost/32`.

## Lo que NO está verificado todavía

- **Que `ui.html` caiga en `Assets/ui.html` dentro del bundle.** El `LogicalName` está puesto,
  pero eso lo resuelve el empaquetado, que es de la Mac. Si la pantalla sale en blanco, el log
  dice `NO se encontro ...` con la ruta exacta.
- **Los cinco pasos de la sonda.** Nadie los corrió en un iPhone todavía.
- **Release con `MtouchInterpreter=-all` y `MtouchLink=SdkOnly`.** Debug es lo que está
  pensado para medir; Release es lo que hay que probar después.
