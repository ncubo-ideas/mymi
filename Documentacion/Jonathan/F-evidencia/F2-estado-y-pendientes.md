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

**Es una propuesta para quien mantiene el motor**, en un worktree, sin commit ni merge ni push.

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
