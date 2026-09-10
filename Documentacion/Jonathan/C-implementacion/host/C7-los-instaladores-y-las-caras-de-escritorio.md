# C7 · Los instaladores MSI y las caras de escritorio

`C3` cerró el `.mcpb` como camino de instalación: el botón nunca se habilitó, y quedó como prueba de
que la costura de instalación es barata. Este es el reemplazo, hecho y probado: **dos instaladores
MSI** (WiX v5, per-usuario, sin admin) y **dos caras de escritorio** que el instalador entrega. Una es
del Host (Gnothi.Desktop); la otra es del comercio (Comercio.Shell). Ninguna es «el Host»: son caras
sobre él, como el front MCP o el teléfono. Ver `A2`.

Las **trampas** de todo esto —WiX v5 y no v7, el `--` que rompe un `.wxs`, nunca interrumpir `msiexec`,
reubicar el user-data de WebView2— viven en `E1`. Acá está el *qué* y el *por qué*, no el *cómo duele*.

---

## Dos instaladores, dos responsabilidades

El corte de `B4` —la institución escribe el manifiesto, el Host escribe la instalación— tiene ahora su
eco a nivel de máquina: **hay un instalador que registra la máquina y otro que no puede**.

| | instalador de **Gnothi** | instalador del **comercio** (Súper La Guaria) |
|---|---|---|
| qué es | el Host + su ventana | el PWA del comercio + su módulo |
| dónde | `%LocalAppData%\Programs\Gnothi` | `%LocalAppData%\Programs\LaGuaria` |
| registra la máquina | **sí** — deja la marca de descubrimiento en `HKCU` | **no** — no toca registry ni startup |
| toca a Gnothi | — | mete el módulo `compras.gmod` adentro de Gnothi |
| si Gnothi no está | — | **bloquea y manda a la página de Gnothi** |

El de Gnothi es el que «registra la máquina»: es nuestro, de confianza, se instala una vez. El del
comercio hace **lo mínimo** —suelta su cara y engancha su módulo— porque una institución de comercio no
tiene por qué poder tocar toda la máquina. El «a raya» del comercio no lo pone su instalador; lo ponen
las dos jaulas donde vive: el PWA adentro del WebView2, el módulo adentro de Gnothi.

### El de Gnothi

Un MSI per-usuario (sin UAC) que:

1. copia el Host + la ventana a `%LocalAppData%\Programs\Gnothi`, con `Modulos\` al lado —el layout
   que la ventana espera para no caer al de desarrollo;
2. escribe la **marca de descubrimiento** en `HKCU\Software\Ncubo\Gnothi`;
3. deja accesos directos (Menú Inicio + Escritorio);
4. trae **wizard** interactivo (`WixUI_InstallDir`, con «Examinar…» para elegir carpeta).

El registro del MCP en Claude —lo que hacía `HostInstaller`— **no** va adentro del MSI: queda como paso
aparte, de primer arranque. Y el MSI **no toca `HKLM` ni startup**: todo es per-usuario.

### El del comercio

Un MSI que **lee** la marca de Gnothi y decide:

```
GNOTHIHOSTEXE / GNOTHIMODULES  <-  HKCU\Software\Ncubo\Gnothi
   |
   +-- vacío  -> <Launch> bloquea: «Gnothi no está instalado. Instalalo primero.»
   |
   +-- presente -> instala PWA + Comercio.Shell + compras.gmod
                   y una custom action corre:
                       Gnothi.Host --install-module compras.gmod <ModulesPath>
```

La custom action **reusa el instalador de módulos ya probado** (`--install-module`): extrae el `.gmod`,
otorga la carpeta de datos en `Documents\Gnothi\compras`, escribe `installation.json`. No reimplementa
nada. Y al **desinstalar**, otra custom action quita el módulo de Gnothi (los datos de la persona en
`Documents\Gnothi\compras` se dejan).

---

## La marca de descubrimiento

Es la pieza que `HostInstaller` nunca dejaba: **Gnothi ahora deja rastro de sí mismo**, para que otro
instalador lo encuentre.

```
HKCU\Software\Ncubo\Gnothi
  InstallPath  = C:\Users\<u>\AppData\Local\Programs\Gnothi\
  ModulesPath  = C:\Users\<u>\AppData\Local\Programs\Gnothi\Modulos   (sin backslash final, a propósito)
  DesktopExe   = ...\Gnothi.Desktop.exe
  HostExe      = ...\Gnothi.Host.exe
  Version      = 0.1.0
```

Gnothi la **escribe**, el comercio la **lee**, y nadie la elige en runtime — es el mismo corte de `B4`
subido un piso: identidad declarada por quien instala, no negociada. El `ModulesPath` va **sin barra
final** porque el comercio lo pasa entre comillas a la línea de comando, y un `...\"` al cierre
escaparía la comilla.

---

## Gnothi.Desktop — la ventana del Host

Un proyecto nuevo: **Blazor Hybrid** (WinForms + `BlazorWebView`, `net9.0-windows`, WinExe). Muestra los
módulos instalados como tarjetas —de quién son, prendido/apagado— y corre una consulta con un botón.

Lo que hay que entender es que **no es un cliente MCP**: no tiene `.mcp.json` ni `claude_desktop_config`
ni stdio. Los componentes corren **nativos, en este proceso**, y llaman al `Dispatcher` **directo** —sin
wasm, sin puerto, sin http—. Es la Puerta 1 llevada al extremo: acá el que llama ya está adentro de la
máquina, así que ni el spawn hace falta. Ver `B1`.

Referencia al Host por `ProjectReference` y **solo su superficie pública** (`ModuleCatalog`,
`Dispatcher`, `Module`, `Request`, `Response`, `Meter`) —igual que `Gnothi.Assistant`—. Si algún día
necesita un tipo interno, esa es la señal de que la costura pública quedó corta: se ensancha la costura
en el Host, nunca se atraviesa.

⚠️ **Está fuera del `.slnx` a propósito.** `net9.0-windows`/WinExe solo compila en Windows y rompería
`dotnet build` para cualquiera sin ese target (CI, Linux). Se construye aparte. Misma razón que la app
Android.

---

## Comercio.Shell — el PWA en una ventana

La cara del comercio es un **PWA wasm** (la vitrina con la marca de La Guaria). Para que la persona lo
abra como app, un shell de escritorio lo hospeda: `Comercio.Shell`, WinForms + **WebView2**.

**Ojo, no es `BlazorWebView`** —eso es Hybrid, para componentes nativos como Gnothi.Desktop—. Un PWA
wasm se hospeda con **WebView2** apuntado a un **host virtual**:

```csharp
web.CoreWebView2.SetVirtualHostNameToFolderMapping(
    "laguaria.local", webDir, CoreWebView2HostResourceAccessKind.Allow);
web.CoreWebView2.Navigate("https://laguaria.local/index.html");
```

El mapeo es la clave: el PWA cree que está en un origen `https://` normal, no en `file://` —que le rompe
`fetch` y las rutas absolutas—. El shell es **genérico**: lee un `comercio.json` (título, host, carpeta)
al lado del exe, así el mismo shell sirve a cualquier comercio.

Esta ventana **es la contención** («a raya»): el PWA corre adentro de ella, no toca la máquina.

Dos ajustes al PWA para hospedarlo local (detalle fino en `E1`): base href `/app/` → `/`, y **quitarle
el service worker** —sin conexión que fingir, el SW solo traería la trampa de caché de `E3`—.

---

## Las decisiones, con opinión

**MSI y no MSIX.** MSIX es el empaquetado moderno de Microsoft, pero es **sandboxed**: prohíbe
justamente escribir el registry/startup libremente. Eso mata la marca de descubrimiento —el concepto
piedra-angular de que la app registre la máquina—. Para este modelo el instalador tiene que ser
clásico: MSI. Ver la entrada de MSIX en `E1`.

**Per-usuario, sin admin.** Toca `HKCU` y `%LocalAppData%`, no `HKLM` ni `Program Files`, así no pide
elevación. Encaja con el ethos: la persona es dueña de sus datos, y no hace falta un administrador para
instalar lo suyo. Los **binarios** van per-usuario; los **datos** (jornales, carpetas otorgadas) siguen
en `Documents\Gnothi\`. Separados.

**El «a raya» del comercio son dos jaulas, no una.** El PWA (wasm) queda confinado por el WebView2; el
módulo (DLL cargo) queda confinado por Gnothi (carpeta otorgada, solo-lectura, sin motor propio). Son
confinamientos distintos para dos piezas distintas —el PWA es la vitrina, el módulo es el dato—. Ver
`B5`.

---

## Verificado

Instalando Gnothi, después el comercio, y consultando el módulo que el segundo dejó adentro del primero:

```
msiexec /i Gnothi.msi        -> marca en HKCU, Modulos\, accesos directos, ventana abre
msiexec /i LaGuaria.msi      -> lee la marca; custom action corre --install-module

  Modulos\compras\           -> manifest.json + view.json + dominio\*.dll + installation.json
  Documents\Gnothi\compras   -> carpeta otorgada (datos de la persona)

Gnothi.Host --query <ModulesPath> compras resumen
  -> { "hayDatos": true, "totalGastado": 3174606, "cantidadDeCompras": 87, "ultimoEventoLeido": 387 }

Comercio.Shell.exe           -> ventana «Súper La Guaria» con el PWA verde adentro
```

Dos autores (nosotros el Host, el súper su módulo), un solo Gnothi, un dato real. Es la tesis de `A1`
con cuerpo de instalador.

---

## Lo que falta

- **Firma.** Los dos MSI van sin firmar (demo): SmartScreen pide «Ejecutar de todas formas». Firmar de
  verdad es un certificado de firma de código.
- **Abrir la página automáticamente.** Hoy, si Gnothi no está, el MSI del comercio **muestra un
  mensaje** que manda a instalarlo; abrir el navegador solo es una custom action más.
- **Firma del módulo.** El `.gmod` sigue sin firma —la introspección del Host es la única defensa—. Es
  el mismo pendiente de `A3`.
