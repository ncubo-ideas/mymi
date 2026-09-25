# Lo que quedó pendiente de probar en Mac

**Qué es esto:** la lista de lo que se tocó de `Gnothi.iOS` **sin poder compilarlo nunca**, y lo que
hay que mirar en cada cosa cuando alguien abra la Mac.

> ## ⚠️ ESTE AVISO DECÍA QUE NO COMPILA, Y ERA FALSO · corregido 2026-09-25
>
> Decía: *«nada de lo que sigue está compilado; en Windows `Gnothi.iOS` no compila, el SDK exige una
> versión exacta de Xcode y el build ni empieza»*. **Se probó y compila.** En esta misma máquina:
>
> ```
> dotnet build Gnothi.iOS -c Debug
> Gnothi.iOS -> ...\bin\Debug\net9.0-ios\iossimulator-x64\Gnothi.iOS.dll
> 0 Errores — 12,7 s
> ```
>
> Y se comprobó que **sirve para algo**, rompiéndolo a propósito: un `RoomShell.LogQueNoExiste`
> devuelve `error CS0117` y desaparece al restaurar. O sea que **atrapa exactamente la clase de error
> que se venía buscando a mano** — el `sala.Calentar` contra `WarmUp` de §2.2 lo habría cantado el
> compilador en doce segundos.
>
> **Lo que la Mac sigue haciendo falta para:** enlazar y empaquetar el `.app`, correrlo en simulador
> o aparato, `libGnothiIntents.a`, y todo lo que sólo se ve ejecutando (WKWebView, los intents, la
> caja bajo Pulley). **Compilar no es correr** — que es la misma distinción que calentar no es
> cargar, y las dos se pagaron el mismo día.
>
> **La lección, porque es la del día:** la afirmación nunca se probó. Se heredó, se repitió en dos
> documentos y **sostuvo un método manual entero** —verificación símbolo por símbolo— durante tres
> días. El comando que la tumbaba tarda doce segundos.

---

## 0 · Lo único que hace falta

- Una **Mac con Xcode** y el workload de .NET para iOS.
- Un **simulador de iOS 18+**, o un iPhone. Para lo de §4 conviene aparato de verdad.
- El build necesita saltarse la validación de versión de Xcode, que es lo que hace que ni empiece:

```bash
dotnet build Gnothi.iOS/Gnothi.iOS.csproj -p:ValidateXcodeVersion=false
```

---

## 1 · Lo que bloquea todo lo demás: que compile

**Es la única tarea que no se puede saltar**, y hasta que dé verde el resto de este documento es
teoría. Dos cosas la pueden hacer fallar, y son distintas:

### 1.1 · La `.a` vieja — «Undefined symbols»

`master` agregó `gnothi_fm_razon` del lado de Swift, y **`libGnothiIntents.a` hay que regenerarla**.
Si el build muere con *Undefined symbols*, es eso y no otra cosa. La receta está en
[`README.md` §3b](README.md).

Del lado de C# la declaración ya está puesta (`BridgeProbe.cs`): entró el 2026-09-22 porque faltaba
y sin ella el proyecto no compilaba — la llamada había llegado con `77f529b` y su `DllImport` no.

### 1.2 · Errores de compilación de lo de §2

Todo lo de la sección siguiente se escribió a ciegas. Si aparece un `CS0103` o un `CS1061` con un
nombre de la sala, es de ahí.

---

## 2 · Lo que se tocó sin compilar, archivo por archivo

### 2.1 · `IosRoom.cs` — renombrado completo, 27 símbolos

El archivo se llamaba `IosSala.cs` y la clase `IosSala`. Los identificadores pasaron a inglés para
que la sala sea simétrica con la caja, que ya los tenía así desde el 2026-09-17.

| antes | ahora |
|---|---|
| `IosSala` · `ISalaHost` | `IosRoom` · `IRoomHost` |
| `Preparar` · `Pedir` · `Soltar` · `Guardar` · `Descartar` | `Prepare` · `Ask` · `Release` · `Save` · `Discard` |
| `Servir` · `Servible` · `Servida` · `Arrancar` · `Calentar` | `Serve` · `Servable` · `Served` · `Start` · `WarmUp` |
| `Texto`(JsonElement) · `Texto`(cuerpo) · `Tipo` | `Field` · `TextBody` · `MimeOf` |
| `PuenteDeLaSala` · `ServidorDeLaSala` | `RoomBridge` · `RoomServer` |

**Lo que NO cambió, y es la mitad que importa:** el protocolo con el sustrato. Los verbos
(`"cargar"`, `"desalojar"`, `"consultar"`, `"vista"`, `"entregar"`, `"cursor"`, `"repertorio"`), los
campos del json (`ok`, `motivo`, `contenido`, `escrituras`, `ruta`, `vaciada`, `vecinos`, `entrada`,
`archivos`, `rutas`), las rutas (`modulo/<m>/indice.json`, `dominio`, `journal`), los nombres de
JavaScript (`window.sala.pedido`, `SalaPuente`) y el `"type": "sala"`.

**Eso se verificó campo por campo contra `Spikes/Lab.Sala/Sustrato/Sala.cs`, pero por lectura.**
Cómo se ve si algo se rompió ahí: §3.

### 2.2 · `ViewController.cs` — y acá hubo un error real

Tres cambios: los renombres de arriba, la llamada nueva a `ChainShellFactory`, y comentarios.

**EL ERROR QUE YA APARECIÓ, y que da la medida del riesgo:** `ViewController` seguía llamando
`sala.Calentar(...)` cuando `IosRoom` ya exponía `WarmUp`. Lo encontró la verificación a mano, no el
compilador — porque acá no hay compilador. **Está arreglado.** Se cuenta porque es la prueba de que
este documento hace falta.

El otro cambio es la costura:

```csharp
if (Gnothi.Host.Module.ChainShellFactory((name, installation) => names.Contains(name)
        ? new Gnothi.Host.RoomShell(name, installation.Shell, installation.GrantedFolder)
        : null))
    Log("sala: ya habia una fabrica de cascaras; la sala se encadena…");
```

**En iOS eso NO debería loguear nada**, y es lo correcto: no hay caja de este lado, así que la sala
es la única fábrica y `ChainShellFactory` devuelve `false`. Si esa línea aparece en el log, hay una
segunda fábrica que nadie sabía que existía — y eso es un hallazgo, no un error.

### 2.3 · Sólo comentarios — `SceneDelegate.cs`, `ModelFoundation.cs`, `BridgeProbe.cs`

Traducidos al inglés, sin una línea de código tocada (verificado filtrando el diff). Riesgo bajo,
pero entran igual en el build de §1.

---

## 3 · Que la sala siga contestando, que es lo que el renombrado pudo romper

El modo de falla del protocolo es **silencioso del lado del compilador**: compila, pasa las pruebas
de la suite, y deja de contestar en el aparato. Así que esto no se puede saltar.

**Cómo se prende** (sin tocar el `installation.json` de nadie — es un archivo de la persona):

```bash
xcrun simctl get_app_container booted com.ncubo.gnothi.host data
echo comprasw,examenesw > "<ese contenedor>/Library/Application Support/sala"
```

Va en `Library/Application Support` y no en `Documents` porque la app Archivos **no** expone la
primera: la persona no la toca por accidente. Se lee **una vez**, en el arranque.

**El archivo vacío ya dice algo**: arranca la sala sin ningún módulo, que es como se contesta la
pregunta del origen sin mezclarla con un alta que pudo fallar.

### Lo que tiene que verse en el log

```
sala: creando el documento
sala: P0 · secureContext=true crypto.subtle=true storage=true wasm=true
           wasm.streaming=true origin=gnothi-sala://sala
sala: P0 · fetch(mismo origen)=200 basic
sala-web: log: la sala esta lista, hospedada por Gnothi. Esperando pedidos.
sala: el sustrato contesta
```

**El renglón que decide es el del `fetch`, no el del contexto seguro.** La forma hospedada nunca hace
fetch por su origen; el sustrato no puede evitarlo —Blazor baja `_framework/*` con fetch— así que si
WebKit no entregara el esquema propio a fetch, la sala no arrancaría aunque el origen fuera seguro.

Y después, con un módulo:

```
sala: <modulo> preparado desde <carpeta>
sala/cargar <modulo>: {"ok":true,"consultas":[…],"vecinos":[…]}
```

### Si algo del protocolo se rompió, así se ve

| síntoma | qué significa |
|---|---|
| `sala/cargar …: {"ok":false,"motivo":"no llego la entrada '…'"}` | el campo `entrada` o `archivos` del `indice.json` cambió de nombre |
| `sala: 500 modulo/<m>/indice.json` | la ruta que el sustrato pide no coincide con la que `Serve` atiende |
| el documento arranca y **nunca** llega `el sustrato contesta` | el nombre de la función JS (`window.sala.pedido`) o del puente (`SalaPuente`) cambió |
| carga y **no falla nunca** por vecinos | `rutas`/`vecinos` cambiaron: el chequeo de frontera está leyendo un campo que no existe, o sea **no está chequeando** |

**La última es la peligrosa**, porque se ve como éxito. Es el mismo modo de falla que este proyecto
viene documentando: dos cosas que tienen que coincidir, nadie las compara, y falla en silencio.

---

## 4 · Lo que está pendiente de MEDIR, no de arreglar

### 4.1 · iOS rehidrata TODOS los módulos al arrancar, dos veces, y nadie lo midió

`Shell.Run` hace `await Fotografiar()` **antes** de `VerifyNewModules` y del calentado; `Snapshot.Write`
llama `dispatcher.Serve` por **cada tool sin parámetros de cada módulo habilitado**, lo que arranca
el actor de todos. iOS pasa `FotoLocal: true` **y** `Instantaneas: SharedContainer()`: **dos pasadas
completas**. Android no lo corre.

**Con cinco módulos a ~14 s, es del orden de un minuto antes de que Gnothi esté listo.**
Extrapolación, no medición: nadie corrió `Gnothi.iOS` con los cinco. Ver `C10`.

### 4.2 · El modelo de Apple y el tool calling en iOS 27

Medido el 2026-09-17 con el mismo binario y la misma tool: **4 de 4 en iOS 26.6.2 y 0 de 8 en
iOS 27.0**. Dos de esas ocho invirtieron el veredicto al cambiar el idioma de la pregunta. El banco
para volver a medirlo es `Gnothi.Assistant.Tests/Preguntas.cs`, que trae cada pregunta en los dos
idiomas justamente por esto.

**No está cerrado.** Si la Mac tiene iOS 27, vale volver a correrlo antes que cualquier otra cosa
del asistente.

---

## 5 · El orden que propone este documento

1. **Que compile** (§1). Sin esto lo demás es teoría.
2. **La sala arranca sin módulos** (§3, archivo vacío). Contesta la pregunta del origen sin mezclarla con un alta.
3. **La sala con un módulo** (§3). Acá se ve si el renombrado rompió el protocolo.
4. **Que no aparezca la línea del encadenado** (§2.2). Es un chequeo de un segundo.
5. Lo de medir (§4), que no bloquea nada pero es lo único que va a dar números.

---

## 6 · Y lo que NO está en esta lista: la caja

Todo lo de arriba es **la sala**. **La caja de wasm es otra forma y tiene su propio plan**, que no
depende de que `Gnothi.iOS` compile y se corre en un proyecto aparte:
[`SPIKE-LA-CAJA-EN-IOS.md`](SPIKE-LA-CAJA-EN-IOS.md).

Los dos se pueden atacar en cualquier orden. Si la Mac es prestada y hay una sola tarde, **la caja
da más información por hora**: sus pasos 1 a 5 corren en el simulador y el primero que falla cierra
una pregunta que hoy está abierta en `B12` desde el 2026-09-17.
