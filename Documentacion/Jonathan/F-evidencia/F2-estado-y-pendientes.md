# F2 · Estado y pendientes

**Corte original 2026-08-26. Actualizado el 2026-09-07** con lo que pasó en móvil: **P5 y P7 se
resolvieron** y están medidos, y aparecieron cuatro pendientes nuevos que antes no existían porque
el terreno no existía. Cada cosa lleva su fecha.

## El estado, en líneas

- **Anda, y es lo más fuerte que hay: los módulos hospedados en Android.** Cuatro instituciones —
  Exámenes, Compras, Cargas y DealershipCar—, cada una con la app web que ya había publicado, **la
  misma y sin recompilar**, corriendo adentro de Gnothi en un WebView, sin red propia, escribiendo
  su journal en los archivos del Host. Corrido de punta a punta el **2026-09-04** sobre un emulador
  `sdk_gphone64_x86_64` (Android 16 / SDK 36); la contención y la memoria vienen del **2026-09-03**
  sobre un **Xiaomi 14T Pro físico**, con Exámenes. Eso trae el alta con consentimiento, el cartero,
  la credencial fuera del journal y la marca de migración. Ver `F6`, y el guion completo con sus 14
  capturas en `Gnothi.Mobile/PRUEBAS-WEBVIEW.md`.
- **Anda:** el eslabón de agregación, en tres superficies de asistente distintas y en Docker.
- **Anda:** el consentimiento por consulta, y desaparece en vez de denegar.
- **Anda:** la verificación de lo declarado contra lo real.
- **Anda:** la **ventana de escritorio nativa** —`Gnothi.Desktop`, Blazor Hybrid— corre las consultas
  llamando al Dispatcher **directo**, sin wasm, sin puerto, sin http. Es una cara más del Host. Ver `C7`.
- **Anda:** los **dos instaladores MSI** — el de Gnothi deja la marca de descubrimiento en `HKCU`; el del
  comercio la lee, **bloquea si Gnothi no está** y engancha `compras.gmod`. Ver `C7`.
- **Anda:** un **segundo módulo** demo, de otro autor — compras / Super La Guaria — sobre el mismo mecanismo.
- **Novedad:** el motor `3.0.0-beta.10029` **ya trae el replay secuencial**, así que la carátula wasm arranca
  con el paquete. Antes hacía falta apuntar al repo del motor.
- **No anda / no está:** el sandbox de capacidades por módulo (en el camino in-process), las consultas con
  parámetros, y el catálogo en caliente de verdad.

---

## Pendientes, en orden de lo que desbloquea

### P1 · El guard de hilos por capacidad — ~~y no es código nuestro~~ · **CERRADO, verificado el 2026-09-16**

**Ya está hecho en el motor, y nadie se enteró de este lado.** El pedido era cambiar la pregunta
«¿estoy en un browser?» por una sobre **capacidad del hospedaje**. El motor hoy pregunta exactamente
eso, con ese nombre, y cubre WASI:

```csharp
// The probes name the capability, not a preference: a threaded wasm build also
// takes the one-thread flow, correctly if without the parallelism.
internal static bool HasNoWorkerThreads =>
    OperatingSystem.IsBrowser() || OperatingSystem.IsWasi();
```

Está en los dos lugares que hacían falta —`ActorHandler.cs:1351` (la rehidratación) y
`Dispatch.cs:59` (la reducción)— y trae los dos overrides para que un host con hilos ejercite el
mismo camino (`ForceSequentialRehydration`, `DispatchOptions.ForceInlineReduction`). Los commits son
del 25 y 26 de agosto de 2026, **antes del corte de este tablero**, y sus comentarios dicen
*«Measured on standalone WASI»*: el equipo del motor ya lo corrió ahí.

**PERO OJO CON LA VERSIÓN, que es la parte que sí nos toca.** Verificado abriendo los `.nupkg` de la
caché local:

| versión | `ActorHandler` (rehidratación) | `Choreography` (reducción) |
|---|---|---|
| `3.0.0-beta.10027` | no | no |
| **`3.0.0-beta.10029`** — con la que armamos todo | **sí** | **NO** |
| `3.0.0-beta.10029-lector.4` | sí | **sí** |

La mitad de `Choreography` entró un día después de la 10029 y **no viajó en ella**. O sea: con la
versión que usamos hoy, un módulo bajo WASI arrancaría la rehidratación y moriría en el despacho.
**No hay nada que reportar: hay que subir de versión.** La `lector.4` ya está en la caché local; la
rama pide la `10044`, que el feed no entrega (403).

**Consecuencia para el tablero:** `P8` deja de estar bloqueado. Y el argumento *«el camino de wasm
depende de código ajeno»* —que se repitió en esta misma sesión— **es falso desde hace tres semanas**.

### P2 · El fixture del grafo de referencias

Poner el frente MCP en su propio assembly y un test que afirme que el assembly del despacho **no lo
referencia**.

**Por qué ahora.** Son veinte líneas, y es lo que hace que el principio se cobre en vez de quedar en
intención. **El día que se pierda la propiedad, se pierde en silencio** — ningún tipo ni método la expresa,
solo el grafo. Hay precedente exacto en el repo del motor.

**Criterio:** el test falla si alguien mete un `using` del SDK de MCP en el despacho.

### P3 · Consultas con parámetros, por source generator

Hoy se omiten con aviso. **Por qué el generador y no reflexión:** un SDK basado en atributos + reflection
**se rompe con trimming, y en silencio**.

**Camino intermedio si hace falta antes:** manejar la llamada a mano con los handlers del SDK, que existen.

**Criterio:** una consulta con dos parámetros tipados se publica con su esquema correcto y se invoca.

### P4 · El catálogo en caliente

**La instalación ya existe:** el MSI del comercio y el comando `--install-module` dejan el módulo nuevo en la
carpeta y se consulta (ver `C7`). Lo que falta es cablear la notificación de lista cambiada — el Host ya
declara que su lista puede cambiar; el mecanismo está, no está conectado.

**Criterio:** instalar un módulo nuevo y que el asistente vea sus consultas **sin reiniciar**.

### ~~P5 · Sacar los tokens del journal~~ — ✅ **RESUELTO el 2026-09-03**

**Lo que decía este documento en su corte del 26 de agosto** —que se asentaban en texto plano en un
journal append-only— **dejó de ser cierto el 2026-09-03.** El device key, el access token y el
refresh token salieron del dominio en los **cinco** módulos.

Se tomó el segundo camino de los dos que estaban acá anotados, el bueno: **el módulo hospedado no
recibe ningún token.** Y para cuando la app corre suelta, la credencial la guarda **quien hospeda**
y no la historia: `localStorage` del origen en el navegador, el Keystore del aparato dentro de
Gnothi, memoria que muere con el proceso en consola.

**El criterio se cumplió, y está medido el 2026-09-04** en los cuatro módulos de Android: un journal
nuevo después de un ciclo completo, **cero coincidencias de credencial**, con un grep de control
legible dando 6, 2, 4 y 6 — o sea que el grep sabía buscar y el journal estaba limpio. En consola,
Cargas aplicó 19 eventos con el mismo resultado. Ver `F6`, y P5 del guion.

**Por qué importa más que un pendiente cerrado:** un token en un journal append-only le da a un
contexto de sesión la durabilidad y la portabilidad de la historia de una persona. Viajaba en claro
con cualquier copia.

---

## Lo que cambia la forma del POC

### P6 · El módulo sin transporte

Hoy trae su propio servidor MCP. La arquitectura destino se lo quita: **expone su superficie y el Host elige
la cáscara**.

**Por qué importa más de lo que parece.** Está cerrado con 🚫 en la especificación, por dos razones
independientes: stdio/ssh solo tiene sentido entre máquinas distintas, y **si el módulo implementa un
aparato de transporte, el módulo tiene sockets** — con lo cual se cae la garantía que hace defendible todo lo
demás.

Y arreglarlo **cierra del todo el bug de los dos escritores**: sin proceso aparte, no hay dos escritores
posibles.

**Criterio:** un solo artefacto de módulo corre en los dos hospedajes sin recompilar, y no expone ningún
puerto ni tubería propia.

> **Nota del 2026-09-07 — esto está más avanzado de lo que dice el párrafo de arriba, que es del
> corte viejo.** `F4` ya registra el módulo sin transporte como hecho, con su cita, y `F3` lo mide.
> En móvil el criterio se cumple **literalmente**: es el mismo binario en los dos modos —suelto en
> el navegador y hospedado dentro de Gnothi—, sin puerto ni tubería, y sin recompilar. Lo que queda
> abierto de esta sección es la variante wasm de escritorio, que es P8.

### ~~P7 · El Host como cartero~~ — ✅ **RESUELTO en escritorio antes del corte; corrido en Android el 2026-09-04**

Este pendiente ya estaba hecho cuando se escribió esta página y no se actualizó: `F4` lo registra
con su cita desde el corte del 26 de agosto. **Lo nuevo es que ahora está corrido en móvil, que es
donde el módulo de verdad no tiene con qué salir a la red.**

El Host hace el pull del feed del publicador **con su propia credencial** y le pasa el payload al
módulo como parámetro; el módulo asienta. El endpoint es **fijo por publicador, registrado en la
instalación y no elegible en runtime** — el manifiesto lo declara, el Host lo copia a
`instalacion.json` y de ahí manda esa copia.

**El criterio se cumplió, y sigue siendo falsable:** la garantía no es una promesa sino la forma del
código —**el Host no tiene ninguna llamada con la que escribir una entrada**—, y el bucle del
sustrato pide tres piezas repartidas: la red la pone el Host, la ruta y el diario los pone el
módulo.

**Medido el 2026-09-04**, pull inicial por institución: 6 673 · 72 928 · 3 985 · 7 825 bytes; la
segunda vuelta de Compras dio `after=387 → 24 bytes`, con el cursor avanzado y nada nuevo. Ver `F6`,
y P4 del guion.

### P8 · El módulo como `.wasm`

Se parte en dos, y conviene no mezclarlas:

**¿Puede un servidor MCP vivir en un `.wasm`?** Probablemente sí: WASI expone lectura y escritura sobre los
descriptores 0/1/2, que es lo que stdio necesita. **Se prueba con un módulo trivial, sin el motor**, para que
la respuesta no dependa de él.

Prerrequisitos, **ninguno instalado**: el workload de WASI experimental, y un runtime wasm. Candidato para
móvil según la especificación: uno que haga intérprete y AOT en las dos plataformas.

**¿Puede el módulo con el motor?** **Bloqueado por P1.**

---

## Lo que abrió el móvil — pendientes nuevos del 2026-09-07

Ninguno de estos existía en el corte del 26 de agosto, porque el terreno no existía. Van en orden de
lo que desbloquean. El detalle largo de los cuatro está en `13` §10.2 a §10.5.

### P9 · Las tres rendijas de la contención

**La contención es UNA sola capa, y conviene decirlo así.** El interceptor **no bloquea**: devuelve
`null` para todo host ajeno, y `null` significa *«andá a la red»*. Lo único que impide salir es la
CSP. Le faltan tres cosas:

| | qué falta | cómo se cierra |
|---|---|---|
| **`form-action`** | no está puesta, y **no hereda de `default-src`** — es de las poquísimas que no. Un `<form action="https://…" method="post">` con submit **saldría** | agregarla a la cabecera |
| **navegación de nivel superior** | ninguna CSP frena `window.location = 'https://…'` —`navigate-to` nunca se estandarizó— y no hay control de navegación. No filtraría el journal, pero se podría llevar datos en la URL | `ShouldOverrideUrlLoading` en Android; `WKNavigationDelegate` en iOS |
| **la negación es muda** | el módulo intenta, el motor lo niega, y Gnothi **no se entera** | `report-to` en la CSP, o escuchar `securitypolicyviolation` |

**Por qué primero.** La tercera es la que más compra: convierte la contención de silenciosa en
**auditada**, y con eso la prueba P6 del guion **pasa a ejecutarse sola** en vez de necesitar una
sonda temporal.

**Criterio:** un intento de salida del módulo queda registrado en Gnothi sin que nadie prepare nada.

### P10 · El sustrato viaja adentro del bundle

**Medido:** cada módulo trae **su propio motor** — 20 MB de `_framework` con `Puppeteer.wasm`,
`Choreography.wasm` y el runtime de .NET. Gnothi le da al módulo la red, los archivos y el ciclo de
vida; **el sustrato no**. Dos consecuencias, y las dos incomodan:

- El manifiesto declara `substrate.range` (`">=3.0.0 <4.0.0"`) y **nadie lo valida**. No falta por
  descuido: **no hay un sustrato compartido cuya versión negociar**. Mientras el motor viaje en el
  bundle, ese campo es documentación.
- El costo de memoria es **por módulo**, y por eso la regla de desalojo —un módulo vivo a la vez—
  deja de ser precaución y pasa a ser requisito. Es lo que sostiene los 199 MB con cuatro
  instalados.

**Por qué importa.** En iOS, con un presupuesto de memoria más duro, esto puede pasar de hueco
anotado a bloqueante. Y que Gnothi provea el motor es una decisión de **arquitectura**, no un
ajuste.

### P11 · ¿La mudanza es consecuencia de instalar, o una decisión aparte? — **es para el arquitecto**

Esta no es deuda técnica: es la pregunta de diseño más interesante que quedó, y por eso va acá.

**Lo que pasa hoy.** Gnothi le manda al publicador `{deviceKey, recurso, id}` y nada más — ni la
palabra `migrated`, ni un verbo de migración. **Quien decide que eso significa una mudanza es el
publicador**, que ve el prefijo del device key y anota la cuenta como vinculada. Gnothi lo causó, no
lo hizo, y no se entera de que lo causó.

**El hueco.** Con la pantalla de consentimiento, la persona acepta **instalar el módulo**. Lo que esa
pantalla no dice es que su app suelta va a dejar de actualizarse. Toca «Instalar», y de rebote se le
apaga la otra instalación. **Es la misma forma del problema que el consentimiento vino a arreglar,
un escalón más adentro.**

**Y el arreglo obvio no es obviamente el correcto.** Lo barato es que Gnothi agregue una línea de
aviso — pero eso sería **Gnothi afirmando algo sobre el comportamiento de un tercero**. Que ese
publicador migre la cuenta al emitir credencial es política de la institución, no del protocolo: es
cierto con nuestro mock y podría ser **falso** con otro. Una advertencia falsa en la pantalla más
importante del sistema es peor que no tenerla.

Las cinco opciones, con lo que compra y lo que cuesta cada una, están en `13` §10.5. **La pregunta a
decidir no es técnica.**

### P12 · La revocación del lado del publicador

La app suelta deja de sincronizar porque **decidió obedecer** la marca de migración. Verificado: el
guardia del endpoint comprueba token, expiración y recurso, **pero no si la cuenta está migrada**.
O sea que la contención de la app suelta es **cooperativa, no impuesta**. Con la app tal como está
alcanza; con una modificada o vieja, no — y ahí hay dos historias divergiendo, que es exactamente lo
que la marca existe para evitar.

**La marca sirve para que la app se despida bien. La que garantiza es la revocación.**

### P13 · La opción A — la PWA afuera y un `.gmod` leyéndole el journal — **cerrada por medición el 2026-09-08**

No es un pendiente: es una pregunta que estuvo abierta varios días y ya tiene respuesta. Se deja
acá para que no se reabra sola.

**La idea era** que la PWA de la institución siguiera viviendo **afuera** de Gnothi, eligiera su
carpeta con el picker del navegador (en Chrome Android ≥132 eso es el SAF) y que un módulo `.gmod`
**adentro** de Gnothi leyera ese mismo journal. Una segunda ruta contenida en la que ya está
construida.

**Se midió y no se puede**, y el detalle con su reproducción está en `F1`. En una palabra: con la
concesión de árbol tomada y viva —verificado leyendo los bytes por `ContentResolver`—,
`Directory.EnumerateFiles` sobre la ruta deducida devuelve **cero archivos sin lanzar excepción** y
`File.OpenRead` da `UnauthorizedAccessException`. Con «acceso a todos los archivos» sí funciona, lo
que prueba **dónde está el corte**: la concesión del SAF vive en la tabla del `ContentResolver`,
indexada por URI, y el `open()` al que `System.IO` baja pasa por scoped storage, que nunca consulta
esa tabla. **El permiso no cruza de un mundo al otro.**

**Pero se midió una cosa más, y cambia el veredicto de «no se puede» a «falta UNA cosa, y es
chica».** SAF no solo da streams: `ContentResolver.OpenFileDescriptor(uri, "rw")` da un
**descriptor de archivo POSIX de verdad**, que envuelto en un `SafeFileHandle` produce un
`FileStream` normal. Medido en el mismo aparato: `CanSeek=True`, seek + leer del medio, escribir al
final con flush que **persiste** (verificado desde `adb shell`), y **crear archivos nuevos** por
`DocumentsContract.CreateDocument`. Las cuatro cosas que el motor hace.

**Y después se leyó el motor, que era lo que faltaba.** No hay `mmap` —`MemoryMappedFile` no
aparece ni una vez—, así que ese miedo se descarta. Pero **el pedido no es chico**, y la primera
lectura de esta medición lo subestimó:

El motor no es cliente de un archivo, es cliente de un **sistema de archivos**. Su backend
`FileSystem` son unas **6 000 líneas** que crean 8 directorios al arrancar, abren por ruta para
rolar y compactar, **listan por glob** (`journal_*.bin`, `*.batch`) y hacen **rename atómico** con un
puerto propio por plataforma. Y de eso, **dos cosas SAF no las tiene**: un *replace-si-existe*
atómico, y el **lock a nivel de SO** que hoy consigue con `FileShare.None` — que es un concepto del
open-por-ruta y por `ContentResolver` no se puede expresar. Las dos son de seguridad ante cortes y
ante dos escritores. El detalle con archivo y línea está en `F1`.

**La buena noticia es estructural:** la abstracción ya está donde corresponde. `DiaryStorage` es
abstracta, `DatabaseType` tiene **cinco** valores conviviendo, y `IAtomicFileOperation` prueba que ya
aceptaron que una operación de archivo puede ser específica de la plataforma. **El pedido correcto
es un puerto de archivos inyectado** en lugar de llamar a `System.IO` directo — interfaz chica,
travesía grande.

**Y lo que NO hay que pedirle es que entienda SAF.** El motor corre en wasm dentro del navegador, en
.NET de escritorio y en Android; meterle SAF sería colgarle una plataforma al sustrato y no ayudaría
ni al navegador ni a iOS. El puerto va del lado del Host, que sí sabe de Android.

**Y el peligro concreto que la medición evita:** el fallo es **mudo**. Un motor apuntado a esa ruta
no vería un error, vería un journal vacío, y arrancaría uno nuevo — con la historia de la persona
intacta al lado, invisible.

**El segundo bloqueante —los dos escritores— también se midió, y ese SÍ tiene arreglo chico.**
Spike `Spikes/ReaderModeSpike/` en un worktree del repo del motor. Tres hallazgos: la puerta de
solo-lectura **que ya existe** (`ConfigureStorageForIntrospection`) **no es de solo lectura**;
**leer no escribe, abrir sí** —toda la mutación está en el constructor de
`DiaryStorageFileSystem`—; y las dos escrituras son **reconciliación de caché, no estado**, porque
`index.bin` es *«a hint, not an authority»* y los totales de `meta.bin` son *«potentially stale»*,
recomputados desde los archivos del journal, que son la autoridad.

Se midió sobre **tres** escenarios, porque un journal limpio y chico ejercita las menos rutas de
escritura y favorecería cualquier respuesta: **CLEAN** (3 entradas), **ROLLED** (40 entradas y varios
archivos sellados) y **TORN** (un registro partido pegado al final, lo que deja un proceso muerto a
media escritura). ROLLED no agregó nada; **TORN destapó dos escrituras más**, y ninguna estaba en el
`open`: el **truncado de la cola partida** en `Initialize()` —curaba el archivo del dueño, 715→695
bytes— y una reescritura del **header al cerrar**, en `Dispose()`, que apareció como un *crash* y no
como un diff.

**El parche final son 72 líneas en 3 archivos, 19 de lógica**: un `readOnly=true` en el connection
string —la convención del propio repo—, cuatro compuertas, y el archivo activo abierto en
`FileAccess.Read`. Con eso los tres escenarios salen **byte por byte idénticos** y el lector **lee
las mismas 41 entradas**, incluso en TORN. Es no-op sin opt-in, y la suite completa del motor da
igual que master limpio: **5 fallan · 2 087 pasan**, los mismos cinco nombres, todos de locale.

El detalle está en `F1` — incluida **la trampa del instrumento, que vale más que el parche**:
`GracefulExit()` no libera el `JournalWriter`, así que un diff tomado al cerrar corre una carrera
contra el GC, y me dio un falso negativo y después un falso positivo. **Un journal recién cerrado no
está quieto.**

Lo que ese arreglo **no** resuelve: al lector escribiendo el dueño **al mismo tiempo**. Y quedan sin
sembrar elisiones, checkpoints de reacciones y outbox — es el próximo escenario, y donde podría
aparecer una quinta escritura.

### CORRECCIÓN: el modo lector alcanza en escritorio, NO en Android

Esto se dijo mal en su momento —*«el modo lector es lo único que desbloquea algo vivo»*— y conviene
arreglarlo antes de que alguien planifique con eso.

**El modo lector resuelve que el lector no MOLESTE al dueño. No resuelve que LLEGUE a la carpeta.**
Y en Android no llega: la concesión del SAF no alcanza a `System.IO`, medido arriba.

| | ¿llega a la carpeta? | ¿molesta al dueño? | qué hace falta |
|---|---|---|---|
| **escritorio** | ✅ es una ruta real, `System.IO` llega | ❌ sí | **solo el modo lector** |
| **Android (SAF)** | ❌ no llega | ❌ sí | **puerto de archivos + modo lector** |

Así que el modo lector es un camino real **en escritorio**, y ahí el parche medido alcanza tal cual.
En un teléfono es **la mitad del trabajo**: sin el puerto de archivos tendrías un motor que no
perturba a nadie y que igual no puede abrir la carpeta.

**Y el orden para atacar el puerto no empieza en el motor.** Primero hay que medir, en Android, si
SAF puede sostener el contrato que el puerto necesitaría — porque ya hay **dos operaciones sin
equivalente conocido**: el *replace-si-existe* **atómico** (`IAtomicFileOperation`, 35 usos de
Move/Replace/Delete) y el **lock entre procesos** de `FileShare.None`. Eso se responde en un rato y
sin tocar una línea del motor; si sale que no, el puerto nacería con un agujero y no vale la pena
recorrer las 6 000 líneas.

**Es una propuesta para quien mantiene el motor**, en un worktree, sin commit ni merge ni push.

### El camino elegido: A4, la copia — medido el 2026-09-08

**Y no necesita nada de lo anterior.** La PWA vive afuera, escribe en la carpeta que la persona
eligió, y Gnothi **la copia** a su área privada por `ContentResolver`; el `.gmod` lee **la copia**,
donde `System.IO` sí llega. Ni puerto de archivos en el motor, ni modo lector: **cero cambios al
motor**. Medido de punta a punta en `F1` — el motor abrió la copia y leyó sus 13 entradas.

Las dos condiciones que se pidieron, y cómo quedaron:

| condición | cómo quedó |
|---|---|
| **permiso explícito la primera vez** | ✅ lo hace el SAF. Se pide **solo lectura**, y el sistema **sí la impone** sobre la concesión guardada —falla con `SecurityException` tras reiniciar—, **pero no durante la sesión del alta**, donde el picker entrega lectura+escritura. Ahí es disciplina, no incapacidad, y hay que decirlo así |
| **no copiar si alguien tiene tomados los archivos** | ✅ **resuelta, y resultó innecesaria.** Medido en el Chrome real del emulador: la escritura por File System Access es **atómica al `close()`** — el archivo mantuvo el contenido viejo durante los 28 s que el writable estuvo abierto y cambió de golpe al cerrar. Una copia tomada en cualquier instante obtiene **la versión anterior completa o la nueva completa, nunca una partida**. Y **no hay `.crswap`**: la hipótesis del detector era falsa |

**Decidido además:** la carpeta se reconoce por **huella intrínseca** (`<actor>/meta.bin` con mágico
`PPMM` + `journal/` al lado) — la institución no tiene que plantar nada; y el **refresco va al entrar
a Gnothi**, más alguna pasada extra durante el día, porque el `.gmod` no se abre y no hay evento de
apertura. Medido que corre **desatendido**, sin picker, con la concesión guardada.

**El costo del refresco: medido de 4 a 120 MB.** Refresco completo (copiar + abrir con el motor),
sin persona y sin picker:

| journal | archivos | refresco |
|---|---:|---:|
| 4 MB | 1 | **~1,3–1,6 s** |
| 40 MB | 10 | **~2,7–4,1 s** |
| 120 MB | 29 | **~6,8–8,0 s** |

⚠️ **Esa tabla es optimista y hay que leerla con la corrección al lado:** las tres filas tienen
**2 000 entradas**; para llegar al tamaño se subió la carga por registro. Pero abrir hace **replay**,
que cuesta **por entrada**. Aislado a tamaño constante: los mismos ~4 MB en **40 000** entradas chicas
tardan **4,5 s** en abrir contra **0,95 s** en 2 000 gordas — la copia no se mueve, abrir se
multiplica por 4,4. Ajuste grueso: `abrir ≈ 0,6 s + 0,09 ms/entrada + 37 ms/MB`.

**Con registros realistas de 2 KB**, 120 MB son ~58 800 entradas y abrir sale **~10 s**, no 5,2.

**Viable al entrar a Gnothi hasta la decena de MB.** En el orden de los 40 MB ya son varios segundos,
y a 120 MB el refresco tiene que pasar a segundo plano, fuera del camino de abrir la app.

**Y la palanca no es la que parecía: es ABRIR, no copiar.** A 120 MB abrir el journal con el motor
cuesta **2–3× lo que copiarlo**. Comparar tamaño y fecha para copiar menos solo recortaría la mitad
más chica; lo que valdría la pena es **no reabrir el motor en cada refresco** — mantener vivo el
actor del módulo.

Dos cosas que sí conviene saber:

- **Lo que manda no son los bytes, es la cantidad de archivos.** Los mismos 4 MB partidos en 16
  cuestan **~2× más** de copiar (≈15 ms de sobrecarga por archivo, cada uno una consulta al
  `ContentResolver`). Eso lo gobierna el `maxFileSize` del journal, que hoy nadie eligió pensando en
  esto.
- **El costo dominante es abrir la copia con el motor, no copiarla.** El SAF no es el cuello.

**Y cuántos registros pesan 1 MB:** ≈ **43,5 bytes de sobrecarga por registro** más el contenido. Van
de **~25 900** registros mínimos a **~513** de 2 KB. Con eso se estima cualquier institución sin
volver a medir.

**Dos cosas más que salieron del Chrome real** y hay que tener presentes:

- **El sellado se justifica por el CONJUNTO, no por el archivo.** La atomicidad es por archivo; un
  journal son varios, y copiar entre la escritura de uno y la del siguiente da un conjunto coherente
  pero desactualizado. Como `meta.bin` e `index.bin` son caché reconstruible y el journal es
  append-only, eso queda «viejo», no «roto».
- **El permiso de escritura de la PWA es por sesión de pestañas.** Chrome lo dice: *«until you close
  all tabs for this site»*. Contrasta con el de Gnothi, que persiste y sobrevive reinicios. La app
  suelta tiene que volver a pedir permiso más seguido que el Host.

**Lo que falta:** journals de decenas de MB —el punto donde la copia entera deje de pagar existe,
solo que está más lejos de lo que se suponía—, y volver a medir la atomicidad si cambia el navegador
o la plataforma: es de **Chrome sobre SAF**, no del estándar.

### Y tres más chicos, para que no se pierdan

| | qué | dónde está el detalle |
|---|---|---|
| **el traspaso en los otros tres** | las aserciones que ingresó la persona viajan de la app suelta a Gnothi. Existe **solo en Exámenes**; cuáles viajan es una decisión **por institución** (Cargas: confirmación de tanque y odómetro; DealershipCar: aplazamientos y perfil de uso) | `13` §4 |
| **los textos que suenan a despojo** | varias pantallas dicen que el journal *«ahora vive en Gnothi»*, y eso se lee como que Sonia dejó de ser la dueña. La redacción de reemplazo ya está escrita y sin aplicar | `13` §10.6 |
| **el bundle no está autenticado** | Gnothi baja **cualquier URL** que venga en el parámetro. Las guardas que hay —techo de 64 MB, zip-slip, nombre válido— protegen el sistema de archivos, no a la persona de una institución falsa. Hoy lo único que hay es que la hoja de alta **dice quién sirvió el bundle**: eso es honestidad, no autenticación | `F6` §1 |

---

## Dónde hay latitud de diseño

**El formato del manifiesto.** Lo que hay es **una propuesta, no la especificación**. Se sabe qué campos
lleva; falta la forma, la extensibilidad, y cómo se versiona el manifiesto mismo.

Una restricción que no es negociable: **el modelo de capacidades tiene que ser contrato, no recomendación.**
Un módulo que declara «cero red» debe **fallar** en un host que se la conceda igual.

**La view declarativa.** Se sabe que el módulo describe y el Host renderiza. Qué lenguaje, sin decidir. Y hay
una tensión real: *«tan buena como quiera»* empuja hacia libertad expresiva, *«pocos KB»* hacia un
vocabulario acotado. Las dos no pueden ser máximas.

**Firma, conformance y revocación.** Es la capa de gobernanza, y está sin diseñar. Y hay un costo que hay que
aceptar de entrada: **esto nos convierte en el guardián.** No hay salida puramente técnica; se mitiga con
Host open source, builds reproducibles y manifiestos públicos.

**La explosión de consultas.** N instituciones × M consultas degradan la precisión de selección del asistente
y se comen la ventana de contexto. Con veinte módulos es un problema real, y **no aparece en ningún panel de
espacio**.

**Y el hueco de la superficie como canal de atención.** La asimetría es «el módulo responde, no pregunta».
Pero **el nombre y la descripción de una consulta son un canal hacia la atención de la persona.** Un módulo
puede publicar `completar_encuesta` y el modelo se lo va a ofrecer. Ahí la incapacidad no alcanza, y el
diseño falta.

---

## Qué reportar de vuelta

Tres cosas cambian decisiones aguas arriba:

1. **Si el dominio necesitó cambios** para correr confinado. Eso no es un inconveniente, **es el hallazgo**:
   significa que hay una fuga de infraestructura y hay que nombrarla.
2. **Cualquier número.** Cold start, latencia, payload en Release trimmed, batería. El proyecto se argumenta
   con mediciones; cada una mueve el pitch de «creemos» a «medimos».
3. **Cualquier cosa de estos documentos que el código contradiga.** Ya pasó una vez con los comentarios del
   motor. Mejor corregirlo que defenderlo.

---

## P14 · El modo lector, cerrado — y qué queda del puerto de archivos · 2026-09-08

**Resuelto lo que estaba declarado como hueco.** Elisiones, checkpoints de reacciones y outbox se
sembraron. Sobre un journal ordenado no agregan escrituras: el parche aguanta.

**Pero aparecieron dos compuertas más**, y las dos las destapó ponerse en el caso real en vez del
caso cómodo:

| # | qué escribía una «lectura» | cómo apareció |
|---|---|---|
| 5 | **crea 6 carpetas** en la carpeta ajena (`Directory.CreateDirectory` ×7 al abrir) | escenario **FOREIGN**: un journal sin el esqueleto de este backend |
| 6 | **borra 12 archivos** del dueño (`RecoverFromIncompleteOperation` desde cada `LoadX`) | escenario **RICH-INTERRUPTED**: el dueño muerto a mitad de un replace atómico |

La 6 es la grave: no modifica bytes, **elimina archivos**. Un consumidor al que le prestaron una
carpeta le estaba terminando la transacción al dueño.

Las dos salieron del mismo punto de inyección, así que la 6 es **una línea**: en modo lector los
stores reciben un `IAtomicFileOperation` inerte.

**Estado del parche: 131 líneas en 4 archivos, ~44 de lógica, 6 compuertas.** Seis escenarios
(CLEAN · ROLLED · TORN · RICH · RICH-INTERRUPTED · FOREIGN), **los seis sin escribir un byte** y
leyendo lo mismo que el control. No-op sin opt-in. Suite completa: **5 fallan · 2 087 pasan**, igual
que master limpio.

### Y de paso quedó medido algo de A2

El `AtomicReplace` del modo lector **lanza** en vez de callar, a propósito. **No lanzó en ninguno de
los seis escenarios** — el camino de lectura no lo llama. Junto con el censo (ver F1), eso deja el
puerto que un lector necesitaría en **cinco operaciones**:

| operación | ¿medida sobre SAF? |
|---|---|
| `File.Exists` | ✅ sí |
| `File.ReadAllBytes` | ✅ sí |
| `FileStream` de solo lectura con `Seek` | ✅ sí |
| listar la carpeta | ✅ sí |
| `File.GetLastWriteTimeUtc` | ❌ **NO** |

**Corrección:** se dijo *«las cinco ya medidas»*. Son **cuatro**. `GetLastWriteTimeUtc` no se midió
nunca: `FileCourier` consulta solo `ColumnSize`, y la sonda del SAF no leía la fecha. SAF expone
`COLUMN_LAST_MODIFIED` en la consulta del documento, así que casi seguro anda — pero *casi seguro*
no es medido, y por eso está en la columna de la derecha como falta.

**El replace atómico y el lock entre procesos —los dos que SAF no tiene— son del lado de la
escritura, y el camino de lectura no los toca.** Eso viene del censo (código) y del guardia de
`AtomicReplace` (medición); el lock **no** tiene todavía un guardia equivalente.

### Lo que sigue abierto

- **El lector mientras el dueño escribe al mismo tiempo.** Sin medir. Es lo que separa «puedo leer
  una carpeta prestada» de «puedo leerla en vivo», y es el experimento que falta para que A3 sirva
  de algo en escritorio.
- Las reacciones del escenario RICH **se saltaron la entrada 1** (un ScriptEvent literal de estilo
  V1). Los stores quedaron creados igual, pero `elision.bin` puede haber quedado sin registros.
- Nada de esto está commiteado ni fusionado. Worktree `Puppeteer-spike-reader`, desprendido de master
  `8ef4415`.

---

## P15 · Leer en vivo SÍ se puede — y el argumento del modo lector se corre de lugar · 2026-09-08

**Medido** (ver F1): un lector abriendo diez veces mientras **otro proceso** escribe 120 entradas,
con **6 rollovers** en el medio. Ni el dueño ni el lector fallaron, quedaron las 126 entradas y la
última dice lo que el dueño escribió último.

**Y el control pasó igual.** Dos motores completos sobre el mismo journal, sin parche, tampoco
rompieron nada. Con lo cual:

1. **«En vivo» no está muerto.** La concurrencia no es catastrófica. Eso abre A2, A3 y A6 como
   caminos con sentido, en vez de rodeos a un callejón.
2. **Pero esta prueba no justifica el parche.** Su justificación sigue siendo la otra: el lector sin
   parche **toca lo que no es suyo** — crea 6 carpetas, escribe el índice, y con una transacción a
   medias **borra 12 archivos del dueño**. Es propiedad y consentimiento, no corrupción.

**Y un dato que conviene mirar antes de pedirle nada a nadie:** el lector ve **una foto del momento
en que abrió** — llegó a 64 de 126 y nunca alcanzó al dueño. «En vivo» quiere decir «fresco en cada
apertura», no un flujo. Si el módulo tiene que reabrir igual, **la distancia entre leer en vivo y
leer una copia (A4) es latencia y disco, no naturaleza**.

### Lo que queda abierto en este frente

- **El consolidador de elisiones sin ejercitar.** Es el único lugar donde el backend diseña
  explícitamente para dos procesos, con el lock de testigo por `FileShare.None`. Ninguna prueba lo
  tocó. Es el hueco más filoso.
- La comprobación bajo concurrencia es cantidad + contenido de la última entrada, **no** integridad
  byte a byte de cada registro.
- **Solo Windows.**

---

## P16 · El canal de la lámina 04 — abierto, con un bloqueo y un hallazgo · 2026-09-11

**Para qué.** Defender o descartar la lámina 04 (*«la app es solo la cara»*) ante quien mantiene el
motor, con una tabla **medida**, no leída. El razonamiento está en `B7`; la evidencia literal, en
`F1`, «Los canales de la lámina 04».

### El hallazgo, que reordena el frente

**El ícono pelea con el canal.** Todo mecanismo que da pedido-y-respuesta de verdad exige **o un APK
de la institución, o que Gnothi sea quien abra la página**. Una página con su propio ícono y sin APK
**solo puede usar loopback** — y paga notificación permanente y ninguna identidad del llamador.

Dos consecuencias que se desprenden solas:

- **La puerta que la lámina 04 necesita ya existe y está medida** — `IpcService`, Messenger sobre
  Binder, identidad por firma del APK, **sin notificación permanente**, **153 ms en caliente** en un
  A54. Lo único que se lo impide a una PWA es que una página no tiene uid ni APK.
- **La variante 04d —que Gnothi abra la página— ya está implementada: es el camino 01.** Y la
  versión «la sirve la institución y Gnothi solo la abre» es estrictamente peor, porque pierde la
  única garantía de red medida, que existe **porque Gnothi sirve la página**. No vale construirla.

### Lo que está listo

- `Spikes/Lab.SondaWeb/` — la sonda web, su versión inyectable, el cliente CDP y el README con la
  tabla medida.
- `Gnothi.Mobile/LoopbackSpikeService.cs` — la puerta de loopback, escrita y compilando. Es un
  `Service` exportado que **no toca el producto**: nada en `MainActivity` lo llama, se arranca desde
  adb. Espeja a `IpcService` a propósito —contesta con `Dispatcher.Serve` y reporta `catalogMs` y
  `serveMs`— para que el número sea comparable con los 153 ms.
- Y con el costo de 04a **escrito en el código, no en un párrafo**: `FOREGROUND_SERVICE`,
  `FOREGROUND_SERVICE_SPECIAL_USE`, `ForegroundServiceType = TypeSpecialUse` y una notificación
  `SetOngoing(true)`.

### El bloqueo

**Gnothi no completa el arranque en el emulador**: `ANR in com.ncubo.gnothi.host — failed to
complete startup`, **en Debug y en Release**. Sin eso no se miden latencia, la reja de CORS ni el
token. Dos causas candidatas, sin decidir:

1. **El motor bajado a `3.0.0-beta.10029`.** El branch pide `3.0.0-beta.10044`; compila con 10029
   pero puede colgarse al inicializar. Se descarta con credenciales del feed de Azure DevOps, que
   hoy devuelve **403**.
2. **El emulador.** Se descarta en el A54, que es donde hay que medir supervivencia igual.

### Pendiente, en orden de lo que desbloquea

| qué | qué lo bloquea |
|---|---|
| latencia del loopback, comparable con los 153 ms | el ANR |
| la reja de CORS (`/gnothi/ping` contra `/gnothi/sincors`) | el ANR |
| token e identidad del llamador | el ANR |
| contenido mixto sobre `ws://`, y el cruce de espacios de direcciones | el origen de la WebAPK: hace falta reinstalarla desde `https://192.168.100.95:8453` |
| el **prompt** de Local Network Access | **imposible en este aparato**: Chrome 133, y llega en 142 |
| supervivencia del proceso (Doze, gestores de fabricante) | sin construir, y **el emulador no sirve**: requiere el A54 |
| `DocumentsProvider` de Gnothi en el picker de Chrome | sin construir. **La única fila que podría abrir un camino nuevo**: está medido que el picker abre DocumentsUI (`F1`), y ese picker lista proveedores de terceros |
| Web Share Target, e intents (ms del salto y ausencia de retorno) | sin correr; los intents están en `sonda.html`, son dos botones |
| Binder desde la PWA, **por ausencia** | sin correr: `adb logcat -s GNOTHI` mientras la página intenta escapadas |

### Deuda de método que quedó abierta

**`F2` estaba desfasado de `F1`** cuando se abrió este frente: `F1` llegaba al 09-09 y `F2` se
detenía el 09-08, con dos pasajes que las corridas del 09-09 ya habían contestado —el que dice que
«el orden para atacar el puerto no empieza en el motor», y el que llama a A4 «el camino elegido».
**Sigue sin reconciliarse**, y hay que marcar esos dos pasajes como superados en vez de borrarlos.

---

## P17 · La forma híbrida — construida y corriendo en un teléfono · 2026-09-11

**Qué resuelve.** Que el motor no viaje en el módulo. La cara es solo UI, el dominio y la superficie
van al lado en `dominio/*.dll`, y **Puppeteer lo presta Gnothi**. El diseño está en `C9`, los
números en `F1`.

**Estado: anda de punta a punta, y la cara es la del laboratorio.** Xiaomi con Android 16, build
Release: el paquete pasa la aduana, carga el DLL por reflexión, el cartero trae 26 eventos, la
superficie los asienta y la cara los pinta — con la portada, los chips, **las cinco formas de
dibujo** y la cronología de visitas portadas de la app Blazor. **97 KB contra 12 594 KB.**

> El primer número publicado fue 77 KB, con una cara que mostraba cuatro números y una tabla.
> Portar la pantalla real la llevó a 97. **El segundo es el que compara**, porque es el primero que
> pinta lo mismo.

### Lo que esto desbloquea, y no es poco

**Un solo motor sobre el journal**, que es el requisito que el arquitecto del motor viene pidiendo.
Y sin ninguno de los problemas de canal de la lámina 04: el puente del WebView ya existía y es en
proceso.

**Y contesta la pregunta que `B7` dejó abierta**: en Android, `InProcessShell` con reflexión sobre
un ensamblado ajeno **funciona**. No había ninguna corrida que lo demostrara.

### Lo que NO resuelve

**La objeción de tienda, entera.** Un DLL de la institución corriendo nativo es código que llega de
afuera. El trilema de `B7` no se movió: el híbrido es cargo con pantalla — mejora el peso y el dueño
único, no la política.

### ~~Un actor, no dos~~ — ✅ **CERRADO el 2026-09-11**

Las consultas rehidrataban un `ReadOnly` por llamada —un diseño que venía de A2/A3, donde una app
externa escribía la carpeta— y eso ponía **dos actores sobre el journal a la vez**: el
`FileNotFoundException: Temp file not found` que quedaba en la tabla.

Se cerró con **un solo actor vivo** (`static` bajo `lock`, compartido por las tres puertas) más
**`keepLoaded: true`** en el `InProcessShell` de la cáscara, porque el `Unload` es cooperativo y no
completa si el anterior dejó referencias vivas.

Y destapó el matiz que dio vuelta la conclusión: **la semilla es obligatoria**, porque es lo que
crea los globales `expediente` y `bitacora` que el DSL direcciona. Sin ella el bucle muere pidiendo
el cursor. El detalle está en `C9`.

### Lo que se agregó después, y también está corriendo

**Un icono de la institución en la pantalla de inicio** (`requestPinShortcut`), desde el detalle del
módulo: logo y nombre de la institución, intent explícito, y al tocarlo —con Gnothi cerrada— levanta
directo la cara del módulo. **No es exclusivo del híbrido**: sirve para cualquier módulo encendido.

Dos previsiones salieron al revés en MIUI, y la segunda abre una decisión de producto: **no mostró
diálogo de confirmación**, y **no estampa el badge de Gnothi**. Sin badge, en la pantalla de inicio
nada distingue ese icono del de una app nativa de la institución. Los números en `F1`.

### Pendiente, en orden

| qué | por qué |
|---|---|
| **latencia de `consultar` contra los 153 ms** de la puerta Binder | es el número que la lámina 04 necesita, y el cronómetro tiene que ser el mismo |
| **el badge que MIUI no pone** | decidir si se acepta o se compensa en otro lado — por ejemplo en la primera apertura desde el icono. **Es para el arquitecto**, no para la implementación |
| **`--pack` no conoce la tercera forma** | el híbrido se arma con `Compress-Archive` y pierde la inspección temprana; la del alta sí corre |
| **la guía para la institución habla de dos formas** | ahora son tres, con dos marcadores nuevos en el contrato (`[View]`, `[Deliver]`) y un verbo nuevo en el puente (`consultar`) — y los seis verbos actuales *«corren en cuatro bundles publicados»* |
| **el feed de NuGet devuelve 403** | todo se arma con `-Motor 3.0.0-beta.10029`; la rama pide la `10044` |
| **la pantalla de detalle por serie** | la cara no la tiene, y por eso sus tarjetas son `<article>` y no `<button>`, y el pedido del sexo es informativo y no editable |

### Deuda de método, otra vez

Cuatro de los defectos de este frente **solo aparecieron corriéndolo en el aparato** —`INTERNET` que
no viaja en Release, la semilla que crea los globales, una carrera de cableado, y el orden de los
argumentos—, y están en `F1` con su síntoma. Tres de los cuatro se leían como otra cosa: el de red
parecía una restricción de MIUI, y no lo era.

**Y una quinta, que es de comparación y no de aparato.** Se afirmó que la cara se parecía a la
pantalla original **sin haberlas puesto lado a lado**: se habían copiado los tokens de color, y de
ahí se dedujo el parecido. La estructura estaba inventada. **Copiar la paleta no es portar la
pantalla**, y el chequeo que faltaba —abrir el `.razor`— costaba dos minutos.

---

## P18 · Gnothi se baja de la tienda, al teléfono de la persona — y eso reordena el tablero · 2026-09-16

**La decisión, que faltaba y no estaba escrita en ningún lado:**

> *«el teléfono de la persona, bajado de la tienda»*

No es un detalle de despliegue. Es lo que decide **cuáles de las cuatro formas son viables**, porque
sideload, MDM y escritorio no tienen reglas de tienda y la tienda de consumidor sí.

Con los textos ya leídos y citados en [`B11`](../B-arquitectura/B11-lo-que-dicen-las-tiendas.md),
esto es lo que la decisión implica.

### El golpe, dicho sin rodeos

**Lo que corre hoy en el teléfono no es publicable por su forma.** Cuatro módulos en forma híbrida y
uno hospedado; la híbrida carga el DLL de la institución en el proceso de Gnothi, y eso da acceso
**directo** a las APIs de Android — justo lo que la excepción de Google Play no cubre, porque pide
acceso **indirecto**.

| forma | ¿pasa el texto de Play? | dónde sigue siendo legítima |
|---|---|---|
| 1 · cargo | no | escritorio (MSI, sin tienda) |
| 2 · hospedada | **sí** | teléfono y escritorio |
| 3 · híbrida | no | escritorio |
| 4 · Blazor en el Host (`F7`) | no | escritorio |
| caja de wasm (`F5`) | **sí, palabra por palabra** | todo |

**Las formas 1, 3 y 4 no se tiran: se mudan de plataforma.** En escritorio no hay tienda y la
objeción no existe. Lo que cambia es que **dejan de ser el futuro del teléfono**, y `F7` —que cerró
diciendo que la cuarta forma es *«la puerta más ancha de las cuatro»*— hay que leerlo con esto al
lado: es la más ancha **en escritorio**.

### Qué sube de prioridad

**`P10` (el sustrato viaja adentro del bundle) deja de ser un hueco anotado.** Con la hospedada como
única forma publicable en el teléfono, su costo —12,4 a 14,1 MB por módulo, de los cuales ~8 MB son
las copias `.gz`/`.br` del mismo motor— pasa a ser **el costo del producto**, no de una forma entre
varias.

Y hay una salida que el tablero no tenía anotada: **`BundleServer` es nuestro.** Hoy sirve el bundle
de cada módulo desde su propia carpeta; nada impide que sirva **un solo `_framework` compartido** y
que el bundle del módulo traiga únicamente sus propios DLLs. Eso:

- baja el disco de ~13 MB por módulo a la fracción que es el módulo (`examenes` mide 188 KB en
  proceso, para dar la escala);
- **le devuelve el sentido a `substrate.range`**, que hoy el manifiesto declara y nadie valida
  justamente porque no hay sustrato compartido que negociar;
- **no baja la memoria.** Cada WebView carga su propio runtime igual: los ~200 MB por módulo vivo
  siguen ahí, y con ellos la regla de un módulo vivo a la vez.
- **Riesgo conocido:** el trimming de Blazor es por app, así que la rebanada del BCL difiere entre
  módulos. Un `_framework` compartido tiene que ser uniforme, y eso lo hace más grande que
  cualquiera de los individuales. Hay que medirlo antes de prometerlo.

**`P8` (el módulo como `.wasm`) pasa a ser el camino crítico**, y por una razón nueva: es la única
arquitectura que baja la memoria **y** encaja en el texto de Google —*«a virtual machine or an
interpreter … indirect access»*— con una sola decisión. **Y ya no está bloqueado**: `P1` está cerrado
en el motor desde el 26 de agosto; lo único que falta de ese lado es subir de versión, porque la
mitad del guard no viajó en la `10029` con la que armamos todo. Ver `P1`.

### Lo que ninguna arquitectura arregla

Dos cosas de `B11` le pegan a Gnothi **como producto**, no a la forma del módulo, y sobreviven a
cualquier decisión técnica:

- **Apple §4.7.2** — no exponer APIs nativas al software que no viene en el binario sin permiso
  previo. El puente (`listar`, `leer`, `escribir`, `pull`) es exactamente eso, y le pega también a
  la hospedada y a la caja de wasm.
- **Apple §3.3.1(B)(c)** — no crear una tienda o escaparate para otras aplicaciones. El catálogo de
  módulos con su hoja, su firma y su actualización se le parece.

Esas dos sólo se contestan **preguntando**, no diseñando. Van a la sonda.

---

## P19 · La caja de wasm — la propuesta, con el porqué primero · 2026-09-16

**Qué se propone, en una línea:** que el dominio de la institución corra **adentro de una caja de
wasm que maneja Gnothi**, en vez de como un DLL suelto cargado en el proceso de Gnothi.

**Lo que NO cambia**, para que el alcance quede claro: la cara sigue siendo HTML/JS en el WebView,
las tres puertas del Host siguen igual, el journal sigue siendo del módulo. **Cambia una sola cosa:
quién ejecuta el C# de la institución y qué puede alcanzar desde ahí.**

### El porqué, en orden de peso

**1 · Porque hoy la contención es una promesa, y debería ser un mecanismo.**

Es la razón de fondo y no tiene nada que ver con las tiendas. `A1` dice que el dato es de la
persona y que las autoridades están repartidas. Pero el DLL de La Guaria corre **con los permisos de
Gnothi**: podría abrir la carpeta de otro módulo, podría salir a la red, podría leer el journal del
laboratorio. **Nada en el sistema se lo impide. Lo único que lo impide es que no lo hace.**

En la caja no es disciplina: **no tiene syscalls**. Si Gnothi no le pasa la carpeta, la carpeta no
existe para él. `D2` ya pide que la contención sea **falsable** —que se pueda intentar romperla y
verla fallar— y hoy no lo es: no hay nada que intentar, porque no hay cerca.

Esta razón es nuestra. Las otras dependen de reglas ajenas que pueden cambiar; ésta no.

**2 · ~~Sin esto no hay teléfono~~ → sin esto el teléfono sale caro. REBAJADO el 2026-09-16.**

Esta razón decía dos cosas de más, y las dos las corrigió un compañero el mismo día. **Se deja el
texto viejo tachado porque circuló como argumento.**

**Lo que decía:** que la caja era *«la única arquitectura que pasa el texto»* y que sin ella *«no hay
teléfono»*, con la hospedada costando *«~200 MB por módulo vivo»*.

**Lo que es cierto:**

- **No es la única que pasa.** Apple exime a **cualquier** intérprete desde junio de 2017, no sólo a
  WebKit — con precedente vivo en Roblox, que corre Lua en su propia VM y está publicado (`B11`).
  Contra el texto de Play, pasan la hospedada y la caja; lo que **no** pasa es cargar un DLL en el
  proceso, que es acceso directo a las APIs. **Un revisor de seguridad refuta la exclusividad en
  cinco minutos, y con razón.**
- **Y la hospedada no cuesta 200 MB por módulo vivo**: cuesta **+58,8 MB** en este teléfono, y un
  compañero midió **82 MB totales con 42 privados** para exámenes hospedado en un A54 y un emulador.
  Con esos números **la hospedada es cara pero vive**; con 200 MB no viviría. Es la diferencia entre
  «condición de existencia» y «la mejor mejora disponible».

**Lo que queda de esta razón, dicho bien:** entre las formas que **además** resuelven el tamaño
—cargo, híbrida, Blazor— **la caja es la única que pasa el texto de Play**. La hospedada pasa el
texto y **es** el problema de tamaño. Eso sigue siendo cierto y sigue siendo un motivo. **No es el
pilar: el pilar es la razón 1.**

**3 · Porque baja la memoria por módulo vivo — EN PIE, medido en el teléfono, con condiciones.**

**Esta razón se dio por caída y después se midió bien.** Se deja la historia entera porque las tres
versiones circularon:

- **se afirmaba sin medir** que la hospedada costaba *«~200 MB por módulo vivo»*. **Falso**: eran
  238,7 MB de base, **+58,8 MB el primer módulo** y entre +2,2 y +7,7 MB los módulos 2 a 5. Los
  199 MB eran el total con cuatro instalados.
- **se midió en la laptop** —+72 MB por instancia de memoria privada— y con eso se declaró **caída**.
- **se midió en el teléfono**, con `dumpsys meminfo`, que es el mismo instrumento con el que se midió
  la hospedada: **+~16 MB por instancia viva**, estable en cinco corridas.

- **y se precompiló el componente**, que era la condición que faltaba: **+29 MB por instancia viva y
  costo fijo ~0**.

| en el mismo teléfono, mismo instrumento | primer módulo vivo | vivos a la vez | costo fijo |
|---|---|---|---|
| hospedada | **+58,8 MB** | **uno**, por la regla de desalojo | — |
| caja, compilando en cada arranque | +~16 MB *(lectura sucia)* | tres | **+163 MB** |
| **caja, precompilada** | **+29 MB** | tres, y la serie es lineal | **~0** |

**Con el precompilado la caja gana desde el primer módulo**: la mitad de memoria por módulo vivo, sin
costo fijo y sin la regla de «uno vivo a la vez». `wasmtime_component_serialize` baja el arranque de
**1 109 ms a 4 ms** y el PSS con cero instancias de **249 MB a 80 MB**, porque el artefacto se
**mapea desde disco** en vez de generarse en memoria anónima.

**Y el ahorro se multiplica:** el artefacto es del **runtime**, no del módulo — `dotnet.wasm` es
idéntico para todas las instituciones. Son **11 MB en disco una vez, para todos los módulos**, que es
exactamente el sustrato compartido de `P10`.

*(El +16 MB de la serie anterior era una lectura sucia: midiendo con el componente recién compilado,
Android reclamaba páginas del código generado y el PSS oscilaba entre 243 y 321 MB. Con el
precompilado la serie sale monótona y repetible al megabyte: 80 · 113 · 142 · 171 MB.)*

La medición de la laptop no estaba mal: medía otra cosa —memoria privada en Windows contra PSS en
Android, con otro sistema de páginas compartidas—. **La que vale para decidir es la del teléfono,
porque el teléfono es el destino.** Todo en `F8`.

**4 · Porque le devuelve sentido a piezas que ya escribimos y hoy no valen nada.**

`substrate.range` está declarado en el manifiesto de los cinco módulos y **nadie lo valida** — no por
descuido, sino porque no hay sustrato compartido cuya versión negociar (`P10`). Con un motor del
Host, lo hay. Y el módulo vuelve a pesar lo que es: `examenes` mide **188 KB** en proceso contra los
13 MB de su bundle hospedado. Eso no es vanidad: es lo que hace que *«la institución te entrega un
módulo»* sea un acto plausible y no una descarga.

**5 · Porque quita el argumento que más caro nos sale defender.**

Toda conversación seria sobre Gnothi termina en el mismo punto: *«estás cargando código nativo de un
tercero en tu proceso»*. Lo dice la tienda, lo va a decir una revisión de seguridad, y lo va a decir
el área legal de la primera institución grande. Hoy la respuesta es *«confiamos en él»*. Con la caja
la respuesta es *«no puede»*.

### Y por qué ahora y no después

**Porque el contrato está a punto de crecer.** Hoy el Host encuentra los cinco marcadores por
reflexión sobre un `Type` de .NET. Cruzar a wasm obliga a declarar esa interfaz y serializar en el
borde — **y cada marcador nuevo que se agregue por reflexión encarece esa mudanza.** Ya hay cinco
instituciones publicando contra ese contrato; en seis meses habrá más y una versión del contrato que
mantener.

### Lo que costaría, dicho antes de empezar

- **Toca `Gnothi.Contract`**, que es la pieza más cara de cambiar: es lo que las instituciones ya
  compilan contra.
- **`wasi-wasm` es experimental en .NET.** No es lo mismo que el wasm del navegador, que sí está
  probado.
- **La latencia no está medida**, y en iOS no hay JIT: wasmtime correría interpretado o AOT.

### Los cuatro pasos, y qué mata cada uno

| paso | pregunta | dónde | costo |
|---|---|---|---|
| **0** | ¿arranca el motor bajo WASI con un módulo real? | laptop, sin Gnothi | 1 día |
| **1** | ¿se puede hablar con él sin reflexión? — una consulta cruzando el borde | laptop | 2-3 días |
| **2** | ¿la contención es falsable? — salir de la carpeta falla y queda registrado | laptop | 2 días |
| **3** | ¿cuánto cuesta? — memoria, tamaño, latencia contra los 153 ms del Binder | teléfono | por medir |

**Requisito del paso 0:** la versión del motor tiene que ser al menos `3.0.0-beta.10029-lector.4`.
La `10029` con la que armamos todo **no trae la mitad del guard de hilos** — ver `P1`.

**Cuándo se para:** si el paso 0 falla y la causa está en el motor, **ahí** sale un reporte, con una
corrida reproducible detrás. No antes: un reporte sin corrida es una molestia, no un hallazgo.

### Paso 0 · HECHO y en verde · 2026-09-16

**El motor arranca bajo WASI, y reproduce un journal real.** `ExamenesApp.Start()` completa en
wasmtime con `ProcessorCount=1`, y sobre el journal del fixture -89 entradas escritas por .NET
nativo en Windows- devuelve **exactamente la misma respuesta** que el nativo: 19 exámenes, 1 fuera
de rango, cursor 303. **2,2× más lento**, con un costo marginal de **0,6 ms por entrada**. Y el
módulo pesa **227 KB** contra los 12,4 MB de su bundle hospedado.

La evidencia completa, con las cuatro trampas que costó y lo que NO prueba, está en
[`F8`](F8-el-paso-0-de-la-caja-de-wasm.md). El spike, en `Spikes/Lab.WasiCaja`.

### Paso 1 · HECHO y en verde · 2026-09-16

**Gnothi le habla a un módulo que no cargó.** El lado de afuera (`Spikes/Lab.WasiCaja.Host`, 138
líneas) no declara UNA referencia ni menciona `Assembly`, `Type` o `MethodInfo`. El módulo declara
lo que ofrece —2 consultas, 7 vistas, entrega y cursor— y el Host llama por nombre.

**El hallazgo de diseño: la reflexión no desaparece, se muda adentro del módulo.** Hoy el Host mira
adentro del DLL ajeno; en la caja el módulo dice lo que ofrece y el Host le cree o no. Es el reparto
de autoridad de `A1` aplicado al único lugar donde no estaba.

**El borde cuesta ~1 ms** — contra los 153 ms de la puerta Binder que la cara híbrida ya cruza hoy.
Y **las tres puertas de `B8` siguen aislando**: una vista pedida por la puerta del asistente no se
deniega, no existe.

Costó dos cosas, y una es vieja conocida: **`stdout` es el protocolo y el motor no lo sabe** (541
bytes de ruido, la trampa de `E2` en sitio nuevo), y **serializar pasa a ser del módulo**.

**Sigue el paso 2: la contención falsable.**

### Paso 2 · HECHO · verde con dos huecos nombrados · 2026-09-16

**El mismo binario, dos anfitriones.** Un módulo que se porta mal —nueve intentos, ninguno un
ataque— corrido en proceso y adentro de la caja:

| intento | en proceso (hoy) | en la caja |
|---|---|---|
| leer/escribir la carpeta de **otro módulo** | **PUDO** | NO PUDO |
| listar el disco de la persona | **PUDO** · 31 entradas de `C:` | NO PUDO |
| salir a la red | **PUDO** · HTTP 200 | NO PUDO |

**Siete de ocho funcionan en proceso.** Hoy el módulo del supermercado puede leer el journal del
laboratorio, escribir en él y salir a Internet; lo único que lo impide es que no lo hace.

**Los dos huecos, con dueño:** `..` desde la carpeta concedida cae en el propio paquete del módulo
—porque el `dotnet.wasm` precompilado obliga a montarlo en `/`— y desde ahí **puede reescribir sus
propios DLLs**; y la red falló **por falta de hilos, no por política**, así que la contención de red
no está demostrada.

**Y la mitad que no se cumplió:** los intentos fallan pero **Gnothi no los ve**. wasmtime los niega
en silencio. El equivalente de `WebModule.Violations` —el watchdog de la CSP— necesita wasmtime como
librería embebida.

**Tres cosas distintas apuntan al mismo trabajo** y eso es lo más útil que dejó el paso: wasmtime
embebido cierra el hueco del paquete, quita el proceso por módulo y habilita el contador de
intentos. **Ése es el paso 3, antes que el teléfono.**

### Paso 3 · SONDADO, no construido · 2026-09-16

**Se puede, y no por el camino que parecía.**

- **`wasmtime-dotnet` 44.0.0 NO sirve**, medido: `expected a WebAssembly module but was given a
  WebAssembly component`. El .NET de WASI produce un **componente**, y el soporte de component model
  sigue siendo un issue **abierto** en esas bindings.
- **La C API de wasmtime v48 SÍ**, con las cinco funciones que hacen falta: `add_wasip2`,
  `add_wasi_http` (**separada**, o sea WASI sin red), `define_unknown_imports_as_traps`,
  `instance_add_func` y `instantiate`. El camino es **P/Invoke desde C#**, que además es el mismo
  que haría falta en Android.

**Y resuelve dos problemas de los pasos anteriores:** la red deja de depender de la falta de hilos
(`add_wasi_http` aparte + trampa en el import), y el borde deja de viajar por `stdout`
(`instance_add_func` da un canal propio) — que es la respuesta de verdad a la trampa de `E2`.

**Lo que falta es construirlo**, y es el trabajo más grande de `P19`: el envoltorio de P/Invoke
sobre `component.h`, el `.wasm` de un solo archivo (necesita el `wasi-sdk`) y el módulo como
*reactor* con funciones exportadas en vez de un `main`.

**Sin saber todavía:** si `dotnet.wasm` arranca como reactor sin `wasi:cli/run`, cuánta memoria
ocupa una instancia frente a los ~200 MB de un WebView, y cómo se porta wasmtime en Android e iOS,
donde no hay JIT.

### Condición del paso 3 · El borde y el contrato los PRESTA el Host · 2026-09-16

**Salió de una pregunta de un compañero —*«¿y Gnothi no se lo puede prestar a la caja?»*— y cambia
el diseño del reactor, así que va escrito antes de construirlo.**

**Por qué el préstamo de hoy no sirve tal cual.** Hoy funciona por el cargador de ensamblados: el
módulo referencia `Gnothi.Contract` con `ExcludeAssets="runtime"` —compila contra él y **no lo
empaqueta**—, y en ejecución el `AssemblyLoadContext` del módulo cae al contexto por omisión, **donde
ya está la copia del Host**. Una sola copia, un solo `Type`.

En la caja eso no existe. El módulo corre sobre **su propio runtime de .NET** (Mono compilado a
wasm), dentro de la memoria lineal; el CLR del Host está afuera. **No comparten cargador, ni sistema
de tipos, ni heap.** El DLL del Host no llega por ahí.

**Pero por el sistema de archivos sí, y queda mejor que hoy.** En la caja **Gnothi compone la carpeta
que el módulo ve**: de los 18 MB montados, sólo **227 KB** son de la institución — el runtime, el
BCL y el motor ya los pone Gnothi. Prestar el contrato es **un archivo más de 4,6 KB** en una carpeta
que Gnothi ya arma entera. `ExcludeAssets="runtime"` se queda como está.

**Y presta también el lector de marcas**, que es lo que importa. Si `ElBorde` viene de Gnothi,
entonces **el que reflexiona adentro de la caja es código de Gnothi**, leyendo los atributos reales
del DLL de la institución. El módulo no *declara* una lista: **se la extraen**, igual que hoy, sólo
que el extractor está adentro.

| pieza | de quién |
|---|---|
| las marcas sobre los métodos | **la institución**, en su código |
| el lector de marcas (`ElBorde`) | **Gnothi**, prestado |
| el paquete donde vive | **Gnothi**, compuesto y montado **de solo lectura** |

**Las dos condiciones, y una ya está medida:**

1. **El paquete montado de solo lectura** — `wasi_config_preopen_dir(..., fs_mutable: false)`,
   **medido en el paso 3**. Sin esto el módulo sustituye el `ElBorde.dll` de Gnothi por el suyo.
2. **El módulo como *reactor*, con las funciones exportadas puestas por el borde de Gnothi**, no como
   `command` con su propio `Main`. **No construido.** Ahora tiene una segunda razón para hacerse.

**Lo que esto corrige de lo dicho antes.** Se escribió que en la caja *«la comprobación se debilita
porque el Host compara contra lo que el módulo dice»*. **Con el préstamo eso es mucho menos cierto**:
el privilegio de mirar adentro no se pierde, **se muda adentro con el lector**. Lo que sigue siendo
irreducible es que el módulo puede mentir sobre **lo que hace**, no sobre lo que declara — y para eso
está la segunda mitad de la comprobación, *«y que conteste de verdad»*, que no depende de dónde esté
el lector.

**Y el estado real, que hay que decir:** en lo que **corrió**, `ElBorde` está compilado **dentro** del
módulo. Sin el préstamo, la declaración la produce código de la institución. **El spike de hoy está
del lado de creerle al módulo, no de leerlo.**

---

## P20 · El tablero de la tienda — qué falta de verdad para publicar · 2026-09-16

Con la decisión de `P18` tomada —**teléfono de la persona, bajado de la tienda**— y los textos
leídos en [`B11`](../B-arquitectura/B11-lo-que-dicen-las-tiendas.md), esto es lo que queda entre
Gnothi y una ficha publicada. **Siete bloqueantes, y no pesan lo que parecía.**

### La corrección que este tablero necesitó el mismo día

Su primera versión decía *«Android ya está desbloqueado hoy: la forma hospedada pasa el texto de
Play»*, y de ahí sacaba que publicar hospedado era un paso del plan. **Es cierto sobre la regla e
inútil como plan, porque se olvida del problema que originó toda esta investigación.**

**La forma hospedada ES el problema.** Pesa 12,4–14,1 MB por módulo y se lleva ~200 MB de memoria
por módulo vivo, tanto que «un módulo vivo a la vez» dejó de ser precaución y pasó a ser requisito.
La forma **híbrida** (74 KB) y la de **Blazor en el Host** (227 KB) se construyeron exactamente para
escapar de eso. Publicar hospedado no resuelve nada: publica lo que veníamos rechazando.

**Son DOS restricciones a la vez, y hay que satisfacer las dos.**

| forma | disco por módulo | memoria por módulo | ¿pasa Play? | Apple §4.7.2 |
|---|---|---|---|---|
| **hospedada** | **12,4 MB** | **+58,8 MB** por módulo vivo, y **uno vivo a la vez** | sí | no |
| híbrida | 74 KB | comparte el motor del Host | **no** | no |
| Blazor en el Host | 227 KB | comparte el motor del Host | **no** | no |
| hospedada con **motor compartido** (`P10`) | ~300 KB *(sin medir)* | igual que hospedada | sí | no |
| **caja de wasm** (`P19`) | **227 KB** + su cara (~240 KB) | **+29 MB** por instancia viva, costo fijo **~0** con el componente precompilado | **sí** | no, pero con el mejor argumento |

**Todo lo de esta tabla está medido en el mismo teléfono con el mismo instrumento** (`dumpsys
meminfo`), el 2026-09-16. La columna de memoria se reescribió **dos veces** ese día: primero decía
«~200 MB por módulo vivo» para la hospedada, que era el total con cuatro instalados; después una
medición en la laptop dio +72 MB por instancia y pareció hundir a la caja. La buena es ésta.

**Lectura, con el componente precompilado — medido, no proyectado.** La caja gana **desde el primer
módulo**: **+29 MB por módulo vivo contra +58,8 MB**, costo fijo ~0, y **sin la regla de «uno vivo a
la vez»**. `wasmtime_component_serialize` baja el arranque de 1 109 ms a **4 ms** y el PSS con cero
instancias de 249 MB a **80 MB**, porque el artefacto se **mapea desde disco**.

Y el artefacto es del **runtime**, no del módulo: **11 MB en disco una vez para todas las
instituciones**, que es el sustrato compartido que `P10` pedía.

**La caja de wasm es la única que satisface las dos.** No es «lo que hace bueno el producto después
de publicar»: es **la única forma que resuelve el problema original Y pasa la regla de la tienda**.
Por eso es el camino crítico, y por eso `P19` va primero y no cuarto.

**El motor compartido (`P10`) sigue valiendo como paso intermedio**, y hay que ser honesto sobre qué
compra: baja el **disco** de 12,4 MB a la fracción que es el módulo, mantiene la posición frente a
Play, y **no toca la memoria** — cada WebView carga su propio runtime igual. Es mucho más barato que
la caja y arregla la mitad del problema.

**Y el §4.7.2 de Apple es ortogonal:** le pega a las cinco filas. Ninguna decisión técnica lo mueve,
es lo único que bloquea todo en iOS, y tarda semanas de calendario. Por eso preguntar arranca **en
paralelo**, no después.

### Los siete, con dueño

| | bloqueante | tienda | dueño | qué lo cierra | estado |
|---|---|---|---|---|---|
| **T1** | código nativo de un tercero corriendo en el proceso | Play · Apple 2.5.2 | **nosotros** | la caja de wasm (`P19`) — o publicar hospedado | **Play: resuelto hoy con la hospedada.** La caja: sondada, falta construir |
| **T2** | **§4.7.2** — exponer APIs nativas al software que no viene en el binario | Apple | **Apple** | permiso previo, o argumentar que el puente no expone APIs nativas | **sin preguntar** — es el bloqueante real de iOS |
| **T3** | **§3.3.1(B)(c)** — no crear una tienda o escaparate | Apple | producto | no tener catálogo adentro | **ya estamos del lado bueno** (verificado hoy) |
| **T4** | **§4.7.4** — índice del software con universal links | Apple | nosotros | chico | a medias |
| **T5** | **§4.7.5** — control de edad | Apple | nosotros | chico | no existe |
| **T6** | iOS no tiene JIT | técnico | nosotros | wasmtime interpretado (Pulley) | sin medir, y sólo importa si se va a la caja |
| **T7** | §2.5.2 es más estricto que §3.3.1(B) y es lo que aplica el revisor | Apple | **Apple** | preguntar | **sin preguntar** |

### T3 · La buena noticia, y hay que protegerla

**Gnothi no tiene catálogo, y eso ya es cierto hoy.** Verificado en `Gnothi.Mobile/Assets/ui.html`:
el botón `+ Agregar` abre **tres vías y ninguna ofrece nada**:

- **escanear el QR** de la página de la institución,
- **pegar el enlace** que le llegó por correo o WhatsApp,
- **elegir archivos** de su propio teléfono.

La única lista que existe —*«Esto encontré en lo que elegiste»*— enumera los módulos hallados
**entre archivos que la persona ya eligió**, no una oferta. **No hay directorio, no hay búsqueda, no
hay listado servido por red.** La institución le entrega el enlace a la persona; Gnothi instala lo
que la persona trae.

**Contra el texto de Apple —*«does not create a store or storefront for other Applications»*— esa es
la mejor posición posible, y se tiene sin hacer nada.** Lo que hay que hacer es **no perderla**:

> **REGLA DE PRODUCTO: Gnothi no lleva catálogo de módulos.** Ni lista de instituciones disponibles,
> ni búsqueda, ni destacados, ni «también te puede interesar». El descubrimiento es de la
> institución, afuera. El día que alguien proponga una pantalla de «explorar módulos», esta línea es
> la razón por la que no.

### T2 · El bloqueante real, y lo que se puede argumentar

> **4.7.2** Your app may not extend or expose native platform APIs or technologies to the software
> without prior permission from Apple.

El puente de Gnothi expone `listar`, `leer`, `escribir` y `pull`. Hay que ir con un argumento, no con
una negación:

- **`pull` ya está del lado correcto.** El módulo **no** sale a la red: le pide a Gnothi que traiga,
  y Gnothi trae. No se le expone ninguna API de red — se le entrega un resultado.
- **`listar`/`leer`/`escribir` no son APIs de plataforma: son el contenedor de Gnothi.** No hay
  selector de carpetas, no hay SAF, no hay acceso al sistema de archivos del aparato. Es una carpeta
  que Gnothi creó, dentro de su propio contenedor, y que concede por módulo.
- **Y en la caja de wasm el argumento es más fuerte todavía**, porque deja de ser una promesa: el
  módulo importa las funciones que Gnothi define y **no tiene syscalls** (`F8`, paso 2 — medido).
- **El §4.7.3 lo cumplimos de sobra** y es lo que se pone al lado: consentimiento por módulo y por
  consulta, carpeta concedida por módulo, y la consulta apagada **desaparece** en vez de denegarse.

**Eso no lo decide nadie de este lado.** Va en las notas de revisión y por «Contact App Review».

### El orden que propone este tablero

**En paralelo, porque tienen dueños distintos y uno de los dos no depende de nosotros:**

**Camino A — construir (nosotros)**

1. **La caja de wasm, `P19` paso 3.** Es el camino crítico: la única forma que baja el módulo a
   227 KB **y** pasa Play. El envoltorio de P/Invoke sobre `component.h`, el `.wasm` de un solo
   archivo y el módulo como *reactor*.
2. ~~Medir la memoria de una instancia~~ **— HECHO en el teléfono el 2026-09-16: +~16 MB por
   instancia viva, contra +58,8 MB de un módulo hospedado.** La caja cuesta la cuarta parte por
   módulo vivo y permite varios. **Pero paga 163 MB para compilar el componente**, así que con UN
   módulo vivo la hospedada todavía gana. Lo siguiente es **precompilar el componente**
   (`wasmtime_component_serialize`): si eso baja el costo fijo, la caja gana desde el primer módulo.
3. **`T4` y `T5`**, chicos, desactivan dos renglones del 4.7 sin discusión.
4. **`P10`, el motor compartido**, si la caja se demora: baja el disco sin tocar la memoria, y es
   mucho más barato.
5. **`T6`**, wasmtime sin JIT — **en Android ya está contestado**: Cranelift compiló 12 MB de wasm
   a arm64 en 1,2 s y lo ejecutó. Queda **iOS**, que es donde no hay JIT.

**Camino B — preguntar (Apple)**

1. **`T2` (§4.7.2) y `T7` (§2.5.2 contra §3.3.1(B))**, con el argumento del puente y el §4.7.3 al
   lado. Tarda semanas de calendario y **bloquea iOS entero pase lo que pase con el camino A**.
2. **La regla del catálogo escrita**, para que `T3` siga ganado el día que alguien diseñe la
   pantalla que lo rompe. *(Hecha, acá arriba.)*

**Lo que NO está en el plan:** publicar Android hospedado. Pasa la regla y publica los 12,4 MB y los
200 MB que esta investigación existe para eliminar.

---

## P21 · Una sola línea base de memoria, antes de seguir comparando · 2026-09-16

**Lo pidió un compañero y tiene toda la razón:** el paso 3 de `P19` promete comparar la caja *«contra
los ~200 MB de hoy»*, y ese número estaba inflado. Si la línea base está inflada, la comparación
también.

### Las tres cifras que existen hoy para lo mismo

| quién | aparato | qué midió | número |
|---|---|---|---|
| nosotros | Xiaomi 2407FPN8EG | PSS total del proceso, `dumpsys meminfo`, delta al abrir el primer módulo hospedado | **+58,8 MB** |
| un compañero | Galaxy A54 + emulador | exámenes hospedado | **82 MB totales, 42 privados** |
| un compañero | — | cada Blazor abierto | **~45 MB** |
| nosotros | Xiaomi 2407FPN8EG | PSS total, delta por instancia de la caja de wasm | **+~16 MB** |

**Las tres primeras pueden ser la misma cosa contada distinto** —PSS incluye la parte proporcional de
lo compartido, «privado» no; y no está claro si alguna incluye el proceso del WebView, que en Android
es **otro proceso**—. Hasta saberlo, **ninguna comparación entre ellas vale**.

### El protocolo que hace falta acordar

Antes de seguir midiendo, las cuatro decisiones que hacen comparables dos números:

1. **Qué métrica.** Propuesta: **TOTAL PSS de `dumpsys meminfo`**, porque es lo que el sistema usa
   para decidir a quién mata y porque es la que ya tienen las mediciones viejas.
2. **Qué procesos entran.** En Android el WebView corre en un proceso aparte: **hay que sumar el del
   módulo**, o la hospedada sale barata por contabilidad. `dumpsys meminfo <paquete>` no lo trae.
3. **En qué momento.** Propuesta: proceso **fresco por cada N**, con un `GC` y unos segundos de
   asiento, y **tres repeticiones** — se midió que con tres instancias el PSS oscila entre 243 y
   321 MB según cuándo se mire.
4. **Contra qué cero.** La app vacía, o la app con el motor ya compilado. Son dos preguntas
   distintas y dan respuestas opuestas: la caja paga **163 MB una vez** y **16 MB por módulo**.

### Y una advertencia que ya se pagó hoy

La misma caja midió **+72 MB por instancia en la laptop** (memoria privada, Windows) y **+16 MB en el
teléfono** (PSS, Android). Las dos mediciones eran correctas. **Con la de la laptop se declaró caída
una razón entera de `P19`, y con la del teléfono se la volvió a levantar, el mismo día.**

**Ningún número de memoria vale sin decir con qué instrumento y en qué aparato se tomó.**

---

## P22 · El borde con argumentos, cerrado — y los tres defectos que destapó · 2026-09-16

**Cerrado.** `ElBorde.Llamar`/`Ligar` ligan los parámetros desde el sobre `{"args":…,"accepts":…}`
que arma el Host, con `accepts` leído **antes** que los argumentos —la misma regla que
`InProcessShell.Bind`—. Verificado en el teléfono contra el journal real: **la pantalla en caja quedó
igual a la híbrida, tarjeta por tarjeta** (`MERECE ATENCIÓN` pasó de 8 a 9, y volvieron la cabecera
y el «17 de tus 18 valores»). Detalle en F8 y C10.

### Lo que se arregló de paso, y es la misma lección dos veces

| | comparaba | mentía porque | ahora |
|---|---|---|---|
| `CajaDeWasm` | nada: el `.cwasm` no estaba atado a su fuente | un wasm nuevo con un precompilado viejo y sano corría **el código anterior** | el artefacto se llama `sustrato-<huella16>.cwasm` |
| `LaCaja` | **el largo** del asset del APK | el build nuevo salió con **los mismos 6 583 761 bytes** | huella SHA-256 escrita al lado de la copia |

**El largo no identifica un contenido.** Se arregló en `CajaDeWasm` y se volvió a cometer en
`LaCaja` el mismo día, un eslabón más arriba de la misma cadena. Costó una corrida entera.

### Dos cosas que parecían de la caja y no lo son

**1 · `Color de la orina` y `Color de las heces` pintan `undefined`.** Se comprobó apagando la caja:
**en híbrida sale idéntico**. `cara.js:264` tiene dos ramas —`presencia` y `hechos`— y la vista
`tendencia` devuelve tres formas: la tercera, «categoría en el tiempo», trae `constancia` y no tiene
rama. El valor existe (`constancia.ultimaCategoria` = `"ámbar"`). **Arreglado el mismo día.** `tarjetaAtencion` tiene ahora la tercera rama: dibuja la tira sobre
`observaciones` -- marcando «se salió de lo esperable» en vez de «positivo» -- y la cifra sale de
`constancia.ultimaCategoria`. Verificado en el teléfono en las DOS formas: «Color de la orina →
ámbar», «Color de las heces → marrón», con su «4 de 4 dentro de lo esperable». Detalle en E4 §2.

Y al empaquetarlo apareció otro, que es de la cadena y no de la cara: el arreglo del «212 → 212»
estaba escrito desde el 14 y **nunca se había armado el bundle**, así que se seguía viendo en el
teléfono el 16. Va como E4 §7.

**2 · `no se pudo traer lo nuevo: Connection refused (127.0.0.1:8095)`** al pie de la pantalla. Es el
**cartero**, que es de Gnothi, y faltaba el túnel al publicador de prueba. Al runbook:

```
adb reverse tcp:8095 tcp:8095   # el publicador, para el cartero
adb reverse tcp:8096 tcp:8096   # la pagina de la institucion, para el alta
```

Y vale distinguirlo de su vecino, porque se leen parecido y no son lo mismo:
`Permission denied (127.0.0.1:8095)` es **INTERNET sin declarar** en el manifiesto —documentado en
`MainActivity`—; `Connection refused` es que el socket abrió bien y **no había nadie** del otro lado.

### Lo que sigue en la caja, por prioridad

**`1` Separar el sustrato del módulo — y esto subió a primero por un filo que apareció el
2026-09-16.** El `.wasm` lleva exámenes adentro, y el `.gmod` **también** trae sus DLL, que en modo
caja **no se usan nunca**: `Cage.Build` monta un solo preopen y el paquete no se monta. O sea que hay
**dos copias del código del módulo** y la que corre es la del wasm.

> **Actualizar el módulo NO actualiza lo que corre en la caja.** Ese día se pasó exámenes de 3.9.0 a
> 3.10.0 y la caja siguió ejecutando la copia del wasm. No se notó porque lo que cambió fue la cara,
> que sí viaja en el bundle; **si hubiera cambiado el dominio, la caja habría corrido el viejo**.

Es la misma familia que los cuatro defectos del día: dos copias, una comparación que no se hace, y
falla en silencio. Hoy está contenido porque hay **un** módulo en caja y quien toque su dominio
rearma el wasm a mano — que no es una salvaguarda, es alguien acordándose. Separarlo deja **una sola
copia** y la pregunta desaparece. Toca `PackageInspector`, `ModuleEnrollment` y el alta.

`2` **el canario adentro de Gnothi** — escrito y compilando (`Cage.cs`, `Canary.cs`, la compuerta en
`WasmBox.Open`); falta correrlo en el teléfono en las dos condiciones · `3` **iOS**, sin probar ·
`4` **cuánto cuesta pintar la pantalla** — bajado de prioridad, el número no existe y conviene que se
sepa que no existe.

## P23 · Los dos primeros de la caja, cerrados en el teléfono · 2026-09-17

**`1` y `2` de la lista de P22 quedaron cerrados en la misma corrida**, porque van en el mismo
arranque.

**El sustrato ya no lleva el módulo adentro.** `Gnothi.Mobile/Assets/sustrato.wasm` es el runtime de
.NET, el BCL, el motor y el borde —uno para todos— y exámenes entra desde su bundle por un segundo
preopen, `/modulo`, montado de **sólo lectura**. En el teléfono abrió en 34 ms y la cara pintó igual
que en híbrida. **Con eso se va el filo**: ya no hay dos copias del dominio, y actualizar el módulo
actualiza lo que corre.

**El canario cantó en el aparato, en las dos condiciones.** Normal: 8 de 8 fugas cerradas, 2 de 2
puertas abiertas. Con la jaula mal armada a propósito (`--ez jaulamala true`): la vio, nombró las dos
fugas que se abrían y **cerró la caja**; y el canario normal siguió funcionando después, porque la
prueba de control no toca el veredicto real. Eso es lo que separa un canario que funciona de uno que
siempre dice «cerrada»: hasta que no se comprueba que **ve** una jaula rota, los dos se leen igual.

**Y el canario encontró algo que no buscaba.** El listado que imprime para probar la fuga mostró dos
artefactos precompilados del esquema de nombres anterior, que el barrido no borraba por buscar
`sustrato-*` con guion. Corregido a `sustrato*`. **Tercera vez en este set que una comparación
escrita contra el estado de ese día deja de servir el día que el estado cambia** — el largo que
mentía, el artefacto viejo que corría, el barrido que no barría.

Y se verificó con el mismo instrumento: se reinstaló, exámenes logueó `artefacto viejo borrado
(sustrato.cwasm)`, y la jaula mala volvió a listar la raíz — **de 15 renglones a 13**. El listado que
prueba la contención sirve además de inventario de la carpeta privada.

### Una piedra del día, y no es del código

El build quedó bloqueado un rato: el feed privado devolvió **403** y `Ncubo.Puppeteer
3.0.0-beta.10044` ya no estaba en la caché local. Dos cosas que conviene dejar escritas porque
cuestan tiempo y no se deducen del mensaje:

- **No hay proveedor de credenciales de Azure Artifacts instalado** (`~/.nuget/plugins` no existe).
  El `nuget.config` ya avisa que la autenticación va por ahí; mientras el paquete esté en caché no se
  nota, y el día que no está, sí.
- **Un restore fallido pisa `obj/project.assets.json`.** Queda un stub de 5 KB, así que después del
  403 tampoco funciona `--no-restore`: se pierden las dos salidas de una vez.

### Lo que queda de la lista

`3` **iOS** — no hay JIT, wasmtime tendría que ir interpretado; sin probar. · `4` **cuánto cuesta
pintar la pantalla entera contra la híbrida** — bajado de prioridad, el número no existe y conviene
que se sepa que no existe.
