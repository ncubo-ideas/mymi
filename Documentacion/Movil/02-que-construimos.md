# 02 · Qué construimos

**Corte: 2026-09-07.** Pieza por pieza, con dónde vive. No se transcribe código: se apunta.

---

## 1 · El Host, adentro de la app

**El núcleo del Host no se carga en el teléfono: se compila adentro.** `Gnothi.Mobile.csproj`
incluye los mismos archivos del Host de escritorio dentro del APK. Lo único que se carga en
runtime son los **DLL de los módulos**, y de eso se encarga `InProcessShell` con
`AssemblyLoadContext`.

Eso importa más de lo que parece, y es la corrección de la nota `31` por la `32`: cuando
preguntamos si el AOT obligatorio de iOS mataba la tesis, la pregunta estaba mal planteada. Lo
que hay que cargar en runtime es mucho menos de lo que asumimos. Ver `04`.

## 2 · La cáscara con cara

Una app con pantalla, no una sonda de botones: catálogo de módulos, consentimiento por
herramienta, el asistente, el journal y los ajustes. La UI es una página (`Assets/ui.html`) que
corre en un WebView y habla con el Host por un puente.

**El mismo archivo `ui.html` sirve a las dos plataformas** — está enlazado, no copiado.

## 3 · La Puerta 2, cerrada de punta a punta

Nuestro `AgentLoop` corriendo **en el teléfono**: la persona pregunta, Gnothi arma el repertorio
de tools de los módulos encendidos, llama al modelo, ejecuta las tools contra los journals
locales y devuelve la respuesta. **[MEDIDO]** en un Galaxy A54 físico y en emulador.

El modelo es intercambiable: hay proveedor remoto con llave de la persona (BYOK) y, en iOS, el
modelo del propio aparato sin llave ni red — ver `03`.

## 4 · Los módulos web hospedados

Lo más nuevo, y lo que cambia qué es un módulo. La institución entrega un **bundle**: su app web
publicada más un manifiesto. Gnothi lo sirve **desde el aparato**, en un origen propio por
módulo, con una CSP que el Host calcula al servir.

Qué resuelve: hasta ahora un módulo era un DLL que sólo el asistente veía. Ahora la institución
trae **su propia pantalla**, y la persona la abre adentro de Gnothi, con su registro guardándose
en su teléfono.

Lo neutral vive una vez, en `Gnothi.Movil/`:

| pieza | qué decide |
|---|---|
| `WebModule.cs` | el journal, el cursor, el protocolo del puente y su despacho |
| `BundleServer.cs` | qué archivo se sirve, con qué tipo y **con qué política** |
| `GnothiCredential.cs` | la credencial: de dónde sale, dónde se guarda, cuándo se renueva |
| `ModuleEnrollment.cs` | el alta: bajar, examinar, proponer, instalar |
| `Shell.cs` | el catálogo, el consentimiento, el push a la pantalla |

Y cada plataforma pone sólo su adaptador: `AndroidWebModule.cs` (un host virtual `https`
interceptado antes de la red) y `IosWebModule.cs` (un esquema propio con `WKURLSchemeHandler`,
porque WebKit no deja manejar `http` ni `https`).

## 5 · El alta pide permiso

Un módulo no entra solo. Llega por un enlace de la institución, Gnothi **baja el bundle, lo
examina y muestra una hoja** con qué va a poder contestar, dónde se va a guardar el registro y
con quién va a hablar. Nada se instala hasta que la persona toca **«Instalar en mi teléfono»**, y
«Ahora no» **no deja nada**.

## 6 · La contención, en dos capas

- La **CSP** que el Host calcula por documento, con el hash de cada script inline computado al
  servir — nunca escrito a mano, porque uno escrito a mano se desincroniza al primer rebuild y
  el síntoma es una pantalla en blanco sin pista.
- El **rechazo de navegación** fuera del origen propio, en cada plataforma, porque ninguna CSP
  cubre la navegación de nivel superior.
- Y desde el 2026-09-07, **cada rechazo le llega a Gnothi como un hecho** —directiva, qué se
  bloqueó, de dónde salió y la cuenta— en vez de como una línea de consola.

## 7 · La migración, y que el publicador la haga cumplir

Cuando Gnothi obtiene su credencial, el publicador marca la cuenta como mudada y la app suelta
de la institución se entera **por el sobre de eventos**, no por una marca local (ninguna marca
del lado web sobrevive a «borrar datos del sitio»).

Y desde el 2026-09-07 el publicador además **revoca**: hay un plazo de gracia, y vencido, la
cuenta es de Gnothi en las tres puertas —emitir, rotar y leer—. Antes la app suelta se apagaba
porque *decidía* obedecer; una app vieja o modificada seguía leyendo.

## 8 · Los guiones de prueba

Dos, uno por plataforma, escritos para que alguien que no vivió las mediciones pueda
reproducirlas: `Gnothi.Mobile/PRUEBAS-WEBVIEW.md` y `Gnothi.iOS/PRUEBAS-WEBVIEW-IOS.md`. Cada
prueba dice qué hay que ver, qué significa si sale mal, y qué **no** prueba.

Y un escenario que levanta el mundo con un comando: cuatro instituciones, ocho servidores.
`escenario-webview.ps1` en Windows, `scenario-ios.sh` en la Mac.
