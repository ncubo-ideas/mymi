# F1 · Lo medido, con cómo reproducirlo

**Regla del proyecto:** un claim inflado que se cae se lleva puestos a los que sí aguantan. Si algo no está
en la primera tabla, decilo como hipótesis.

---

## El eslabón de agregación

| ✅ medido | evidencia |
|---|---|
| El asistente ve **una** lista con consultas de dos orígenes | tres consultas: dos del Host, una del manifiesto de un módulo |
| El catálogo se arma **sin arrancar el módulo** | lo que publica sale en el arranque, antes de que exista el proceso |
| El módulo ve al Host como **su** cliente MCP | `Server (app1-facturas 0.1.0), Client (Gnothi.Host 1.0.0.0) method 'tools/call'` |
| Arranque perezoso | `levantado: false` entre consultas — el chat de la app lo leyó y lo explicó solo |
| La respuesta pasa entera y sin tocar | `{"total":27991.5,"cantidadDeFacturas":4,"ultimoEventoLeido":104}` |
| Apagar **desaparece**, no deniega | el listado baja a dos; la llamada da `-32602 Unknown tool` |
| Lo declarado se verifica contra lo real | `coincide: true, declaradas: [...], reales: [...]` |
| El tamaño lo mide el Host **desde afuera** | `bytesDelRegistro: 1774`, por `stat` sobre la carpeta concedida |
| **Tres superficies distintas dan el mismo dato** | CLI del asistente · chat de la app · cliente propio |
| **El mismo mecanismo, con otro autor y otro dato** | compras / Super La Guaria, «resumen» → `{"hayDatos":true,"totalGastado":3174606,"cantidadDeCompras":87,"ultimoEventoLeido":387}` — Ver `C7` |

**Cómo reproducirlo:** `probar.ps1`, o `Gnothi.Prueba`, o el escenario en Docker. Ver la capa D.

## El confinamiento en Docker

| ✅ medido | resultado |
|---|---|
| El módulo **no tiene red** | única interfaz `lo`, sin DNS |
| **No puede escribir** fuera de su carpeta | falla en `/`, `/app`, `/etc`, `/usr`, `/journal-origen`; sí en `/journal` |
| **Cero capacidades del kernel** | `CapEff: 0000000000000000` |
| El journal real **no se toca** | mismo md5 y misma fecha después de N corridas |

```bash
docker compose run --rm --entrypoint /bin/sh gnothi -c 'ls /sys/class/net; touch /app/x; touch /journal/x; grep CapEff /proc/self/status'
```

## El motor

| ✅ medido | evidencia |
|---|---|
| **10029 ya trae el replay secuencial** | `10027` → 0 referencias al guard; `10029` → 1 |
| **La carátula wasm arranca con el paquete** | su total, sin `Cannot wait on monitors` |
| El paquete adelgazó | declara dos dependencias: un analizador y una librería de hashing |

## El ciclo completo de datos

| ✅ medido | evidencia |
|---|---|
| El proveedor solo expone su narrativa | un verbo de lectura, y el señuelo devuelve **403** con el token de la app |
| El asistente **no puede** disparar el refresh | después de inyectar, siguió dando el total viejo y pidió abrir la app |
| El número se mueve con el Refresh | 19.321,50 → **27.991,50**, checkpoint 103 → 104 |
| **El proveedor olvida, el journal no** | tras un reinicio, el mock perdió el evento y el journal lo conservó |

## La introspección del motor

| ✅ medido | evidencia |
|---|---|
| `IActorIntrospection` existe, con **nueve verbos** | `ShowEntry`, `ShowAction`, `ShowSymbols`, `ShowSymbol`, `ShowClass`, `ShowReactions`, `ShowReaction`, `FindPattern`, `ShowParameterPools` |
| Es **intrínseca**, no del dominio | *«every actor exposes by virtue of being Puppeteer»* |
| Es **solo lectura por contrato** | *«writing goes through the invocation surface, never through here»* |
| **El catálogo de escritura ES el journal** | tres `define action` con su firma canónica en el DSL |
| Hay un CLI del motor, **AI-first** | *«If you are Claude…: run `describe` first. That is your onboarding contract»* — *«always current, derived from runtime, not docs»* |
| Existe aislamiento por shadow | una sesión en memoria contra un journal primario, que se descarta al salir |

```bash
puppeteer show entry 2 --journal "path=<carpeta>" --actor-name <actor>
```

## Los tamaños, con la advertencia al lado

Medido sobre el conjunto que declara el manifiesto de arranque — **lo que el browser realmente baja**:

| build del motor | por la red (gz) | descargado | assemblies |
|---|---|---|---|
| ProjectReference al árbol viejo | 14,79 MB | 37,42 MB | 237 |
| **NuGet `3.0.0-beta.10029`** | **~10 MB** | ~26 MB | **199** |

De qué está hecho el peso del primero:

| | por la red | archivos |
|---|---|---|
| BCL + framework de UI | **7,32 MB** | 194 |
| **base de datos / nube / cripto** | **4,38 MB** | 17 |
| runtime + globalización | 2,12 MB | 6 |
| otros terceros | 0,54 MB | 14 |
| **el motor** | **0,38 MB** | 2 |
| **el código de la app** | **0,04 MB** | 4 |

**El código propio son 40 KB de 14,79 MB. El 0,27%.** Todo lo demás es sustrato.

Y el assembly más grande de toda la app no era el runtime: era una librería de criptografía de **4,68 MB
crudos**, arrastrada por dependencias del motor que en un browser no se ejecutan jamás. El paquete ya no las
trae.

> ⚠ **Debug-ish, sin trimming.** La especificación pide la que falta: **Release trimmed** contra el árbol
> actual. Y advierte que probablemente **degrade el argumento de tamaño y ascienda el de auditoría** — que
> es mejor, porque no envejece con el próximo release del trimmer. **No citar estos números afuera como si
> fueran el peso final.**

## El bundle instalable

| ✅ medido | evidencia |
|---|---|
| El manifiesto valida | «todos los requisitos cumplidos», con autor, versión y licencia leídos |
| El bundle extraído funciona | corrido desde un directorio limpio: 3 consultas y el total correcto |
| El ejecutable autocontenido anda solo | 70 MB de exe, 30 MB comprimido |

> El `.mcpb` y el exe autocontenido **dejaron de ser la forma de entrega**: hoy la persona
> baja **dos MSI** —uno de Gnothi, otro del comercio— y esos 70 MB quedan como lo que **pesa
> el binario**, no lo que instala la persona. Ver `C7`.

---

## La carpeta que la PWA eligió, leída con `System.IO` — 2026-09-08

**Qué se fue a contestar.** La opción A del diagrama quiere que la PWA viva **afuera** de Gnothi,
elija su carpeta con el picker del navegador —que en Chrome Android ≥132 es el SAF— y que un
módulo `.gmod` **adentro** de Gnothi lea ese mismo journal. Para eso el motor tiene que poder
abrir esa carpeta, y `ConfigureStorage(DatabaseType.FileSystem, "path=…")` **solo acepta una
ruta**: hace `System.IO` sobre ella. Nada de content-URIs.

Instrumento: `Gnothi.Mobile/SondaSaf.cs`, emulador `sdk_gphone64_x86_64` (Android 16 / SDK 36),
app con `targetSdk=35` y **sin ningún permiso de almacenamiento declarado**, que es como está hoy.

| paso | resultado |
|---|---|
| concesión de árbol (`ACTION_OPEN_DOCUMENT_TREE` + `takePersistableUriPermission`) | ✅ tomada, lectura y escritura |
| **contraste:** leer por `ContentResolver`, que es la vía del SAF | ✅ 2 archivos, y los 28 bytes correctos de `journal_000001.bin` |
| ruta deducida del tree URI (`primary:Documents/sonda-saf`) | `/storage/emulated/0/Documents/sonda-saf` |
| `Directory.Exists` | **True** |
| `Directory.EnumerateFiles` | **0 archivos, y SIN excepción** |
| `File.Exists` | **True** |
| `File.OpenRead` | ❌ `UnauthorizedAccessException: Access to the path … is denied` |

**El contraste es la mitad que vuelve esto concluyente.** Sin él, un `EnumerateFiles` vacío se
lee como «elegí la carpeta equivocada». Con él queda claro que la carpeta es la correcta, que la
concesión está viva y que los bytes están ahí.

**Y el modo de fallar es el peor posible: miente en silencio.** `Directory.Exists` y `File.Exists`
dan **True** —el `stat` sí pasa— y `EnumerateFiles` devuelve **cero archivos sin lanzar nada**. O
sea que un motor apuntado a esa ruta **no vería un error: vería un journal vacío**, y lo lógico
sería que arrancara uno nuevo. La historia de la persona seguiría intacta al lado, invisible.

### La segunda mitad, que es la que explica el porqué

Con `MANEJAR TODOS LOS ARCHIVOS` concedido (`appops set … MANAGE_EXTERNAL_STORAGE allow`), en el
mismo aparato y la misma carpeta: `EnumerateFiles` → **2 archivos**, `File.OpenRead` → **28 bytes
correctos**. Existe una puerta, y funciona.

**Entonces lo que bloquea NO es la falta de permiso: es que se le dio el permiso a otra API.** La
concesión del SAF vive en la tabla de permisos del `ContentResolver`, indexada **por URI**. La
llamada `open()` a la que `System.IO` termina bajando pasa por la capa de scoped storage del
sistema de archivos, y **esa capa nunca consulta esa tabla**. Son dos mundos, y el permiso no
cruza.

**Y esa puerta no se puede tomar**, por razones que no son técnicas: le daría a Gnothi lectura de
**todo** el almacenamiento compartido de la persona —sus fotos, sus descargas, los journals de
instituciones que ella no le concedió—, que es lo contrario de la tesis; y Google Play lo
restringe a gestores de archivos y apps de respaldo. Está medida para que nadie tenga que volver
a preguntarlo, no para usarla.

**Cómo se reproduce:**

```bash
adb shell mkdir -p /sdcard/Documents/sonda-saf
adb shell "printf 'PUPPETEER-JOURNAL-SONDA-0001' > /sdcard/Documents/sonda-saf/journal_000001.bin"
adb shell am start -n com.ncubo.gnothi.host/com.ncubo.gnothi.host.SondaSaf
adb logcat -s SONDA:I
```

Se elige `Documents/sonda-saf` en el picker y se acepta. Para la segunda mitad hay que
descomentar el `UsesPermission` que está en la cabecera de `SondaSaf.cs`, reinstalar y correr el
`appops`.

### Y la tercera mitad: el descriptor. **SAF sí da un `FileStream` con seek**

Los pasos 3 y 4 dicen que `System.IO` **sobre la ruta** no llega. Pero el motor no necesita una
ruta: necesita hacer seek, leer del medio y agregar al final. Así que se midió si SAF puede dar un
descriptor de archivo de verdad. **Puede, y las cuatro cosas que el motor hace funcionan:**

```
✅ 5a  ParcelFileDescriptor abierto en «rw», fd POSIX = 107
       CanSeek=True  CanRead=True  CanWrite=True  Length=28
✅ 5b  Seek(14) + Read → 12 bytes: «NAL-SONDA-00»
✅ 5c  Write al final + Flush: 28 → 37 bytes
✅ 5d  creado y escrito journal_000002.bin (15 bytes) — seek=True
```

El puente es `ContentResolver.OpenFileDescriptor(uri, "rw")` → `pfd.Fd` → un
`SafeFileHandle` → un `FileStream` normal. Y crear archivos nuevos —que el motor necesita para
rolar journals y mantener `meta.bin`— va por `DocumentsContract.CreateDocument`.

**Verificado desde afuera de la app**, con `adb shell`, que es lo que descarta que el flush haya
mentido:

```
-rw-rw---- media_rw 37 journal_000001.bin   →  PUPPETEER-JOURNAL-SONDA-0001|EVENTO-2
-rw-rw---- media_rw 15 journal_000002.bin   →  JOURNAL-2-NUEVO
```

### Y leyendo el motor: no hay `mmap`, pero el pedido NO es chico

**Corrección.** La primera lectura de esta medición fue *«entonces alcanza con que el motor acepte
un `FileStream` ya abierto»*. **Eso está mal**, y se ve leyendo `C:\…\repos\Puppeteer` (master).

**Lo que sí se contesta, y era la duda concreta:** `MemoryMappedFile` **no aparece ni una vez** en
todo el motor. **No hace `mmap`.** Ese miedo queda descartado.

**Pero el motor no es cliente de un ARCHIVO: es cliente de un SISTEMA DE ARCHIVOS.** Del backend
`DatabaseType.FileSystem` —unas **6 000 líneas** entre `DiaryStorageFileSystem.cs` y
`EventSourcing/DB/FileSystem/`—:

| lo que hace | dónde | ¿SAF puede? |
|---|---|---|
| crea **8 directorios** al arrancar (journal, skips, followers, index, reactions, elision, materialization, outbox) | `DiaryStorageFileSystem.cs:78-85` | sí, `CreateDocument` con MIME de directorio |
| abre **por ruta**, en cualquier momento, para rolar y compactar (`FileMode.Create`, luego reabre para leer) | `JournalWriter.cs:58,143,486-487,598` | sí, pero hace falta un puerto: no hay rutas |
| **lista por glob**: `Directory.GetFiles(journalDir, "journal_*.bin")`, `*.batch`, `*.tmp` | `JournalWriter.cs:283`, `PendingBatchDirectory.cs:83,155,166` | ⚠️ **acá pega el fallo mudo medido arriba**: enumerar la ruta da **0 archivos sin excepción** |
| **rename atómico** con puerto propio por plataforma (`File.Move(overwrite:true)`, `File.Replace`) | `IAtomicFileOperation` + `Ntfs`/`Posix`/`Safe`; 35 usos de Move/Replace/Delete | ❌ SAF tiene `renameDocument` y `moveDocument`, **pero no un replace-si-existe atómico** |
| **lock a nivel de SO por `FileShare.None`** — el comentario lo dice: *«makes the lock OS-level»* | `PendingBatchDirectory.cs:93,101-118`; `StoragePathValidator.cs:55` | ❌ `FileShare` es un concepto del open-por-ruta; por `ContentResolver` **no se puede expresar** |
| `SetLength` para truncar la cola inválida tras un corte | `JournalWriter.cs:233` | probablemente sí (`ftruncate` sobre el fd), **sin medir** |

**La buena noticia es estructural, no chica: la abstracción ya está en el lugar correcto.**
`DiaryStorage` es una clase abstracta y `DatabaseType` tiene **cinco** valores —`SQLServer`,
`MySQL`, `IN_MEMORY`, `FileSystem`, `PlainText`—, o sea que ya hay cinco backends conviviendo. Y
`IAtomicFileOperation` prueba que ya aceptaron que **una** operación de archivo es específica de la
plataforma y merece un puerto.

**Entonces el pedido de verdad es:** que el backend de filesystem hable con un **puerto de archivos
inyectado** en vez de llamar a `System.IO` directo. La interfaz sería chica —crear directorio, abrir
por nombre, listar por glob, mover atómico, borrar, truncar— pero **atravesarla son esas 6 000
líneas**, y dos de sus operaciones —el replace atómico y el lock exclusivo entre procesos— **SAF no
las tiene**. Y las dos son justo de seguridad ante cortes y ante dos escritores, que es el problema
que la opción A ya arrastraba.

**Lo que esto NO dice.** Nada sobre iOS, donde no hay SAF ni equivalente. Y nada sobre el camino que
sí está construido —la PWA **adentro** de Gnothi—, donde el problema no se resuelve: **no existe**,
porque la carpeta la pone Gnothi y es suya.

---

## El modo lector del motor: qué escribe una lectura pura — 2026-09-08

**Qué se fue a contestar.** El otro bloqueante de la opción A, y el que sobrevive a resolver los
permisos: el motor **no tiene modo solo-lectura**, así que un `.gmod` que «lee» el journal vivo de la
PWA es **físicamente un segundo escritor**.

Instrumento: `Spikes/ReaderModeSpike/` en un **worktree** del repo del motor (master `8ef4415`), sin
commitear. El método es un **diff de bytes**: se siembra un journal, se saca huella SHA-256 de cada
archivo y de cada directorio, y cada caso corre sobre **su propia copia** — dejar que las escrituras
de un caso sean la línea base del siguiente es el error que hace parecer segura una carpeta
compartida.

**Tres escenarios, porque un journal limpio y chico ejercita las menos rutas de escritura y
favorecería cualquier respuesta:**

| escenario | qué es |
|---|---|
| **CLEAN** | 3 entradas en un archivo |
| **ROLLED** | 40 entradas con `maxFileSize=1024` → varios archivos sellados |
| **TORN** | ROLLED, más un registro partido pegado al archivo activo: lo que deja un proceso muerto a media escritura |

### Lo que escribe cada puerta

```
                    rehydrate      introspect     readOnly (las dos)
CLEAN               +index ~meta   +index ~meta   NADA   · leyó 4 entradas
ROLLED              +index ~meta   +index ~meta   NADA   · leyó 41
TORN                +index ~meta   +index ~meta   NADA   · leyó 41
                    ~journal       ~journal
```

**Cuatro hallazgos:**

1. **La puerta de solo-lectura que ya existe NO es de solo lectura.**
   `ConfigureStorageForIntrospection` se saltea la rehidratación, el misfire plane y el checkpoint —
   pero construye el storage, y **el constructor escribe exactamente igual** que la ruta completa.
2. **Las dos escrituras del `open` son reconciliación de CACHÉ, no estado**, y el propio código lo
   dice: `index.bin` es *«a hint, not an authority»* (línea 126) y los totales de `meta.bin` son
   *«potentially stale»* (línea 135). La autoridad son los archivos del journal, y el `open` los
   recomputa desde ahí; se persisten solo para que **el próximo** open salga barato.
3. **TORN destapó dos escrituras más**, y ninguna de las dos estaba en el `open`:
   - `Initialize()` hace `activeStream.SetLength(lastValidPosition)` para tirar el registro partido
     — o sea que **curaba el archivo del dueño**: `715 → 695 bytes`. Curar el archivo de otro es
     asunto del dueño, no de un lector.
   - `Dispose()` → `UpdateFileHeader()` reescribe el header **al cerrar**. Medir solo el `open` no la
     veía. Y apareció **como un crash, no como un diff**: con el handle en `FileAccess.Read` el
     `Write` tiró `NotSupportedException`, y `Dispose` corre desde el **finalizador**, así que salió
     como excepción no manejada en el hilo del GC. Con un handle de escritura habría funcionado en
     silencio.
4. **ROLLED no agregó ninguna ruta nueva.** Varios archivos sellados se reconcilian sin escribir más
   que el caso limpio.

### El parche

**72 líneas en 3 archivos, 19 de lógica.** Opt-in `readOnly=true` por connection string —la
convención del propio repo—, y cuatro compuertas más el handle:

| | qué |
|---|---|
| `DiaryStorageFileSystem:133` | no persistir el índice reconciliado |
| `DiaryStorageFileSystem:146` | no persistir los totales corregidos |
| `JournalWriter` · `Initialize` | **no truncar** la cola partida |
| `JournalWriter` · `Dispose` | **no** reescribir el header ni hacer flush durable |
| `JournalWriter` · `Initialize` | abrir el archivo activo en **`FileAccess.Read`** |

Ese último no es un detalle: un consumidor con handle de **escritura** sobre el archivo activo del
dueño ya es un segundo escritor para el sistema operativo, escriba o no — y cualquier escritura
accidental ahora **falla fuerte** en vez de tener éxito calladamente. Fue lo que destapó el hallazgo 3b.

**La fila que decide es `rehydrate+readOnly`**, porque la introspección **no rehidrata** —su propio
comentario avisa que `Perform`/`Tell`/`Reactions` fallan— y un `.gmod` que proyecta estado de dominio
necesita rehidratar. Con la bandera: **rehidrata, contesta, y no deja rastro.**

**Y en TORN lee las mismas 41 entradas.** Saltarse la reparación no le hace leer basura: el escaneo
ya se detiene en el último registro con CRC válido, así que su vista termina donde el truncado la
hubiera terminado. La basura queda en disco hasta que el dueño abra su journal y la trunque él.

**Es no-op sin opt-in:** `ReadOnly` arranca en `false` y todas las compuertas son `&& !readOnly`.

**Cero regresión, y verificado con control.** No alcanza con «la suite pasa»: la suite de este motor
**ya viene con 5 fallos rojos en esta máquina**, todos de cultura —`12:00 a. m.` contra `12:00 AM`,
`2,25` contra `2.25`—. Se corrió la suite completa en master limpio y en el worktree con el parche:
los **mismos cinco nombres** y los mismos totales, **5 fallan · 2 087 pasan · 1 omitido · 2 093**.
Ninguno pasa por el storage.

### La trampa del instrumento, que costó dos lecturas falsas

**Vale más que el parche.** `GracefulExit()` **no libera** el `JournalWriter`: su última escritura
del header ocurre en el **finalizador**. Así que un diff tomado justo después de cerrar **corre una
carrera contra el GC**, y falló en las dos direcciones:

- **Falso negativo:** dio «no escribió nada» porque la escritura todavía no había aterrizado. Ese fue
  el primer resultado de este spike, y era mentira.
- **Falso positivo:** al forzar el GC, el **sembrador** escribió el baseline *después* de
  fotografiarlo, y entonces los casos copiados más tarde salían «modificados» contra una foto vieja —
  **culpando al lector de una escritura que no hizo**. Se cazó comparando los bytes con `cmp`: eran
  idénticos.

El arreglo es sellar con `GC.WaitForPendingFinalizers()` **en los dos lados**: después de sembrar y
después de cada caso. **Moraleja para cualquier medición de este motor: un journal recién cerrado no
está quieto.**

**Lo que el spike NO cubre, y hay que decirlo al pasárselo:**

- **Elisiones, checkpoints de reacciones y outbox: sin sembrar.** Necesitan un dominio con clases que
  el pattern matcher enganche. Es el próximo escenario, y es donde podría aparecer una quinta
  escritura.
- **No hace al lector seguro contra el dueño escribiendo AL MISMO TIEMPO.** Resuelve que el lector no
  perturbe al dueño; no resuelve el rolado de archivos ni los handles del dueño mientras el lector lee.
- **Es una propuesta para quien mantiene el motor, no una decisión nuestra.** Vive en un worktree, sin
  commit, sin merge y sin push.

## Lo que NO está medido

| ❓ | por qué importa |
|---|---|
| **Que un servidor MCP pueda vivir en un `.wasm`** | es la etapa siguiente. Falta el workload de WASI y un runtime wasm; **no hay ninguno instalado** |
| Que el módulo con el motor corra bajo WASI | **bloqueado**: el guard tiene forma de plataforma |
| **Cold start con historia de 5–10 años** | la métrica que en dos años va a importar más que los megabytes, y la más fácil de instrumentar ahora |
| Que la reflexión sobreviva a trimming/AOT | spike bloqueante de tamaño |
| El aislamiento **por módulo** | el contenedor pone paredes alrededor de *todo* |
| **El cruce entre módulos de dos autores distintos** | es el corazón de la propuesta y queda **pendiente de correr con ambos módulos instalados** |
| Por qué el botón de instalar estaba bloqueado | hay un gate por versión; **no se confirmó que fuera eso** |

---

## Dos hallazgos que salieron sin buscarlos

**1 · Los tokens están en el journal, en texto plano.** Un token no es un hecho del dominio: es contexto de
sesión, y va al Playbill. Y en el diseño destino esa acción no debería existir, porque el que custodia la
credencial es el Host. Como el journal es append-only, ahí van a quedar.

**2 · El «solo lectura» era cierto sobre los asientos, no sobre la carpeta.** El actor nunca asienta,
verificado. Pero el storage **sí escribe archivos al inicializar**. Es la causa del bug de concurrencia. Ver
`E3`.

## Y una corrección de hecho que vale registrar

**El prototipo no usa OPFS.** Grep sobre todo el código: cero ocurrencias de `opfs`,
`navigator.storage`, `storage.getDirectory`. Usa la File System Access API e IndexedDB **solo** para
recordar el handle del directorio.

La distinción no es cosmética: **OPFS es un almacén privado del origen que ningún otro proceso puede
abrir** — usarlo habría matado el caso de uso.

## Y una retractación, para que quede el método

Se afirmó que una versión nueva de la aplicación **sí** soportaba el tipo de servidor `binary`, basándose en
un conteo de ocurrencias de la cadena en sus recursos.

**La evidencia no lo sostenía.** Al mirar el contexto, los hits eran falsos positivos: encodings de buffer,
gramáticas de resaltado de sintaxis. Un conteo no es una medición.
