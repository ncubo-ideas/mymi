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

---

## `Malo/` — el módulo que se porta mal

El mismo papel que `Spikes/Lab.WasiCaja/LosIntentos.cs` hace dentro de la caja de wasmtime, pero
**adentro de la sala y como código de una institución**, no como código del sustrato. Esa
distinción es toda la prueba: lo que el sustrato alcanza no dice nada, porque el sustrato es de
Gnothi.

No referencia nada — ni `Gnothi.Contract`, ni el motor. Declara sus propios `[ModuleSurface]` y
`[Query]`, porque el descubrimiento es **por nombre de tipo**. Que eso alcance para entrar a la
sala es, en sí mismo, parte de lo que se está midiendo.

**Cómo se corre, y esto no es opcional.** Jonathan lo dejó escrito: con un solo módulo cargado la
corrida da «no pudo» ocho de ocho **y es mentira**, porque no hay nada de nadie en MEMFS — falla
porque está vacío, no porque esté protegido. Es el mismo error que el propio `LosIntentos.cs`
documenta en su intento 6. Hay que cargar un módulo de verdad, desalojarlo, y **recién entonces**
entrar con éste:

```bash
P=/data/user/0/com.ncubo.gnothi.host/files
dotnet build Spikes/Lab.Sala/Malo/Malo.App.csproj -c Release -o /tmp/malo
adb push /tmp/malo/Malo.App.dll /data/local/tmp/Malo.App.dll
adb shell run-as com.ncubo.gnothi.host cp /data/local/tmp/Malo.App.dll $P/Modulos/malow/dominio/Malo.App.dll
# 1 · un modulo de verdad, que monta su journal
curl "http://127.0.0.1:47623/gnothi/consulta?token=<t>&modulo=comprasw&tool=resumen"
# 2 · y ahora el que se porta mal, con el anterior ya desalojado
curl "http://127.0.0.1:47623/gnothi/consulta?token=<t>&modulo=malow&tool=resumen"
```

**El control de la red es obligatorio.** Un «no pudo» hacia afuera no vale nada si el aparato no
tenía salida: medido el 2026-09-17, el emulador **no alcanza Internet** aunque la máquina sí, así
que `example.com` falla con CSP y sin CSP. El destino que sí decide es uno propio, publicado
adentro con `adb reverse tcp:8123 tcp:8123`. Con `connect-src *` el módulo lo alcanza (27 bytes, y
el servidor registra el golpe); con la CSP real, `TypeError: Failed to fetch` y el servidor no
recibe nada. Esa pareja es la medición; un lado solo no lo es.

Tres intentos de red dieron «no pudo» por la razón equivocada antes de medir de verdad:
`HttpClient.Timeout` y `GetAsync` están recortados del sustrato, y `GetAwaiter().GetResult()` tira
`PlatformNotSupportedException` porque el runtime del motor web es de un solo hilo. Sirven
`GetStringAsync` y `GetByteArrayAsync`, y la consulta tiene que ser `async`.
