# 13 — Móvil: la transición de la PWA standalone a Gnothi (identidad · migración · alta)

**Corte 2026-09-03, rev. 1.** Complemento de `11` (§6 cartero, §11 migración) y de `12`. Cierra los huecos que `11` §11 dejaba abiertos y corrige una premisa del cartero. Escrita contra `master_gnothiHost` (`Examenes/App/Examenes.Wasm/EstadoDeLaApp.cs`, `Examenes/App/Examenes.App/ExamenesAPI.cs`, `Examenes/Dominio/Bitacora.cs`, `Examenes/EndPointDeEventos/Program.cs`) y la investigación de plataforma del 2026-09-03 (fuentes en §11).

**Vocabulario:** el de `11`. **La standalone** = la PWA de la institución corriendo sola en el browser de Sonia. **El módulo** = la misma PWA dentro del WebView de Gnothi. **El publicador** = el endpoint de eventos de la institución.

---

## 0. En una frase

> **La standalone y el módulo son etapas, no apps en paralelo. El cambio de etapa lo decide Sonia con un toque; la marca de que ocurrió vive en el publicador; la credencial la sostiene Gnothi fuera del journal y fuera del WebView; y el actor —no Gnothi— es quien sabe hasta dónde leyó.**

---

## 1. Los cinco huecos de `11` §11, y su cierre

| # | hueco | decisión | detalle |
|---|---|---|---|
| **a** | cómo la standalone sabe que Gnothi existe y le pasa la mano | **Nunca desde JS: siempre un toque de Sonia.** Android: botón `intent://…;package=…;S.browser_fallback_url=…`; detección opcional con `getInstalledRelatedApps()`. iOS: `<a href>` a un universal link de Gnothi; **no hay detección posible**. El "solo lectura" **no depende del handoff** sino de la marca (§4) | §6 |
| **b** | con qué credencial habla Gnothi con el publicador en nombre de Sonia | **Gnothi es otro cliente** y obtiene **su propia** concesión. El traspaso de aserciones **no lleva tokens**. Prerrequisito: hoy la standalone los tiene **en el journal** | §2, §3 |
| **c** | quién dispara el traspaso de aserciones y qué pasa si se repite | Botón "Pasar mis datos a Gnothi" en la standalone, con gesto. **No es un export del journal.** Un dato → parámetro del enlace; muchas → archivo `text/plain` (Web Share rechaza JSON). Gnothi entrega con `deliver`; el módulo asienta con `check` ⇒ **repetir no duplica**. Hoy en Exámenes es un dato; el mecanismo queda genérico | §5 |
| **d** | qué significa que la standalone "desaparece" | **No es desinstalar.** Al leer `account.state = migrated` se vuelve **visor de solo lectura**, deja de sincronizar y ofrece abrir Gnothi. El icono lo quita Sonia | §6 |
| **e** | cómo Sonia da de alta el módulo desde la página de la institución | Un enlace **al dominio de Gnothi** (`https://go.gnothi…/alta?bundle=…`): App Link / universal link; sin Gnothi, landing → tienda; Android recupera el `bundle` con Install Referrer | §7 |

Y una **corrección** que no era hueco sino premisa equivocada: el cursor no es de Gnothi (§8; ya corregido en `11` §6).

---

## 2. Prerrequisito: las credenciales están HOY en el journal — sacarlas antes de cualquier traspaso

**Medido en el código.** La standalone de Exámenes genera una identidad de instalación y la persiste **como hechos del dominio**:

| qué | dónde | línea |
|---|---|---|
| `RegistrarDevice` y `GuardarCredencial` son **comandos del DSL** (`app.Using("bitacora.RegistrarDevice(@device)")`, `…GuardarCredencial(@token,@refresh)`) ⇒ **entradas del journal** | `Examenes.App/ExamenesAPI.cs` | 33-48 |
| `DeviceKey`, `Token`, `RefreshToken` son **estado del dominio** | `Dominio/Bitacora.cs` | 32-36 |
| se leen del journal al abrir: `TokenGuardado()`, `RefreshTokenGuardado()`, `DeviceKeyGuardado()` | `Examenes.App/ExamenesAPI.cs` | 337-341 |
| la intención, textual: *"Las tres piezas viven en el journal, así que al reabrir la app ya sabe quién es"* | `Examenes.Wasm/EstadoDeLaApp.cs` | 210-212 |

Esto **contradice `02`** (*"cursores, tokens y reintentos son del Host y no son historia de Sonia"*) y ya estaba anotado como trampa en el corpus de trabajo (E3 §7, *"Los tokens quedaron en el journal, en texto plano"*). En escritorio era una deuda; **en la transición móvil es bloqueante**, por dos razones:

1. **Cualquier cosa que salga del journal arrastra la credencial.** El traspaso de aserciones de `11` §11.4 (§5) sacaría device key + access + refresh token en claro, por el share sheet, a la carpeta de descargas y a los backups.
2. **El módulo hospedado no debe tener credencial** (`11` §4, §6): con `connect-src 'none'` no la puede usar, y si la tiene en el journal la tiene igual.

**Decisión.** `Bitacora` deja de tener `DeviceKey`/`Token`/`RefreshToken`; `ExamenesAPI` deja de journalizarlos. La credencial pasa a **storage de la app, por modo**:

| modo | dónde vive la credencial |
|---|---|
| standalone | storage del browser del origen (IndexedDB/localStorage), **no el journal**. Si el browser la desaloja, la app vuelve a pedir concesión: es exactamente lo que ya hace `Renovar` → `PedirCredencialNueva` |
| módulo (hospedado) | **nunca la tiene**: la sostiene Gnothi (§3.3) y hace la red por él |

Lo que **sí** queda en el journal: el cursor, porque se deriva de lo asentado (`UltimoEventoProcesado()`) — es procedencia, no secreto (§8).

---

## 3. Identidad ante el publicador: Gnothi es otro cliente

### 3.1 Lo que hay hoy

El publicador emite una **concesión por dispositivo**: `POST /device/token {deviceKey, recurso, id}` → access token corto + refresh token rotatorio, scope `{recurso}.events.read` sobre **ese** recurso (`EndPointDeEventos/Program.cs` 43-64). El diseño es correcto — scope mínimo, nada de perfil — pero **el mock no ata el dispositivo a ninguna persona**: cualquier `deviceKey` recibe concesión para el `recurso/id` que pida (defaults `expediente`/`1`). **Cómo el publicador sabe que ese dispositivo es de Sonia es un hueco del publicador, no de Gnothi**, y hoy no existe ni como stub.

### 3.2 Decisión: Gnothi obtiene su propia concesión

Gnothi es, ante el publicador, **un segundo dispositivo/cliente** — con su propia `deviceKey` (o `client_id`), su propia concesión y su propio refresh. Dos mecanismos aceptables, **a elección del publicador**, declarados en el manifiesto del módulo:

| mecanismo | cómo | cuándo conviene |
|---|---|---|
| **enrolamiento del publicador** (extiende el flujo actual) | Sonia obtiene en su cuenta del laboratorio un **código o QR de vinculación** de un solo uso y corta vida; Gnothi lo canjea en `POST /device/token` junto con **su** `deviceKey` ⇒ concesión atada a `recurso/id` | publicadores sin authorization server; es el mock actual más un paso |
| **OAuth 2.0 authorization code + PKCE** (RFC 8252) | Gnothi abre el login del publicador **en el browser del sistema** — Android: Auth Tab (Chrome 132+) / Custom Tabs; iOS: `ASWebAuthenticationSession` — con redirect `https` reclamado en un dominio de Gnothi (App Links / Universal Links). Cliente público, **sin client secret** | publicadores con identidad federada |

⚠️ **Nunca dentro del WebView de Gnothi.** Google **bloquea** OAuth en WebViews embebidos desde 2021 (`403 disallowed_useragent`); RFC 8252 §8.12: *"native apps MUST NOT use embedded user-agents to perform authorization requests"*. Además, el browser del sistema **no comparte** cookies con el WebView de la app, así que el login "dentro" ni siquiera heredaría la sesión de Sonia. Es una operación **nativa de Gnothi**.

En ambos casos el `recurso/id` de Sonia **lo devuelve el enrolamiento**, no el traspaso.

### 3.3 Dónde la guarda y cómo la renueva

| | Android | iOS |
|---|---|---|
| llave | Android Keystore: AES-GCM **no exportable**, TEE o StrongBox | Keychain, clase `…ThisDeviceOnly` (`AfterFirstUnlock` si habrá trabajo en background) |
| blob | el estado de la concesión cifrado con esa llave, en archivo privado / DataStore. **No** `EncryptedSharedPreferences` (deprecado abr-2025) | el ítem del Keychain |
| ámbito | **una concesión por módulo**, aislada | ídem |

Renovación: refresh proactivo antes del vencimiento y reactivo ante `401`; si el refresh falla (rotación detecta reuso, inactividad, revocación), se borra y se vuelve a §3.2. Al quitar el módulo, Gnothi **revoca** (RFC 7009) y borra. Fase 2 opcional: DPoP (RFC 9449) con llave P-256 en Keystore / Secure Enclave — un token exfiltrado no sirve sin la llave.

### 3.4 Lo que el traspaso NO lleva

Ni access token, ni refresh token, ni `deviceKey`. Tres razones: el token fue emitido **a otro cliente** y el publicador perdería la trazabilidad de quién sostiene qué; un archivo en claro por share sheet, descargas y backups es la ventana de robo más amplia posible (RFC 10017: *persistent token theft*); y viola la regla de tokens cifrados en reposo. **[SUPUESTO — fase 2]** Si el publicador implementa *token exchange* (RFC 8693), la standalone, mientras aún tiene sesión, puede pedir un **código de handover** de un solo uso (minutos) que Gnothi canjea por su propia concesión: evita la pantalla de login, pero **no reemplaza** §3.2 — si la standalone ya perdió su storage, no tiene con qué pedirlo.

---

## 4. La marca de migración vive en el publicador

**Por qué ahí y no en la PWA.** Verificado en las tres plataformas: **nada del lado web sobrevive a un borrado deliberado**. Chromium desaloja el origen menos usado bajo presión de disco salvo `persist()` concedido, y "borrar datos del sitio" borra todo incluida la WebAPK; WebKit exime a la PWA en pantalla de inicio del tope de 7 días, pero "datos de sitios web" y quitar el icono la borran igual. Y una marca escrita **por la propia PWA** no prueba que Gnothi haya recibido nada. La única marca que sobrevive y que **certifica** la recepción es la que se escribe cuando Gnothi ya tiene concesión: **en la cuenta del publicador**.

**Mecanismo.**

1. En la cuenta, un registro `host_binding {cliente: gnothi, dispositivo, fecha, gracia_hasta}`. Se escribe cuando el publicador emite la **primera concesión al cliente Gnothi** para esa cuenta (§3.2) y Gnothi confirma la importación (§5). La pantalla de vinculación lo dice explícitamente — *"Mover el registro de esta cuenta a Gnothi en este teléfono"* — así la marca es **un acto de Sonia**, no un efecto secundario.
2. El sobre de `GET /events` pasa de `{events, last}` a **`{account: {state: active|migrated, migratedAt, host}, events, last}`**. Cambio aditivo; el `EndPointDeEventos` del mock lo incorpora.
3. **La standalone lo lee en su próximo pull** — durante todo el período de gracia (p. ej. 30 días) — y pasa a solo lectura (§6).
4. Al vencer la gracia, el publicador **revoca** la concesión de la standalone; su próximo pull recibe `401 invalid_token`. El campo explícito existe para que ese 401 no sea la primera noticia.
5. **Reversible:** desvincular desde Gnothi = revocar su concesión + `host_binding = null`; la standalone vuelve a `active` en su siguiente pull o re-enrolamiento.

**Segunda copia, solo Android:** si la standalone escribía en una carpeta SAF, Gnothi deja ahí un archivo marcador (`.gnothi-migrado`) que la standalone puede leer sin red. **Caché local** (IndexedDB + `persist()`): sirve para no parpadear, **nunca para decidir re-migrar a ciegas**.

Es **el único cambio que se le pide al publicador** además del enrolamiento (§3.1).

---

## 5. El traspaso único de las aserciones de Sonia: qué es, qué lleva, cómo viaja, por qué no duplica

**Qué es y qué no es.** **No es un export del journal.** La Ruta 3 —la standalone exporta su journal completo y Gnothi lee esa foto como fuente— está **descartada**: produce un journal muerto. Acá el journal **no se mueve**: el módulo hospedado nace con cursor 0 y re-pide los hechos institucionales (`11` §11.3, §8). Lo único que cruza, **una vez**, es lo que Sonia misma afirmó en la standalone (`11` §11.4).

**Cuánto es hoy.** En la standalone de Exámenes la única aserción de Sonia es **declarar su sexo** (`expediente.DeclararSexo`, `ExamenesAPI.cs` 75). Todo lo demás del journal es institucional (se re-pide) o credencial (se saca, §2). El traspaso, hoy, es **un dato**. **Decisión (2026-09-03): el mecanismo se especifica genérico igual**, porque otras instituciones van a esperar más aserciones ingresadas por Sonia (listas, consentimientos, declaraciones). Exámenes es el caso mínimo, no el caso general.

### 5.1 Qué lleva
Solo las aserciones con `autor=sonia`, por límite de evento, en el formato de eventos del módulo para que `deliver` las entregue tal cual. Metadatos: publicador (URL), versión del formato, fecha. **Sin credenciales** (§2, §3.4) y **sin `recurso/id`** (lo da el enrolamiento). Los hechos institucionales **no viajan**.

### 5.2 Cómo viaja — según cuánto es

| volumen | canal | plataformas |
|---|---|---|
| **un dato o pocos** (Exámenes hoy) | como parámetro del **mismo enlace del handoff** (§6.1): `…/abrir?modulo=…&aserciones=<eventos codificados>`; o simplemente **volver a declararlo en Gnothi** | ambas |
| **muchas aserciones** | **Android:** `navigator.share({files:[new File([json], 'mis-datos-<fecha>.txt', {type:'text/plain'})]})` → Gnothi con `intent-filter ACTION_SEND` (`text/plain`), lee `EXTRA_STREAM`; o, si la standalone escribía en una **carpeta SAF**, Gnothi lee el archivo directo de ahí. **iOS:** `navigator.share({files})` (iOS 15+) → **Share Extension** de Gnothi, guarda en el App Group y dispara la importación | según columna |

⚠️ Chromium **rechaza `.json` y `application/json`** en Web Share (lista blanca cerrada): el JSON viaja como `.txt` / `text/plain`; tope 10 archivos, 50 MB en total. Siempre con **gesto** y comprobando `navigator.canShare({files})`. Si Gnothi no está instalado, el share sheet no lo lista: el botón hace primero el handoff/alta (§6, §7). Lo que separa las dos filas es el largo utilizable de una URL; el umbral se fija al medir (§9).


**LAS DOS PUERTAS, IMPLEMENTADAS Y MEDIDAS el 2026-09-03** (Xiaomi 14T Pro). Gnothi las atiende con dos `IntentFilter`, y las dos terminan en el mismo verbo del módulo:

| caso | qué llegó | resultado |
|---|---|---|
| Enlace de vuelta, con el módulo nombrado | 90 bytes | `aplicadas=1, omitidas=0` |
| Hoja de compartir, **sin** nombrar módulo | 91 bytes | `aplicadas=1, omitidas=0` |
| El mismo traspaso, repetido | 91 bytes | `aplicadas=0, omitidas=1` |
| Sin decir de qué publicador viene | 52 bytes | *no se pudo saber a qué módulo corresponde; no se asentó nada* |

**Cuando el traspaso no dice a qué módulo va**, Gnothi lo deduce del publicador que las aserciones declaran, comparándolo con el endpoint que cada módulo tiene registrado. Y si aun así no puede saberlo, **se niega y lo dice**: asentar las aserciones de una persona en el módulo equivocado es peor que no asentarlas.

⚠️ **La hoja de compartir acepta `text/plain` y no `application/json`**, y eso no es una elección: la lista de tipos que Web Share permite es cerrada y no incluye JSON. El nombre `.txt` es envoltorio; lo que importa es el contenido.

### 5.3 Por qué no duplica — MEDIDO el 2026-09-03
Gnothi entrega cada aserción con `deliver`; el módulo la pasa por **`check`** (autor `sonia` + identidad del evento) antes de asentar: lo ya asentado produce **cero asientos**. **Medido:** con la app corriendo, se le volvió a entregar por `gnothi.deliver` el sobre del último evento que ya tenía asentado. Resultado: `Leidos=1, Aplicados=0, Rechazados=1`, y el resumen del módulo quedó idéntico (19 exámenes, cursor 218). Un sobre vacío da `Leidos=0`. Repetir el traspaso es inocuo — por eso el botón "Volver a pasar mis datos" puede quedar visible en la standalone sin riesgo. Gnothi **confirma al publicador (§4) al terminar, no antes.**

### 5.4 La secuencia completa

```
Sonia toca "Pasar mis datos a Gnothi" (standalone)
  → enlace con parámetro, o share sheet → Gnothi importa: deliver × N → check → asienta (autor=sonia)
  → Gnothi tiene concesión propia (§3.2; si no, la pide ahora, browser del sistema)
  → publicador escribe host_binding; el sobre de /events dice migrated (§4)
  → la standalone, en su próximo pull, queda en solo lectura (§6)
  → el módulo pide eventos desde cursor 0 y se completa (§8)
```

---

## 6. Pasar la mano y "desaparecer"

### 6.1 El handoff es siempre un toque de Sonia

| | Android | iOS |
|---|---|---|
| gesto | `onclick` → `location.href = "intent://gnothi…/abrir?modulo=…#Intent;scheme=https;package=<gnothi>;S.browser_fallback_url=<landing>;end"`. Sin gesto Chrome **no lanza** la app y va al fallback (la cadena debe empezar con gesto; ventana de 15 s) | `<a href="https://go.gnothi…/abrir?modulo=…">` **tocado**, dominio **distinto** al de la PWA, sin redirect intermedio, sin `setTimeout`. `window.location` desde JS se trata como pegar la URL: **no abre la app** |
| detección de Gnothi | **opcional**: `navigator.getInstalledRelatedApps()` — la PWA declara `related_applications` y el APK de Gnothi declara el **origen exacto** de esa PWA en `asset_statements`. Devuelve `[]` si falta cualquiera de los dos lados. Escala mal: cada institución nueva obliga a redeclarar orígenes en Gnothi (el `include` remoto **no está confirmado**) | **ninguna**: `getInstalledRelatedApps` no existe en WebKit; `canOpenURL` es solo para apps nativas; `gnothi://` sin app muestra *"no se puede abrir la página"* **sin fallback** |
| sin Gnothi instalado | fallback → landing → Play (`referrer=bundle…`) | landing en Safari View Controller → enlace explícito a App Store (el Smart App Banner **no** aparece ahí) |

Regla de UX derivada: **un solo botón**, "Abrir en Gnothi", que resuelve el SO; la detección, donde exista, solo cambia su texto.

### 6.2 "Desaparecer" son tres cosas, ninguna es desinstalar — IMPLEMENTADO y medido el 2026-09-03

1. **Deja de escribir y de sincronizar** al leer `account.state = migrated` (§4): muestra *"Tu historia ahora vive en Gnothi"* con "Abrir Gnothi" y "Volver a pasar mis datos".
2. **Sigue siendo visor** de lo que tenía. No se borra nada: es de Sonia.
3. **Al vencer la gracia**, `401` ⇒ mismo aviso, sin datos nuevos.

El icono lo quita Sonia. Ninguna API web desinstala una PWA ni borra su storage desde otra app. Si Sonia sigue usando la standalone después de migrar, **no hay divergencia**: no escribe.

---

## 7. Alta desde la página de la institución

**Un enlace, el mismo en las dos plataformas, al dominio de Gnothi:**

```
https://go.gnothi.<dominio>/alta?bundle=<URL del manifiesto del módulo>
```

Va al dominio de Gnothi y **no** al de la institución por dos reglas de plataforma: Chromium mantiene en el browser los enlaces al mismo host de la página actual, y TN3155 (Apple) hace lo mismo con los universal links del mismo dominio que la navegación previa.

| | Android | iOS |
|---|---|---|
| con Gnothi instalado | App Link: `intent-filter` con `autoVerify` para `https://go.gnothi…/alta` + `/.well-known/assetlinks.json` con la huella de **Play App Signing**. Abre Gnothi sin diálogo | universal link: AASA en `go.gnothi…`, entitlement Associated Domains. Abre Gnothi sin prompt |
| sin Gnothi | la landing abre con botón a Play: `market://details?id=<gnothi>&referrer=bundle%3D<URL>`; Gnothi lee el `bundle` al primer arranque con **Install Referrer** (90 días) y completa el alta | la landing con botón a App Store y `<meta name="apple-itunes-app" app-id=… app-argument=<URL>>`; al abrirse por el banner recibe el `app-argument` |
| variante | `intent://go.gnothi…/alta?bundle=…#Intent;scheme=https;package=<gnothi>;S.browser_fallback_url=…;end` para forzar el package | — |

QR, correo y otras apps usan **la misma URL**.

**IMPLEMENTADO Y MEDIDO el 2026-09-03** (Xiaomi 14T Pro). Gnothi arrancó **sin ningún módulo**, recibió el enlace, y quedó con el laboratorio instalado y contestando:

```
0 modules installed
alta: bajando el bundle de 127.0.0.1
alta: 'examenes' instalado en …/files/Modulos/examenes
alta: 'examenes' quedó instalado; 1 módulos
examenes: Gnothi consiguió su credencial ante el publicador
examenes: pull after=0 -> 6 673 bytes
comprobación de examenes: {"matches":true, …}
  resumen contestó: {"examenes":35,"ultimoEventoLeido":234}
```

**El bundle es un zip con dos cosas:** el `manifest.json` de la institución y su carpeta `web/`. Nada más. **No trae `installation.json`** — ese lo escribe Gnothi, porque es su archivo y su autoridad: ahí se decide con qué cáscara corre el módulo, dónde vive su journal, y que el endpoint del publicador quede **fijo**. La institución lo *declara* en su manifiesto (`publicador: { eventos, recurso, id }`, campo nuevo); elegirlo y congelarlo es de Gnothi.

**Dos defensas que el alta tiene que traer, y no son opcionales:**

- **Zip slip.** Una entrada del zip puede llamarse `../../algo` y escribir fuera de donde uno cree. Cada ruta se resuelve y se exige que caiga adentro. Lo mismo con el nombre del módulo, que es una ruta: se rechaza si trae separadores o `..`.
- **Un techo al bundle** (64 MB), aplicado *mientras baja* y no solo contra el `Content-Length`, que puede mentir o no venir. Un módulo medido pesa 11 MB; el techo existe para que una URL equivocada no llene el aparato antes de que nadie se entere.

⚠️ **Una trampa de formato que costó una corrida:** los zip hechos con herramientas de Windows escriben las rutas con **barra invertida**, que el estándar no admite y que en Android no separa carpetas. Sin normalizar, el bundle se desempaqueta como archivos con barras en el nombre. El desempaquetador lo tolera.


**Gnothi al recibir el enlace:** descarga manifiesto y bundle (`11` §3), registra endpoint del publicador y mecanismo de enrolamiento (§3.2), crea el módulo, y **arranca el enrolamiento**. Si Sonia venía de una standalone, ofrece "Pasar mis datos a Gnothi" (§5). El manifiesto (`11` §15.5) suma: `publicador.eventos`, `publicador.enrolamiento` (`device/token` | `oauth {authorization, token, revocation, client_id}`), `scope`.

---

## 8. El cartero, corregido (es lo que ahora dice `11` §6)

**Antes (rev. 2):** *"Cursores, tokens y reintentos: almacenamiento de Gnothi"* y *"Gnothi hace el pull y entrega con `deliver`"*. Eso le quitaba a Sonia la autoridad sobre su propia lectura y creaba **dos verdades** sobre "hasta dónde leí".

**Ahora:** el cursor **es del actor**. Se deriva del replay (`UltimoEventoProcesado()`), exactamente como ya lo hace la standalone (`BucleDeRefresh.Ejecutar`: `long desde = diario.UltimoEventoProcesado()`). El módulo hospedado **pide**:

```
módulo:  gnothi.pull(after=N)                       ← N sale del replay
Gnothi:  GET {endpoint fijo}/events?after=N          ← Bearer DE GNOTHI (§3), endpoint no elegible
Gnothi:  deliver(sobre)                              ← {account, events, last} tal cual
módulo:  check → asienta                             ← mismo camino que hoy
```

Gnothi valida que `after` sea un entero, **no guarda cursor**, y no compone nada. Bridge: `window.gnothi.pull(after) → Promise<string>` se suma a `call`, `tools`, `deliver` (`11` §7); `deliver` queda para lo que Gnothi **empuja** (el traspaso de §5 y la cola de tránsito).

**Consecuencia sobre el background.** `deliver` exige un WebView vivo ⇒ **el pull ocurre cuando Sonia abre el módulo** o cuando Gnothi pasa a primer plano (`11` §12). Un pull en background (WorkManager / BGTaskScheduler) solo puede **dejar los bytes en tránsito** —fuera del journal, en storage de Gnothi— y entregarlos al abrir. Optimización, no día 1; y no pierde nada, porque el módulo no inicia nada.

---

## 9. Medido · verificado · supuesto · a medir

| | |
|---|---|
| ✅ medido | **la idempotencia del traspaso** (§5.3): reentregar un sobre ya asentado da 0 aplicados y 1 rechazado, sin mover el resumen (2026-09-03) |
| ✅ **medido** | **la transición entera, en el navegador** (2026-09-03): el publicador vincula la cuenta al emitirle credencial a Gnothi, la app suelta se entera en su siguiente pull (`account.state = migrated`), pasa a **solo lectura** sin borrar nada, y el enlace del handoff toma la forma `intent://` con `package` y `browser_fallback_url` en Android y `https` en el resto, con las aserciones adentro |
| ✅ **medido** | **el handoff entero en Android** (§5.3, §6.1): el enlace que arma la suelta abre Gnothi, que espera el aviso del módulo y le entrega las aserciones. Repetir el enlace da `aplicadas=0, omitidas=1` |
| ✅ **medido** | **el alta desde el enlace de la institución** (§7): Gnothi sin módulos recibe el enlace, baja el bundle, lo instala, escribe él la instalación, verifica y el módulo trae su historia. En teléfono |
| ✅ **medido** | **las dos puertas del traspaso en Gnothi** (§5.2): el enlace de vuelta y la hoja de compartir; deduce el módulo del publicador cuando no se lo nombran, y **se niega** si no puede saberlo |
| ✅ medido (código) | device key, access y refresh token **en el journal** (`ExamenesAPI.cs` 33-48, 337-341; `Bitacora.cs` 32-36); `/device/token` sin identidad de persona (`Program.cs` 43-52); cursor derivado del replay (`BucleDeRefresh.Ejecutar`); **única aserción de Sonia hoy en Exámenes: `DeclararSexo`** (`ExamenesAPI.cs` 75) |
| ✅ verificado (doc oficial) | `getInstalledRelatedApps` en Chrome Android ≥80 con relación bidireccional; `intent://` exige gesto y va al fallback sin él; Web Share **rechaza** JSON, acepta `text/plain`, 10 archivos / 50 MB; App Links + Install Referrer; universal links **no** desde JS y no al mismo dominio (TN3155); iOS sin detección de app desde web; `navigator.share({files})` en iOS 15+; storage web borrable en las tres plataformas; Google bloquea OAuth en WebViews embebidos; RFC 8252 §8.12; Keystore / Keychain; `EncryptedSharedPreferences` deprecado |
| 🔍 probable | permisos de carpeta persistentes (Chrome 122) también en Android ≥132; el share sheet de una PWA en inicio en iOS lista Share Extensions de terceros; el toque en `<a href>` dentro de la PWA en inicio abre la app directo |
| 🔍 supuesto | el `include` remoto de `asset_statements` resuelve muchos orígenes (hay reporte de que solo el primero); *token exchange* en el publicador (§3.4) |
| ❓ a medir | los de `12` "Qué medir" (nuevas filas); cuántas personas de la standalone sufren desalojo real (para dimensionar la gracia); tasa de finalización del enrolamiento en Auth Tab / `ASWebAuthenticationSession`; largo de URL utilizable para el traspaso por enlace (umbral entre las dos filas de §5.2) |

---

## 10. Cambios en el repo

**PWA (plantilla de la institución) — bloqueantes:**
1. **Sacar la credencial del journal** — ✅ hecho en **los cinco módulos**. En Exámenes el 2026-09-03; en Cargas, Compras, Facturas y DealershipCar el 2026-09-04 (rama `fase0/credenciales-fuera-del-journal`). En todos: `Bitacora`/`Logbook` solo cursor, con `RegistrarDevice`/`GuardarCredencial` (`RegisterDevice`/`StoreCredential` en DealershipCar) como no-ops para que los journals anteriores repliquen; `IDiarioDeLaApp` sin `TokenGuardado()`; el bucle recibe la credencial por delegado del host; la standalone guarda en `localStorage` del origen; el modo hospedado no guarda nada (§2). Ver §10.1 para lo que la réplica dejó dicho.
2. Leer `account.state` del sobre ⇒ modo **solo lectura** con los dos botones (§6.2).
3. Botón "Pasar mis datos a Gnothi": traspaso de aserciones `autor=sonia` — parámetro del enlace si es poco (hoy: `DeclararSexo`), `.txt`/`text/plain` por `navigator.share` o carpeta SAF si es mucho (§5).
4. Botón "Abrir en Gnothi" con `intent://` + fallback (Android) y `<a href>` universal link (iOS); `related_applications` en el manifest (§6.1).
5. Adaptador de fuente de eventos en modo hospedado: `gnothi.pull(after)` en vez del pull propio (§8).

**Publicador (mock `EndPointDeEventos`, y contrato para los reales):**
6. Stub de **enrolamiento**: código de vinculación de un solo uso que ata `deviceKey` a `recurso/id` (§3.1).
7. `host_binding` por cuenta y campo `account` en el sobre de `/events`; revocación de la concesión de la standalone al vencer la gracia (§4).

**Gnothi (`Gnothi.Mobile` / `Gnothi.iOS`):**
8. Enrolamiento por browser del sistema (Auth Tab / `ASWebAuthenticationSession`) y canje de código; guardado en Keystore / Keychain; refresh y `401` (§3).
9. `intent-filter ACTION_SEND text/plain` (Android) y Share Extension (iOS) → importación por `deliver` (§5).
10. App Link + `assetlinks.json` y universal link + AASA para `go.gnothi…/alta` y `/abrir`; landing con tiendas; Install Referrer (§7).
11. `asset_statements` con los orígenes de las instituciones (Android, opcional §6.1).
12. Bridge `gnothi.pull(after)`; cola de tránsito opcional para el pull en background (§8).
13. Manifiesto: `publicador.eventos`, `publicador.enrolamiento`, `scope` (§7).

Lo que **no** cambia: `Dispatcher`, `AgentLoop`, `Gnothi.Contract`, el dominio, y el formato del journal.

### 10.1 Lo que dejó dicho la réplica en los otros cuatro — 2026-09-04

**El Portador era copia literal, y se decidió mantenerlo así.** Los cinco módulos tenían los mismos siete archivos de `Puppets.Portador`, **idénticos byte a byte** salvo el fin de línea (verificado antes de tocar nada). Se copió el de Exámenes **entero**, no solo la parte de credencial: eso arrastra `SobreDeEventos` y `FuenteDeEventosPorGnothi`, que hoy son inertes en los otros cuatro, pero mantiene la invariante y deja al próximo módulo que entre a Gnothi sin trabajo de Portador. El bloque `account` es opcional al interpretar el sobre, así que un publicador que todavía no lo emite sigue funcionando igual.

**DealershipCar se tradujo, no se alineó.** Está en inglés (`Logbook`, `StoredToken`, `CredentialStore`) y se respetó: el cambio salió en su propio vocabulario. Alinearlo al español sería otro trabajo y otro commit.

**Se corrigió un comentario que mentía.** `ProveedorDeToken.cs` seguía diciendo que la app *"lo guarda con un comando propio y desde ahí vive en el journal"*. Se corrigió en Exámenes **antes** de copiar, para no propagar la mentira a cuatro módulos más.

**DealershipCar era el único con pruebas de dominio, y ahí apareció lo que sigue abierto.** Seis pruebas se fueron con el estado que fijaban. Una de ellas, `NoAnswerTheAssistantGetsCarriesThePersonsCredential`, vigilaba que **ninguna tool del asistente copiara el token en una respuesta** — escribía uno por la puerta, serializaba todas las tools y verificaba que el valor no apareciera. Hoy la puerta no tiene con qué escribirlo, así que la prueba no se puede escribir igual.

> **Hueco abierto.** Los journals escritos **antes** del 2026-09-03 tienen el token en claro, porque el journal es append-only y no se edita el historial. El asistente lee esas carpetas igual. Lo único que separa al token del asistente sigue siendo que ninguna tool lo pida — y ya no hay prueba que lo vigile. Rehacerla exige construir un journal legado desde la prueba, que es justamente para lo que existen los no-ops de replay.

**Medido el 2026-09-04.** Consola de Cargas contra su mock (`:8111`) sobre carpeta nueva: credencial obtenida en memoria (`scope=cargas.events.read`, 899 s), **19 eventos leídos y aplicados**, segundo refresh sin novedad y sin escribir. Después se buscó en toda la carpeta del journal `at_`, `rt_`, `device-`, `Token`, `Bearer`, `GuardarCredencial` y `RegistrarDevice`: **ninguna coincidencia**. Control de que la búsqueda vale: el journal es texto legible y en él sí aparecen `ABC123`, `bitacora` y `Avanzar`. Y las 444 pruebas de dominio de DealershipCar, en verde.


---


### 10.2 Hueco abierto: el sustrato viaja adentro del bundle — 2026-09-04

**Medido.** Cada módulo trae **su propio motor**: en el bundle de Exámenes, `_framework/` pesa
**20 MB** sin comprimir e incluye `Puppeteer.wasm`, `Choreography.wasm` y el runtime de .NET.
Compras cargaría otra copia, DealershipCar otra.

Lo que Gnothi le da al módulo son **seis verbos nativos** y ninguno es el motor:
`listo`, `pull`, `contesta`, `listar`, `leer`, `escribir`. Es decir la **red** (el cartero, con
la credencial de Gnothi), los **archivos** del journal, y el **ciclo de vida** (el WebView, el
origen, la CSP). El sustrato no.

**Consecuencia 1 — el manifiesto declara algo que nadie valida.** `substrate.range`
(`">=3.0.0 <4.0.0"`) se deserializa en `Manifest.Substrate` y **no se lee en ningún lado**: ni
`AltaDeModulo` ni el Host lo comprueban. No falta por descuido: falta porque **no hay un
sustrato compartido cuya versión negociar**. Mientras el motor viaje en el bundle, el rango es
documentación.

**Consecuencia 2 — el costo de memoria es por módulo.** Los 216–248 MB de PSS medidos son con
UN módulo abierto. La regla de desalojo deja de ser una precaución y pasa a ser obligatoria.

**No se tocó.** Si el sustrato pasa a ser compartido —Gnothi proveyendo el motor y los módulos
trayendo solo su dominio— es una decisión de arquitectura, no un ajuste. El día que se tome,
`substrate.range` es la pieza que decide si un bundle se puede instalar.

---


### 10.3 Hueco abierto: la contención es UNA capa, y tiene dos rendijas — 2026-09-04

**Quién contiene a quién.** Gnothi **escribe** la política; quien la **aplica** es el motor web.
`ServidorDelBundle` sirve cada documento con:

```
default-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self';
frame-src 'none'; worker-src 'none'; script-src 'self' 'wasm-unsafe-eval' 'sha256-…'
```

**El interceptor NO bloquea.** `ShouldInterceptRequest` devuelve `null` para todo host que no sea
el del módulo, y `null` significa *"WebView, atendelo vos"* — o sea, a la red. Solo sirve los
archivos del bundle. Lo único que impide salir es la CSP: **es una capa, no dos.**

**Por qué no hace falta auditar los botones del módulo.** La CSP se aplica **por petición, no por
camino de código**: da igual si el `fetch` está en un `onclick`, en un `setInterval` o en un
`catch`. Auditar el código sería la forma frágil de conseguir lo que el motor ya garantiza en
tiempo de ejecución.

**Las tres cosas pendientes:**

1. **`form-action` no está puesta, y NO hereda de `default-src`** — es de las poquísimas
   directivas que no caen al default. Un `<form action="https://publicador/…" method="post">`
   con submit **saldría**. Se cierra con `form-action 'self'`.
2. **La navegación de nivel superior no está cubierta por ninguna CSP.**
   `window.location = 'https://…'` y `window.open` no los frena la política —`navigate-to` nunca
   se estandarizó— y en el código **no hay `ShouldOverrideUrlLoading`**. Un módulo podría
   navegarse fuera de su origen; no filtraría el journal por sí solo, pero podría llevarse datos
   en la URL. Se cierra rechazando en `ShouldOverrideUrlLoading` todo host que no sea el suyo.
3. **La negación es MUDA.** El módulo intenta, Chromium lo niega, y Gnothi no se entera. Por eso
   P6 del guion de pruebas necesita una sonda temporal. Se cierra con `report-to` en la CSP —o
   escuchando `securitypolicyviolation`—: cada intento bloqueado le llega a Gnothi con la URL
   que se quiso alcanzar y la línea que lo hizo. La contención pasa de silenciosa a **auditada**,
   y P6 se vuelve ejecutable sin tocar el módulo.

---


### 10.4 El alta ahora PIDE CONSENTIMIENTO — implementado el 2026-09-04

**Lo que había.** El enlace de la institución abría Gnothi, Gnothi bajaba el zip y **lo
instalaba**. Sin preguntar nada, y sin mostrar nada mientras lo hacía. Instalar un módulo es el
acto más grande que la persona hace en el sistema —le da a una institución una carpeta suya y un
endpoint registrado— y era un **efecto secundario de tocar un enlace**. La asimetría era
evidente: para que el asistente vea UNA consulta hay un interruptor por tool; para que exista el
módulo entero, nada.

Y el fallo era mudo: `DarDeAlta` escribía en logcat un comentario que decía *"un alta que falla
tiene que DECIRLO"* — a un desarrollador con un cable USB.

**Lo que hay ahora.** `AltaDeModulo` se partió en dos actos, con la decisión de la persona en el
medio:

| | qué hace | qué NO hace |
|---|---|---|
| `Examinar` | baja el bundle, lo desempaqueta en un temporal, lee y valida su manifiesto | no instala nada |
| `Aceptar` | mueve el módulo a su lugar y le escribe su `installation.json` | — |
| `Descartar` | borra el temporal | no deja carpeta, ni journal, ni entrada en el catálogo |

**Bajar no es instalar**, y hay que bajar para poder decir quién es la institución: el manifiesto
viene adentro del zip.

**Qué se le muestra**, y de dónde sale cada cosa:

- quién es y cómo se llama su módulo — lo **declara** el manifiesto
- **qué va a poder contestar** — las `tools`, con su descripción
- **dónde va a vivir su registro** — lo decide **Gnothi**
- **con quién va a hablar** — la institución lo declara, Gnothi lo **fija**; y se dice que *es la
  única red que va a poder tocar, y la hace Gnothi por él*
- **quién sirvió el bundle** — el host real de la descarga, que puede no ser quien dice el
  manifiesto (ver el hueco de abajo)

**Medido el 2026-09-04**, en emulador: «Ahora no» deja `files/Modulos` y `files/journals`
inexistentes; «Instalar» sigue con `pull after=0 -> 73019 bytes` y `matches:true`. Los dos
caminos de entrada probados: Gnothi cerrado (`OnCreate`) y ya abierto (`OnNewIntent`).

> **Lo que esto NO resuelve, y sigue abierto:** el bundle **no está autenticado**. Gnothi baja
> cualquier URL que venga en el parámetro; nadie firma el zip y no se verifica que quien lo sirve
> sea quien el manifiesto dice ser. Las guardas que hay —techo de 64 MB, zip-slip, nombre de
> módulo válido— protegen el sistema de archivos, no a la persona de una institución falsa. Lo
> único que hay hoy contra eso es que la pantalla **dice quién sirvió el bundle**, y que ella
> puede decir que no.

---


### 10.5 PREGUNTA ABIERTA: nadie le pregunta a la persona si quiere MUDARSE — 2026-09-04

**Lo que pasa hoy, exactamente.** Gnothi le manda al publicador esto y nada más:

```json
{ "deviceKey": "device-gnothi-a1b2…", "recurso": "cliente", "id": "1" }
```

Ni la palabra `migrated`, ni un verbo de migración. **Gnothi pide una credencial.** Quien decide
que eso *significa* una mudanza es el **publicador**, que ve el prefijo del device key y anota la
cuenta como vinculada. Gnothi lo causó, pero no lo hizo, y no se entera de que lo causó.

**El hueco.** Con la pantalla de consentimiento de §10.4, la persona acepta **instalar el
módulo**: ve qué va a contestar, dónde vivirá su registro, con quién hablará. Lo que esa pantalla
**no dice** es la consecuencia: *y tu app del supermercado va a dejar de actualizarse*. Toca
«Instalar en mi teléfono», y de rebote su instalación suelta se apaga.

Es la misma forma del problema que §10.4 vino a arreglar —una consecuencia grande cayendo como
efecto secundario de otro acto— un escalón más adentro. Y §4 de este documento ya lo anticipaba:
*"en un proveedor de verdad ese instante lleva una pantalla de consentimiento […] para que la
marca sea un acto de la persona y no un efecto secundario"*. Anticipado, no hecho.

**Por qué el arreglo barato no es obviamente el correcto.** Lo barato es que Gnothi agregue una
línea a la hoja de alta: *"la app suelta de esta institución va a dejar de sincronizar"*. Pero
eso sería **Gnothi afirmando algo sobre el comportamiento de un tercero**. Gnothi no puede saber
que ese publicador migra la cuenta al emitirle credencial: es una política de la institución, no
del protocolo. Es cierto con nuestro mock; con un publicador real que no vincule, la advertencia
sería falsa.

**Las opciones, y qué compra cada una:**

| | quién pregunta | qué compra | qué cuesta |
|---|---|---|---|
| **a. Como está** | nadie | nada que construir | la persona se entera cuando abre la suelta y ya no actualiza |
| **b. Aviso en la hoja de alta** | Gnothi | la sorpresa desaparece | Gnothi afirma algo que no puede saber; falso ante un publicador que no vincule |
| **c. El publicador pregunta** | la institución | la marca pasa a ser un acto de la persona, ante quien de verdad decide | exige el enrolamiento por browser del sistema (§10 punto 8), no un POST |
| **d. Separar los dos actos** | Gnothi, en dos momentos | instalar ≠ mudarse; se podría tener el módulo y seguir usando la suelta | rompe la inferencia del publicador: Gnothi tendría credencial sin que eso signifique migración |
| **e. No migrar** | — | nada que preguntar | dos historias divergiendo, que es lo que la marca existe para evitar |

**La pregunta para decidir**, y no es técnica: ¿la mudanza es una **consecuencia** de instalar el
módulo, o una **decisión aparte**? Si es consecuencia, alcanza con decirla (b) y el problema es
que Gnothi no está en posición de afirmarla. Si es decisión aparte, hace falta (c) o (d), y (c)
es la única en la que quien pregunta es quien tiene la autoridad para hacerlo.

---


### 10.6 HECHO: los textos decían que Sonia dejó de ser dueña de sus datos — 2026-09-04

> **Aplicado en `60fa0f4`.** Lo que sigue se deja como estaba porque el contraste *antes /
> nuevo* es el argumento; «hoy» de acá abajo es el 2026-09-04, no el estado actual.
>
> | | estado |
> |---|---|
> | aviso **hospedado** y **hoja de alta** | **[MEDIDO]** en pantalla: bundle rearmado, app y módulo reinstalados por el enlace, y abierto. Los cuatro Wasm compilan; **mirado en uno** |
> | aviso de **migrada** | **[NO MEDIDO]** con el texto nuevo. Se midió el 2026-09-04 (P8 ✅ los cuatro) pero **con el texto viejo**, y la tabla de P8 del guion se actualizó en `cb46c30` «de paso», sin volver a correrla. Se puede medir hoy: es P1 → P2 → reabrir la app suelta, sin nada especial — el mock pone la marca solo, en cuanto una `device-gnothi*` pide credencial (`EndPointDeEventos/Program.cs:86`). El commit `60fa0f4` dijo que «el mock no tiene ese flujo» y eso era falso |
> | los **comentarios** de las cuatro páginas | reescritos también: era donde se propagaba el encuadre viejo |

**El problema.** Los avisos de los cuatro módulos dicen que el registro **vive en Gnothi**:

| dónde | qué dice hoy |
|---|---|
| aviso de migrada (los 4) | *«Tu registro ahora vive en Gnothi»* · *«Your record now lives in Gnothi»* |
| aviso de hospedada (los 4) | *«Se está guardando en Gnothi»* · *«It is being saved in Gnothi»* |
| `EstadoDeLaApp` de Exámenes | *«tu registro ahora vive en {DondeVive}»* |
| hoja de alta de Gnothi | *«Dónde va a vivir tu registro»* |

**Suena a que se lo llevaron.** Y es exactamente al revés de lo que el sistema hace: el journal
está en el teléfono **de ella**, en una carpeta **suya**, y Gnothi es un Host que se la administra
a la institución. Gnothi no es un lugar adonde los datos se van: es por donde pasan.

Un texto que insinúa cesión de propiedad en la pantalla más sensible del sistema contradice el
primer pilar. No es una cuestión de redacción.

**El giro propuesto:** dejar de decir DÓNDE VIVE el dato y pasar a decir QUIÉN LO TIENE Y DE QUIÉN
ES. «Entran **por** Gnothi», no «viven **en** Gnothi».

**La redacción aplicada.** Se escribió y compiló el 2026-09-04, se **revirtió** ese día para no
meter texto sin verificar en un merge, y se volvió a aplicar —esta vez corrida y mirada en
pantalla— en `60fa0f4`:

```
MIGRADA
  antes:  Tu registro ahora vive en Gnothi.
          Lo que ves acá se queda, pero esta copia ya no se actualiza:
          las compras nuevas te llegan allá.

  nuevo:  Tus compras nuevas ahora entran por Gnothi, en este mismo teléfono.
          Lo que ves acá se queda y sigue siendo tuyo; lo que esta copia
          deja de hacer es buscar novedades.

HOSPEDADA
  antes:  Se está guardando en Gnothi.
          Tu registro vive en el dispositivo, en la carpeta que Gnothi le
          dio a este supermercado, y tu asistente lo puede leer desde ahí.

  nuevo:  Se está guardando en este teléfono.
          Tu registro es tuyo y está acá: Gnothi le dio a este supermercado
          una carpeta en tu teléfono, y tu asistente la puede leer desde ahí.

HOJA DE ALTA (Gnothi.Mobile/Assets/ui.html)
  antes:  Dónde va a vivir tu registro
  nuevo:  Dónde se va a guardar, en tu teléfono
          Es tuyo. Gnothi se lo administra a esta institución, no se lo lleva.
```

Cada módulo lleva su sustantivo —exámenes, compras, cargas— y DealershipCar va en inglés:
*«New maintenance facts now arrive through Gnothi, on this same phone. What you see here stays
and is still yours; what this copy stops doing is looking for news.»* y *«It is being saved on
this phone. Your record is yours and it is right here: …»*

**Archivos a tocar:** `MisExamenes.razor`, `MisCompras.razor`, `MisCargas.razor`,
`MySchedule.razor`, `Examenes.Wasm/EstadoDeLaApp.cs` y `Gnothi.Mobile/Assets/ui.html`.

**Los que citaban el texto viejo, y ya se corrigieron:** `Gnothi.Mobile/PRUEBAS-WEBVIEW.md` (la
tabla del aviso hospedado y la hoja de alta) y `Gnothi.iOS/PRUEBAS-WEBVIEW-IOS.md` (P3 y el
alta). La carpeta de entrega para iOS que decía este párrafo —`02-como-funciona-por-dentro.md`—
ya no existe: es `Documentacion/Movil/`, que nació con el texto nuevo. Queda sin tocar
`Gnothi.iOS/HALLAZGOS-CORRIENDO-EL-GUION.md`, que es el reporte de Renan y era exacto el día
que lo midió.

---

## 11. Fuentes

- Chrome: `getInstalledRelatedApps` — developer.chrome.com/docs/capabilities/get-installed-related-apps · Android Intents — developer.chrome.com/docs/android/intents · Web Share y tipos permitidos — web.dev/articles/web-share, `third_party/blink/renderer/modules/webshare/FILE_TYPES.md` · permisos persistentes FSA — developer.chrome.com/blog/persistent-permissions-for-the-file-system-access-api · Auth Tab — developer.chrome.com/blog/android-auth-tab
- Android: App Links — developer.android.com/training/app-links/verify-android-applinks · Install Referrer — developer.android.com/google/play/installreferrer/library · Keystore — developer.android.com/privacy-and-security/keystore · security-crypto deprecado — developer.android.com/jetpack/androidx/releases/security · recibir `ACTION_SEND` — developer.android.com/training/sharing/receive
- Apple: TN3155 Debugging Universal Links — developer.apple.com/documentation/technotes/tn3155-debugging-universal-links · Allowing apps and websites to link to your content — developer.apple.com/documentation/xcode/allowing-apps-and-websites-to-link-to-your-content · Safari 15 release notes (Web Share files) · `ASWebAuthenticationSession` · Keychain accessibility — developer.apple.com/documentation/security/restricting-keychain-item-accessibility · WebKit tracking prevention y storage policy — webkit.org/tracking-prevention, webkit.org/blog/14403
- OAuth: política de Google "Use secure browsers" — developers.google.com/identity/protocols/oauth2/policies · RFC 8252 (native apps) · RFC 9700 (security BCP) · RFC 8693 (token exchange) · RFC 7009 (revocación) · RFC 6750 (`invalid_token`)
- Storage web: web.dev/articles/storage-for-the-web · MDN Storage quotas and eviction criteria
