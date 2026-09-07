# 12 — Móvil: diferencias entre Android e iOS

**Corte 2026-09-02, rev. 2.** Complemento de `11` (y de `13` para la transición standalone → Gnothi). Todo lo que **no** está acá es idéntico en las dos plataformas: el flujo es uno solo.

## La regla

**Arquitectura: una. Plataforma: dos.** Si una diferencia de esta tabla obliga a tocar `Dispatcher`, `AgentLoop`, el manifiesto o el módulo, algo se puso en la capa equivocada.

## Tabla

| | **Android** | **iOS** |
|---|---|---|
| **contenedor web** | `android.webkit.WebView` (Chromium) | `WKWebView` (WebKit) |
| **servir el bundle** | **host virtual sobre https**, un host por módulo, interceptado con `ShouldInterceptRequest` (o `WebViewAssetLoader`). Medido: funciona y da contexto seguro (`11` §3) | `WKURLSchemeHandler` con esquema propio por módulo. **A medir: si ese origen es contexto seguro**; si no, hay que buscarle el equivalente al host virtual |
| **bridge JS → nativo** (flush, logs) | `@JavascriptInterface` sobre un objeto inyectado | `WKScriptMessageHandler` |
| **bridge nativo → JS** (`call`, `deliver`) | `evaluateJavascript` | `evaluateJavaScript` |
| **inyectar la CSP** | como header en la respuesta del asset loader | como header en la respuesta del scheme handler |
| **aislamiento por origen** | sí, por same-origin | sí, por same-origin; WebKit además separa procesos de contenido |
| **wasm en el WebView** | ✅ Chromium | ✅ WebKit (desde iOS 11) |
| **`OperatingSystem.IsBrowser()` dentro** | `true` ⇒ el guard actual de `wasm-replay` sirve | `true` ⇒ ídem |
| **tope por archivo en storage web** | no reportado | **~10 MB** reportado — irrelevante para el journal (archivos nativos), relevante si el módulo usa OPFS |
| **Puerta 3** | **AppFunctions, dinámico**: carátula generada por Gnothi (17 KB, sin Gradle) → Gnothi. Medido de punta a punta (reporte 19) | **App Intents, estáticos**: declarados en el bundle de Gnothi; sin carátula por módulo. **Plan C** (pocos intents de alto nivel, ruteo interno) o catálogo curado |
| **quién puede invocar por Puerta 3** | callers que Android allowlistea; el asistente de fábrica usa catálogo curado (*private preview*) | el sistema (Siri / Apple Intelligence) |
| **segundo protocolo de tools** | el de la app de Claude (ContentProvider + AIDL, vocabulario abierto) — reconstruido, **no publicado** | no observado |
| **background para el cartero** | WorkManager; políticas de OEM (Xiaomi, Samsung, Huawei) pueden matar procesos | BGTaskScheduler; **tacaño y no negocia** |
| **"app primero" — la PWA standalone escribe en** | carpeta real vía File System Access (Chromium ≥132, SAF) u OPFS | **solo OPFS**; desalojable sin `navigator.storage.persist()` y sin agregar a inicio (7 días sin uso) |
| **pasar la mano de la standalone a Gnothi** | botón **con gesto** → `intent://…;package=com.ncubo.gnothi.host;S.browser_fallback_url=…` (sin gesto, Chrome va al fallback; ventana de 15 s). Detección **opcional**: `navigator.getInstalledRelatedApps()` — exige `related_applications` en el manifest de la PWA y el **origen exacto** de cada institución en `asset_statements` del APK | `<a href>` a un universal link de Gnothi, **tocado**, a un dominio **distinto** al de la PWA; **no** desde JS ni con `setTimeout`. **Sin detección posible** (`getInstalledRelatedApps` no existe en WebKit; `gnothi://` sin app = alerta sin fallback) |
| **recibir el traspaso de aserciones de la standalone** (cuando no cabe en el enlace) | `navigator.share({files})` → `ACTION_SEND` con `text/plain` (Chromium **rechaza** `.json`/`application/json`; tope 10 archivos / 50 MB) · o leer el archivo de la carpeta SAF compartida | `navigator.share({files})` (iOS 15+) → **Share Extension** de Gnothi (`NSExtensionActivationSupportsFileWithMaxCount`) |
| **marca durable "ya migré"** | el publicador (`account.state`) + archivo marcador en la carpeta SAF si la hay; IndexedDB con `persist()` es solo caché | **solo el publicador**; el storage de la PWA en inicio está exento de los 7 días pero se borra con "datos de sitios web" o al quitar el icono |
| **alta desde la página de la institución** | App Link `https://go.gnothi…/alta?bundle=…` con `autoVerify` + `assetlinks.json` (huella de Play App Signing); sin app → Play con `referrer` → **Install Referrer** al primer arranque | universal link al mismo dominio (AASA); sin app → landing con App Store y `<meta name="apple-itunes-app" … app-argument=…>` |
| **login / enrolamiento ante el publicador** | browser del sistema: **Auth Tab** (Chrome 132+) o Custom Tabs — **nunca el WebView** (Google: `disallowed_useragent`; RFC 8252 §8.12) | **`ASWebAuthenticationSession`** — **nunca `WKWebView`** |
| **guardar la credencial de Gnothi** | Android Keystore (llave AES no exportable, TEE/StrongBox) + blob cifrado en archivo privado; `EncryptedSharedPreferences` **deprecado** (abr-2025) | Keychain con `…ThisDeviceOnly` (`AfterFirstUnlock` si habrá background) |
| **cumple tienda** | Play: *"JavaScript in a webview or browser"* | Apple: WebKit / JavaScriptCore |
| **proyecto en el repo** | `Gnothi.Mobile` (existe) | `Gnothi.iOS` (**no existe**) |
| **estado de la medición** | núcleo compilado y corriendo en un A54 (de otro desarrollador, no disponible); **el WebView con la PWA YA está medido** en emulador (Android 36 x86_64, WebView 133): arranca, corre el actor y opera — 2 650 ms de runtime, 145 MB de PSS (`11` §12). Falta repetirlo en un teléfono físico. El disponible es un **Xiaomi**, y eso lo hace doblemente útil: es uno de los fabricantes cuyas políticas de ahorro pueden matar al cartero (ver la fila de background) | **nada medido en dispositivo** |

## Lo que solo iOS pierde

**Puerta 3 dinámica.** App Intents se declaran al compilar Gnothi y las extensiones viajan dentro del bundle: la hipótesis del documento de investigación se confirma por construcción. En iOS **la Puerta 2 es el camino principal**, no la muleta, hasta que Apple mueva.

## Lo que solo Android tiene

**Dos protocolos de tools en el dispositivo**, y el segundo —el de la app de Claude— tiene **vocabulario abierto**, que es lo que le calza a Gnothi (los 117 schemas de AppFunctions no tienen casilla para facturas ni exámenes). Está reconstruido por ingeniería inversa y **no publicado**: sirve para planificar y preguntar, no para comprometer roadmap.

## Qué medir, por plataforma

| Android | iOS |
|---|---|
| **la PWA de Exámenes dentro de un `WebView` de Gnothi**: arranque, memoria, primera respuesta | **lo mismo en `WKWebView`** — y es lo primero, porque no hay nada medido en iOS |
| cold start Puerta 3 completo con Gnothi cerrado (se midió en 2 556 ms sin el WebView) | revisión de Apple: validar el encuadre "browser para la capa de módulos" con quien la conozca |
| supervivencia del cartero bajo políticas de OEM | comportamiento de BGTaskScheduler con el cartero |
| `getInstalledRelatedApps()` desde la WebAPK con la huella de Play App Signing; que el `.txt` llegue intacto por `ACTION_SEND`; si el permiso de carpeta persiste entre arranques en Chrome ≥132 | que el toque en `<a href>` dentro de la PWA en inicio abra Gnothi **directo** (no vía Safari View Controller); que el share sheet de la PWA liste la Share Extension de Gnothi; qué alerta muestra `gnothi://` sin app |
