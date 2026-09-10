# Pedido a quien mantiene `Ncubo.Puppeteer` · 2026-09-09

**Dos cambios al backend `FileSystem`**, para que un consumidor pueda **leer** un journal que otro
proceso escribió, sin modificarlo y sin copiarlo.

Los dos están **escritos, medidos y corridos de punta a punta** en un worktree desprendido de master
`8ef4415`, así que abajo hay números en vez de estimaciones y el parche está para mirar. **La forma
la deciden ustedes** — lo que sigue es lo que necesitamos y lo que aprendimos midiéndolo, no un
diseño cerrado.

---

## El problema, en un dibujo

![Dos tiras de tres cuadros. Arriba, hoy: el dueño está escribiendo en su carpeta; llega un lector y la abre; al abrirla le corta la página que estaba escribiendo, le borra dos papeles sin resolver y le deja ocho carpetas vacías. Abajo, con el parche: la misma carpeta, el mismo lector, y al final queda exactamente igual.](imagenes/por-que-el-parche.svg)

**Ninguna de esas tres cosas es un descuido del motor: las tres son lo que corresponde hacer cuando
la carpeta es tuya.** Curar una página a medio escribir, resolver una transacción que quedó colgada,
garantizar que la estructura exista.

**Lo que falta no es corregirlas — es poder decir «esta carpeta no es mía»**, que hoy no se puede
decir.

| en el dibujo | en el código |
|---|---|
| la página cortada | `JournalWriter :233` — `activeStream.SetLength(lastValidPosition)` |
| los dos papeles borrados | `RecoverFromIncompleteOperation`, llamado desde el `LoadX` de **8 stores** |
| las ocho carpetas | `DiaryStorageFileSystem :78-85` |

Y las tres corren **en el constructor**: antes de `Start()`, antes de la primera consulta. Un
consumidor que solo quiere leer escribe una línea —`ConfigureStorage(FileSystem, "path=…")`— y eso
es todo lo que hace falta para que pase.

---

## Los dos pedidos, en una línea cada uno

| # | qué | dónde |
|---|---|---|
| **1** | que el backend **lea** a través de una abstracción inyectable en vez de llamar a `System.IO` directo — **seis operaciones, todas de lectura** | `IJournalFiles`, resuelta por conexión |
| **2** | un **modo de solo lectura** en el que abrir un journal no le escriba nada | `readOnly=true` en el connection string, **8 compuertas** |

**Se piden los dos, no uno.** El 2 no es prudencia: **sin él la lectura por el puerto no funciona**.
Está medido más abajo.

---

## El caso, corto

Una institución tiene una app web que la persona ya usa. Esa app elige una carpeta de ella y escribe
ahí su journal. Del lado del asistente hay un módulo que necesita **leer ese mismo journal** para
poder contestar preguntas sobre él.

En escritorio la carpeta es una ruta y `System.IO` llega. En Android **no**: el navegador entrega la
carpeta como una concesión del Storage Access Framework, cuyo permiso vive en la tabla del
`ContentResolver` **indexada por URI**, y el `open()` al que baja `System.IO` pasa por scoped storage,
que nunca consulta esa tabla.

**Medido, y el modo de fallar es el peor posible:**

| operación | resultado |
|---|---|
| `Directory.Exists` sobre la carpeta concedida | **True** |
| `File.Exists` sobre un archivo que está ahí | **True** |
| `Directory.EnumerateFiles` | **0 archivos, y sin excepción** |
| `File.OpenRead` | `UnauthorizedAccessException` |
| **contraste:** los mismos bytes por `ContentResolver` | ✅ se leen sin problema |

O sea que un motor apuntado a esa ruta **no ve un error: ve un journal vacío**. El contraste es lo
que vuelve esto concluyente — la carpeta es la correcta, la concesión está viva, los bytes están ahí.
Son **dos puertas a la misma carpeta y la llave abre una sola.**

---

## Pedido 1 · El puerto de lectura

Una interfaz de seis métodos, y la implementación por default es exactamente lo que el backend hace
hoy:

```csharp
public interface IJournalFiles
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    byte[] ReadAllBytes(string path);
    Stream OpenRead(string path);
    string[] ListFiles(string directory, string searchPattern);
    DateTime GetLastWriteTimeUtc(string path);
}
```

**Por qué seis y no un puerto de archivos completo.** Se censaron las llamadas a `System.IO` de todo
el backend (5 890 líneas). El **camino de lectura** usa esas seis y nada más:

| operación | dónde |
|---|---|
| `FileExists` | `JournalReader:54,79` · `SparseIndex:37` · `MetadataStore:43` · los stores auxiliares |
| `ReadAllBytes` | `SparseIndex:40` · `MetadataStore:46` · reacciones, skips, checkpoints, outbox, elisiones |
| `OpenRead` + `Seek` | `JournalReader:91,224` · `JournalWriter:353,671` |
| `ListFiles` | `JournalWriter:305` (`journal_*.bin`) · `PendingBatchDirectory` (`*.batch`) |
| `DirectoryExists` | descubrimiento en `DiaryStorageFileSystem` |
| `GetLastWriteTimeUtc` | `EventElisionStorageFileSystem` — `RefreshIfStale` |

**Y las dos operaciones que una API basada en URIs no tiene, no aparecen.** Las dos son de
escritura y quedan intactas:

- el *replace* atómico — que **ya está detrás de un puerto**, `IAtomicFileOperation`, con su fábrica
  por plataforma y tres implementaciones
- el lock entre procesos por `FileShare.None` — que tiene **dos usos reales**: el rollover en
  `JournalWriter:487` y el consolidador de elisiones en `PendingBatchDirectory:64`

Corroborado también por medición y no solo por lectura: en modo lector el `AtomicReplace` **lanza** en
vez de callar, y **no lanzó en ninguno de los siete escenarios** ni en la corrida real.

### Cómo elige una conexión su puerto

El host **registra por nombre** y la conexión lo nombra, en el mismo lugar donde ya viven `path`,
`readOnly`, `maxFileSize`, `compression` y `encryption`:

```csharp
JournalFiles.Register("examenes", new SafJournalFiles(...));   // el host, una vez
actor.ConfigureStorage(DatabaseType.FileSystem,
    "path=/…/journals/examenes;readOnly=true;files=examenes");
```

**`files=` vacío —que es toda conexión que no pregunta— resuelve a `System.IO`.**

Un nombre que nadie registró **lanza** en vez de caer a `System.IO`, y es deliberado: caer abriría
otra carpeta —en Android, una ruta que no existe— y contestaría «acá no hay nada». **Una respuesta
que parece cierta es peor que una falla.**

> **La primera versión de esto era un único «lector actual» de proceso, y estaba mal.** Con un solo
> global, dos actores leyendo dos carpetas por dos puertas distintas no pueden coexistir: el último
> en registrar decide por los dos, en silencio. Por eso la selección es **por conexión**.

### Una forma más limpia, si la prefieren

Lo de arriba funciona, pero conviene decir en qué se queda corto: **pasamos de un global a un
diccionario global.** Arregla la coexistencia, y sigue siendo un localizador de servicios escondido
en un string.

El síntoma es concreto: **cómo sabe el módulo qué nombre poner en `files=`.** Hoy usa el nombre de su
actor por convención y consulta `JournalFiles.IsRegistered(actor)` para saber si nombrarlo — o sea que
el módulo termina consultando el registro del Host, que es una preocupación ajena filtrada hacia él.
Y además puede nombrar un puerto que nadie le asignó.

**Si en vez del nombre viajara el objeto** —una sobrecarga de `ConfigureStorage` que acepte el
`IJournalFiles`, o el puerto entregado junto con la ruta— esa pregunta desaparece: quien concede la
carpeta entrega también la puerta, el módulo no consulta nada, y no hay registro global.

**Lo elegimos por costo, no por convicción:** meter el objeto significa hilarlo por el handler, y el
mecanismo por nombre nos alcanzaba para medir. **Si les cierra más la otra forma, del lado nuestro no
cambia nada** — el `SafJournalFiles` es el mismo objeto, solo cambia cómo llega.

---

## Pedido 2 · El modo lector

`readOnly=true` en el connection string — la convención del propio backend para las propiedades del
store que se abre, no del llamador.

**Lo que hoy escribe una «lectura»,** medido con diff de bytes sobre siete escenarios:

| # | lo que hace al abrir | dónde |
|---|---|---|
| 1 | escribe el índice reconciliado | `sparseIndex.Save()` |
| 2 | reescribe `meta.bin` | `metadata.Save()` |
| 3 | toma un **handle de escritura** sobre el archivo activo del dueño | `JournalWriter:69` — `FileAccess.ReadWrite` |
| 4 | **trunca la cola partida** del archivo activo — 715 → 695 bytes | `Initialize()` |
| 5 | crea **7 carpetas** en la carpeta ajena | `DiaryStorageFileSystem`, 8 × `CreateDirectory` |
| 6 | **borra 12 archivos del dueño** (`.tmp` y `.bak`) | `RecoverFromIncompleteOperation`, llamado desde cada `LoadX()` |
| 7 | crea la carpeta del journal | `JournalWriter:46` |
| 8 | crea `pending/` y **borra los `.tmp` huérfanos del dueño** | `PendingBatchDirectory`, en el constructor |

Las ocho corren **al abrir**, antes de leer nada. La 6 y la 8 no modifican bytes: **eliminan
archivos**. Un `.tmp` ahí es una transacción cuyo dueño murió a mitad; descartarla es decisión de él,
no de un lector.

**Ninguna de las ocho es un choque entre dos procesos: todas tienen éxito.** Ese es el problema — no
es contención, es mutación que nadie pidió. Y el lector no resulta un segundo escritor por casualidad
de timing: **lo es por construcción**, porque el backend no tiene hoy ningún modo en el que abrir un
journal sea solo leerlo.

### La 4 es de otra categoría, y es la que pondríamos primero

**Un lector le trunca el archivo activo al dueño.** `Initialize()` recorre la cola del archivo
activo, encuentra un registro cuya longitud promete más bytes de los que hay, y lo corta: medido,
715 → 695 bytes.

Sobre un journal cuyo dueño **murió**, eso es lo correcto y es exactamente para lo que está. Sobre un
journal cuyo dueño **está escribiendo ese registro ahora mismo**, esa cola no está partida: está **a
medio escribir**. Y truncarla es perderla.

**No lo vimos pasar en la corrida concurrente** —el dueño escribió sus 120 entradas y no perdió
ninguna—, pero eso no lo descarta: es una ventana, y no encontrarla en una corrida no prueba que no
esté. Es la única de las ocho donde el daño posible no es «te toqué la carpeta» sino **«te perdí un
registro»**.

Y es la razón por la que la 3 y la 4 van juntas: el parche abre el archivo activo `FileAccess.Read`
**y** no trunca. Con solo una de las dos, la otra sigue abierta.

**`ConfigureStorageForIntrospection` suena a que ya es esto y no lo es** — medido: también escribe el
índice y el meta.

---

## Lo que cuesta

| | |
|---|---|
| archivos tocados | **13 existentes** (+295 / −89) y **4 nuevos** (217 líneas) |
| líneas de **lógica** | **~280**; el resto son comentarios |
| suite del motor con el parche | **5 fallan · 2 087 pasan** — **idéntico a master limpio**, los mismos cinco nombres, todos de locale |
| **cambios en sus tests** | **cero** |
| sin opt-in | sin `readOnly=true` y sin `files=`, el backend se comporta igual |

**Lo de «cero cambios en sus tests» costó trabajo y es a propósito.** Hacer el parámetro obligatorio
rompía **144 sitios de compilación** en la suite. Se conservaron las firmas viejas encadenando al
puerto por default:

```csharp
internal MetadataStore(string filePath, IAtomicFileOperation atomicOp)
    : this(filePath, atomicOp, JournalFiles.Resolve("")) { }
```

---

## La evidencia

### El diff de bytes, siete escenarios

Cada caso corre sobre **su propia copia** del journal sembrado, y se comparan hashes antes y después.
Los escenarios están hechos para no halagar la respuesta:

| escenario | qué siembra | `rehydrate` (control) | **con `readOnly`** |
|---|---|---|---|
| CLEAN | 3 entradas, un archivo | escribió 2 | **nada** |
| ROLLED | 40 entradas, varios archivos sellados | escribió 2 | **nada** |
| TORN | ROLLED + un registro partido al final | escribió 3 | **nada** |
| RICH | + reacciones, checkpoint, outbox, elisiones | escribió 1 | **nada** |
| RICH-INTERRUPTED | + un replace atómico a medias | **borró 12 archivos** | **nada** |
| FOREIGN | un journal **sin** las carpetas auxiliares | **creó 6 carpetas** | **nada** |
| CONCURRENT | un lector abriendo mientras el dueño escribe | — | ver abajo |

Y en todos, **el lector leyó las mismas entradas que el control** — no está leyendo menos.

### El escenario concurrente

Dos **procesos** (no dos hilos: con hilos comparten handles y locks, que es lo que la topología real
no comparte). El hijo escribe 120 entradas cada 25 ms con `maxFileSize=1024`; el padre abre un lector
y lo recorre diez veces; **hubo 6 rollovers durante la corrida**.

| | con `readOnly` | sin (control) |
|---|---|---|
| el dueño | escribió las 120, sin fallar | escribió las 120, sin fallar |
| el lector lanzó | nada | nada |
| al final | 126 entradas, la última correcta | 126 entradas, la última correcta |

**Conviene decirlo aunque juegue en contra: la concurrencia no rompió, ni siquiera sin el parche.**
El problema no es contención. Es que leer escribe.

### La corrida real, en un teléfono

La app web de la institución, instalada como WebAPK, escribiendo su journal en una carpeta que la
persona eligió por el SAF. El módulo leyendo **esa** carpeta a través del puerto, sin copia.

**Reparto realista:** la app quedó publicada con el motor **sin parchear** (`3.0.0-beta.10029`), y el
único que lleva el parche es el lector.

| paso | la app dice | el módulo dice |
|---|---|---|
| 1 · la app trae su expediente | 11 exámenes · evento 210 | — |
| 2 · el módulo lee la misma carpeta | 11 · 210 | **11 exámenes · evento 210** |
| 3 · dos eventos nuevos **en el servidor de la institución** | 11 · 210 | **11 · 210** |
| 4 · la app reabre y los trae | **13 · 212** | — |
| 5 · el módulo lee otra vez | 13 · 212 | **13 exámenes · evento 212** |

La carpeta de la persona, por md5, en cada paso de lectura:

| archivo | antes | después |
|---|---|---|
| `meta.bin` | `78524…` | `78524…` |
| `journal/journal_000001.bin` | `61aa4…` | `61aa4…` |
| `index/index.bin` | `98cee…` | `98cee…` |

**Idénticos, y con la misma fecha.** El motor abrió, rehidrató y contestó dos veces sin escribirle
nada.

### El control sin el modo lector

Mismo puerto, misma carpeta, misma concesión. Se quitó **solo** `readOnly=true`:

| | qué contestó el módulo |
|---|---|
| **con** `readOnly=true` | 13 exámenes · evento 212 |
| **sin** | **`null`** |

**Por qué se rompe, que es el argumento central del pedido 2:** el motor le pregunta al puerto si el
archivo activo del journal existe —el puerto dice que sí, y es cierto— y después lo abre **con
`System.IO`** sobre una ruta que **solo existe a través del puerto**. Las dos mitades no coinciden.
Un `open` de lectura-escritura sobre un journal alcanzable solo por el puerto **no está bien
definido**; el único que sí lo está es el de solo lectura.

---

## Lo que NO se pide

- **Nada de Android en el motor.** Cero. El diff del motor no tiene una línea específica de
  plataforma; lo del SAF vive del lado del host (~200 líneas).
- **Ningún puerto de escritura.** Las dos operaciones que SAF no tiene son de escritura y no se
  tocan.
- **Nada sobre `mmap`.** Se buscó: no hay ninguno en el motor.
- **Ninguna reescritura del backend.** El censo dio seis operaciones de lectura, no 6 000 líneas.

---

## Lo que NO está medido, y conviene saberlo antes de aceptar

- **El consolidador de elisiones** —el lock entre procesos por `FileShare.None`, diseño de ustedes—
  **no lo ejercitamos**. Un lector no lo toma, pero eso está sin medir.
- **Escritorio nunca se corrió de punta a punta.** Que ahí el modo lector alcance solo sale del censo
  y del spike, no de una corrida.
- **Elisiones y reacciones escribiéndose concurrentemente**: sin ejercitar. Se sembraron, no se
  escribieron durante la lectura.
- La verificación bajo concurrencia es **cantidad de entradas + contenido de la última**, no
  integridad byte a byte de cada registro: bajo concurrencia no hay línea base estable.
- **La lista de compuertas no está cerrada.** Van ocho; la 7 y la 8 aparecieron después de las
  primeras cinco, y las dos las destapó ponerse en un caso más incómodo, no razonar mejor. Lo que se
  lleva es el mecanismo, no el número.
- **`GetLastWriteTimeUtc` sobre SAF** no se midió. `COLUMN_LAST_MODIFIED` está en el contrato del
  proveedor, así que casi seguro anda — pero *casi seguro* no es medido.

---

## Cómo reproducirlo

El parche vive en un worktree desprendido de master `8ef4415`, **sin commitear**.

| qué | dónde |
|---|---|
| el parche | `Puppeteer/EventSourcing/DB/FileSystem/` + `Puppeteer/JournalFiles.cs` |
| el arnés de diff de bytes | `Spikes/ReaderModeSpike/` — **no está en la solución**, para no ensuciarla |
| la suite | `dotnet test UnitTestPuppeteer` |

```bash
dotnet run --project Spikes/ReaderModeSpike     # los siete escenarios, con su veredicto por caso
dotnet test UnitTestPuppeteer                   # 5 fallan · 2 087 pasan, igual que master
```

Cada escenario imprime su veredicto por caso —qué archivos aparecieron, cambiaron o desaparecieron—
así que el diff de bytes de la tabla de arriba se vuelve a producir sin tener que creernos nada.
