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

## Contra el APK real, en el mismo teléfono — 2026-09-16

Se construyó e instaló `Gnothi.Mobile` en Release sobre el mismo A54 y se cronometró por su propia
puerta de medición, `LoopbackSpikeService`, que contesta **por el Dispatcher de verdad** y emite
`X-Gnothi-Serve-Ms`. Los dos números salen del mismo cronómetro y del mismo aparato.

```
adb shell am start-foreground-service -n com.ncubo.gnothi.host/com.ncubo.gnothi.host.LoopbackSpikeService
adb logcat -d | grep token=
adb forward tcp:47623 tcp:47623
curl -D - "http://127.0.0.1:47623/gnothi/consulta?token=<t>&modulo=dealership&tool=resumen"
```

El módulo `dealership` está instalado como `webview`, o sea la forma hospedada:

| | hospedada, APK real | la sala |
|---|---|---|
| **primera consulta** | **4.153 y 4.179 ms** | **439 y 449 ms** |
| siguientes, módulo vivo | 11 a 22 ms | 0 a 2 ms |
| **memoria del proceso** | **238 MB con UN módulo vivo** | **151 a 164 MB con CINCO** |
| lo que entrega la institución | 11,3 MB servidos | 200 KB |

Los 4,15 s no son de una sola vez: se pagan cada vez que el módulo no está vivo, y el desalojo es
obligatorio. Una pregunta cruzada que toque cinco módulos los paga cinco veces.

**Salvedad:** los otros tres módulos del teléfono están instalados como `in-process` y no
contestaron (`does not expose <tool>`), así que esa forma quedó sin medir. La instalación de esos
módulos es del 9 de septiembre.
