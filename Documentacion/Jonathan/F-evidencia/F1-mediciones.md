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


---

## El camino A4, medido de punta a punta — 2026-09-08

**El camino.** La PWA vive **afuera** de Gnothi y escribe su journal en una carpeta física que la
persona eligió con el picker del navegador (el SAF). Gnothi **no lee esa carpeta con el motor** —está
medido más arriba que no puede—: la **copia** a su área privada por `ContentResolver`, y el módulo
lee la copia.

**Lo que lo distingue de las otras rutas de la opción A: no necesita ningún cambio en el motor.** El
`.gmod` abre la copia, que está en el área privada de Gnothi, donde `System.IO` sí llega. El parche
de modo lector no hace falta acá.

Instrumento: `Gnothi.Mobile/SondaA4.cs` (temporal), emulador `sdk_gphone64_x86_64` (Android 16 / SDK
36), `targetSdk=35`, **sin permisos de almacenamiento**. El journal de prueba lo escribió **el mismo
motor** que después lo lee, para que la prueba no dependa de un formato inventado.

| paso | resultado |
|---|---|
| 1 · concesión de árbol pidiendo **solo lectura** | ✅ tomada y persistida |
| 2 · ¿la concesión de lectura impide escribir? | **depende — ver abajo** |
| 3 · **la huella**: ¿es un journal, y de quién? | ✅ reconoció el actor `pwa-suelta` |
| 4 · copiar con sellado, y **abrir la copia con el motor** | ✅ **13 entradas leídas** |
| 5 · refresco **sin picker**, con la concesión guardada | ✅ funciona desatendido |

### La huella: no hay que plantar nada

El SAF no verifica nada — la persona puede conceder **cualquier** carpeta, incluida una vacía o la de
otra institución. Copiar a ciegas y descubrirlo al abrir es el peor orden posible.

**Y no hace falta que la institución plante una marca: la huella es intrínseca.** Un journal del
motor es `<actor>/meta.bin` cuyos cuatro primeros bytes son el mágico **`PPMM`**, con `journal/` al
lado. El nombre de la carpeta del actor dice de quién es. Medido: reconoció `pwa-suelta` y, si no lo
encontrara, **lo dice en vez de adivinar**.

### El permiso de solo lectura: sí se impone, pero NO en la sesión del picker

Esto tiene un matiz que hay que decir entero, porque medirlo a medias da la respuesta contraria.

- **Dentro de la sesión en que la persona concede la carpeta**, Gnothi **sí puede escribir**: el
  probe creó un archivo. `ACTION_OPEN_DOCUMENT_TREE` entrega en caliente una concesión de
  lectura **y escritura**, y `takePersistableUriPermission` solo decide qué se **persiste**.
- **La concesión guardada sí quedó de solo lectura** — el sistema la reporta como
  `lectura=True  escritura=False`.
- **Y después de reiniciar el aparato, cuando solo queda lo persistido, escribir falla:**

```
SecurityException: Permission Denial: writing com.android.externalstorage.ExternalStorageProvider
uri content://…/tree/primary%3ADocuments%2Fpwa-journal/document/… from pid=2110, uid=10218
requires android.permission.MANAGE_DOCUMENTS, or grantUriPermission()
```

**Conclusión honesta:** el sistema **sí impone** la lectura-sola sobre la concesión duradera, que es
la que Gnothi va a usar el 99% del tiempo. Lo que **no** cubre es la ventana de la sesión del alta.
Ahí la contención es disciplina, no incapacidad — y conviene decirlo así en vez de afirmar «Gnothi no
puede escribir en la carpeta de la persona», que sería falso en ese rato.

### El sellado, y por qué no se detecta el candado

La condición pedida era *«si alguien tiene tomados los files, no copiar»*. **Detectar el candado no
da la garantía que parece**: es una carrera —contesta que no hay nadie y el escritor arranca un
milisegundo después—, y en Android el bloqueo sobre almacenamiento compartido es consultivo.

Lo que sí da garantía es **verificar lo copiado**, y el formato lo permite: el journal es append-only,
cada registro lleva su largo y su CRC, y el motor **se detiene en el último válido** — medido arriba,
en el escenario TORN. Una copia tomada a media escritura no queda corrupta: queda **vieja**, y de
forma detectable.

El **sellado** —listar, copiar, volver a listar, y descartar si el listado cambió— queda como
detector barato del caso común, no como la garantía. Medido: `el listado no cambió durante la copia`.

### El refresco

El módulo es un `.gmod` y **no se abre**, así que no hay evento de apertura del que colgarse. El
refresco va **al entrar a Gnothi**, más alguna pasada extra durante el día. Eso exige que la copia
corra **sin persona y sin picker**, con la concesión guardada — y es el paso 5: corrió desatendido,
reconoció la huella, copió y el motor leyó las 13 entradas. También después del reinicio.

### Cuánto pesa un journal, y cuánto cuesta refrescarlo

**Cuántos registros son 1 MB.** La pregunta no tiene una respuesta sino un rango, porque un registro
mínimo y uno realista no pesan igual. Medido con 2 000 registros en cada caso:

| carga por registro | bytes por registro | registros para 1 MB |
|---|---:|---:|
| ninguna (el mínimo) | **40,5** | **≈ 25 900** |
| 200 b | 243,5 | ≈ 4 300 |
| 2 000 b | 2 043,5 | ≈ 513 |

La relación es limpia: **≈ 43,5 bytes de sobrecarga por registro**, más lo que pese el contenido. Con
eso se estima cualquier institución sin volver a medir — basta saber cuánto ocupa uno de sus hechos.

**Cuánto cuesta el refresco**, que es lo que decide si copiar el árbol entero es viable. Journal de
**4,09 MB**, tres vueltas por configuración (los números son el rango de las tres):

| | 4 MB en **1** archivo | 4 MB en **16** archivos |
|---|---:|---:|
| listar (sellado, 1.ª vez) | 114–163 ms | 101–129 ms |
| **copiar por `ContentResolver`** | **162–224 ms** | **359–502 ms** |
| listar (sellado, 2.ª vez) | 83–112 ms | 68–101 ms |
| **abrir la copia con el motor** | **913–1 035 ms** | 766–882 ms |
| **refresco completo** | **≈ 1,3–1,6 s** | ≈ 1,3–1,6 s |

**Tres cosas que salen de ahí:**

1. **Copiar el árbol entero es barato: 4 MB en ~0,2 s.** La preocupación de que no escalara era
   infundada a esta escala; no hace falta comparar tamaño y fecha todavía.
2. **Lo que manda no son los bytes, es la CANTIDAD DE ARCHIVOS.** Los mismos 4 MB partidos en 16
   cuestan **~2× más** de copiar (≈190 ms → ≈420 ms): unos 15 ms de sobrecarga por archivo, porque
   cada uno es una consulta al `ContentResolver`. El parámetro que gobierna eso es el `maxFileSize`
   del journal, y hoy nadie lo eligió pensando en esto.
3. **El costo dominante es abrir la copia con el motor (~0,9 s), no copiarla.** Y no depende de si
   la carpeta es compartida: es lo que el módulo paga igual. O sea que el SAF **no es** el cuello.

**El sellado cuesta ~200 ms** y es casi constante — listar 16 archivos costó lo mismo que listar 1—,
así que suma poco al refresco pero **no es gratis frente a una copia de 0,2 s**. Con journals chicos
el detector cuesta más que lo que protege.

**Y hasta dónde aguanta.** Se subió a decenas de MB —la carga por registro sube para llegar rápido al
tamaño; para el costo de la copia lo que importa son los bytes y la cantidad de archivos, no cuántos
hechos los produjeron—:

| journal | archivos | copiar | **abrir con el motor** | refresco |
|---|---:|---:|---:|---:|
| **4 MB** | 1 | 0,16–0,22 s | 0,91–1,04 s | **~1,3–1,6 s** |
| **40 MB** | 10 | 0,46–1,67 s | 2,07–2,36 s | **~2,7–4,1 s** |
| **120 MB** | 29 | 1,47–2,64 s | 5,16–5,20 s | **~6,8–8,0 s** |

**La conclusión cambia de lugar respecto de lo que se suponía, y esto es lo importante:**

1. **Refrescar al entrar a Gnothi es viable hasta unas decenas de MB.** Bajo 4 s a los 40 MB. A los
   120 MB son 7–8 s: demasiado para bloquear nada, así que ahí el refresco tiene que ser de fondo,
   no en el camino de abrir la app.
2. **La palanca NO es la copia, es abrir.** A 120 MB abrir cuesta **2–3× lo que copiar**. Optimizar
   la copia —comparar tamaño y fecha en vez de copiar todo— solo recortaría la mitad más chica. Lo
   que valdría la pena es **no reabrir el motor en cada refresco**: mantener vivo el actor del módulo.
3. La copia es **aproximadamente lineal** con el tamaño (20–80 MB/s, muy variable según la caché del
   sistema); abrir tiene un costo fijo de ~0,7 s más ~38 ms por MB.

> ⚠️ **Una trampa que aparece al escalar, y hay que decirla.** Recorrer entradas con `ShowEntry` pasó
> de **170 ms** por entrada a 4 MB, a **1,4 s** a 40 MB, a **3,85 s** a 120 MB. Es O(tamaño del
> journal) **por llamada**. Un `.gmod` no hace eso —rehidrata una vez y proyecta—, pero cualquier
> diseño que recorra entrada por entrada muere a escala. El número de arriba de «abrir» es el que
> vale; el del paseo no es el costo del módulo.

### Corrección: esa tabla tiene la cantidad de entradas fija, y eso la vuelve optimista

**Las tres filas de arriba son de journals de 2 000 entradas.** Para llegar rápido al tamaño se subió
la carga por registro, no la cantidad de hechos. Pero abrir hace **replay**, y el replay cuesta **por
entrada**, no por byte — así que la tabla mide una sola de las dos variables.

Se aisló, a tamaño constante:

| ~4 MB | entradas | copiar | **abrir** |
|---|---:|---:|---:|
| 2 000 registros de 2 KB | 2 000 | 0,16–0,22 s | **0,91–1,04 s** |
| 40 000 registros de 60 b | 40 000 | 0,17–0,19 s | **4,45–4,47 s** |

**Mismo tamaño, 20× las entradas: la copia no se mueve y abrir se multiplica por 4,4.** Copiar depende
de los bytes; **abrir depende sobre todo de cuántas entradas hay.**

Con los cuatro puntos medidos, un ajuste grueso —cuatro puntos, en emulador, tómese como orden de
magnitud y no como fórmula—:

```
abrir ≈ 0,6 s  +  0,09 ms por entrada  +  37 ms por MB
```

**Y esto corrige hacia arriba la tabla anterior para un caso realista.** Un journal de 120 MB hecho de
registros de 2 KB son ~58 800 entradas, no 2 000: abrir sale **~10 s**, casi el doble de los 5,2 s
medidos. A 40 MB realistas (~19 600 entradas) son **~3,9 s** en vez de 2,2 s.

**El techo práctico baja, entonces:** el refresco al entrar a Gnothi aguanta cómodo hasta la decena de
MB; en el orden de los 40 MB ya está en varios segundos, y a 120 MB hay que sacarlo del camino de
abrir la app. Y refuerza la conclusión de dónde está la palanca: no en copiar menos bytes, sino en
**no reabrir el motor** — que es lo que paga por entrada.

### Lo que falta medir

- **`.crswap`.** Chrome, al escribir por File System Access, trabaja sobre un archivo temporal en la
  misma carpeta y lo renombra al cerrar. Si eso es visible por `ContentResolver`, es un detector
  directo de «hay una escritura en vuelo», mejor que el sellado. **No está verificado** — sale de
  cómo está implementada la API, no de una medición nuestra, y exige la PWA real escribiendo.
- **La copia a media escritura, sobre el SAF de verdad.** El escenario TORN se midió en el motor;
  falta hacerlo cruzando la copia.
- **Journals de decenas de MB.** Todo lo de arriba es hasta 4 MB. El punto donde la copia entera deje
  de pagar existe; solo que está más lejos de lo que se suponía.

---

## Qué deja Chrome en la carpeta mientras escribe — 2026-09-08

**Qué se fue a contestar.** La condición que se pidió para A4 era *«si alguien tiene tomados los
archivos, no copiar»*. La hipótesis era que Chrome, al escribir por File System Access, deja un
archivo temporal `.crswap` en la misma carpeta, y que eso serviría de detector directo. **Estaba
anotada como NO verificada.** Ahora está medida, y el resultado es otro — mejor.

Instrumento: una página mínima servida por `PaginaDeLaInstitucion` en `http://127.0.0.1:8200/app/`,
alcanzada por `adb reverse`, abierta en el **Chrome real del emulador**. Mide el mecanismo del
navegador, no una app: da control exacto de cuándo el `createWritable()` está abierto.

### Lo primero, y confirma de primera mano lo que estaba citado de fuentes

```
isSecureContext      : true
showDirectoryPicker  : function
crypto.subtle        : true
navigator.storage    : true
userAgent            : … Chrome/133.0.0.0 Mobile Safari/537.36
```

**Chrome Android SÍ tiene el picker**, y al invocarlo abre **DocumentsUI** — o sea el SAF. Hasta hoy
eso estaba tomado del código de Chromium y de un blog; ahora está visto correr.

### Y una que no esperábamos: el permiso de la PWA es por sesión de pestañas

Chrome muestra su **propio** diálogo, además del del sistema:

> *«Let site edit files? http://127.0.0.1:8200 will be able to edit files in fsa-prueba **until you
> close all tabs for this site**»*

**La concesión de escritura de la PWA no es duradera.** Eso contrasta con la de Gnothi, que sí
persiste y sobrevive reinicios (medido arriba). Asimetría que conviene tener presente: la app suelta
tiene que volver a pedir permiso más seguido que el Host.

### El hallazgo: la escritura es ATÓMICA al cerrar

Se abrió un `createWritable()` y se dejó **abierto 30 segundos**, escribiendo de a un pedazo por
segundo, mientras el shell listaba la carpeta cada 2 s. El writable estuvo abierto de **19:54:48.873
a 19:55:19.178**, y el sondeo cubrió toda esa ventana.

**No apareció ningún `.crswap` ni archivo temporal.** Solo el archivo destino, siempre.

Y como el tamaño no bastaba —la escritura sobrescribía la misma longitud—, se repitió mirando el
**contenido**: primero una escritura normal dejó la marca `escritura-normal-…`, y después la lenta
escribió `chunk-0-…`.

```
t+2s  … t+28s   escritura-normal-.......   ← el contenido VIEJO, con el writable abierto
t+30s … t+36s   chunk-0-................   ← cambió de golpe, al close()
```

**Chrome materializa la escritura de una sola vez, al cerrar.** El archivo en la carpeta compartida
**nunca está a medias** para otra app.

### Qué significa para el diseño

**La condición pedida no es difícil de cumplir: es innecesaria en este camino.** Una copia tomada en
cualquier instante obtiene **la versión anterior completa o la nueva completa**, nunca un archivo
partido. No hace falta detectar el candado, y tampoco hay detector que encontrar.

**Con dos precisiones que hay que decir:**

1. **La atomicidad es POR ARCHIVO, no por conjunto.** Un journal son varios archivos —
   `journal_00000N.bin`, `meta.bin`, `index.bin`—, y copiar entre la escritura de uno y la del
   siguiente da un conjunto **coherente pero desactualizado**. Ahí sigue valiendo lo de siempre:
   `meta.bin` e `index.bin` son caché reconstruible y el journal es append-only, así que el resultado
   es «viejo», no «roto». **El sellado se justifica por esto**, no por el desgarro de un archivo.
2. **Es de Chrome sobre SAF, no del estándar.** El `.crswap` existe en otras implementaciones; acá no
   aparece. Un navegador distinto, o una versión distinta, puede comportarse de otro modo. Si el
   diseño se apoya en esta atomicidad, hay que volver a medirla al cambiar de plataforma —y en iOS
   no hay picker, así que la pregunta no se traslada.

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

## El censo del backend de archivos: qué toca en realidad un lector

*2026-09-08. Censo de código sobre `Puppeteer/EventSourcing/DB/FileSystem` + `DiaryStorageFileSystem.cs`
(5 890 líneas), en `C:\Users\jon32\source\repos\Puppeteer`, master `8ef4415`.*

Vale corregir algo que se dijo antes: **«el puerto de archivos son ~6 000 líneas y dos operaciones que
SAF no tiene»**. Eso mide el puerto **completo** — un motor que lee *y escribe* sobre SAF. No es la
pregunta que hay sobre la mesa. La que hay es más chica: **qué necesita un lector**.

El censo de llamadas estáticas a `System.IO` en todo el backend:

| operación | usos | operación | usos |
|---|---|---|---|
| `File.Exists` | 52 | `Directory.Exists` | 9 |
| `Path.Combine` | 35 *(no es E/S)* | `Directory.GetFiles` | 6 |
| `File.Delete` | 19 | `File.Replace` | 4 |
| `File.WriteAllBytes` | 14 | `File.GetLastWriteTimeUtc` | 3 |
| `File.ReadAllBytes` | 14 | `Directory.GetDirectories` | 2 |
| `File.Move` | 11 | `Directory.EnumerateFiles` | 1 |
| `Directory.CreateDirectory` | 11 | | |

Más 24 `new FileStream`, con `FileShare.ReadWrite` 17 veces, `FileShare.Delete` 13 y **`FileShare.None`
solo 7** (de las cuales 4 son comentarios: los usos reales son **dos**).

### Lo que toca el camino de lectura

Recortado a los archivos que un lector recorre — `JournalReader.cs`, `SparseIndex.cs`,
`MetadataStore.cs` — la superficie entera es **cuatro operaciones**:

| lo que usa | dónde | ¿tiene equivalente en SAF? |
|---|---|---|
| `File.Exists` | `JournalReader:54,79` · `SparseIndex:37` · `MetadataStore:43` | ✅ medido |
| `File.ReadAllBytes` | `SparseIndex:40` · `MetadataStore:46` | ✅ medido (`OpenInputStream`) |
| `FileStream` **solo lectura** + `Seek` | `JournalReader:91,224` | ✅ medido (`OpenFileDescriptor`, buscable) |
| listar la carpeta | `Directory.GetFiles`/`EnumerateFiles` | ✅ medido (query al `ContentResolver`) |

**Y los dos bloqueantes no aparecen en ninguna de las cuatro.** Los dos son del lado de la escritura:

- El **replace atómico** ya está **detrás de un puerto** — `IAtomicFileOperation`, con
  `AtomicFileOperationFactory` que elige por plataforma y tres implementaciones (Ntfs, Posix, y un
  `SafeAtomicFileOperation` de reserva para «cualquier otra»). Solo lo llama quien escribe.
- El **lock entre procesos** tiene **dos usos reales**: el rollover en `JournalWriter:487` y el
  consolidador de elisiones en `PendingBatchDirectory:64`. Ninguno de los dos corre en un lector.

### Lo que esto significa

**El modo lector no solo hace que leer no perturbe: le colapsa la superficie al puerto.** Con las
compuertas del parche puestas, las dos `File.WriteAllBytes` que quedaban en el camino de lectura
(`SparseIndex:85`, `MetadataStore:85`) son exactamente las que el parche cierra.

Así que el pedido a quien mantiene el motor deja de ser *«un puerto de archivos»* y pasa a ser
**un puerto de lectura de cuatro métodos**, más el modo lector ya medido.

### Lo que este censo NO prueba

1. **El hueco es el mismo de A3**: elisiones, checkpoints de reacciones y outbox nunca se sembraron
   en el spike. Si abrir un journal que las tenga saca E/S fuera de esas cuatro operaciones, el cuadro
   cambia. Es un solo experimento que ahora responde dos preguntas.
2. **Sin comprobar**: qué implementación de `IAtomicFileOperation` le toca a Android.
   `IsOSPlatform(OSPlatform.Linux)` probablemente da `false` ahí y caería en `SafeAtomicFileOperation`.
   Importa poco mientras se hable de un lector, pero está sin verificar.
3. Es un censo **estático**. Cuenta sitios de llamada, no ejecuciones.

## Sembrando lo que faltaba: reacciones, checkpoints, outbox y elisiones

*2026-09-08. `Spikes/ReaderModeSpike`, worktree `Puppeteer-spike-reader`, desprendido de master
`8ef4415`. Dos escenarios nuevos sobre los tres de la ronda anterior.*

La ronda anterior dejó un hueco declarado: **elisiones, checkpoints de reacciones y outbox nunca se
sembraron**. Un journal de verdad no es solo entradas — una reacción deja un registro, un checkpoint,
una frontera de cobertura, un mensaje en el outbox y, si elide, un `elision.bin` consolidado más una
carpeta `pending/`. Cada uno es un store aparte con su propio camino de carga.

Dos escenarios nuevos:

- **RICH** — entradas más dos reacciones, una que emite al outbox y otra que elide. Dos y no una,
  porque con una sola quedaría ambiguo cuál produjo cada archivo.
- **RICH-INTERRUPTED** — RICH, y después el dueño **muerto a mitad de un replace atómico**: un `.tmp`
  y un `.bak` al lado de cada store, que es exactamente lo que `NtfsAtomicFileOperation` deja si el
  proceso muere entre `File.Replace` y su limpieza.

RICH-INTERRUPTED es el que decide, y por una razón: `RecoverFromIncompleteOperation` **se llama desde
los `LoadX()`** — o sea **al abrir, en el camino de lectura** — en `ReactionStore` (×3), `SkipStore`,
checkpoints, outbox y elisiones. Sobre un journal ordenado no encuentra nada y no cuesta nada, que es
justo por qué RICH **solo** habría halagado la respuesta.

### Lo que encontró

**RICH: el parche aguantó.** `rehydrate+readOnly` → **no escribió nada**, leyendo las 8 entradas. Las
tres piezas que faltaban no agregan escrituras nuevas al abrir. El hueco declarado quedó cerrado.

**RICH-INTERRUPTED: el parche falló, y de la peor forma.** El lector «de solo lectura» **borró 12
archivos del dueño** — el `.tmp` y el `.bak` de cada store. No modificó un byte: **eliminó archivos**.
(`introspect+readOnly` borró 10; los dos de diferencia son `skips/`, que la introspección no carga.)

Es la misma forma que TORN, una vuelta más arriba: **curar al abrir es escribir**. Un consumidor al
que le prestaron una carpeta le estaba terminando la transacción al dueño — que puede seguir
recuperándose de ella.

### Las dos compuertas que faltaban

Las dos salieron del mismo punto, `DiaryStorageFileSystem`:

**Compuerta 5 — no crear carpetas.** El open hace `Directory.CreateDirectory` **siete veces**. Sobre
un journal que escribió este mismo backend ya existen, que es por qué **un spike que siembra su propio
fixture no puede ver esto**. Sobre uno que escribió otra implementación, un lector dejaría siete
carpetas nuevas en la carpeta de otro. Los stores toleran que falten: cada `LoadX` arranca preguntando
`File.Exists`.

**Compuerta 6 — una operación atómica inerte.** Todos los stores reciben su `IAtomicFileOperation` de
**un solo punto de inyección**, así que la compuerta es una línea: en modo lector se les entrega un
`ReadOnlyAtomicFileOperation` cuyo `RecoverFromIncompleteOperation` no hace nada.

Su `AtomicReplace` **lanza** en vez de no hacer nada — un no-op silencioso ahí convertiría un camino
de escritura que se me pasó en pérdida de datos. **Y nunca lanzó**, en ninguno de los cinco
escenarios: el camino de lectura no la llama. Eso confirma el censo por el lado dinámico.

### El estado final

| escenario | `rehydrate+readOnly` | entradas leídas (control) |
|---|---|---|
| CLEAN | **no escribió nada** | 4 (4) |
| ROLLED | **no escribió nada** | 41 (41) |
| TORN | **no escribió nada** | 41 (41) |
| RICH | **no escribió nada** | 8 (8) |
| RICH-INTERRUPTED | **no escribió nada** | 8 (8) |

**El parche completo son 131 líneas en 4 archivos, ~44 de lógica, 6 compuertas.** Sigue siendo no-op
sin opt-in, y la suite completa del motor da igual que master limpio: **5 fallan · 2 087 pasan**, los
mismos cinco nombres, todos de locale.

### Lo que sigue sin probarse

- El **lector mientras el dueño escribe al mismo tiempo**. Sigue sin medirse, y es lo que separa
  «puedo leer una carpeta prestada» de «puedo leerla en vivo».
- Las reacciones sembradas se saltaron la entrada 1 (ver abajo).
- Las reacciones sembradas **se saltaron la entrada 1** (`upgrade` literal, un ScriptEvent de estilo
  V1). Los stores quedaron creados igual — que es lo que este experimento mide— pero `elision.bin`
  puede haber quedado sin registros.

### FOREIGN: la compuerta 5, medida y no razonada

La compuerta 5 se había puesto **por lectura de código**, y eso no es una medición. El problema para
medirla es que todo escenario que siembra su propio fixture crea las siete carpetas auxiliares, con
lo cual los `CreateDirectory` del open son invisibles.

Pero esa **no es la forma del caso real**. En el caso real la carpeta es de una app de afuera, y lo
que dejó son entradas — no el esqueleto completo de este backend. Un sexto escenario, **FOREIGN**:
sembrar, borrar las carpetas auxiliares que quedaron vacías, y leer.

| caso | veredicto | entradas |
|---|---|---|
| `rehydrate` (control) | **escribió 7: 6 carpetas nuevas + el índice** | 7 |
| `introspect` (control) | **escribió 7: 6 carpetas nuevas + el índice** | 7 |
| `introspect+readOnly` | **no escribió nada** | 7 |
| `rehydrate+readOnly` | **no escribió nada** | 7 |

`followers/`, `index/`, `materialization/`, `outbox/`, `reactions/`, `skips/`. Sin el parche, «leer»
la carpeta de otro **la amuebla**. Con el parche no deja nada, y lee las mismas 7 entradas.

## El lector mientras el dueño escribe — 2026-09-08

Todos los escenarios anteriores siembran, cierran, y recién entonces leen. Eso contesta *«¿puedo leer
una carpeta que me prestaron?»*, no *«¿puedo leerla MIENTRAS la escriben?»* — que es la que decide si
compartir en vivo existe siquiera. Si no existe, ni el puerto ni el proveedor sirven de nada, y la
copia deja de ser un rodeo para pasar a ser **la** respuesta.

**Montaje: dos PROCESOS, no dos hilos.** Con hilos compartirían los handles del backend y sus locks
en proceso, que es exactamente lo que la topología real no comparte: la app que escribe y el módulo
que lee son programas distintos. El hijo escribe 120 entradas separadas por 25 ms con
`maxFileSize=1024`; el padre abre un lector y lo recorre, diez veces, mientras eso pasa. Al final
reabre solo y cuenta — porque un lector que rompió algo cuando nadie miraba pasaría todos los
chequeos anteriores.

Se corrió **dos veces**: con el parche y **sin él** (dos motores completos sobre el mismo journal).

| | control (sin `readOnly`) | con `readOnly` |
|---|---|---|
| el dueño | **escribió las 120, sin fallar** | **escribió las 120, sin fallar** |
| el lector lanzó | **nada** | **nada** |
| lo que el lector fue viendo | 14, 19, 25, 30, 36, 41, 47, 53, 59, 66 | 14, 19, 25, 30, 36, 41, 46, 52, 58, 64 |
| al final | **126 entradas legibles** | **126 entradas legibles** |
| la última entrada | **dice `owner-120`**, lo último que escribió el dueño | **dice `owner-120`** |

Y **hubo 6 rollovers durante la corrida** —siete archivos de journal en ambos casos, del mismo
tamaño byte a byte— así que el momento peligroso, donde el escritor crea un archivo con
`FileShare.None` y lo mueve a su lugar, se ejercitó de sobra con el lector entrando y saliendo.

### Leer en vivo no rompe. Pero esta prueba NO demuestra que el parche sirva.

**El control pasó igual de limpio**, y eso hay que decirlo antes que nada: dos motores completos
martillando el mismo journal, con rollovers, no perdieron una entrada ni corrompieron un registro.
La premisa de que la concurrencia es catastrófica **no se sostiene** con esta medición.

Con lo cual el argumento del modo lector **se corre de lugar**. No es «corrompe» — es que **toca lo
que no es suyo**: crea seis carpetas, escribe el índice, reescribe el header, y con una transacción
a medias **borra doce archivos del dueño**. Eso está medido en los escenarios de diff de bytes, y
sigue en pie. Es un argumento de propiedad y de consentimiento, no de corrupción — que además le
queda mejor a la tesis del proyecto.

### Y un detalle que cambia la comparación con A4

**El lector ve una foto del momento en que abrió.** Nunca alcanza al dueño mientras está abierto:
llegó a 64-66 de 126. O sea que «en vivo» significa **«fresco en cada apertura»**, no un flujo.

Si el módulo tiene que reabrir igual para ver lo nuevo, la distancia entre «leer en vivo» y «leer una
copia» se reduce a **latencia de frescura y costo de disco** — no a una diferencia de naturaleza.
Vale tenerlo a la vista antes de pedirle nada al equipo del motor.

### Lo que esta prueba NO cubre

- **La comprobación es cantidad de entradas y el contenido de la última**, no la integridad byte a
  byte de cada registro. Bajo concurrencia no hay una línea base estable contra la cual diffear.
- **No se escribieron elisiones ni reacciones concurrentemente.** El consolidador de elisiones es
  **el único lugar donde el backend diseña explícitamente para dos procesos** —con el lock de
  testigo por `FileShare.None`— y quedó **sin ejercitar**. Es el hueco más filoso que queda.
- **Solo Windows.** Las semánticas de compartición de POSIX y de Android son otras.

## A2 + A3 de punta a punta, con la app de verdad — 2026-09-09

*Emulador `GnothiWeb` (Android 16 / SDK 36). La PWA de Exámenes instalada como WebAPK, escribiendo
su journal en una carpeta que la persona eligió por el SAF. Gnothi con el motor parcheado —puerto de
lectura (A2) + modo lector (A3)— leyendo ESA carpeta, sin copiarla.*

**El reparto es el realista, y salió así de casualidad:** la PWA quedó publicada con el motor **sin
parchear** (`3.0.0-beta.10029`, de la caché local) y Gnothi con el parcheado. O sea que el dueño
escribe con el motor tal como está publicado hoy, y el único que lleva el parche es el lector.

### La secuencia, y qué contestó cada uno

| paso | la PWA dice | Gnothi dice |
|---|---|---|
| 1 · la PWA trae su expediente | **11 exámenes · evento 210** | — |
| 2 · Gnothi lee la misma carpeta | 11 · 210 | **11 exámenes · evento 210** |
| 3 · dos exámenes nuevos **en el proveedor** (211, 212) | 11 · 210 | **11 · 210** |
| 4 · la PWA reabre y los trae | **13 exámenes · evento 212** | — |
| 5 · Gnothi lee otra vez | 13 · 212 | **13 exámenes · evento 212** |

**El paso 5 es el resultado:** Gnothi vio los datos nuevos **sin copia, sin refresco y sin que la PWA
le avisara nada**. Volvió a leer la misma carpeta y ahí estaban.

**Y el paso 3 es el otro resultado, el que conviene no pasar por alto:** con los eventos ya
publicados en el servidor del laboratorio, Gnothi siguió diciendo 11 y 210. **No puede ver lo que la
persona no trajo** — el módulo no tiene red, y el único camino por el que esos eventos llegan es la
app de ella. Eso no es una limitación del transporte: es la propiedad.

### La carpeta ajena, en cada paso

| momento | `meta.bin` | `journal_000001.bin` | `index/index.bin` |
|---|---|---|---|
| la PWA escribe (01:30) | 48 b | 2 173 b | — |
| **después de que Gnothi leyera** | 48 b · 01:30 | 2 173 b · 01:30 | — |
| la PWA reabre y agrega (14:11) | 48 b | 2 427 b | 30 b |
| **después de que Gnothi leyera otra vez** | 48 b · 14:11 | 2 427 b · 14:11 | 30 b · 14:11 |

**Byte por byte y hora por hora, intacta.** El motor la abrió, rehidrató y contestó dos veces sin
escribirle nada. Y el área de Gnothi terminó con **cero archivos**: no hay copia.

### Lo que la corrida real destapó y el spike no podía

**1 · Una octava compuerta.** El motor sí intentó crear carpetas: apareció
`files/journals/examenes/examenes/elision/pending` en el área de Gnothi. Es `PendingBatchDirectory`,
que quedó fuera del gateo. **No cayó en la carpeta de la persona por la forma del montaje** —la
creación va por `System.IO` y la raíz fingida es una ruta real del área privada—, no por la
compuerta. En escritorio, donde la ruta *es* su carpeta, ese mismo `CreateDirectory` le caería
adentro. El spike no lo vio porque FOREIGN solo borra carpetas **vacías** y `elision/` contenía
`pending/`.

**2 · La asimetría de los permisos, y va a favor del lector.** Tras reiniciar el emulador:

| | ¿sobrevivió? |
|---|---|
| la concesión de **Gnothi** sobre la carpeta | ✅ sí — `takePersistableUriPermission` |
| el permiso de **escritura de la PWA**, que es la dueña | ❌ no — es por sesión de pestaña |

La PWA recuerda *cuál* es su carpeta (el handle vive en IndexedDB) pero tiene que **volver a pedir
permiso de escritura cada vez que se abre**. La app lo dice en pantalla y ofrece el botón, así que
está manejado — pero es fricción real y recurrente, y **el lector resultó más durable que el
escritor.**

**3 · Gnothi le informa que su expediente pesa 0 KB.** La pestaña Journal muestra
*«Mis examenes — 0 KB, 0 archivos»*, porque `Module.BytesOnDisk` pesa la carpeta concedida con
`System.IO` y en este camino está vacía. La pantalla dice, en su propio texto, *«Gnothi lo pesa desde
afuera — sin preguntarle a nadie»*: cierto, pero **acá pesa la carpeta equivocada**. Su expediente
existe y tiene 13 exámenes.

### Lo que sigue sin explicarse

En el paso 2 la PWA marcaba **Colesterol total** fuera de rango y el módulo contestaba
`analitosFueraDeRango: 0`. Después del paso 4 los dos coinciden —la PWA también dice «todo está
bien»—, así que la discrepancia dejó de verse, **pero no se explicó**. Puede ser que calculen
distinto o puede ser un error de uno de los dos.

### El control que faltaba: la misma corrida SIN el modo lector — 2026-09-09

Lo pidió el usuario, y tenía razón en que faltaba: el spike tenía su control, pero **la corrida real
nunca se había hecho sin las compuertas**, así que no estaba medido si el parche hace falta *en este
camino*.

El control correcto no es «sin nada»: **el puerto (A2) hace falta igual**, porque sin él Gnothi no
llega a la carpeta. Así que se quitó **solo** `;readOnly=true` de la superficie, dejando todo lo
demás idéntico: mismo puerto, misma carpeta, misma concesión.

| | qué contestó `resumen` |
|---|---|
| **con** `readOnly=true` | `{"hayDatos":true,"examenes":13,…,"ultimoEventoLeido":212}` |
| **sin** `readOnly=true` | **`null`** — 4 bytes |

Y el diff de la carpeta ajena, por md5:

| archivo | antes | después del control |
|---|---|---|
| `meta.bin` | `78524…` | `78524…` |
| `journal/journal_000001.bin` | `61aa4…` | `61aa4…` |
| `index/index.bin` | `98cee…` | `98cee…` |

**Idénticos.** Pero en el área privada de Gnothi aparecieron **las siete carpetas auxiliares**
(`followers`, `index`, `journal`, `materialization`, `outbox`, `reactions`, `skips`).

### Lo que esto corrige, y es un matiz que cambia el argumento

Yo venía diciendo que el valor del modo lector es **de propiedad, no de corrupción** — que sin él el
lector «toca lo que no es suyo». Eso sale de los escenarios de diff de bytes, donde el journal vive
en una **ruta real**. En este camino no es así, y el control lo muestra:

| | ¿protege la carpeta ajena? | ¿hace falta para que FUNCIONE? |
|---|---|---|
| **escritorio** — la ruta es su carpeta | **sí**: sin él crea carpetas, escribe el índice y le borra `.tmp`/`.bak` | no; sin él «anda», perturbando |
| **Android por el puerto** | no: las escrituras caen en el área de Gnothi, porque van por `System.IO` y la raíz fingida es ruta real ahí | **sí**: sin él la lectura **se rompe** |

**Por qué se rompe, que es el detalle interesante:** el motor le pregunta al puerto si el archivo
activo del journal existe —el puerto dice que sí, y es cierto—, y después lo abre **con `System.IO`**
sobre una ruta que **solo existe a través del puerto**. Las dos mitades no coinciden. Un `open` de
lectura-escritura sobre un journal alcanzable solo por el puerto **no está bien definido**, y el
único `open` que sí lo está es el de solo lectura.

Así que la respuesta a «¿hace falta el parche?» es **sí**, con tres justificaciones distintas según
dónde:

1. **En escritorio: protege.** Sin él, leer la carpeta prestada la modifica y le borra archivos.
2. **En Android por el puerto: habilita.** Sin él no hay lectura, se rompe.
3. **En los dos: la carpeta ajena la protege la asimetría del puerto** —que es de solo lectura—
   tanto o más que las compuertas. Vale saberlo, porque significa que un puerto que algún día
   admita escrituras **traslada toda la protección a las compuertas**.

### Las dos debilidades del pedido, cerradas y verificadas — 2026-09-09

Antes de llevarle esto a quien mantiene el motor había dos cosas que lo debilitaban. Las dos están
cerradas y medidas en el aparato.

**1 · La 8.ª compuerta.** `PendingBatchDirectory` hacía dos escrituras escondidas en su constructor,
y las dos corrían **al abrir**: `CreateDirectory` de un `pending/` que un lector no va a usar, y
`RecoverOrphans`, que **borra los `.tmp` huérfanos del dueño** — la misma clase de daño que
terminarle un reemplazo atómico. Un `.tmp` ahí es un batch cuyo dueño murió entre escribirlo y
renombrarlo; descartarlo es decisión de él, no de un lector.

Verificado borrando el área de Gnothi y volviendo a leer:

| | antes de la compuerta | después |
|---|---|---|
| carpetas creadas al leer | `elision/pending` + las 7 auxiliares | **ninguna** |

**2 · La costura, de estático de proceso a selección por conexión.** Era
`JournalFiles.Install(impl)` — un único «lector actual» para todo el proceso. Estaba mal por una
razón concreta: **dos módulos leyendo dos carpetas por dos puertas distintas no podían coexistir**;
el último en instalar decidía por los dos, en silencio.

Ahora el host **registra por nombre** —`JournalFiles.Register("examenes", impl)`— y la conexión
elige con **`files=examenes`**, en el mismo lugar donde ya viven `readOnly`, `maxFileSize`,
`compression` y `encryption`. Un nombre que nadie registró **lanza** en vez de caer a `System.IO`,
porque caer abriría otra carpeta y contestaría «acá no hay nada»: una respuesta que parece cierta.

**Y sin romperles nada**, que era la propiedad a preservar: las firmas viejas se conservaron
encadenando al puerto por default, así que **la solución completa compila sin tocar un solo test del
motor**. Sin eso, el cambio rompía **144 sitios de compilación** en su suite — y un pedido que te
obliga a reescribir los tests ajenos no se acepta.

| | |
|---|---|
| suite del motor | **5 fallan · 2 087 pasan** — igual que master limpio |
| los 7 escenarios del spike | **ninguno escribe un byte** |
| la corrida en el teléfono | **13 exámenes · evento 212**, carpeta ajena con md5 idénticos |

**Queda una pregunta de diseño abierta, y conviene nombrarla en vez de inventarle respuesta:** cómo
le dice el host al módulo **qué nombre** usar. Acá la convención es el nombre del actor, que funciona
y no agrega plomería, pero la forma honesta es probablemente que **quien concede la carpeta también
diga por qué puerta se lee**.

## La red de una PWA instalada no se puede acotar desde afuera — 2026-09-09

**Qué se fue a contestar.** En el camino B —la app de la institución adentro de Gnothi, en un
WebView— está medido que **el módulo no tiene red**: la interfaz es solo `lo`, sin DNS, y el pull lo
hace el Host con su credencial contra un endpoint fijo. La pregunta es si esa misma garantía se puede
dar cuando la app vive **afuera**, instalada como PWA con su ícono propio.

**Instrumento:** la WebAPK que se instaló desde la página del laboratorio en el emulador —
`org.chromium.webapk.ac970f0088360685e_v2`, standalone, fuera de Gnothi.

| | resultado |
|---|---|
| permisos que pide la WebAPK | `POST_NOTIFICATIONS` y `REORDER_TASKS` |
| **`android.permission.INTERNET` en la WebAPK** | ❌ **no lo pide** |
| en Chrome | ✅ lo pide |
| quién corre la pantalla | `com.android.chrome/…webapps.SameTaskWebApkActivity` |

**La WebAPK no tiene red que restringir.** Es una cáscara: manifiesto, ícono, nombre y `scope`. La red
la hace **Chrome**, en su proceso y bajo su uid.

**Y esto NO significa que la app suelta no tenga red — tiene toda la de Chrome.** Conviene decirlo
así porque la tabla de arriba invita a leerlo al revés: en esta misma corrida la PWA, andando como
WebAPK, le pidió sus eventos al proveedor y pasó de 11 a 13 exámenes. Los uids lo muestran: la
WebAPK es `10222` y Chrome es `10150`, y el `fetch()` de la página sale por **10150**.

La WebAPK no pide `INTERNET` porque **nunca hace una petición**: se la pide a Chrome. Y es justo
**porque la red es prestada que no hay dónde acotarla** — un módulo nativo confinado tiene uid propio
y ahí sí se corta; una PWA hereda el del navegador, así que la perilla es la misma que la de todo lo
que la persona navega.

De ahí que las tres palancas obvias no sirvan:

- restringirle la red al paquete de la WebAPK **no hace nada** — no tiene
- restringírsela a Chrome le rompe el navegador entero a la persona
- lo que Android ofrece por app —datos en segundo plano, ahorro de datos— apunta al paquete
  equivocado

**Lo único que la acota es autoimpuesto:** `Content-Security-Policy: connect-src`, que el navegador
sí hace cumplir de verdad — pero lo sirve **la misma institución** a la que se le quiere poner el
límite. Es una promesa suya verificada por el navegador, no una garantía del aparato. Y si viaja por
cabecera, la persona ni siquiera la puede leer.

### Lo que esto cambia entre A y B, y hay que decirlo así

| | ¿tiene red? | de qué tipo es la garantía |
|---|---|---|
| **adentro de Gnothi** (WebView) | **no**, medido | **de forma**: el módulo no tiene con qué salir |
| **suelta**, PWA instalada | **sí, la de Chrome entera** | **ninguna desde afuera**; solo lo que ella declare |

Esto **no invalida el camino A**: la app de la institución ya tenía red antes de que Gnothi
existiera, y la persona ya se la había concedido al usarla. Lo que cambia es **qué se puede
prometer**. Con la app suelta la frase honesta es *«tus datos quedan en tu carpeta»* — **no** *«esta
app no puede salir a la red»*.

### Las dos preguntas directas sobre la PWA suelta

Esto es **material de respaldo**, no algo para llevar a nadie: son las dos preguntas que vuelven
cada vez que se discute el camino A, con su respuesta y con de dónde sale cada parte.

---

#### 1 · ¿Se puede blindar la PWA suelta para que no salga a la red? **NO**

**Medido** (emulador, WebAPK instalada desde la página del laboratorio):

| | |
|---|---|
| uid de la WebAPK | `10222` — **no pide `INTERNET`** |
| uid de Chrome | `10150` — sí lo pide |
| el `fetch()` de la página sale por | **`10150`** |

**La PWA no tiene red propia que quitarle.** Por eso ninguna palanca alcanza:

| palanca | por qué no sirve |
|---|---|
| restringir el paquete de la WebAPK | no tiene red que restringir |
| datos en 2.º plano / ahorro de datos | apuntan al paquete equivocado |
| firewall por uid vía `VpnService` | el uid es el de **Chrome entero**: le cortás el navegador a la persona |
| CSP `connect-src` | **la sirve la institución**, que es a quien se quiere limitar |
| service worker | es código del mismo sitio |

No hay API del navegador, campo del manifiesto ni permiso de Android que le quite la red a **un solo
origen web**.

**La única forma en que sería «sí», y por eso es «no»:** que el CSP lo ponga alguien que no sea la
institución. El CSP viaja con el documento, o sea que lo pone **quien sirve la página**. Si lo sirve
Gnothi, Gnothi pone `connect-src` y el navegador se lo hace cumplir a la institución — que es
exactamente lo que hace el modo B, con su watchdog reportando violaciones por el puente.

**Pero en el instante en que Gnothi sirve la página, ya no es una PWA suelta.** Una PWA instalada
necesita un origen `https` estable para su manifiesto y sus asset links; `localhost` no puede serlo.

> **Blindar = que alguien más sirva. Suelta = que la sirva ella. Son excluyentes**, y no por un
> tecnicismo: es la definición de las dos cosas.

---

#### 2 · ¿Se puede alimentar su UI con datos que saque de Gnothi? **SÍ**

**Cómo:** Gnothi levanta un servidor HTTP en loopback y la PWA le hace `fetch` a
`http://127.0.0.1:<puerto>`.

**Investigado** (documentación de Chrome, septiembre 2026):

- **`localhost` / `127.0.0.1` está exento del bloqueo de contenido mixto**, explícitamente. Una
  página `https` le puede pegar. *(Corrige lo que se dijo antes en este mismo frente: se había
  afirmado que era contenido mixto con reglas enredadas, y es falso.)*
- Desde **Chrome 142** lo gatea el permiso de **Local Network Access**, con un prompt a la persona.
- Gnothi ya tiene `INTERNET`, así que puede abrir el socket.

**El problema real y su solución.** El puerto no sabe quién llama: cualquier app del aparato, o
cualquier sitio que la persona visite, le puede preguntar. Se resuelve con un **token**, y el canal
para entregarlo **ya está medido**: el enlace profundo que funciona desde el WebAPK. Gnothi genera un
token por app, lo manda en el enlace, la PWA lo guarda, y Gnothi solo contesta pedidos que lo traen.

**Lo que hay que aceptar:**

| | |
|---|---|
| el prompt de LNA dice | *«buscar y conectarse a cualquier dispositivo de tu red local»* — mucho más ancho de lo que hace |
| Gnothi tiene que estar corriendo | con el socket abierto mientras la PWA esté abierta |

##### El proceso en segundo plano, que es lo que esto exige

**Android: se puede, con un costo visible.** Un socket abierto de forma indefinida necesita un
**foreground service**, o sea **una notificación permanente**. Desde Android 14 hay que declarar un
`foregroundServiceType` y **no existe uno para «servidor local»**: caería en `specialUse`, que Play
revisa y obliga a justificar. Sin foreground service, los límites de ejecución en segundo plano lo
matan; y **Doze más los gestores de batería de los fabricantes** lo matan igual — el Xiaomi de las
pruebas es de los agresivos.

Salida parcial: **levantarlo bajo demanda**. Pero nada lo puede despertar salvo que la persona abra
Gnothi por el enlace profundo — un salto de app por sesión.

**iOS: FUERA DE ALCANCE, y no es un pendiente.** El camino A está enfocado en **Android + Chrome**,
que es donde el picker del navegador entrega una carpeta y donde existe el SAF. No hay que ir a
medirlo.

Para que quede la razón por si vuelve: la documentación y los foros de Apple coinciden en que **no
hay forma general de mantener sockets vivos con la app en segundo plano** — un socket de escucha se
congela o lo matan tras un rato de inactividad, y ningún background mode cubre «servidor HTTP local».
Es **coincidente con lo esperado, no medido por nosotros**.

---

#### Lo que hay que decir junto, o el argumento se cae

**Las dos respuestas no se combinan.** Se puede alimentar la UI desde Gnothi (2 = sí) y **no** se
puede impedir que además salga por su cuenta (1 = no).

Que pida por el API es una **convención, no una restricción**, y se ve en el inspector en dos toques.

Eso no la vuelve inútil: da el **dueño único del journal** —un solo juego de semáforos— y adelgaza la
app enormemente. Pero es un argumento de **arquitectura**, no de **garantía**. Presentado como
garantía se cae solo.

---

## Los canales de la lámina 04 — 2026-09-11

**Qué se fue a contestar.** La lámina 04 de `B6` dice que la app de la institución es solo la cara y
le pregunta a Gnothi qué pintar. Eso es **una API en el camino caliente de la UI**, y la pregunta es
por qué canal viaja cuando la cara es **contenido web dentro de un navegador**. El razonamiento y el
cuadro están en `B7`; acá van los números y los textos literales.

**Se midieron también los mecanismos que no sirven.** No es exhaustividad: un «no» medido —con el
error exacto y el número de versión— no se discute, y una cita de documentación sí.

**Instrumento.** Emulador `GnothiWeb` (`emulator-5554`), **Chrome 133.0.6943.137**, WebAPK
`org.chromium.webapk.ac970f0088360685e_v2`, corriendo **dentro** de la PWA instalada
(`display-mode: standalone`, verificado en la propia corrida). El spike es `Spikes/Lab.SondaWeb/`.

| mecanismo | veredicto | la evidencia, literal |
|---|---|---|
| **sockets crudos** | **no existen** | `'TCPSocket' in self === false`, y lo mismo `UDPSocket` y `TCPServerSocket` |
| **`content://`** | **no fetchable** | `Fetch API cannot load content://…  URL scheme "content" is not supported` |
| **Shared Storage (W3C)** | **no es un canal** | existe con toda su superficie (`ctor=SharedStorage`, los 8 métodos), y aun así: `set` → `OperationError: sharedStorage is disabled`; `get` → `OperationError: Cannot call get() outside of a fenced frame`; y en consola `Attestation check for Shared Storage failed` |
| **`targetAddressSpace`** | acepta `local`, `private`, `public`, `unknown` | `'loopback'` → `The provided value 'loopback' is not a valid enum value of type IPAddressSpace`. La escotilla existe, con el vocabulario de *Private Network Access*, no con el de LNA |
| **`getInstalledRelatedApps`** | **lista vacía**, y **por setup** | `[]` — falta el `asset_statement` del lado del APK, y el `related_applications` del manifest declara `platform: "play"`, que un APK cargado de costado no satisface. Lo dice el propio comentario del manifest. **No es límite de plataforma** |
| **HTTP a loopback** | **el navegador lo intentó** | `net::ERR_FAILED` a los 449 ms — tardó, o sea que salió a la red; no fue un bloqueo |
| **WebSocket a loopback** | **el navegador lo intentó** | `net::ERR_CONNECTION_REFUSED` — explícito, nadie escuchando |
| **arranque del foreground service desde adb** | **permitido**, y **levanta Gnothi sin ninguna Activity** | `Background started FGS: Allowed`, y `Start proc … for service {LoopbackSpikeService}` |

### Las dos últimas filas NO valen como respuesta, y la razón es del experimento

La WebAPK instalada fue minteada contra **`http://127.0.0.1:8096/app/`** — se lee en su propio
manifest (`org.chromium.webapk.shell_apk.startUrl`). Ese origen **es** contexto seguro por la
excepción de `localhost`, y por eso se eligió: ahí funcionan el service worker, la instalación y el
picker. Pero para medir el canal lo invalida:

- un `fetch` de `http://127.0.0.1` a `http://127.0.0.1` es **loopback → loopback**, y eso **no es un
  cruce** en la taxonomía de Local Network Access;
- la página es **`http`**, así que **contenido mixto no aplica** — que es justamente la regla que se
  quería medir.

Pasan, y pasan por razones que en producción no se sostienen, donde la PWA la sirve la institución
desde `https://…`.

**Y un segundo techo del aparato:** Chrome **133**. El prompt de Local Network Access aterriza en
**142**, así que este emulador **no puede medirlo nunca**.

Para que esas filas valgan hay que reinstalar la PWA desde un origen **https y no-loopback**. El
certificado de `Examenes/PaginaDeDescarga/certs` **ya tiene el SAN necesario** (`DNS:localhost`,
`IP:127.0.0.1`, `IP:<ip-lan>`): falta servir el `8453` e instalar la CA en el aparato. Eso mide
el cruce `local → loopback`; el `public → loopback` de producción pediría un dominio de verdad.
**Ojo: reinstalar cambia la WebAPK**, y con ella el paquete que estas mediciones citan.

### Tres trampas del método, que valen para cualquier spike futuro

**El service worker se come la sonda.** Su handler de `fetch` contesta **toda** navegación de su
scope con el `index.html` cacheado, salvo que la URL esté en el manifiesto de assets. Abrir
`/app/sonda.html` dentro de la WebAPK devolvió **el 404 del router de Blazor** con esa misma URL en
la barra. Está advertido en `Examenes/PaginaDeDescarga/nginx.conf`, y aun así pasó.

**Chrome de canal estable no manda la consola de la página a logcat.** `adb logcat -s chromium` dio
**cero** líneas de la sonda. Y el motivo de un bloqueo **solo vive en la consola**: desde JS, un
bloqueo por contenido mixto y un `connection refused` entregan el mismo `TypeError`. El instrumento
terminó siendo un cliente CDP por `adb forward` (`Spikes/Lab.SondaWeb/cdp.ps1`), que además evalúa
en la página **ya abierta** y así esquiva al service worker.

**`strings` no existe en esta máquina.** Una búsqueda de rastros en los logs de una app devolvió
«cero coincidencias» que no eran un hallazgo, eran un comando vacío; con `grep -a` había 31. Un
instrumento que falla en silencio produce conclusiones al revés.

---

## La forma híbrida, de punta a punta en un teléfono — 2026-09-11

**Qué se fue a contestar.** Que el motor no viaje en el módulo: que la cara sea solo UI y que
Puppeteer lo preste Gnothi. El diseño y las siete costuras están en `C9`; acá van los números.

**Instrumento.** Xiaomi `2407FPN8EG`, **Android 16**, APK en **Release**, motor
`3.0.0-beta.10029`, módulo `examenes` en forma híbrida, publicador local por `adb reverse`.

### Lo que pesa

| | hospedada | híbrida |
|---|---|---|
| el `.gmod` | **12 594 KB** · 238 archivos | **97 KB** · 9 archivos |
| motor adentro | 4 ensamblados en `web/_framework` | **cero** |
| la cara | Blazor Wasm | **57 KB** (html + css + js) |
| el dominio | en `.wasm` | **200 KB** en 3 `.dll` |

**130 veces menos**, y el ahorro no es el motor: está medido que correr .NET en un navegador es el
88,8 % de una hospedada y el motor el 8,6 %.

> **La primera medición dio 77 KB, con una cara de 9,5 KB.** No era el mismo objeto: esa cara
> mostraba cuatro números y una tabla. Portar la pantalla real —portada, chips, cinco formas de
> dibujo, cronología— la llevó a 57 KB y el paquete a 97. **El número que sirve para comparar contra
> la hospedada es el segundo**, porque es el primero que pinta lo mismo. La conclusión no cambia:
> 130× en vez de 163×, y sigue siendo el runtime lo que se saca, no el dominio.

### Lo que corrió

```
el cartero contestó en 517 ms:
{"entregado":true,"resultado":"leidos=26 aplicados=26 descartados=0 rechazados=0
 ultimoEvento=225 cuenta=migrated"}
```

Y la pantalla, pintada por `[View]` sobre la superficie en proceso: **26 exámenes registrados, 0
analitos fuera de rango**.

**`aplicados=26 descartados=0 rechazados=0`**: la frontera de entrada corrió y no descartó nada.

### Lo que nunca se había medido, y quedó contestado

**Mono carga un ensamblado ajeno en Android.** `InProcessShell` —`AssemblyLoadContext`,
`GetTypes`, `GetMethods`, `GetProperties`— funciona en un teléfono físico, en Release. Verificado
por el alta contra la superficie real, no por nosotros:

```
comprobación de examenes: {"matches":true,
  "declared":["lista_de_examenes","resumen"], "actual":["lista_de_examenes","resumen"]}
```

Las **siete** `[View]` no aparecen ahí — son otro público.

**Y una pantalla de institución completa, sin motor propio.** Con la cara portada (más abajo), lo
que pinta sobre el motor prestado no es una demo de dos números: la portada con sus veredictos, los
chips por analito, las **cinco formas de dibujo** y la cronología de visitas, todo con los datos
reales del journal.

### Cuatro defectos que SOLO aparecieron corriéndolo

Ninguno se habría visto revisando el código, y los cuatro son del tipo que vale registrar.

**1 · `INTERNET` no viaja en Release.** El SDK de .NET Android lo agrega **solo en Debug**, para el
depurador. El manifiesto que se lee en `obj/Debug/…/AndroidManifest.xml` lo tiene, y eso induce a
darlo por declarado. Síntoma: `HttpRequestException: Permission denied (127.0.0.1:8095)` —
**loopback y todo, Android exige `INTERNET` para abrir cualquier socket**. Y se lee como una
restricción del fabricante: `INTERNET` es un permiso normal, no sale en la pantalla de permisos de
la app, y el único lugar donde se ve la verdad es `dumpsys package`.

**2 · La semilla no es una entrada de bitácora: crea los globales.** Un actor escribible con
`OnHydrated` vacío —puesto para no sembrar en cada entrega— deja al DSL sin `expediente` ni
`bitacora`, y el bucle muere al pedir el cursor con `Unknown property or method 'UltimoEvento' on
type 'Object'`. Ya estaba advertido en `ExamenesApp`: *«la vista de SoloLectura del MCP —que hidrata
con OnHydrated vacío— rehidrataba y no encontraba el global»*. La guarda de `upgrade('semilla')` es
una-vez-por-actor, así que no había nada que evitar.

**3 · Una carrera que solo la forma híbrida crea.** El Host cableaba la superficie **después** de
`Start`, y `Start` espera a que la página diga `listo()`. Una cara híbrida dice listo y **pregunta
en el mismo tick**: no tiene actor que hidratar, nada la demora. Resultado: las primeras preguntas
llegaban con el delegado en `null`. El cableado tiene que ir **entre `Create` y `Start`**.

**4 · El orden de los argumentos de la cáscara.** Poner el dll en `Arguments[0]` —porque es lo que
contesta— rompe a `WebModuleHost.Open`, que lee `[0]` como la carpeta del bundle para abrir la
pantalla desde el catálogo: intentaría servir un `.dll` como carpeta, y fallaría lejos del cambio.
Quedó `[web, endpoint, dll]`: **el consumidor nuevo se acomoda, no los que ya andaban**.

### Y un método que vale para cualquier prueba en un teléfono

**El log no sirve en un Xiaomi.** MIUI inunda `logcat` —31 721 líneas en el buffer— y las líneas
propias se rotan antes de poder leerlas. Lo que destrabó el diagnóstico fue **hacer que la pantalla
lo dijera**: una tarjeta con el resultado del refresco. Sin ella, *«no hay exámenes»*, *«el cartero
no pudo»* y *«no hay puente»* se ven exactamente igual.

Y el tiempo, por sí solo, señala dónde está el problema: el cartero pasó de **fallar a los 230 ms**
—sin permiso, antes de tocar la red— a **fallar a los 1003 ms** —salió, volvió y reventó adentro— y
por fin a **contestar en 517 ms**.

---

## Portar la pantalla del laboratorio a la cara híbrida — 2026-09-11

**Qué se fue a contestar.** Si una cara sin motor puede pintar **la misma pantalla** que la app
Blazor, no una parecida.

**El error que hizo falta corregir, primero.** La primera cara tomó los tokens de
`Examenes.Wasm/wwwroot/css/app.css` —el navy, el celeste, Georgia, la gota— y de ahí se afirmó que
la pantalla se parecía a la original. **No se parecía**, y la comparación nunca se había hecho: la
estructura estaba inventada. Vale como trampa de método, porque es fácil de repetir: **copiar la
paleta no es portar la pantalla**. Una cara que reinventa los colores es otra institución para quien
la mira; una que reinventa el layout es otra app, con los colores correctos.

**Lo que había que traer, y estaba en dos archivos:**

| pieza | de dónde |
|---|---|
| portada con veredictos, caritas y `Actualizar` **adentro** de la tarjeta | `Pages/MisExamenes.razor` |
| tarjeta de serie: chip, relato, valor en serif, referencia en mono, lectura abajo | `Pages/MisExamenes.razor` |
| cinco componentes de dibujo | `Formas/*.razor` |
| historial como cronología de visitas | `Pages/MisExamenes.razor` |

### Las cinco formas, y el defecto que ocultaba tenerlas colapsadas

El dominio dice qué forma le toca a cada analito. La primera cara **las colapsó todas en un gráfico
de línea genérico**, que es exactamente lo que el comentario del `.razor` advierte que no se haga:
*dibujarlas igual sería la misma tabla que se rechazó en el dominio, un piso más arriba*.

Y el defecto que eso produce es medible, no estético: **un binario no tiene puntos con número**, así
que pedirle la serie devuelve ceros y sale **una línea plana en cero** donde debería decir «apareció
en las 2 seguidas».

El arreglo mueve la decisión a donde vive: `[View("tendencia")]` **pregunta por la forma antes que
por los hechos** y devuelve un paquete distinto según cuál sea — `presencia`, `constancia`, o
`puntos + referencias`.

| forma | qué dibuja |
|---|---|
| `trayectoria con linea base` | la serie contra la banda del nivel de siempre |
| `progreso hacia meta` | el eje es el **valor**, de la partida a la meta |
| `posicion en banda` | dónde caés entre piso y techo |
| `presencia en el tiempo` | la tira de pruebas, la racha final en grueso |
| `categoria en el tiempo` | igual, con vocabulario de colores |

### Tres defectos más de la cara, todos vistos en el teléfono

**1 · Cada serie salía DOS veces, y no era el dominio.** La primera sospecha fue que
`expediente.Analitos` devolvía nombres repetidos. No: la pantalla **se pinta dos veces** —lo que ya
hay, y otra vez cuando vuelve el cartero— y las dos cadenas de `tendencia` seguían agregando sobre
la lista recién limpiada. Se arregló con un contador de pasada. **Vale como recordatorio**: el
duplicado en una lista asíncrona casi nunca está en el origen de los datos.

**2 · El semáforo mandaba dos severidades al verde.** El vocabulario del dominio es cerrado
—`informativo`, `vigilar`, `consulte pronto`, `urgente` (`Examenes/Dominio/Severidad.cs`)— y la cara
lo adivinaba por prefijo, así que `consulte pronto` e `informativo` caían en el caso por omisión.
Dos reglas que el dominio ya tenía escritas y la pantalla rompía: **el rojo es solo de `urgente`**, e
**`informativo` no es verde** — el verde dice «esto está bien», y un dato que falta no está bien ni
mal.

**3 · Un solo punto no es una tendencia.** Dibujarlo igual daba un rectángulo de banda con un
círculo al medio: ocupa lo mismo que un gráfico y no dice nada de lo que un gráfico dice. Y el
dominio ya lo decía mejor con palabras: *«una sola medición: todavía no hay serie para decir para
dónde viene»*.

---

## Un icono de la institución en la pantalla de inicio — 2026-09-11

**Qué se fue a contestar.** Si al instalar un módulo se puede dejar un icono que parezca «la app de
la institución» y por dentro sea Gnothi abriendo su módulo.

**Sí, con `ShortcutManager.requestPinShortcut`** (API 26; el mínimo de Gnothi es 24, así que la
guarda pasa de verdad). Verificado en el Xiaomi: el icono queda en la pantalla de inicio, con el
logo y el nombre de la institución, y al tocarlo —**con Gnothi cerrada**— levanta directo la cara
del módulo, sin pasar por la pantalla de Gnothi.

```
Pinned: modulo:examenes
  intents=[Intent { act=VIEW cmp=com.ncubo.gnothi.host/…MainActivity }
           /PersistableBundle[{gnothi.modulo=examenes}]]
  bitmapPath=…/shortcut_service/bitmaps/com.ncubo.gnothi.host/…png
```

### Dos previsiones que salieron al revés

Las dos estaban escritas antes de medir, y las dos fallaron. Quedan acá porque el valor está en que
fallaron:

| se predijo | MIUI hizo |
|---|---|
| Android 8+ **siempre** muestra un diálogo de confirmación | **ninguno**: puso el icono directo |
| el launcher estampa un **badge de Gnothi** en la esquina | **no lo estampa** |

La segunda importa más que la primera. El badge era el argumento de honestidad —*«dice quién corre
el código»*— y sin él, **en la pantalla de inicio nada distingue ese icono del de una app nativa de
la institución**. Es una decisión de producto abierta, no un bug.

### Tres decisiones de implementación que la medición justificó

- **Intent explícito, no el deep link.** `go.gnothi.app/abrir?modulo=…` ya existe, pero un `https`
  sin `assetlinks.json` verificado saca el diálogo de «¿con qué app abro esto?», depende de DNS, y
  cualquiera puede registrar un handler para ese dominio. Un intent contra la clase no sale del
  teléfono.
- **El icono se compone.** Un icono adaptativo recorta todo lo que queda fuera del 66 % central, así
  que el logo a sangre pierde los bordes. Se dibuja sobre el color de marca, al 52 % de un lienzo de
  432 px.
- **El rótulo es el nombre, en los dos campos.** Empezó siendo el título del módulo —lo que uno
  escribiría— y MIUI, que prefiere el rótulo largo, lo cortó a **«Tus examenes,…»**. Un icono lleva
  un nombre, no un eslogan, y los dos campos tienen que poder leerse cortados.

### Y una herramienta que no era obvia

**`adb shell cmd shortcut clear-shortcuts <paquete>`** quita los accesos directos fijados **sin
tocar los módulos ni el journal**. Desde adentro de la app no se puede: la API solo deja
*deshabilitarlos* con un mensaje. Sirve para volver a correr la prueba desde cero sin desinstalar.
