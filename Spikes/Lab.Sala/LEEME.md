# Lab.Sala — la sala, medida en un teléfono

Lo que la nota 49 llama **la sala**: un solo documento del motor web, propiedad de Gnothi, con el
runtime .NET y el motor adentro, que recibe después los `.dll` de las instituciones y los corre a
todos en el mismo runtime.

- `Sustrato/` — el sustrato: Blazor WebAssembly publicado en Release **con el trimmer activo**.
  Referencia `Ncubo.Puppeteer`, `Gnothi.Contract` y `Puppets.Portador` **sin nombrarlos en C#**,
  igual que hoy los presta el Host. Los módulos no se referencian: llegan como bytes.
- `Android/` — el mismo sustrato adentro de un APK, servido desde sus propios assets a un WebView
  de la app. **No declara el permiso de internet**, a propósito.

## Correrlo

```
dotnet publish Sustrato -c Release -o Sustrato/publicado
dotnet build Android -c Release
adb -s <serie> install -r <el apk firmado>
adb -s <serie> logcat -s SALA:I SALA-MEDIDA:I
```

## Lo medido — Galaxy A54 físico, Android 16, 2026-09-16

| | |
|---|---|
| APK completo, con el sustrato y los cinco módulos | **14,1 MB** |
| sustrato solo, memoria lineal del wasm | **46,1 MB** |
| con los cinco módulos cargados y contestando | **66,5 MB** (4,1 MB por módulo) |
| PSS del proceso entero, cinco módulos vivos | **151 a 164 MB** |
| proceso → los cinco contestados | **3.095 a 3.930 ms** |
| primera consulta, con el arranque del motor adentro | **439 a 449 ms** |
| las otras cuatro primeras consultas | 3 a 188 ms |
| cualquier consulta con el módulo tibio | **0 a 2 ms** |

Sin `wasm-tools`, o sea sin AOT ni relinking: los tiempos juegan en contra, no a favor.
