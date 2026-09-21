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

### P1 · El guard de hilos por capacidad — **y no es código nuestro**

Cambiar la pregunta «¿estoy en un browser?» por una pregunta sobre **capacidad del hospedaje**. Un flag
inyectable, o el conteo de procesadores como default con override explícito del Host.

**Por qué primero.** Bloquea toda la etapa de WASI: sin esto, un módulo con el motor no arranca fuera del
browser. Y es barato ahora — si entra con forma de plataforma, cada hospedaje nuevo agrega una condición más
a esa línea.

**Criterio:** el replay secuencial se activa en un hospedaje sin hilos que **no** sea el browser.

**Va reportado, no parcheado.**

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
interpreter … indirect access»*— con una sola decisión. Sigue bloqueado por `P1`, que no es código
nuestro.

### Lo que ninguna arquitectura arregla

Dos cosas de `B11` le pegan a Gnothi **como producto**, no a la forma del módulo, y sobreviven a
cualquier decisión técnica:

- **Apple §4.7.2** — no exponer APIs nativas al software que no viene en el binario sin permiso
  previo. El puente (`listar`, `leer`, `escribir`, `pull`) es exactamente eso, y le pega también a
  la hospedada y a la caja de wasm.
- **Apple §3.3.1(B)(c)** — no crear una tienda o escaparate para otras aplicaciones. El catálogo de
  módulos con su hoja, su firma y su actualización se le parece.

Esas dos sólo se contestan **preguntando**, no diseñando. Van a la sonda.
