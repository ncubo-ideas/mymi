# BORRADOR · Coordinación PWA ↔ Gnothi (para retomar)

> Nota de trabajo, no corpus pulido. Captura la sesión sobre **cómo el dato del PWA de una
> institución llega al módulo que Gnothi carga**, en desktop y móvil. Corte: 2026-09-02.

---

## 0 · El problema, en una línea
La institución tiene un **PWA** (Blazor WASM) que la persona usa y que **escribe su journal**
(append-only, event-sourcing con el motor Puppeteer). **Gnothi** es una app nativa que carga un
**módulo de carga (`.gmod`)** in-process; ese módulo **lee el journal** para exponerle *tools* al
modelo de IA. Los dos tienen que ponerse de acuerdo sobre **una sola fuente de dato**.

**El muro:** la File System Access API del browser **NO le da la ruta absoluta al PWA** (el
`FileSystemDirectoryHandle` solo expone `.name`). Así que el PWA no puede "decirle a Gnothi" en qué
carpeta escribió.

---

## 1 · Restricciones / valores (lo no-negociable)
- **PWA independiente**: la app de la institución funciona **sin Gnothi**; Gnothi es una **capa
  opcional** que se suma después (solo para que el modelo conozca las tools). Orden: **app primero,
  Gnothi opcional después**.
- **Un solo escritor**: el PWA escribe; Gnothi/el módulo solo **lee**. Nunca dos escritores sobre una
  carpeta (trampa de `E3`).
- **El journal es de la persona** (Sonia), no de Gnothi. Gnothi-el-host **no es dueño de nada**; hospeda
  el módulo. La **ubicación** de la carpeta es autoridad del Host (corte de `B4`), el **dato** es de la
  persona.

---

## 2 · Las 3 rutas de coordinación (investigado, con veredictos)

**Ruta 1 · Hospedado (WebView2 inyecta el handle)** — `Comercio.Shell` (host .NET) es dueño de la
carpeta, la crea/conoce, y le **forja un `FileSystemDirectoryHandle` real** al PWA con
`CoreWebView2Environment.CreateWebFileSystemDirectoryHandle` + `PostWebMessageAsJsonWithAdditionalObjects`
(GA 2024; la máquina ya tiene el SDK). El PWA escribe con su código actual, permiso `granted` sin picker.
*Contra:* el PWA vive dentro de Gnothi → choca con "app independiente".

**Ruta 2 · Standalone + marca** — el instalador crea `Documents\Gnothi\<módulo>` y planta un
**archivo-marca** (`.gnothi-anchor.json` con token/fabricante); el PWA abre el picker (`startIn:'documents'`),
la persona elige la carpeta, y el PWA **lee la marca para CONFIRMAR** que aterrizó bien. Gnothi lee la
misma carpeta. *Contra:* fricción del picker; y **choca con el orden** (si el PWA elige la carpeta ANTES
de que Gnothi exista, no hay marca plantada).

**Ruta 3 · Replicación (handoff)** — el PWA guarda donde sea (OPFS o carpeta), y cuando la persona
"actualiza" **entrega el journal** (export `journal.v1` → un `.gmod`/archivo) a Gnothi por **asociación de
archivo** (`Gnothi.Host --ingest-journal "%1"`) o un helper local. Gnothi lo ingiere en **su propia**
carpeta. **La pregunta "¿qué carpeta eligió el PWA?" desaparece.** *Contra:* no es vivo (foto del último
sync); es una **réplica de solo-lectura** (patrón nativo del motor: "compartir la historia, replay tras una
demora"). **Respeta el orden y la independencia** → es la más natural para "app primero, Gnothi después".

**Descartadas (verificado):**
- **Búsqueda por huella** (la idea del arquitecto: Gnothi escanea el disco buscando la carpeta por
  nombre+archivo+fabricante del DLL). El *fingerprint* es bueno **como marca** (adoptarlo), pero **el
  *Find*/escaneo sobra**: si Gnothi planta la marca, ya conoce la ruta; y escanear es frágil (la persona
  elige `D:\loquesea`) e invasivo. Colapsa en la Ruta 2, que es mejor.
- Web Share hacia app nativa (imposible sin MSIX, ya descartado); `gnothi://` (no transporta el journal,
  tope ~2048 chars); convención pura sin marca (divergencia silenciosa); leer el OPFS del WebView desde
  disco (ofuscado, sin contrato).

---

## 3 · Móvil — CORRECCIÓN IMPORTANTE (antes lo dimos mal)
Se dijo "en móvil no hay carpeta compartida, solo OPFS". **Estaba desactualizado** (caniuse/MDN viejos).
Verificado con fuente oficial de Chrome + **probado en un Android real**:

> **Chrome para Android ≥132 (14-ene-2025) SÍ tiene `showDirectoryPicker`**, activado por defecto. En
> Android lo monta sobre el **Storage Access Framework (SAF)** (content-URIs) → escribe en
> **almacenamiento compartido REAL** (el `journal_000001.bin` quedó visible en el explorador, NO es OPFS).
> Y **otra app nativa (Gnothi.Mobile) PUEDE leer esa carpeta**: `ACTION_OPEN_DOCUMENT_TREE` +
> `takePersistableUriPermission()` + `DocumentFile`/`ContentResolver`, sin permisos peligrosos, sobrevive
> reinicios.

**Conclusión:** la **carpeta compartida SÍ es viable en Android moderno**, igual que en desktop. La
divisoria NO es "desktop vs móvil":

| | picker | carpeta compartida |
|---|---|---|
| Desktop (Chrome/Edge/Opera) | sí | **sí** |
| **Android Chrome ≥132** | **sí** (SAF) | **sí** |
| iOS (todos = WebKit) | no | no → **handoff** |
| Firefox Android | no | no → **handoff** |

**El holdout real es iOS** (y Firefox Android). Caveats: requiere Chromium ≥132; en código,
**feature-detect** (`typeof window.showDirectoryPicker === 'function'`), no confiar en caniuse.

---

## 4 · Contención del motor (por qué la réplica no es solo estética)
Verificado en el código:
- **No es un lock** ni "dos lectores" — el journal se abre con `FileShare.ReadWrite` a propósito.
- El motor **no tiene modo solo-lectura**: "leer" **muta** (`meta.bin`, índice). Así que el módulo que
  "lee" es, físicamente, un **segundo escritor** → dos escritores sobre un journal (prohibido).
- Fallo **medido**: tocar `meta.bin` invalida los handles cacheados del PWA → su próximo *flush* muere con
  `InvalidStateError`. El `Evict` tras cada llamada solo achica la ventana, no la cierra.
- **El transporte da igual** (MCP del app o `.gmod` in-process): colisiona **cuántos motores tocan la
  carpeta viva**. **Solo separar las carpetas (réplica) lo cierra.**

---

## 5 · El `.gmod` y por qué NO se reusan los DLLs del PWA
- **Para qué el `.gmod`:** es **código + tools** (2 DLLs: dominio + superficie con `resumen`,
  `lista_de_*`…), NO el dato. Le dice a la IA "así se lee mi dato y estas son las preguntas". Es minúsculo
  porque **el runtime/motor los presta Gnothi** (lámina del sustrato).
- **¿Gnothi puede usar los DLLs del PWA?** Desktop: técnicamente con trabas; móvil: **no**.
  - Los assemblies del PWA son **WebCIL `.wasm`** (verificado: 0 `.dll`, 58 `.wasm`, con nombre hasheado),
    **podados (trim)**, y Gnothi carga `.dll` nativo por reflexión → no los carga directo.
  - Móvil: viven en el **sandbox del navegador** → Gnothi.Mobile no los lee (el SAF comparte el **journal**,
    no el **código**).
  - Mismo dominio en fuente, **dos formas** (WebCIL-podado-browser vs `.dll`-nativo-completo). El `.gmod` es
    el vehículo correcto.

---

## 6 · Instalación / orden (Gnothi opcional) — pregunta de producto abierta
Desde el equipo se planteó: *¿al instalar la app, si no detecta Gnothi, instalarlo solo?* Aclaraciones:
- Separar **"opcional de correr"** (la app anda sin Gnothi, siempre) de **"opcional de instalar"** (si la
  app arrastra Gnothi al equipo). El auto-install cambia lo segundo.
- **La carpeta NO es razón para forzar la instalación**: con replicación el problema ya no existe (Gnothi
  usa su carpeta y recibe el dato). Forzar la instalación es una **decisión de producto** (Gnothi
  infraestructura obligatoria vs capa opcional), no un requisito técnico. **PENDIENTE de decidir.**

---

## 7 · Los 4 puntos para el arquitecto (actualizados)
1. **Para qué el `.gmod`**: código + tools, no el dato; hace a la app "consultable por IA"; minúsculo
   porque Gnothi presta el motor.
2. **MCP no evita el bloqueo**: no es lock; el "lector" es un segundo escritor (motor sin modo
   solo-lectura); el PWA muere con `InvalidStateError`; transporte irrelevante; **solo la réplica lo cierra**.
3. **Móvil (corregido)**: Chrome Android ≥132 tiene el picker vía SAF → carpeta compartida viable y
   Gnothi.Mobile la lee. El holdout es **iOS** (y Firefox Android), no "móvil".
4. **Móvil no fuerza el handoff (corregido)** + **la réplica es del MÓDULO/persona, no de Gnothi**: el
   journal autoritativo es del app (persona); el módulo lo lee desde su carpeta otorgada; Gnothi solo
   hospeda. La app **ya está hecha para esto** (3 modos: `carpeta` donde hay picker, `export` donde no).

---

## 8 · Aportes del arquitecto
- *"Gnothi no necesita el journal"* → matizado: **el módulo** que Gnothi carga SÍ lo usa (dominio + tools).
- *Buscar carpetas que se llamen X con archivo Y, y el archivo con posfijo = fabricante del DLL* → el
  **fingerprint** (carpeta + archivo + fabricante, que ya viaja en el DLL/manifest) es bueno y elimina
  falsos positivos; se **adopta como marca**. Lo que sobra es el *Find*/escaneo (frágil): mejor que Gnothi
  **plante** la marca y el PWA la **confirme**.

---

## 9 · Decisiones ABIERTAS (dónde retomar)
1. **Ruta base — RESUELTA para MÓVIL (2026-09-02).** Es **binaria**, no hay término medio:
   - **Chrome Android ≥132** → **carpeta compartida** (File System Access sobre SAF; ver §3 y §12): una sola
     fuente de verdad, Gnothi.Mobile la lee. Es el "link simbólico" que buscaba el arquitecto, ya funcionando.
   - **iOS (todo Apple, siempre WebKit) y Firefox Android** → **handoff/export (Ruta 3)**, coherente por
     append-only. Único camino: no hay picker nativo y el **plugin tampoco es viable en móvil** (ver §11).
   Criterio de código: **feature-detect** (`typeof window.showDirectoryPicker === 'function'`), que la app ya
   aplica con sus 3 modos. El **plugin sale del mapa**: no es una cuarta ruta.
2. **¿Gnothi obligatorio o opcional?** (auto-install sí/no) — decisión de producto, no técnica.
3. **La "réplica ¿suena a que es de Gnothi?"** → resuelto conceptualmente (read-replica derivada, patrón
   del motor), pero conviene la frase clara para el arquitecto (punto 4b).
4. Si se va por carpeta compartida: **el mecanismo de marca** (qué planta el instalador, qué confirma el
   PWA/Gnothi), en desktop y en Android (SAF también oculta el path → misma coordinación).

---

## 10 · Estado práctico dejado andando
- **Examenes servido por HTTPS en la LAN** para probar en el teléfono: `https://192.168.100.95:8453/app/`
  (contenedor docker `examenes-wasm-host`). Se **regeneró el cert** incluyendo la IP de LAN en el SAN.
- **`generar-certificado.sh` corregido para Windows** (era Mac-only): prioridad a `LAN_IP` por entorno,
  archivo real en vez de `<(...)`, y correr con `MSYS_NO_PATHCONV=1`. Regenerar tras cambio de IP:
  `cd Examenes/PaginaDeDescarga && MSYS_NO_PATHCONV=1 LAN_IP=<ip> bash generar-certificado.sh && docker restart examenes-wasm-host`
- La CA a instalar en el teléfono: `Examenes/PaginaDeDescarga/certs/ca.crt`.
- **Probado en Android**: el picker funcionó y escribió el journal físico → confirmó la corrección del punto 3.

---

## 11 · ¿Un plugin de navegador para dar carpeta física? — para MÓVIL: NO (investigado 2026-09-02)
Idea del arquitecto (opción 2b): una extensión que le dé a la PWA la selección de carpeta donde el navegador no la
tiene. Veredicto **para móvil: no es viable en ninguno de los dos casos problemáticos.**
- **Firefox Android**: el único modo por el que una extensión toca el disco es **Native Messaging**, y
  `connectNative`/`sendNativeMessage` **NO existen en Firefox Android**. Sin ese puente, la extensión solo
  alcanza OPFS o la carpeta de Descargas. (El plugin real que sí existe, `ichaoX/ext-file`, es **desktop-only**.)
- **iOS**: sandbox estricto; la extensión no puede lanzar un host nativo ni compartir una carpeta arbitraria con
  la página. Y **todos** los navegadores en iOS son WebKit por obligación → cambiar de navegador no ayuda.
- **De fondo**: ni Mozilla ni WebKit implementarán el picker nativo — posición oficial *negative*/*oppose*
  (Mozilla standards-positions #154, WebKit #28). Solo hacen OPFS.

**Conclusión:** el plugin **no es una cuarta ruta**; colapsa en "instalá un nativo", y en móvil ni eso. Para
móvil sin picker (iOS, Firefox Android) el único camino sigue siendo **handoff/export (Ruta 3)**. (En desktop el
plugin sí es viable con fricción alta, pero no aporta nada sobre el shell WebView2 / un helper nativo.)

---

## 12 · Evidencia dura: cómo Chrome Android habilita el file system (código fuente) — 2026-09-02
Confirmado en el **código fuente de Chromium**, no solo en un blog. Archivo
`ui/android/java/src/org/chromium/ui/base/SelectFileDialog.java`. Cuando la PWA llama `showDirectoryPicker`,
Chrome Android por debajo:
1. Lanza el **Storage Access Framework** con `Intent.ACTION_OPEN_DOCUMENT_TREE` (el selector de carpetas nativo
   de Android, no uno propio de Chrome).
2. Toma **permiso persistible de lectura Y escritura**:
   `takePersistableUriPermission(uri, FLAG_GRANT_READ_URI_PERMISSION)` y `..._WRITE_...`.
3. Devuelve un **content-URI** (no una ruta de filesystem) al motor.

- Versión: **Chrome 132** — *"From Chrome 132, the API is now available on Android and in WebViews"* (también en
  **WebView**, relevante para un shell Android).
- Implicaciones: el PWA recibe **acceso, nunca la ruta** (el muro); y **Gnothi.Mobile lee la misma carpeta con
  las mismas APIs** (`ACTION_OPEN_DOCUMENT_TREE` + `takePersistableUriPermission` + `DocumentFile`/`ContentResolver`).
- Ojo con Google: mucha doc vieja dice "Chrome Android no expone showDirectoryPicker" — es **pre-132**, obsoleto.

Fuentes: `developer.chrome.com/blog/new-in-chrome-132`; `SelectFileDialog.java` (source.chromium.org);
`mozilla/standards-positions#154`; `WebKit/standards-positions#28`.

---

## 13 · OPFS: qué hay realmente en disco y por qué NO se puede "linkear" a una carpeta física — 2026-09-02
La **opción 1 del arquitecto** (un "link simbólico" entre el OPFS y una carpeta física visible). Investigado con
código fuente de Chromium y Firefox.

**Cómo se guarda el OPFS (NO es un espejo).** Lo que ve el JS son nombres LÓGICOS; en disco hay una capa de
metadatos + blobs con nombres internos ofuscados:
- **Chromium**: `<perfil>/File System/` (storage por bucket/quota). Una LevelDB `Origins` mapea origen → carpeta
  numerada (000, 001…); una LevelDB `Paths` por origen (`ObfuscatedFileUtil`/`SandboxDirectoryDatabase`) traduce
  nombre lógico → archivo físico con nombre numérico (subcarpetas `p`/`t`, ej. `00/00000013`).
- **Firefox**: `<perfil>/storage/default/<origin>/fs/`. Un SQLite `metadata.sqlite` guarda la jerarquía lógica
  (tablas Entries/Directories/Files); los datos van en `fs/XX/<hash>`, nombre = hash de (padre+nombre).
- En ambos, el nombre físico NO corresponde 1:1 con el de la app; la correspondencia vive SOLO en esa capa de
  metadatos → no es navegable, no aparece en el explorador, ni DevTools lo lista como árbol. Se borra con
  "clear site data" o eviction de cuota.

**En móvil, aún más cerrado.** El OPFS vive dentro del **sandbox de datos de la app del navegador**
(`/data/data/<package>` en Android; contenedor de app en iOS — en WKWebView embebido hay tope ~10 MB/archivo).
Aislado por UID a nivel de kernel: **sin root, ninguna otra app lo lee** (ni ADB). Suma DOS aislamientos (origen
del navegador + sandbox de app del SO). No ofrece ruta, content-URI ni permiso cedible — lo contrario del SAF (§12).

**¿Se puede linkear/espejar el OPFS a una carpeta física desde la PWA? NO** (respuesta al arquitecto):
- No hay API para elegir/reubicar dónde vive el OPFS: lo decide el navegador.
- No existe symlink/alias entre el OPFS y un handle del picker: mundos separados con seguridad distinta (OPFS
  omite Safe Browsing; los archivos locales llevan Mark-of-the-Web).
- `FileSystemFileHandle.move()` solo opera DENTRO del OPFS; cruzar la frontera OPFS↔disco lanza `NotSupportedError`,
  y el estándar futuro (whatwg/fs `MovingNonOpfsFiles`) declara ese cruce como **non-goal** explícito.
- "Dual-write" (escribir a OPFS y a un handle del picker a la vez) NO es un espejo: son **dos copias** a reconciliar
  a mano.
- La ÚNICA relación posible OPFS↔carpeta física es **COPIAR** bytes (leer del OPFS, escribir en el picker) = el
  **export/handoff (Ruta 3)**.

**Corolario.** El "link simbólico" del arquitecto **sí existe, pero NO es sobre el OPFS**: es la File System Access API
(handle a carpeta real). OPFS y "link a disco" son **excluyentes**: donde hay picker (Chrome Android ≥132, desktop)
escribís en la carpeta real directo y NO necesitás OPFS; donde SOLO hay OPFS (iOS, Firefox Android) **no hay link
posible** → export. No existe un tercer modo "OPFS enlazado".

Fuentes: MDN OPFS; web.dev OPFS; Chromium `storage/browser/file_system/README.md`; Firefox searchfox
`FileSystemFileManager.cpp` + `metadata.sqlite`; whatwg/fs `MovingNonOpfsFiles.md` (non-goal); AOSP app-sandbox.
