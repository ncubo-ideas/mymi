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

## Cómo correrlo

**Hace falta una Mac con Xcode.** En Windows compila el C# (útil para verificar que no se
rompió nada), pero el empaquetado del `.app` es de Xcode.

```bash
cd Gnothi.iOS && dotnet build -f net9.0-ios -c Debug
```

Para el simulador o el aparato, desde la Mac:

```bash
dotnet build -t:Run -f net9.0-ios
```

### Los logs

El equivalente de `adb logcat -s GNOTHI`:

```bash
log stream --predicate 'processImagePath CONTAINS "Gnothi"' --style compact
```

### Cómo entran los journals y los módulos

Dos caminos, y el segundo es mejor que cualquier cosa que tenga Android:

1. **Desde el bundle.** Si `Gnothi.Mobile/paquete/` existe cuando se compila (lo arma
   `armar-paquete.ps1`), viaja adentro de la app y se siembra en `Documents/incoming` la
   primera vez.
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
